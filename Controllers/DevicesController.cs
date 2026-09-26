using Microsoft.AspNetCore.Mvc;
using VanriseBackend.Models;
using Microsoft.Data.SqlClient;
using VanriseBackend.Mappers;
namespace VanriseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly string _connectionString;

        public DevicesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public List<Device> GetAllDevices()
        {
            List<Device> devices = new List<Device>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id, Name FROM Device";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            devices.Add(DeviceMapper.Map(reader));
                        }
                    }
                }
            }

            return devices;
        }


        [HttpGet("filter")]
        public List<Device> GetFilteredDevices(string search)
        {
            List<Device> devices = new List<Device>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id, Name FROM Device WHERE Name LIKE @Search";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Search", "%" + search + "%");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            devices.Add(DeviceMapper.Map(reader));
                        }
                    }
                }
            }

            return devices;
        }

        [HttpPost]
        public void AddDevice(string name)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Device (Name) VALUES (@Name)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.ExecuteNonQuery();
                }
            }
        }

        [HttpPut]
        public void UpdateDevice(Device device)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "UPDATE Device SET Name = @Name WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", device.Name);
                    command.Parameters.AddWithValue("@Id", device.Id);

                    command.ExecuteNonQuery();
                }
            }
        }


    }


}
