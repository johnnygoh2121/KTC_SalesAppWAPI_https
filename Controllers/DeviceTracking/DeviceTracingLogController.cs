using Dapper;
using KTC_SalesAppWAPI.DTOs.DeviceTracing;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers.DeviceTracking
{
    [Route("[controller]")]
    [ApiController]
    public class DeviceTracingLogController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<DeviceTracingLogController> _logger;

        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public DeviceTracingLogController(IConfiguration configuration, ILogger<DeviceTracingLogController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost] // POST https://yourdomain.com/DeviceTracingLog
                   // AI generated code

        public async Task<IActionResult> Logging([FromBody] DeviceTracingLog_Dto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request data.");

            try
            {
                // Validate DB info
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                    return BadRequest("Invalid company name for DB info.");

                // Validate log data
                if (dto.Log == null)
                    return BadRequest("Log data is null or empty.");

                // Build SQL safely
                var sql = $@"
                            INSERT INTO [{db.WEBDB}]..[FTAPP_DeviceTraceLog] (
                                DeviceId, TruckNo, Latitude, Longitude, StopTime, DeviceDateTime,
                                Speed, StreetName, BuildingName, BusinessName, City, Synced
                            ) VALUES (
                                @DeviceId, @TruckNo, @Latitude, @Longitude, GETDATE(), @DeviceDateTime,
                                @Speed, @StreetName, @BuildingName, @BusinessName, @City, @Synced
                            );";

                // Use using for connection disposal
                using var conn = new SqlConnection(_commDbConnStr);

                // Execute asynchronously
                var inserted = await conn.ExecuteAsync(sql, dto.Log);

                if (inserted > 0)
                    return Ok(new { Message = "Log inserted successfully." });

                return BadRequest("Insertion failed. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting device trace log.");
                return StatusCode(500, new
                {
                    Error = "Internal Server Error",
                    Details = ex.Message
                });
            }
        }


        [HttpPost("bulk")] // POST https://yourdomain.com/api/DeviceTracingLog/bulk
        public async Task<IActionResult> BulkLogging([FromBody] DeviceTracingLog_Dto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request data.");

            try
            {
                // Validate DB info
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                    return BadRequest("Invalid company name for DB info.");

                if (dto.Logs == null || !dto.Logs.Any())
                    return BadRequest("No logs provided.");

                // Prepare DataTable for TVP
                var dataTable = new DataTable();
                dataTable.Columns.Add("DeviceId", typeof(string));
                dataTable.Columns.Add("TruckNo", typeof(string));
                dataTable.Columns.Add("Latitude", typeof(double));
                dataTable.Columns.Add("Longitude", typeof(double));
                dataTable.Columns.Add("StopTime", typeof(DateTime));
                dataTable.Columns.Add("DeviceDateTime", typeof(DateTime));
                dataTable.Columns.Add("Speed", typeof(double));
                dataTable.Columns.Add("StreetName", typeof(string));
                dataTable.Columns.Add("BuildingName", typeof(string));
                dataTable.Columns.Add("BusinessName", typeof(string));
                dataTable.Columns.Add("City", typeof(string));
                dataTable.Columns.Add("Synced", typeof(bool));
                dataTable.Columns.Add("DriverName", typeof (string));               

                foreach (var log in dto.Logs)
                {
                    dataTable.Rows.Add(
                        log.DeviceId,
                        log.TruckNo,
                        log.Latitude,
                        log.Longitude,                        
                        DateTime.Now, // change to server time  //log.StopTime,
                        log.DeviceDateTime,
                        log.Speed,
                        log.StreetName,
                        log.BuildingName,
                        log.BusinessName,
                        log.City,
                        1,  // always set to 1 
                        log.DriverName
                    );
                }

                // DeviceId, TruckNo, Latitude, Longitude, StopTime, DeviceDateTime,
                 //Speed, StreetName, BuildingName, BusinessName, City, Synced, DriverName

                using var conn = new SqlConnection(_commDbConnStr);
                var parameters = new DynamicParameters();
                parameters.Add("@Logs", dataTable.AsTableValuedParameter("dbo.DeviceTraceLogType"));
                parameters.Add("@webDb", db.WEBDB);

                var inserted = await conn.ExecuteAsync(
                    "[dbo].[BulkInsertDeviceTraceLog]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new { InsertedCount = inserted });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting bulk device trace logs.");
                return StatusCode(500, new
                {
                    Error = "Internal Server Error",
                    Details = ex.Message
                });
            }
        }

    }
}

// reference 
/*
--use KTCW_KK
--use KTCW_TG
--use KTCW_ML
--use KTCW_SW
--use KTCW_TW
--use KTCW_DI
--use KTCW_CR
--use KTCW_GT
--use KTCW_AM
--use KTCW_PT
--use KTCW_SH
--use TEST_WEB_KK
--use TEST_WEB_TW
--use TEST_WEB_AM
--use ktcw_common
go

  -- DeviceId, TruckNo, Latitude, Longitude, StopTime, DeviceDateTime,
  -- Speed, StreetName, BuildingName, BusinessName, City, Synced, DriverName

CREATE TYPE dbo.DeviceTraceLogType AS TABLE
(
    DeviceId NVARCHAR(100),
    TruckNo NVARCHAR(50),
    Latitude FLOAT,
    Longitude FLOAT,
    StopTime DATETIME,
    DeviceDateTime DATETIME,
    Speed FLOAT NULL,
    StreetName NVARCHAR(200) NULL,
    BuildingName NVARCHAR(200) NULL,
	BusinessName NVARCHAR(200) NULL,   
	City NVARCHAR(200) null,
    Synced  bit,
	DriverName nvarchar(120) null
);
go 

CREATE PROCEDURE [dbo].[BulkInsertDeviceTraceLog]
    @Logs dbo.DeviceTraceLogType READONLY,
    @webDb NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @sql NVARCHAR(MAX) = '
        INSERT INTO ' + QUOTENAME(@webDb) + '..[FTAPP_DeviceTraceLog] (
            DeviceId, TruckNo, Latitude, Longitude, StopTime, DeviceDateTime,
            Speed, StreetName, BuildingName, BusinessName, City, Synced, DriverName
        )
        SELECT DeviceId, TruckNo, Latitude, Longitude, StopTime, DeviceDateTime,
               Speed, StreetName, BuildingName, BusinessName, City, Synced, DriverName
        FROM @Logs;';

    EXEC sp_executesql @sql,
        N'@Logs dbo.DeviceTraceLogType READONLY',
        @Logs;
END
*/