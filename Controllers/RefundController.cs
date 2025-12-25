using Dapper;
using KTC_SalesAppWAPI.Controllers.IBT;
using KTC_SalesAppWAPI.DTOs.IBTReceipt;
using KTC_SalesAppWAPI.DTOs.Refunds;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Refund;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RefundController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<RefundController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";
        string _webHostAddrEndPoint = "";

        public RefundController(IConfiguration configuration, ILogger<RefundController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
            _webHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;

        }
        [HttpPost]
        public IActionResult PostAsync(DTO_Refund dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetRefunds": // base RIB 
                    {
                        return GetRefunds(dto);
                    }

                default:
                    {
                        return BadRequest("no recognized request");
                    }
            }
        }

        IActionResult GetRefunds(DTO_Refund dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid SUBSI");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid End Date");
                }

                var db = new DbNameHelper().GetDbInfo( _commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid SUBSI, not found");
                }

                var sp_Query = $@"Select '{db.COMPANYID}' [SubSiId],
                                         '{db.COMPANYNAME}' [SubSiName],
                                         t0.* 

                                  from {db.WEBDB}..REFUND t0 with (NOLOCK) 
                                  where  t0.DOCDATE >= @startDt
                                  and    t0.DOCDATE <= @endDt 
                                  and    t0.UCREATED = @userCode";

                var conn = new SqlConnection(_commDbConnStr);
                var docs = conn.Query<Refund>(sp_Query, new
                {
                    startDt = dto.StartDt,
                    endDt = dto.EndDt,
                    userCode = dto.UserCode
                }).ToList();

                return NotFound();
            }
            catch (Exception except)
            {
                LastError = $"{except.Message}\n{except.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}
