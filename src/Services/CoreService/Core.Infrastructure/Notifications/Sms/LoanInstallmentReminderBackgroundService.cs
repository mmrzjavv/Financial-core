using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Notifications.Sms;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.Notifications.Sms;

public sealed class LoanInstallmentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<LoanSettingsOptions> options,
    ILogger<LoanInstallmentReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollMinutes = Math.Max(1, options.Value.ReminderPollIntervalMinutes);
        var delay = TimeSpan.FromMinutes(pollMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueInstallmentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Loan installment reminder job failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task ProcessDueInstallmentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
        var smsDispatcher = scope.ServiceProvider.GetRequiredService<ISmsDispatcher>();

        var reminderDays = Math.Max(0, options.Value.InstallmentReminderDays);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(reminderDays));

        var dueInstallments = await dbContext.LoanInstallments
            .AsNoTracking()
            .Where(x =>
                !x.IsPaid &&
                !x.IsGracePeriod &&
                x.ReminderSentAt == null &&
                x.InstallmentDate == targetDate)
            .Join(
                dbContext.LoanCases.AsNoTracking(),
                installment => installment.CaseId,
                loanCase => loanCase.Id,
                (installment, loanCase) => new { installment, loanCase })
            .Where(x => x.loanCase.CurrentStatus == LoanCaseStatus.RepaymentPhase)
            .ToListAsync(ct);

        if (dueInstallments.Count == 0)
            return;

        foreach (var item in dueInstallments)
        {
            if (!Guid.TryParse(item.loanCase.ApplicantUserId, out var userId))
                continue;

            var mobile = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => u.PhoneNumber)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(mobile))
                continue;

            var args = new Dictionary<string, string>
            {
                ["caseNumber"] = item.loanCase.CaseNumber,
                ["installmentDate"] = item.installment.InstallmentDate.ToString("yyyy-MM-dd"),
                ["amount"] = item.installment.TotalAmount.ToString("0")
            };

            await smsDispatcher.EnqueueAsync(
                SmsTemplateId.LoanInstallmentDueReminder,
                mobile,
                args,
                cancellationToken: ct);

            await dbContext.LoanInstallments
                .Where(x => x.Id == item.installment.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ReminderSentAt, DateTimeOffset.UtcNow),
                    ct);
        }

        logger.LogInformation("Queued {Count} loan installment reminder SMS messages", dueInstallments.Count);
    }
}
