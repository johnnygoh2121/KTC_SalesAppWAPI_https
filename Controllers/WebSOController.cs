using Dapper;
using KTC_SalesAppWAPI.DTOs.WebSo;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.SalesOrder;
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
    public class WebSOController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<WebSOController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;
        public WebSOController(IConfiguration configuration, ILogger<WebSOController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(WebSO_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "SOs":
                    {
                        return SOs(dto);

                    }
                case "SOLines":
                    {
                        return SOLines(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult SOs(WebSO_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }               
                if (string.IsNullOrWhiteSpace(dto.SlpCode))
                {
                    return BadRequest("User Slp Code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User Code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("error getting web db");
                }

                var sql = @"exec sp_SelectSo @companyName, @webDb, @slpCode, @userCode, @start, @end";
                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var results = conn.Query<SO>(sql,
                        new
                        {
                            companyName = dto.CompanyName,
                            webDb = db.WEBDB,
                            slpCode = dto.SlpCode,
                            userCode = dto.UserCode,
                            start = dto.StartDt,
                            end = dto.EndDt

                        }).ToList();

                    if (results != null && results.Count > 0)
                    {
                        // loop each sales order lines for app to access
                        for (int s = 0; s < results.Count; s ++)
                        {
                            var doc = results[s];
                            if (doc == null) continue;

                            // load it sales order line 
                            var sq_line = @$"Select * from {db.WEBDB}..SO1 with (nolock) Where docentry = @docentry ";
                            results[s].Lines = conn.Query<SO1>(sq_line, new { docentry = doc.DOCENTRY }).ToList();
                        }
                        return Ok(results);
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

        IActionResult SOLines(WebSO_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Doc entry is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("error getting web db");
                }
                var sql = @"exec sp_SelectSoLines @companyName, @webDb, @docEntry";
                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var results = conn.Query<SO1>(sql,
                        new
                        {
                            companyName = db.COMPANYNAME,
                            webDb = db.WEBDB,
                            docEntry = dto.DocEntry
                        }).ToList();

                    if (results != null && results.Count > 0)
                    {
                        return Ok(results);
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
    }
}
