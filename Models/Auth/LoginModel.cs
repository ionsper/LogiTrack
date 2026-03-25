using System.ComponentModel.DataAnnotations;
namespace LogiTrack.Models.Auth
{
    /// <summary>
    /// DTO for login requests.
    /// </summary>
    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
