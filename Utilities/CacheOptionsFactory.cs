using Microsoft.Extensions.Caching.Memory;
using System;

namespace LogiTrack.Utilities
{
    public static class CacheOptionsFactory
    {
        /// <summary>
        /// Short duration cache options for frequently changing data (e.g. 30 seconds).
        /// </summary>
        public static MemoryCacheEntryOptions Short() => new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Session cache options intended for server-side sessions (e.g. 1 hour).
        /// </summary>
        public static MemoryCacheEntryOptions Session() => new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
    }
}
