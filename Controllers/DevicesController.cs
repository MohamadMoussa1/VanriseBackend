using Microsoft.AspNetCore.Mvc;
using VanriseBackend.Models;
using Microsoft.Data.SqlClient;
namespace VanriseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private static List<Device> devices = new List<Device>
        {
            new Device { Id = 1, Name = "Laptop" },
            new Device { Id = 2, Name = "Phone" },
            new Device { Id = 3, Name = "Tablet" }
        };

        [HttpGet]
        public List<Device> GetAllDevices()
        {
            return devices;
        }


        [HttpGet("filter")]
        public List<Device> GetFilteredDevices(string search)
        {
            return devices
                .Where(device => device.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        [HttpPost]
        public void AddDevice(Device device)
        {
            devices.Add(device);
        }

        [HttpPut]
        public void UpdateDevice(Device device)
        {
            Device? existingDevice = devices.FirstOrDefault(d => d.Id == device.Id);

            if (existingDevice != null)
            {
                existingDevice.Name = device.Name;
            }
        }


    }


}
