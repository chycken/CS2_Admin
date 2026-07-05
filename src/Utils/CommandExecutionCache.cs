using System.Collections.Concurrent;
using SwiftlyS2.Shared.Commands;

namespace CS2_Admin.Utils;

public static class CommandExecutionCache
{
    private static readonly ConcurrentDictionary<string, DateTime> _cache = new();

    public static bool ShouldExecute(ICommandContext context, Type commandType)
    {
        if (context.Sender == null) return true;

        var key = $"{context.Sender.SteamID}_{commandType.Name}_{string.Join("_", context.Args)}";
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(key, out var lastExecution))
        {
            if ((now - lastExecution).TotalMilliseconds < 50)
            {
                return false;
            }
        }

        _cache[key] = now;
        return true;
    }
}
