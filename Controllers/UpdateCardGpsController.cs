using Dapper;
using KTC_SalesAppWAPI.DTOs.UpdateCardGps;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.UpdateCardGps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UpdateCardGpsController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;        
        readonly ILogger<UpdateCardGpsController> _logger;
        string _commDbConnStr = "";
        string LastError = "";

        public UpdateCardGpsController(IConfiguration configuration, ILogger<UpdateCardGpsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);            
        }

        public IActionResult Post(Dto_UpdateGps dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "Update":
                    {
                        return Update(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult Update(Dto_UpdateGps dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("CardCode is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardName))
                {
                    return BadRequest("CardName is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("User name is empty");
                }
                if (dto.NewLongi <= 0)
                {
                    return BadRequest("Invalid new logi value");
                }
                if (dto.NewLat <= 0)
                {
                    return BadRequest("Invalid new lat value");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }
                //public int id { get; set; }
                //public string CardCode { get; set; }
                //public string CardName { get; set; }
                //public string UserCode { get; set; }
                //public string UserName { get; set; }
                //public decimal CurrentLatitude { get; set; }
                //public decimal CurrentLogitude { get; set; }
                //public decimal NewLatitude { get; set; }
                //public decimal NewLongitude { get; set; }
                //public DateTime RequestDt { get; set; }
                //public string UpdateStatus { get; set; }
                //public string CompanyName { get; set; }
                //public int CompanyId { get; set; }

                var newReq = new FTAPP_ReqUpdateCardGps
                {
                    CardCode = dto.CardCode,
                    CardName = dto.CardName,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    CurrentLatitude = dto.CurrentLat,
                    CurrentLogitude = dto.CurrentLongi,
                    NewLatitude = dto.NewLat,
                    NewLongitude = dto.NewLongi,
                    UpdateStatus = "New",
                    CompanyName = db.COMPANYNAME,
                    CompanyId = db.COMPANYID
                };

                var sql = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_ReqUpdateCardGps]
                                   ( CardCode
                                   , CardName
                                   , UserCode
                                   , UserName
                                   , CurrentLatitude
                                   , CurrentLogitude
                                   , NewLatitude
                                   , NewLongitude
                                   , RequestDt
                                   , UpdateStatus
                                   , CompanyName
                                   , CompanyId 
                                    ) VALUES (
                                            @CardCode
                                          , @CardName
                                          , @UserCode
                                          , @UserName
                                          , @CurrentLatitude
                                          , @CurrentLogitude
                                          , @NewLatitude
                                          , @NewLongitude
                                          , GETDATE ()
                                          , @UpdateStatus
                                          , @CompanyName
                                          , @CompanyId )";

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Execute(sql, newReq);
                if (res > 0)
                {
                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = "NA";
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }
                    return Ok();
                }

                return BadRequest("req update error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = LastError;
                    new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                }
                
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}
