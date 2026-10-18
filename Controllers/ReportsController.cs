using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace VanriseBackend.Controllers
{
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
        public List<object> GetClientsPerType(int? type)
        {
            List<object> report = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT
                Type,
                COUNT(*) AS NoOfClients
            FROM Client
            WHERE (@Type IS NULL OR Type = @Type)
            GROUP BY Type
            ORDER BY Type";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Type", SqlDbType.Int).Value =
                        (object?)type ?? DBNull.Value;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int clientType = Convert.ToInt32(reader["Type"]);

                            report.Add(new
                            {
                                Type = clientType == 0
                                    ? "Individual"
                                    : "Organization",

                                NoOfClients =
                                    Convert.ToInt32(reader["NoOfClients"])
                            });
                        }
                    }
                }
            }

            return report;
        }
        [HttpGet("phone-numbers-per-device")]
        public List<object> GetPhoneNumbersPerDevice(
    int? deviceId,
    string? status)
        {
            List<object> report = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            WITH PhoneStatuses AS
            (
                SELECT
                    p.Id,
                    p.DeviceId,
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM PhoneNumberReservation r
                            WHERE r.PhoneNumberId = p.Id
                              AND r.BED < GETDATE()
                              AND (r.EED > GETDATE() OR r.EED IS NULL)
                        )
                        THEN 'Reserved'
                        ELSE 'Unreserved'
                    END AS PhoneStatus
                FROM PhoneNumber p
            )
            SELECT
                d.Name AS Device,
                ps.PhoneStatus AS PhoneNumbersStatus,
                COUNT(*) AS NoOfPhoneNumbers
            FROM PhoneStatuses ps
            INNER JOIN Device d
                ON ps.DeviceId = d.Id
            WHERE
                (@DeviceId IS NULL OR d.Id = @DeviceId)
                AND
                (@Status IS NULL OR ps.PhoneStatus = @Status)
            GROUP BY
                d.Name,
                ps.PhoneStatus
            ORDER BY
                d.Name,
                ps.PhoneStatus";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                        (object?)deviceId ?? DBNull.Value;

                    command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value =
                        string.IsNullOrWhiteSpace(status)
                            ? DBNull.Value
                            : status;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            report.Add(new
                            {
                                Device = reader["Device"].ToString()!,

                                PhoneNumbersStatus =
                                    reader["PhoneNumbersStatus"].ToString()!,

                                NoOfPhoneNumbers =
                                    Convert.ToInt32(reader["NoOfPhoneNumbers"])
                            });
                        }
                    }
                }
            }

            return report;
        }
    }
}
