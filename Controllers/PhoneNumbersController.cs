using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using VanriseBackend.Models;

namespace VanriseBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PhoneNumbersController : ControllerBase
    {
        private readonly string _connectionString;

        public PhoneNumbersController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public List<object> GetAllPhoneNumbers()
        {
            List<object> phoneNumbers = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT p.Id, p.Number, d.Name AS Device
                                 FROM PhoneNumber p
                                 INNER JOIN Device d ON p.DeviceId = d.Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        phoneNumbers.Add(new
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Number = reader["Number"].ToString()!,
                            Device = reader["Device"].ToString()!
                        });
                    }
                }
            }

            return phoneNumbers;
        }

        [HttpPost]
        public IActionResult AddPhoneNumber(PhoneNumber phoneNumber)
        {
            if (phoneNumber == null)
            {
                return BadRequest(new { message = "Phone number data is required." });
            }

            if (string.IsNullOrWhiteSpace(phoneNumber.Number))
            {
                return BadRequest(new { message = "Phone number is required." });
            }

            string number = phoneNumber.Number.Trim();

            if (number.Length > 50)
            {
                return BadRequest(new { message = "Phone number cannot exceed 50 characters." });
            }

            if (phoneNumber.DeviceId <= 0)
            {
                return BadRequest(new { message = "A valid Device ID is required." });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string checkDevice = "SELECT COUNT(1) FROM Device WHERE Id = @DeviceId";

                using (SqlCommand command = new SqlCommand(checkDevice, connection))
                {
                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        phoneNumber.DeviceId;

                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                    {
                        return BadRequest(new { message = "The selected device does not exist." });
                    }
                }

                string query = @"INSERT INTO PhoneNumber (Number, DeviceId)
                                 VALUES (@Number, @DeviceId)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Number", SqlDbType.NVarChar, 50).Value = number;

                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        phoneNumber.DeviceId;

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Phone number added successfully." });
        }

        [HttpPut]
        public IActionResult UpdatePhoneNumber(PhoneNumber phoneNumber)
        {
            if (phoneNumber == null)
            {
                return BadRequest(new { message = "Phone number data is required." });
            }

            if (phoneNumber.Id <= 0)
            {
                return BadRequest(new { message = "A valid Phone Number ID is required." });
            }

            if (string.IsNullOrWhiteSpace(phoneNumber.Number))
            {
                return BadRequest(new { message = "Phone number is required." });
            }

            string number = phoneNumber.Number.Trim();

            if (number.Length > 50)
            {
                return BadRequest(new { message = "Phone number cannot exceed 50 characters." });
            }

            if (phoneNumber.DeviceId <= 0)
            {
                return BadRequest(new { message = "A valid Device ID is required." });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string checkDevice = "SELECT COUNT(1) FROM Device WHERE Id = @DeviceId";

                using (SqlCommand command = new SqlCommand(checkDevice, connection))
                {
                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        phoneNumber.DeviceId;

                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                    {
                        return BadRequest(new { message = "The selected device does not exist." });
                    }
                }

                string query = @"UPDATE PhoneNumber
                                 SET Number = @Number,
                                     DeviceId = @DeviceId
                                 WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value =
                        phoneNumber.Id;

                    command.Parameters.Add("@Number", SqlDbType.NVarChar, 50).Value = number;

                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        phoneNumber.DeviceId;

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = "Phone number not found." });
                    }
                }
            }

            return Ok(new { message = "Phone number updated successfully." });
        }

        [HttpGet("filter")]
        public IActionResult GetFilteredPhoneNumbers(
            string? number,
            int? deviceId)
        {
            if (deviceId.HasValue && deviceId.Value <= 0)
            {
                return BadRequest(new { message = "Device ID must be greater than zero." });
            }

            if (!string.IsNullOrWhiteSpace(number))
            {
                number = number.Trim();

                if (number.Length > 50)
                {
                    return BadRequest(new { message = "Phone number filter cannot exceed 50 characters." });
                }
            }
            else
            {
                number = null;
            }

            List<object> phoneNumbers = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT p.Id, p.Number, d.Name AS Device
                                 FROM PhoneNumber p
                                 INNER JOIN Device d ON p.DeviceId = d.Id
                                 WHERE (@Number IS NULL
                                        OR p.Number LIKE '%' + @Number + '%')
                                 AND (@DeviceId IS NULL
                                      OR p.DeviceId = @DeviceId)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Number", SqlDbType.NVarChar, 50).Value =
                        (object?)number ?? DBNull.Value;

                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        deviceId.HasValue
                            ? deviceId.Value
                            : DBNull.Value;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phoneNumbers.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Number = reader["Number"].ToString()!,
                                Device = reader["Device"].ToString()!
                            });
                        }
                    }
                }
            }

            return Ok(phoneNumbers);
        }
    }
}