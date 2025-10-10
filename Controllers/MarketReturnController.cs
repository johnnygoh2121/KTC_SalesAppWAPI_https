using Dapper;
using KTC_SalesAppWAPI.DTOs.MarketReturn;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Cdn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MarketReturnController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<MarketReturnController> _logger;
        //string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public MarketReturnController(IConfiguration configuration, ILogger<MarketReturnController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
          //  WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_MarketReturn dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetCN":
                    {
                        return GetCN(dto);
                    }
                case "SaveCn":
                    {
                        return SaveCn(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult SaveCn (Dto_MarketReturn dto)
        {
            return Ok();
        }

        // get the credit memo by doc entry
        // and it lines
        IActionResult GetCN(Dto_MarketReturn dto)
        {            
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.CnDocNum))
                {
                    return BadRequest("Invalid CN doc num");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db");
                }

                var sql = @$"SELECT * FROM [{db.SAPDB}]..ORIN with (nolock) 
                            Where docentry= @DocEntry";

                var conn = new SqlConnection(_commDbConnStr);
                var cn = conn.Query<ORIN>(sql, new { DocEntry = dto.CnDocEntry }).FirstOrDefault();
                if (cn == null) return NotFound();

                // get the cn liines 
                sql = @$"SELECT * FROM [{db.SAPDB}]..RIN1 with (nolock) 
                            Where docentry= @DocEntry";

                cn.Lines = conn.Query<RIN1>(sql, new { DocEntry = dto.CnDocEntry }).ToList();

                if (cn.Lines == null) return BadRequest("CN Line(s) empty. [NU]");
                if (cn.Lines.Count == 0) return BadRequest("CN Line(s) empty.[ZR]");

                return Ok(cn);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

    }
}
