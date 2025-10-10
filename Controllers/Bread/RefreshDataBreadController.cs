using KTC_SalesAppWAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class RefreshDataBreadController : ControllerBase
    {
        readonly string _dbComm = "MasterConn_Bread";
        readonly IConfiguration _configuration;
        readonly ILogger<RefreshDataBreadController> _logger;
        string LastError { get; set; } = string.Empty;
        string CommDbConnStr { get; set; } = string.Empty;

        public RefreshDataBreadController(IConfiguration configuration, ILogger<RefreshDataBreadController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Post()
        {
            try
            {
                CommDbConnStr = _configuration.GetConnectionString(_dbComm);
                var helper = new DataRefreshHelper(_logger, CommDbConnStr);
                helper.LoadUserToFromAllSubsiDb_Bread();
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
