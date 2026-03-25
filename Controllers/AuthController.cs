using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LogiTrack.Models;
using Microsoft.Extensions.Caching.Memory;
using LogiTrack.Models.Auth;
using LogiTrack.Utilities;

namespace LogiTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Controller responsible for authentication operations: register, login, logout and session refresh.
    /// Exposes endpoints that manage user sessions and issue JWT tokens.
    /// </summary>
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _memoryCache = memoryCache;
        }

        [HttpPost("register")]
        /// <summary>
        /// Registers a new user with the provided credentials. Returns 200 on success.
        /// Error responses are intentionally generic to avoid user enumeration.
        /// </summary>
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Registration failed. Please check your input.");

            var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign only the User role
                await _userManager.AddToRoleAsync(user, Roles.User);
                return Ok();
            }

            // Harden and consolidate error messages to prevent user enumeration
            return BadRequest("Registration failed. Please check your input.");
        }

        [HttpPost("login")]
        /// <summary>
        /// Authenticates a user and creates a server-side session and JWT token.
        /// Returns 200 with a JSON object containing the JWT on success.
        /// </summary>
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized();

            // Create server-side session stored in IMemoryCache and set secure cookie
            var sessionId = System.Guid.NewGuid().ToString();
            var sessionKey = CacheKeys.SessionKey(sessionId);

            var userRoles = await _userManager.GetRolesAsync(user);
            var sessionInfo = new LogiTrack.Models.SessionInfo
            {
                UserId = user.Id,
                Roles = userRoles.ToArray(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _memoryCache.Set(sessionKey, sessionInfo, CacheOptionsFactory.Session());

            Response.Cookies.Append(CacheKeys.SessionCookieName, sessionId, CreateSessionCookieOptions());

            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }

        [HttpPost("logout")]
        /// <summary>
        /// Logs the current user out by clearing the session cache and deleting the session cookie.
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            try
            {
                var sessionId = Request.Cookies[CacheKeys.SessionCookieName];
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var sessionKey = CacheKeys.SessionKey(sessionId);
                    _memoryCache.Remove(sessionKey);
                    Response.Cookies.Delete(CacheKeys.SessionCookieName);
                }

                await _signInManager.SignOutAsync();
            }
            catch
            {
                // ignore errors during logout
            }

            return NoContent();
        }

        [HttpPost("refresh-session")]
        /// <summary>
        /// Refreshes the server-side session TTL and updates the session cookie expiry.
        /// Returns 204 No Content on success or 401 when the session is invalid.
        /// </summary>
        public IActionResult RefreshSession()
        {
            var sessionId = Request.Cookies[CacheKeys.SessionCookieName];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized();

            var sessionKey = CacheKeys.SessionKey(sessionId);
            if (!_memoryCache.TryGetValue(sessionKey, out LogiTrack.Models.SessionInfo? info) || info == null)
                return Unauthorized();

            // Re-set the session with a fresh TTL
            _memoryCache.Set(sessionKey, info, CacheOptionsFactory.Session());

            // Update cookie expiry as well
            Response.Cookies.Append(CacheKeys.SessionCookieName, sessionId, CreateSessionCookieOptions());

            return NoContent();
        }

        /// <summary>
        /// Options used for the session cookie to enforce secure attributes and expiry.
        /// </summary>
        private CookieOptions CreateSessionCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            };
        }

        /// <summary>
        /// Generates a signed JWT for the provided <see cref="ApplicationUser"/> including role claims.
        /// Throws when required JWT configuration is missing.
        /// </summary>
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, System.Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // Add role claims
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("Missing configuration: Jwt:Key");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var issuer = _configuration["Jwt:Issuer"] ?? string.Empty;
            var audience = _configuration["Jwt:Audience"] ?? string.Empty;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: System.DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
