using Microsoft.Data.SqlClient;
using VanriseBackend.Models;

namespace VanriseBackend.Mappers
{
    public static class DeviceMapper
    {
        public static Device Map(SqlDataReader reader)
        {
            return new Device
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!
            };
        }
    }
}