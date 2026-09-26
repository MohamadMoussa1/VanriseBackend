using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VanriseBackend.Models;

namespace VanriseBackend.Controllers
{
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
        public void AddPhoneNumber(PhoneNumber phoneNumber)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"INSERT INTO PhoneNumber (Number, DeviceId)
                         VALUES (@Number, @DeviceId)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Number", phoneNumber.Number);

                    command.Parameters.AddWithValue(
                        "@DeviceId", phoneNumber.DeviceId);

                    command.ExecuteNonQuery();
                }
            }
        }
        [HttpPut]
        public void UpdatePhoneNumber(PhoneNumber phoneNumber)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"UPDATE PhoneNumber
                         SET Number = @Number,
                             DeviceId = @DeviceId
                         WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Id", phoneNumber.Id);

                    command.Parameters.AddWithValue(
                        "@Number", phoneNumber.Number);

                    command.Parameters.AddWithValue(
                        "@DeviceId", phoneNumber.DeviceId);

                    command.ExecuteNonQuery();
                }
            }
        }
        [HttpGet("filter")]
        public List<object> GetFilteredPhoneNumbers(
        string? number,
        int? deviceId)
        {
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
                    command.Parameters.AddWithValue(
                        "@Number",
                        (object?)number ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@DeviceId",
                        deviceId.HasValue
                            ? deviceId.Value
                            : DBNull.Value);

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

            return phoneNumbers;
        }
    }
}