using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.BreadRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadRequestController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadRequestController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;

        public BreadRequestController (IConfiguration configuration, ILogger<BreadRequestController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
        }

        [HttpPost]
        public IActionResult Post(Dto_BreaRequest dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadRequestList":
                    {
                        return LoadRequestList(dto);
                    }
                case "LoadRequestLines":
                    {
                        return LoadRequestLines(dto);
                    }
                case "SaveBreadRequest":
                    {
                        return SaveBreadRequest(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult SaveBreadRequest (Dto_BreaRequest dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (dto.RequestHead == null)
                {
                    return BadRequest("invalid request doc head");
                }
                if (dto.RequestLine == null)
                {
                    return BadRequest("invalid request doc lines");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // update the company id
                dto.RequestHead.SubSiId = db.COMPANYID;
                dto.RequestHead.DocDate = DateTime.Now;

                // clear the head and line from 
                var checksql = $@"Select * from {db.WEBDB}..FTAPP_RequestHead 
                                  where convert(date,  DeliveryDate) = @deliveryDt
                                        and UserCode = @userCode";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var head = conn.Query<BreadRequestHead>(checksql, new
                {
                    deliveryDt = dto.RequestHead.DeliveryDate,
                    userCode = dto.UserCode
                }).FirstOrDefault();

                if (head != null)
                {
                    var delSql = $@"Delete from {db.WEBDB}..FTAPP_RequestHead Where HeadGuid = @HeadGuid";
                    conn.Execute(delSql, new { HeadGuid = head.HeadGuid });

                    delSql = $@"Delete from {db.WEBDB}..FTAPP_RequestLine Where HeadGuid = @HeadGuid";
                    conn.Execute(delSql, new { HeadGuid = head.HeadGuid });
                    dto.RequestHead.DocDate = head.DocDate;
                }

                // insert the header
                var insertHead = $@"INSERT INTO {db.WEBDB}..FTAPP_RequestHead ( 
                                                 DeliveryDate
                                               , DocDate
                                               , UserCode
                                               , UserName                                             
                                               , HeadGuid
                                               , Remarks
                                               , SubSi
                                               , SubSiId 
                                                ) values (
                                                 @DeliveryDate
                                                ,GetDate()
                                                ,@UserCode
                                                ,@UserName
                                                ,@HeadGuid
                                                ,@Remarks
                                                ,@SubSi
                                                ,@SubSiId  )";

                conn.Execute(insertHead, dto.RequestHead);

                // insert the line 
                var insertLines = $@"INSERT INTO {db.WEBDB}..FTAPP_RequestLine (
                                         HeadGuid
                                       , LineGuid
                                       , ItemCode
                                       , ItemName
                                       , RequestQty
                                       , Remarks
                                       , LineNum
                                    ) VALUES (
                                         @HeadGuid
                                       , @LineGuid
                                       , @ItemCode
                                       , @ItemName
                                       , @RequestQty
                                       , @Remarks
                                       , @LineNum )";

                conn.Execute(insertLines, dto.RequestLine);
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadRequestLines (Dto_BreaRequest dto)
        {
            try
            {
                if (dto.HeadGuid == default)
                {
                    return BadRequest("invalid head guid");
                }
                if (dto.HeadGuid == null)
                {
                    return BadRequest("invalid head guid [null]");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"exec sp_QueryRequestLine @webDb, @guid";
                var results = new SqlConnection(_commDbConnStr_Bread).Query<BreadRequestLine>(sql, new
                {
                    webDb = db.WEBDB,
                    guid = dto.HeadGuid
                }).ToList();

                if (results.Count == 0) return NotFound();
                return Ok(results);

            }
            catch (Exception e) 
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadRequestList (Dto_BreaRequest dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"sp_QueryRequestList @webDb ,@userCode ,  @startDt, @endDt";
                var results = new SqlConnection(_commDbConnStr_Bread).Query<BreadRequestHead>(sql, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    startdt = dto.StartDt, 
                    enddt = dto.EndDt
                }).ToList();

                if (results.Count == 0) return NotFound();
                return Ok(results);
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
