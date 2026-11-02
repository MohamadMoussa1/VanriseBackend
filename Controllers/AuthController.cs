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
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            byte[]? storedSalt = null;
            byte[]? storedHash = null;

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT PasswordSalt, PasswordHash
                    FROM AppUser
                    WHERE Username = @Username";

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    command.Parameters.Add(
                        "@Username", SqlDbType.NVarChar, 100
                    ).Value = request.Username;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            storedSalt = (byte[])reader["PasswordSalt"];
                            storedHash = (byte[])reader["PasswordHash"];
                        }
                    }
                }
            }

            if (storedSalt == null || storedHash == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            // SQL Server NVARCHAR uses UTF-16LE.
            byte[] passwordBytes =
                Encoding.Unicode.GetBytes(request.Password);

            byte[] combined =
                new byte[storedSalt.Length + passwordBytes.Length];

            Buffer.BlockCopy(
                storedSalt, 0, combined, 0, storedSalt.Length);

            Buffer.BlockCopy(
                passwordBytes, 0, combined,
                storedSalt.Length, passwordBytes.Length);

            // SHA-256 hashing on the server.
            byte[] enteredHash = SHA256.HashData(combined);

            bool isValid = CryptographicOperations.FixedTimeEquals(
                enteredHash, storedHash);

            if (!isValid)
            {
                return Unauthorized("Invalid username or password.");
            }

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
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return Ok(new
            {
                Message = "Logout successful"
            });
        }
    }

}