using Dapper;
using KTC_SalesAppWAPI.DTOs.AppUpdate;
using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.AppUpdate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AppUpdateController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<AppUpdateController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public AppUpdateController(IConfiguration configuration, ILogger<AppUpdateController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(AppUpdate_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "CheckUpdate":
                    {
                        return CheckUpdate(dto);
                    }
                case "CheckUpdate_Delivery":
                    {
                        return CheckUpdate_Delivery(dto);
                    }
                case "CheckCurrentVersion":
                    {
                        return CheckCurrentVersion(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        

        IActionResult CheckCurrentVersion (AppUpdate_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DeviceAppVersion))
                {
                    return BadRequest("ver value is empty");
                }

                // query the current version 
                var sql = @"select top 1 * 
                            from FTAPP_Update WITH (NOLOCK)                        
                            order by id desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<FTAPP_Update>(sql).FirstOrDefault();
                if (result == null)
                {
                    return BadRequest("No such app");
                }

                if (result.AppVersion.Equals(dto.DeviceAppVersion))
                {
                    return Ok("latest updated");
                }

                // build the download path and replied
                sql = @"SELECT SetupValue
                        FROM FTApp_Config WITH (NOLOCK)  
                        where SetupName = 'AppApkUpdateWebPath'";

                var webaddress = conn.ExecuteScalar<string>(sql);

                return Ok($"{webaddress}{result.ApkName}");
            }
            catch (Exception e)
            {

                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult CheckUpdate_Delivery(AppUpdate_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentAppName))
                {
                    return BadRequest("App name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CurrentAppVersion))
                {
                    return BadRequest("App version is empty");
                }

                //if (string.IsNullOrWhiteSpace(dto.UserCode))
                //{
                //    return BadRequest("user code is empty");
                //}
                //if (string.IsNullOrWhiteSpace(dto.UserName))
                //{
                //    return BadRequest("user name is empty");
                //}

                // query the current version 
                var sql = @"select top 1 * 
                            from FTAPP_Update WITH (NOLOCK)  
                            Where AppName = @CurrentAppName
                            order by id desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<FTAPP_Update>(sql, new { dto.CurrentAppName }).FirstOrDefault();
                if (result == null)
                {
                    return BadRequest("No such app");
                }

                if (result.AppVersion.Equals(dto.CurrentAppVersion))
                {
                    return Ok("you are in latest version.");
                }

                var sql_query = @"SELECT 
                                      SetupName
                                      ,SetupValue
                                      ,SetupDesc
                                  FROM [FTApp_Config] WITH (NOLOCK)   
                                  where SetupName = 'AppApkUpdateWebPath'";

                var config = conn.Query<FTApp_Config>(sql_query).FirstOrDefault();
                if (config == null) return NotFound();

                var newLog = new FTAPP_UpdateLog
                {
                    ApkDownloadPath = $"{config.SetupValue}{result.ApkName}",
                    AppName = dto.CurrentAppName,
                    UserCode = "DelApp", //dto.UserCode,
                    UserName = "DelApp", //dto.UserName,
                    PhoneDt = dto.DeviceDt,
                    VersionFrom = dto.CurrentAppVersion,
                    VersionTo = result.AppVersion,
                    Subsi = dto.Subsi
                };
                InsertUpdateLog(conn, newLog);

                return Ok($"{config.SetupValue}{result.ApkName}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult CheckUpdate(AppUpdate_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentAppName))
                {
                    return BadRequest("App name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CurrentAppVersion))
                {
                    return BadRequest("App version is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("User name is empty");
                }

                // query the current version 
                var sql = @"select top 1 * 
                            from FTAPP_Update WITH (NOLOCK)  
                            Where AppName = @CurrentAppName
                            order by id desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<FTAPP_Update>(sql, new { dto.CurrentAppName }).FirstOrDefault();
                if (result == null)
                {
                    return BadRequest("No such app");
                }

                if (result.AppVersion.Equals(dto.CurrentAppVersion))
                {
                    return Ok("you are in latest version");
                }

                var sql_query = @"SELECT 
                                      SetupName
                                      ,SetupValue
                                      ,SetupDesc
                                  FROM [FTApp_Config] WITH (NOLOCK)   
                                  where SetupName = 'AppApkUpdateWebPath'";

                var config = conn.Query<FTApp_Config>(sql_query).FirstOrDefault();
                if (config == null) return NotFound();

                var newLog = new FTAPP_UpdateLog
                {
                    ApkDownloadPath = $"{config.SetupValue}{result.ApkName}",
                    AppName = dto.CurrentAppName,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    PhoneDt = dto.DeviceDt,
                    VersionFrom = dto.CurrentAppVersion,
                    VersionTo = result.AppVersion,
                    Subsi = dto.Subsi
                };
                InsertUpdateLog(conn, newLog);

                return Ok($"{config.SetupValue}{result.ApkName}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        void InsertUpdateLog(SqlConnection conn, FTAPP_UpdateLog log)
        {
            try
            {
                var sql = @"INSERT INTO [FTAPP_UpdateLog] (
                                        ApkDownloadPath
                                       ,AppName
                                       ,UserCode
                                       ,UserName
                                       ,PhoneDt
                                       ,ServerDt
                                       ,VersionFrom
                                       ,VersionTo
                                       ,TransDt 
                                       ,Subsi
                                        ) VALUES (
                                        @ApkDownloadPath
                                       ,@AppName
                                       ,@UserCode
                                       ,@UserName
                                       ,@PhoneDt
                                       ,GETDATE()
                                       ,@VersionFrom
                                       ,@VersionTo
                                       ,GETDATE()
                                       ,@Subsi
                                    )";
                var result = conn.ExecuteScalar<int>(sql, log);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return;
            }
        }
    }
}
