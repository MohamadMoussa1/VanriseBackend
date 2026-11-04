using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VanriseBackend.Models;

namespace VanriseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            byte[] salt;
            byte[] storedHash;

            using (var connection = new SqlConnection(
                _connectionString))
            {
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    "SELECT PasswordSalt, PasswordHash FROM AppUser WHERE Username = @Username",
                    connection);

                command.Parameters.AddWithValue("@Username", request.Username);

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Unauthorized("Invalid username or password.");
                }

                salt = (byte[])reader["PasswordSalt"];
                storedHash = (byte[])reader["PasswordHash"];
            }

            // Keep the existing SHA-256 password verification
            byte[] passwordBytes = Encoding.Unicode.GetBytes(request.Password);

            byte[] combined = new byte[salt.Length + passwordBytes.Length];

            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

            byte[] enteredHash = SHA256.HashData(combined);

            if (!CryptographicOperations.FixedTimeEquals(enteredHash, storedHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            // Cookie authentication
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, request.Username)
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Ok(new
            {
                Message = "Login successful",
                Username = request.Username
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new
            {
                Message = "Logout successful"
            });
        }
    }

}