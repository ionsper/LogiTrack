using Microsoft.AspNetCore.Identity;

namespace LogiTrack.Models
{
    /// <summary>
    /// Application-specific user object extending IdentityUser for future custom properties.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Intentionally left minimal. Add custom properties here if required by business logic.
    }
}
