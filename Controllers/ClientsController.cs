using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VanriseBackend.Models;
using VanriseBackend.Enums;
using Microsoft.AspNetCore.Authorization;
namespace VanriseBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly string _connectionString;

        public ClientsController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public List<object> GetAllClients()
        {
            List<object> clients = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id, Name, Type, BirthDate FROM Client";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ClientType type =
                            (ClientType)Convert.ToInt32(reader["Type"]);

                        clients.Add(new
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString()!,
                            Type = type.ToString(),
                            BirthDate = reader["BirthDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["BirthDate"])
                        });
                    }
                }
            }

            return clients;
        }

        [HttpPost]
        public IActionResult AddClient(Client client)
        {
            if (string.IsNullOrWhiteSpace(client.Name))
            {
                return BadRequest(new
                {
                    message = "Client name is required."
                });
            }

            client.Name = client.Name.Trim();

            if (client.Name.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Client name cannot exceed 255 characters."
                });
            }

            if (client.Type != ClientType.Individual &&
                client.Type != ClientType.Organization)
            {
                return BadRequest(new
                {
                    message = "Invalid client type."
                });
            }

            if (client.Type == ClientType.Individual &&
                client.BirthDate.HasValue)
            {
                DateTime birthDate = client.BirthDate.Value.Date;

                if (birthDate > DateTime.Today)
                {
                    return BadRequest(new
                    {
                        message = "Birth date cannot be in the future."
                    });
                }

                if (birthDate >= DateTime.Today.AddYears(-18))
                {
                    return BadRequest(new
                    {
                        message = "Client must be older than 18."
                    });
                }
            }

            if (client.Type == ClientType.Organization)
            {
                client.BirthDate = null;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"INSERT INTO Client (Name, Type, BirthDate)
                         VALUES (@Name, @Type, @BirthDate)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", client.Name);
                    command.Parameters.AddWithValue("@Type", (int)client.Type);

                    command.Parameters.AddWithValue(
                        "@BirthDate",
                        (object?)client.BirthDate ?? DBNull.Value
                    );

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new
            {
                message = "Client added successfully."
            });
        }

        [HttpPut]
        public IActionResult UpdateClient(Client client)
        {
            if (client.Id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid client ID."
                });
            }

            if (string.IsNullOrWhiteSpace(client.Name))
            {
                return BadRequest(new
                {
                    message = "Client name is required."
                });
            }

            client.Name = client.Name.Trim();

            if (client.Name.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Client name cannot exceed 255 characters."
                });
            }

            if (client.Type != ClientType.Individual &&
                client.Type != ClientType.Organization)
            {
                return BadRequest(new
                {
                    message = "Invalid client type."
                });
            }

            if (client.Type == ClientType.Individual &&
                client.BirthDate.HasValue)
            {
                DateTime birthDate = client.BirthDate.Value.Date;

                if (birthDate > DateTime.Today)
                {
                    return BadRequest(new
                    {
                        message = "Birth date cannot be in the future."
                    });
                }

                if (birthDate >= DateTime.Today.AddYears(-18))
                {
                    return BadRequest(new
                    {
                        message = "Client must be older than 18."
                    });
                }
            }

            if (client.Type == ClientType.Organization)
            {
                client.BirthDate = null;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"UPDATE Client
                         SET Name = @Name,
                             Type = @Type,
                             BirthDate = @BirthDate
                         WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", client.Id);
                    command.Parameters.AddWithValue("@Name", client.Name);
                    command.Parameters.AddWithValue("@Type", (int)client.Type);

                    command.Parameters.AddWithValue(
                        "@BirthDate",
                        (object?)client.BirthDate ?? DBNull.Value
                    );

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        return NotFound(new
                        {
                            message = "Client not found."
                        });
                    }
                }
            }

            return Ok(new
            {
                message = "Client updated successfully."
            });
        }

        [HttpGet("filter")]
        public IActionResult GetFilteredClients(string? name, ClientType? type)
        {
            if (type.HasValue &&
                type.Value != ClientType.Individual &&
                type.Value != ClientType.Organization)
            {
                return BadRequest(new
                {
                    message = "Invalid client type."
                });
            }

            List<object> clients = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT Id, Name, Type, BirthDate
                         FROM Client
                         WHERE (@Name IS NULL OR Name LIKE '%' + @Name + '%')
                         AND (@Type IS NULL OR Type = @Type)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Name",
                        (object?)name ?? DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@Type",
                        type.HasValue
                            ? (object)(int)type.Value
                            : DBNull.Value
                    );

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ClientType clientType =
                                (ClientType)Convert.ToInt32(reader["Type"]);

                            clients.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString()!,
                                Type = clientType.ToString(),

                                BirthDate = reader["BirthDate"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["BirthDate"])
                            });
                        }
                    }
                }
            }

            return Ok(clients);
        }
    }
}