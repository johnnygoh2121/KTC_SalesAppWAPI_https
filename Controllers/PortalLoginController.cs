using Dapper;
using KTC_SalesAppWAPI.DTOs.PortalLogin;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppUpdate;
using KTC_SalesAppWAPI.Models.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PortalLoginController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PortalLoginController> _logger;
        readonly string APP_JSON = "application/json";
        string LastError { get; set; } = string.Empty;
        string WebHostAddrEndPoint = string.Empty;
        string _commDbConnStr;

        //readonly string AppType = "SalesApp";

        public PortalLoginController(IConfiguration configuration, ILogger<PortalLoginController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            WebHostAddrEndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPointLogin").Value;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(PortalLogin_dto dto)
        {
            try
            {
                // 20250925
                if (dto.DeviceDt == default)
                {
                    goto continueLogin;
                }
                var serverHour = DateTime.Now;
                if (serverHour.Hour != dto.DeviceDt.Hour)
                {
                    return BadRequest("Device DateTime no equal to server DateTime. [Hour]");
                }
                if (serverHour.Day != dto.DeviceDt.Day)
                {
                    return BadRequest("Device DateTime no equal to server DateTime. [Day]");
                }
                if (serverHour.Year != dto.DeviceDt.Year)
                {
                    return BadRequest("Device DateTime no equal to server DateTime.[Year]");
                }


            continueLogin:

                // original
                if (dto.isHelperLogin)
                {
                    return VerifyHelperLogin(dto);
                }

                if (!string.IsNullOrWhiteSpace(dto.appVersion) &&
                    !string.IsNullOrWhiteSpace(dto.appType))
                {
                    // check the latest version number
                    var sql = @"SELECT TOP 1 * 
                            FROM FTAPP_Update WITH (NOLOCK)    
                            Where AppType = @appType
                            ORDER by id DESC";

                    using var conn = new SqlConnection(_commDbConnStr);
                    var result = conn.Query<FTAPP_Update>(sql, new { dto.appType }).FirstOrDefault();

                    if (result == null)
                    {
                        goto ProcessNormal;
                    }

                    if (!$"{result.AppVersion}".Equals(dto.appVersion))
                    {
                        var message = $"Pls login with latest version {result.AppVersion}, " +
                                      $"current version {dto.appVersion} is outdated !";
                        return BadRequest(message);
                    }
                }

            ProcessNormal:
                using (var httpclient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(dto);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);
                    var uri = new Uri(WebHostAddrEndPoint);
                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var token = JsonConvert.DeserializeObject<BearerToken>(content);
                        token.companyId = dto.company;
                        return Ok(token);
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult VerifyHelperLogin(PortalLogin_dto dto)
        {
            try
            {
                //req.username = userName;
                //req.password = password;
                //req.company = companyId;
                //req.isHelperLogin = true;

                if (string.IsNullOrWhiteSpace(dto.userName))
                {
                    return BadRequest("Invald trick no");
                }


                if (string.IsNullOrWhiteSpace(dto.company))
                {
                    return BadRequest("Invald helper login company or subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.company);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db");
                }


                using var conn = new SqlConnection(_commDbConnStr);
                var sql = @$"Select top 1* 
                            From {db.WEBDB}..truck1 
                            Where TRUCKNO = @TRUCKNO 
                            and ISNULL(PASS2, '') = @PASS2";

                var result = conn.Query<TRUCK1>(sql, new { TRUCKNO = dto.userName, PASS2 = dto.password })
                                 .FirstOrDefault();

                if (result == null) return NotFound();

                return Ok();

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

    }
}
