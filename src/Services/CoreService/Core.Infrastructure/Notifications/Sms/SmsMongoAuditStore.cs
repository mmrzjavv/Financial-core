using Core.Application.Identity.Common.Options;
using Core.Application.Notifications.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Core.Infrastructure.Notifications.Sms;

public sealed class SmsMongoAuditStore : ISmsAuditStore
{
    private readonly IMongoCollection<SmsAuditDocument> _collection;
    private readonly ILogger<SmsMongoAuditStore> _logger;

    public SmsMongoAuditStore(IOptions<SmsOptions> options, ILogger<SmsMongoAuditStore> logger)
    {
        _logger = logger;
        var mongo = options.Value.MongoLogging;
        var client = new MongoClient(mongo.ConnectionString);
        var database = client.GetDatabase(mongo.DatabaseName);
        _collection = database.GetCollection<SmsAuditDocument>("sms_audit");
    }

    public async Task AppendAsync(SmsAuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(new SmsAuditDocument
            {
                Id = entry.Id,
                TemplateId = entry.TemplateId.ToString(),
                Mobile = entry.Mobile,
                Message = entry.Message,
                Success = entry.Success,
                Error = entry.Error,
                CaseId = entry.CaseId,
                CreatedAt = entry.CreatedAt
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write SMS audit entry {EntryId}", entry.Id);
        }
    }

    private sealed class SmsAuditDocument
    {
        public Guid Id { get; set; }
        public string TemplateId { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? CaseId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
