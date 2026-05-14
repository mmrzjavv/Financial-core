using System.Diagnostics;

namespace Core.Infrastructure.Identity.Tracing;

public static class TracingService
{
    public static readonly ActivitySource ActivitySource = new("Core.Application.Identity", "1.0.0");

    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    public static Activity? StartActivity(string name, ActivityKind kind, IEnumerable<KeyValuePair<string, object?>> tags)
    {
        var activity = ActivitySource.StartActivity(name, kind);
        if (activity != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }
        return activity;
    }
}