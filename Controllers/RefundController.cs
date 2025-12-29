using Dapper;
using KTC_SalesAppWAPI.DTOs.Refunds;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Refund;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
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
        string WebHostAddrEndPoint = "";

        public RefundController(IConfiguration configuration, ILogger<RefundController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
            _webHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;

            
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;

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
                case "PostRefund":
                    {
                        return PostRefund(dto);
                    }
                default:
                    {
                        return BadRequest("no recognized request");
                    }
            }
        }

        IActionResult PostRefund (DTO_Refund dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid SUBSI");
                }
                if (dto.RefundDoc == null)
                {
                    return BadRequest("Invalid refund doc.");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid logon pass key");
                }
                if (string.IsNullOrWhiteSpace(dto.RequestName))
                {
                    return BadRequest("Invalid request name");
                }
                if (string.IsNullOrWhiteSpace(dto.DocUpdateType))
                {
                    return BadRequest("Invalid update type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid SUBSI name");
                }

                // post with redsharp
                //var client = new RestClient($"{WebHostAddrEndPoint}{dto.RequestName}/{dto.CompanyId}/{dto.DocUpdateType}");
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}{dto.RequestName}/{db.COMPANYID}/{dto.DocUpdateType}");

                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.RefundDoc);

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content = response.Content;
                    var result = JsonConvert.DeserializeObject<RefundResult>(content);

                    // need to save the file 
                    return Ok(result);
                }
                else
                {
                    var content = response.Content;
                    var result = JsonConvert.DeserializeObject<RefundResult>(content);

                    return BadRequest(result);
                }                
            }
            catch (Exception except)
            {
                LastError = $"{except.Message}\n{except.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
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
                                   and   t0.DOCDATE <= @endDt 
                                   and   t0.UCREATED = @userCode
                                  order by t0.DocEntry desc ";

                var conn = new SqlConnection(_commDbConnStr);
                var docs = conn.Query<Refund>(sp_Query, new
                {
                    startDt = dto.StartDt,
                    endDt = dto.EndDt,
                    userCode = dto.UserCode
                }).ToList();

                if (docs.Count == 0)
                    return NotFound();

                // load each refund 1 and refund 2 
                for (int i = 0; i < docs.Count; i++)
                {
                    var refund = docs[i];
                    if (refund == null) continue;

                    var sp_load_ref1 = @$"select * from {db.WEBDB}..REFUND1 with (NOLOCK) where DocEntry = @DocEntry";

                    docs[i].Cheques = conn.Query<Refund1>(sp_load_ref1, new { refund.DocEntry }).ToList();

                    var sp_load_ref2 = @$"select * from {db.WEBDB}..REFUND2 with (NOLOCK) where DocEntry = @DocEntry";
                    docs[i].Documents = conn.Query<Refund2>(sp_load_ref2, new { refund.DocEntry }).ToList();

                    var sp_load_ref3 = @$"select * from {db.WEBDB}..REFUND3 with (NOLOCK) where DocEntry = @DocEntry";
                    docs[i].Attachments = conn.Query<Refund3>(sp_load_ref3, new { refund.DocEntry }).ToList();
                }

                return Ok(docs);

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
