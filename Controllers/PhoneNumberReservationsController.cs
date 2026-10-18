using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace VanriseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhoneNumberReservationsController : ControllerBase
    {
        private readonly string _connectionString;

        public PhoneNumberReservationsController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public List<object> GetAllPhoneNumberReservations()
        {
            List<object> reservations = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT
                r.Id,
                c.Name AS Client,
                p.Number AS PhoneNumber,
                r.BED,
                r.EED
            FROM PhoneNumberReservation r
            INNER JOIN Client c
                ON r.ClientId = c.Id
            INNER JOIN PhoneNumber p
                ON r.PhoneNumberId = p.Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservations.Add(new
                        {
                            Id = Convert.ToInt32(reader["Id"]),

                            Client = reader["Client"].ToString()!,

                            PhoneNumber = reader["PhoneNumber"].ToString()!,

                            BED = Convert.ToDateTime(reader["BED"]),

                            EED = reader["EED"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["EED"])
                        });
                    }
                }
            }

            return reservations;
        }
        [HttpGet("filter")]
        public List<object> GetFilteredPhoneNumberReservations(int? clientId,int? phoneNumberId)
        {
            List<object> reservations = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT
                r.Id,
                c.Name AS Client,
                p.Number AS PhoneNumber,
                r.BED,
                r.EED
            FROM PhoneNumberReservation r
            INNER JOIN Client c
                ON r.ClientId = c.Id
            INNER JOIN PhoneNumber p
                ON r.PhoneNumberId = p.Id
            WHERE (@ClientId IS NULL OR r.ClientId = @ClientId)
              AND (@PhoneNumberId IS NULL OR r.PhoneNumberId = @PhoneNumberId)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@ClientId",
                        clientId.HasValue
                            ? clientId.Value
                            : DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@PhoneNumberId",
                        phoneNumberId.HasValue
                            ? phoneNumberId.Value
                            : DBNull.Value);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reservations.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),

                                Client = reader["Client"].ToString()!,

                                PhoneNumber = reader["PhoneNumber"].ToString()!,

                                BED = Convert.ToDateTime(reader["BED"]),

                                EED = reader["EED"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["EED"])
                            });
                        }
                    }
                }
            }

            return reservations;
        }
        [HttpGet("active/{clientId}")]
        public List<object> GetActiveReservations(int clientId)
        {
            List<object> reservations = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT
                r.Id,
                p.Id AS PhoneNumberId,
                p.Number
            FROM PhoneNumberReservation r
            INNER JOIN PhoneNumber p
                ON r.PhoneNumberId = p.Id
            WHERE r.ClientId = @ClientId
              AND r.BED < GETDATE()
              AND (r.EED > GETDATE() OR r.EED IS NULL)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reservations.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                PhoneNumberId = Convert.ToInt32(reader["PhoneNumberId"]),
                                Number = reader["Number"].ToString()!
                            });
                        }
                    }
                }
            }

            return reservations;
        }
        [HttpPost("reserve")]
        public IActionResult ReservePhoneNumber(int clientId, int phoneNumberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            INSERT INTO PhoneNumberReservation
                (ClientId, PhoneNumberId, BED, EED)
            VALUES
                (@ClientId, @PhoneNumberId, @BED, NULL)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);
                    command.Parameters.AddWithValue("@PhoneNumberId", phoneNumberId);
                    command.Parameters.AddWithValue("@BED", DateTime.Now);

                    command.ExecuteNonQuery();
                }
            }

            return Ok();
        }
        [HttpPut("unreserve")]
        public IActionResult UnreservePhoneNumber(int clientId, int phoneNumberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            UPDATE PhoneNumberReservation
            SET EED = GETDATE()
            WHERE ClientId = @ClientId
              AND PhoneNumberId = @PhoneNumberId
              AND BED < GETDATE()
              AND (EED > GETDATE() OR EED IS NULL)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);
                    command.Parameters.AddWithValue("@PhoneNumberId", phoneNumberId);

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                        return NotFound("No active reservation found.");
                }
            }

            return Ok();
        }
    }

}