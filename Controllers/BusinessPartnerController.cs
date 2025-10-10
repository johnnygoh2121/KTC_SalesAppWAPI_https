using KTC_SalesAppWAPI.DTOs.BusinessPartner;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.BusinessPartner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BusinessPartnerController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PaymentController> _logger;        
        string WebHostAddrEndPoint = string.Empty;        
        string LastError = string.Empty;
        string _dbConnStr = string.Empty;

        public BusinessPartnerController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;            
            _dbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult Post(Bp_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {

                case "GetStores":
                    {
                        return GetStores(dto);
                    }
                case "GetStore":
                    {
                        return GetStore(dto);
                    }
                default:
                    return BadRequest("Reques not recognised");
            }
        }

        IActionResult GetStore (Bp_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Company id is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKey))
                {
                    return BadRequest("key is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("card type empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("card code empty");
                }

                var db = new DbNameHelper().GetDbInfoById(_dbConnStr, dto.CompanyId);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                var svrAddress = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                // 20220413
                var address = $"{svrAddress}BusinessPartner/{dto.CompanyId}/{dto.CardType}/{dto.CardCode}";

                var client = new RestClient(address);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKey}");
                IRestResponse response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    return BadRequest($"request fail {response.Content}");
                }

                var bps = JsonConvert.DeserializeObject<PortalBp>(response.Content);
                if (bps != null)
                {
                    return Ok(bps);
                }
                return BadRequest($"request fail {response.Content}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetStores(Bp_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Company id is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKey))
                {
                    return BadRequest("key is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("card type empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_dbConnStr, dto.CompanyId);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // 20220413
                var svrAddress = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var address = $"{svrAddress}BusinessPartner/{dto.CompanyId}/{dto.CardType}";

                //var address = $"{WebHostAddrEndPoint}BusinessPartner/{dto.CompanyId}/{dto.CardType}";
                var client = new RestClient(address);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKey}");
                IRestResponse response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    return BadRequest($"request fail {response.Content}");
                }

                var bps = JsonConvert.DeserializeObject<List<PortalBp>>(response.Content);
                if (bps != null)
                {
                    return Ok(bps);
                }
                return BadRequest($"request fail {response.Content}");
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
