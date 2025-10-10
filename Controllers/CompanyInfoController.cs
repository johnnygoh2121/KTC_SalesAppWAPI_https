using Dapper;
using KTC_SalesAppWAPI.DTOs.CompanyInfo;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CompanyInfo;
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
    public class CompanyInfoController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PaymentController> _logger;
        //readonly string APP_JSON = "application/json";
        //string WebHostAddrEndPoint = string.Empty;
        string CommDbConnStr = string.Empty;
        string _commDbConnStr_Bread = string.Empty;

        string LastError = string.Empty;

        public CompanyInfoController (IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            CommDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
        }

        [HttpPost]
        public IActionResult Post(CoyInfo_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {

                case "CompanyInfo":
                    {
                        return CompanyInfo(dto);
                    }
                case "CompanyInfo1":
                    {
                        return CompanyInfo1(dto);
                    }

                case "CompanyInfo_Bread":
                    {
                        return CompanyInfo_Bread(dto);
                    }
                case "CompanyInfo1_Bread1":
                    {
                        return CompanyInfo1_Bread1(dto);
                    }


                default:
                    return BadRequest("Reques not recognised");
            }
        }

        IActionResult CompanyInfo (CoyInfo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request company name is empty");
                }

                var dbinfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);

                if (dbinfo == null)
                {
                    return BadRequest("Reading company info error, please try again.");
                }

                var sql = @$"SELECT * FROM [{dbinfo.SAPDB}].[dbo].[OADM] WITH (NOLOCK) ";
                            //Where CompnyName = @CompanyName";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<OADM>(sql, new { dto.CompanyName }).FirstOrDefault();
                    if (result == null) return NotFound();

                    return Ok(result);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }

        }

        IActionResult CompanyInfo1(CoyInfo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request company name is empty");
                }

                var dbinfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);

                if (dbinfo == null)
                {
                    return BadRequest("Reading company info error, please try again.");
                }

                var sql = @$"SELECT * FROM [{dbinfo.SAPDB}].[dbo].[ADM1] WITH (NOLOCK) ";
                //Where CompnyName = @CompanyName";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<ADM1>(sql, new { dto.CompanyName }).FirstOrDefault();
                    if (result == null) return NotFound();

                    return Ok(result);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }

        }

        // for bread 
        IActionResult CompanyInfo_Bread(CoyInfo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request company name is empty");
                }

                var dbinfo = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);

                if (dbinfo == null)
                {
                    return BadRequest("Reading company info error, please try again.");
                }

                var sql = @$"SELECT * FROM [{dbinfo.SAPDB}].[dbo].[OADM] WITH (NOLOCK) ";
                //Where CompnyName = @CompanyName";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<OADM>(sql, new { dto.CompanyName }).FirstOrDefault();
                    if (result == null) return NotFound();

                    return Ok(result);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }

        }

        IActionResult CompanyInfo1_Bread1(CoyInfo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request company name is empty");
                }

                var dbinfo = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);

                if (dbinfo == null)
                {
                    return BadRequest("Reading company info error, please try again.");
                }

                var sql = @$"SELECT * FROM [{dbinfo.SAPDB}].[dbo].[ADM1] WITH (NOLOCK) ";
                //Where CompnyName = @CompanyName";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<ADM1>(sql, new { dto.CompanyName }).FirstOrDefault();
                    if (result == null) return NotFound();

                    return Ok(result);
                }
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
