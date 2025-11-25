using Dapper;
using KTC_SalesAppWAPI.DTOs.BaseRoute;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.GeoFence;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.BaseRouteSchedule
{
    [Route("[controller]")]
    [ApiController]
    public class BaseRouteController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<BaseRouteController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public BaseRouteController(IConfiguration configuration, ILogger<BaseRouteController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [EnableCors("AllowAngular")]
        [HttpPost]
        public IActionResult Post(DTO_BaseRoute dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetSubsi":
                    {
                        return GetSubsi();
                    }
                case "GetSchedule":
                    {
                        return GetSchedule(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetSchedule(DTO_BaseRoute dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi.");
                }
                if (dto.StartDate == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDate == default)
                {
                    return BadRequest("Invalid end date");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                // get the date 
                var sp_Query = @$"Select * from {db.WEBDB}..FTApp_BaseRouteSchedule 
                                  Where UserCode = @UserCode  
                                  and convert(date, ScheduleDate) >= @StartDate
                                  and convert(date, ScheduleDate) <= @EndDate ;";

                using var conn = new SqlConnection(_commDbConnStr);
                var founds = conn.Query<FTApp_BaseRouteSchedule>(sp_Query, new
                {
                    UserCode = dto.UserCode,
                    StartDate = $"{dto.StartDate:yyyyMMdd}",
                    EndDate = $"{dto.EndDate:yyyyMMdd}"
                }).ToList();

                if (founds.Count == 0)
                {
                    return NotFound();
                }

                return Ok(founds);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetSubsi()
        {
            try
            {
                var subsis = new DbNameHelper().GetDbInfos(_commDbConnStr).Select(c => c.COMPANYNAME).ToList();
                if (subsis.Count == 0) return NotFound();
                return Ok(subsis); // list of string for company name 
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

    }
}
