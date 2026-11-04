using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VanriseBackend.Mappers;
using VanriseBackend.Models;

namespace VanriseBackend.Controllers
{
    [Authorize]
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
        public IActionResult AddDevice(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Device name is required."
                });
            }

            if (name.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Device name cannot exceed 255 characters."
                });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Device (Name) VALUES (@Name)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name.Trim());
                    command.ExecuteNonQuery();
                }
            }

            return Ok(new
            {
                message = "Device added successfully."
            });
        }

        [HttpPut]
        public IActionResult UpdateDevice(Device device)
        {
            if (device.Id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid device ID."
                });
            }

            if (string.IsNullOrWhiteSpace(device.Name))
            {
                return BadRequest(new
                {
                    message = "Device name is required."
                });
            }

            if (device.Name.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Device name cannot exceed 255 characters."
                });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "UPDATE Device SET Name = @Name WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", device.Id);
                    command.Parameters.AddWithValue("@Name", device.Name.Trim());

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        return NotFound(new
                        {
                            message = "Device not found."
                        });
                    }
                }
            }

            return Ok(new
            {
                message = "Device updated successfully."
            });
        }


    }


}
