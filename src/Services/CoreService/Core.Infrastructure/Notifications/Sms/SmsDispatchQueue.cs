using System.Threading.Channels;

namespace Core.Infrastructure.Notifications.Sms;

public sealed class SmsDispatchQueue
{
    private readonly Channel<SmsQueuedMessage> _channel =
        Channel.CreateUnbounded<SmsQueuedMessage>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(SmsQueuedMessage message, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<SmsQueuedMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
