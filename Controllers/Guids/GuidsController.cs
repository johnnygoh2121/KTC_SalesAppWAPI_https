using KTC_SalesAppWAPI.DTOs.Guids;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace KTC_SalesAppWAPI.Controllers.Guids
{
    [Route("[controller]")]
    [ApiController]
    public class GuidsController : ControllerBase
    {
        //readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<GuidsController> _logger;

        //string LastError { get; set; } = string.Empty;
        //string _commDbConnStr { get; set; } = string.Empty;

        public GuidsController(IConfiguration configuration, ILogger<GuidsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            //_commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(DTO_Guid dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetNewGuid":
                    {
                        return Ok(Guid.NewGuid());
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

    }
}
