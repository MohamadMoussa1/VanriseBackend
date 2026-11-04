using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace VanriseBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly string _connectionString;

        public ReportsController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet("clients-per-type")]
        public async Task<IActionResult> GetClientsPerType(int? type)
        {
            // Type: 0 = Individual, 1 = Organization
            if (type.HasValue && type.Value != 0 && type.Value != 1)
            {
                return BadRequest(new
                {
                    Message = "Invalid client type. Use 0 for Individual or 1 for Organization."
                });
            }

            var result = new List<object>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT Type, COUNT(*) AS NoOfClients
            FROM Client
            WHERE (@Type IS NULL OR Type = @Type)
            GROUP BY Type
            ORDER BY Type";

                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Type", SqlDbType.Int).Value =
                    type.HasValue ? type.Value : DBNull.Value;

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int clientType = Convert.ToInt32(reader["Type"]);

                    result.Add(new
                    {
                        Type = clientType == 0 ? "Individual" : "Organization",
                        NoOfClients = Convert.ToInt32(reader["NoOfClients"])
                    });
                }
            }

            return Ok(result);
        }
        [HttpGet("phone-numbers-per-device")]
        public async Task<IActionResult> GetPhoneNumbersPerDevice(
    int? deviceId,
    string? status)
        {
            if (deviceId.HasValue && deviceId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Device ID must be greater than zero."
                });
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();

                if (!status.Equals("Reserved", StringComparison.OrdinalIgnoreCase) &&
                    !status.Equals("Unreserved", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        Message = "Invalid status. Use Reserved or Unreserved."
                    });
                }

                status = status.Equals("Reserved", StringComparison.OrdinalIgnoreCase)
                    ? "Reserved"
                    : "Unreserved";
            }
            else
            {
                status = null;
            }

            var result = new List<object>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            WITH PhoneNumberStatus AS
            (
                SELECT
                    d.Name AS Device,
                    pn.DeviceId,
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM PhoneNumberReservation r
                            WHERE r.PhoneNumberId = pn.Id
                              AND r.BED < GETDATE()
                              AND (r.EED > GETDATE() OR r.EED IS NULL)
                        )
                        THEN 'Reserved'
                        ELSE 'Unreserved'
                    END AS PhoneNumbersStatus
                FROM PhoneNumber pn
                INNER JOIN Device d ON pn.DeviceId = d.Id
            )
            SELECT
                Device,
                PhoneNumbersStatus,
                COUNT(*) AS NoOfPhoneNumbers
            FROM PhoneNumberStatus
            WHERE (@DeviceId IS NULL OR DeviceId = @DeviceId)
              AND (@Status IS NULL OR PhoneNumbersStatus = @Status)
            GROUP BY Device, PhoneNumbersStatus
            ORDER BY Device, PhoneNumbersStatus";

                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                    deviceId.HasValue ? deviceId.Value : DBNull.Value;

                command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value =
                    status == null ? DBNull.Value : status;

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        Device = reader["Device"].ToString(),
                        PhoneNumbersStatus = reader["PhoneNumbersStatus"].ToString(),
                        NoOfPhoneNumbers = Convert.ToInt32(reader["NoOfPhoneNumbers"])
                    });
                }
            }

            return Ok(result);
        }
    }
}
