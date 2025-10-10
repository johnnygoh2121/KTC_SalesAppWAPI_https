using Dapper;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.FileUpload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;


namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UploadLogController : ControllerBase
    {
        readonly IConfiguration _configuration;
        ILogger _logger;

        string LastError = string.Empty;
        string _webPortal_Host_EndPoint = "";
        
        string _commDbConnStr = "";

        public UploadLogController(IConfiguration configuration, ILogger<UploadLogController> logger)
        {
            _configuration = configuration;
        
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _webPortal_Host_EndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult UploadLog(FTAPP_FileUploadLog log)
        {
            try
            {
                if (log == null)
                {
                    _logger.LogError("invalid log");
                    return BadRequest("invalid log");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, log.SubSi);
                if (db == null)
                {
                    _logger.LogError("db info query error, or invalid subsi");
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql_insertLog = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_FileUploadLog] (
                                            Module
                                            ,FileType
                                            ,TransDt
                                            ,UserCode
                                            ,UserName
                                            ,Filename
                                            ,Version
                                            ,SubSi
                                            ,CardCode
                                            ,UploadStatus
                                          ) VALUES ( 
                                            @Module
                                            ,@FileType
                                            ,GETDATE()
                                            ,@UserCode
                                            ,@UserName
                                            ,@Filename
                                            ,@Version
                                            ,@SubSi 
                                            ,@CardCode
                                            ,@UploadStatus
                                    )";

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Execute(sql_insertLog, log);
                if (res <= 0)
                {
                    var json = JsonConvert.SerializeObject(log);
                    _logger.LogError($"error insert log,{json}");
                    return BadRequest("Error insert file upload load");
                }

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

    }
}
