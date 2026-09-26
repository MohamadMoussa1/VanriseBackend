using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VanriseBackend.Models;
using VanriseBackend.Enums;

namespace VanriseBackend.Controllers
{
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
        public void AddClient(Client client)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"INSERT INTO Client (Name, Type, BirthDate)
                         VALUES (@Name, @Type, @BirthDate)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", client.Name);
                    command.Parameters.AddWithValue("@Type", (int)client.Type);

                    if (client.Type == ClientType.Individual)
                    {
                        command.Parameters.AddWithValue(
                            "@BirthDate",
                            (object?)client.BirthDate ?? DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue(
                            "@BirthDate",
                            DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        [HttpPut]
        public void UpdateClient(Client client)
        {
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

                    if (client.Type == ClientType.Individual)
                    {
                        command.Parameters.AddWithValue(
                            "@BirthDate",
                            (object?)client.BirthDate ?? DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue(
                            "@BirthDate",
                            DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        [HttpGet("filter")]
        
        public List<object> GetFilteredClients(string? name, ClientType? type)
        {
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
                        (object?)name ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@Type",
                        type.HasValue
                            ? (int)type.Value
                            : DBNull.Value);

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

            return clients;
        }
    }
}