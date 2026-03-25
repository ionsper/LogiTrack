using System;

namespace LogiTrack.Models
{
    /// <summary>
    /// Server-side session payload stored in IMemoryCache.
    /// Contains minimal identity information used to reconstruct a ClaimsPrincipal.
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Identifier of the authenticated user.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Roles associated with the user at session creation time.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Creation timestamp used for session expiry and auditing.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
