using KTC_SalesAppWAPI.Models.PortalCompany;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PortalCompaniesController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PortalCompaniesController> _logger;
        string LastError { get; set; } = string.Empty;

        string WebHostAddrEndPoint { get; set; } = string.Empty;

        public PortalCompaniesController(IConfiguration configuration, ILogger<PortalCompaniesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            WebHostAddrEndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPointCompany").Value;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            try
            {
                var result = await GetStringAsync(WebHostAddrEndPoint);
                var PortalCompanies = JsonConvert.DeserializeObject<List<PortalCompany>>(result);

                return Ok(PortalCompanies);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<string> GetStringAsync(string endpoint)
        {
            try
            {
                var httpClient = new HttpClient(); // reference to the single http client
                var content = await httpClient.GetStringAsync(endpoint);

                if (string.IsNullOrWhiteSpace(content)) return string.Empty;

                return content;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return "";
            }
        }
    }
}
