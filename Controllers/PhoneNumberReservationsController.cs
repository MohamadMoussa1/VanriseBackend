using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace VanriseBackend.Controllers
{
    [Authorize]
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
        [HttpGet]
        public IActionResult GetAllPhoneNumberReservations()
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
            INNER JOIN Client c ON r.ClientId = c.Id
            INNER JOIN PhoneNumber p ON r.PhoneNumberId = p.Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
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

            return Ok(reservations);
        }
        [HttpGet("filter")]
        public IActionResult GetFilteredPhoneNumberReservations(
    int? clientId,
    int? phoneNumberId)
        {
            if (clientId.HasValue && clientId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid client ID."
                });
            }

            if (phoneNumberId.HasValue && phoneNumberId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid phone number ID."
                });
            }

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
            INNER JOIN Client c ON r.ClientId = c.Id
            INNER JOIN PhoneNumber p ON r.PhoneNumberId = p.Id
            WHERE (@ClientId IS NULL OR r.ClientId = @ClientId)
              AND (@PhoneNumberId IS NULL OR r.PhoneNumberId = @PhoneNumberId)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value =
                        clientId.HasValue ? (object)clientId.Value : DBNull.Value;

                    command.Parameters.Add("@PhoneNumberId", SqlDbType.Int).Value =
                        phoneNumberId.HasValue ? (object)phoneNumberId.Value : DBNull.Value;

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

            return Ok(reservations);
        }
        [HttpGet("active/{clientId}")]
        public IActionResult GetActiveReservations(int clientId)
        {
            if (clientId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid client ID."
                });
            }

            List<object> reservations = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if Client exists
                string checkQuery = "SELECT COUNT(*) FROM Client WHERE Id = @ClientId";

                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Client not found."
                        });
                    }
                }

                DateTime now = DateTime.Now;

                string query = @"
            SELECT
                r.Id,
                r.PhoneNumberId,
                p.Number
            FROM PhoneNumberReservation r
            INNER JOIN PhoneNumber p ON r.PhoneNumberId = p.Id
            WHERE r.ClientId = @ClientId
              AND r.BED < @Now
              AND (r.EED > @Now OR r.EED IS NULL)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
                    command.Parameters.Add("@Now", SqlDbType.DateTime).Value = now;

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

            return Ok(reservations);
        }
        [HttpPost("reserve")]
        public IActionResult ReservePhoneNumber(int clientId, int phoneNumberId)
        {
            if (clientId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid client ID."
                });
            }

            if (phoneNumberId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid phone number ID."
                });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if Client exists
                string clientQuery = "SELECT COUNT(*) FROM Client WHERE Id = @ClientId";

                using (SqlCommand command = new SqlCommand(clientQuery, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Client not found."
                        });
                    }
                }

                // Check if Phone Number exists
                string phoneQuery = "SELECT COUNT(*) FROM PhoneNumber WHERE Id = @PhoneNumberId";

                using (SqlCommand command = new SqlCommand(phoneQuery, connection))
                {
                    command.Parameters.Add("@PhoneNumberId", SqlDbType.Int).Value = phoneNumberId;

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Phone number not found."
                        });
                    }
                }

                // Insert reservation
                DateTime bed = DateTime.Now;

                string query = @"
            INSERT INTO PhoneNumberReservation
                (ClientId, PhoneNumberId, BED, EED)
            VALUES
                (@ClientId, @PhoneNumberId, @BED, NULL)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
                    command.Parameters.Add("@PhoneNumberId", SqlDbType.Int).Value = phoneNumberId;
                    command.Parameters.Add("@BED", SqlDbType.DateTime).Value = bed;

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new
            {
                message = "Phone number reserved successfully."
            });
        }
        [HttpPut("unreserve")]
        public IActionResult UnreservePhoneNumber(int clientId, int phoneNumberId)
        {
            if (clientId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid client ID."
                });
            }

            if (phoneNumberId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid phone number ID."
                });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if Client exists
                string clientQuery = "SELECT COUNT(*) FROM Client WHERE Id = @ClientId";

                using (SqlCommand command = new SqlCommand(clientQuery, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Client not found."
                        });
                    }
                }

                // Check if Phone Number exists
                string phoneQuery = "SELECT COUNT(*) FROM PhoneNumber WHERE Id = @PhoneNumberId";

                using (SqlCommand command = new SqlCommand(phoneQuery, connection))
                {
                    command.Parameters.Add("@PhoneNumberId", SqlDbType.Int).Value = phoneNumberId;

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Phone number not found."
                        });
                    }
                }

                DateTime now = DateTime.Now;

                string query = @"
            ;WITH ActiveReservation AS
            (
                SELECT TOP (1) Id
                FROM PhoneNumberReservation
                WHERE ClientId = @ClientId
                  AND PhoneNumberId = @PhoneNumberId
                  AND BED < @Now
                  AND (EED > @Now OR EED IS NULL)
                ORDER BY BED DESC, Id DESC
            )
            UPDATE r
            SET EED = @Now
            FROM PhoneNumberReservation r
            INNER JOIN ActiveReservation a ON r.Id = a.Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
                    command.Parameters.Add("@PhoneNumberId", SqlDbType.Int).Value = phoneNumberId;
                    command.Parameters.Add("@Now", SqlDbType.DateTime).Value = now;

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        return NotFound(new
                        {
                            message = "No active reservation found for this client and phone number."
                        });
                    }
                }
            }

            return Ok(new
            {
                message = "Phone number unreserved successfully."
            });
        }
    }

}