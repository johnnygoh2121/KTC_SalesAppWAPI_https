using Dapper;
using KTC_SalesAppWAPI.Models.PortalCompany;
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
    public class PortalCompaniesBreadController : ControllerBase
    {
        readonly string _dbComm = "MasterConn_Bread";
        readonly IConfiguration _configuration;
        readonly ILogger<PortalCompaniesBreadController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;
        

        public PortalCompaniesBreadController(IConfiguration configuration, ILogger<PortalCompaniesBreadController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post()
        {
            try
            {
                var conn = new SqlConnection(_commDbConnStr);
                var sql = @"Select * from DbInfo with (nolock)";
                var PortalCompanies = conn.Query<PortalCompany>(sql).ToList();

                return Ok(PortalCompanies);
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
