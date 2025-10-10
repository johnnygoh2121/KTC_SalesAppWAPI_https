using Dapper;
using KTC_SalesAppWAPI.DTOs.CreditApplication;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CreditApplication;
using KTC_SalesAppWAPI.Models.Payment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.CreditApplication
{
    [Route("[controller]")]
    [ApiController]
    public class CreditApplicationController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<CreditApplicationController> _logger;
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";
        string WebHostAddrEndPoint = "";

        public CreditApplicationController(IConfiguration configuration, ILogger<CreditApplicationController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_CreditApplication dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetTypeOfSecurity":
                    {
                        return GetTypeOfSecurity(dto);
                    }
                case "GetTypeOfFeqCall":
                    {
                        return GetTypeOfFeqCall(dto);
                    }
                case "GetCountries":
                    {
                        return GetCountries(dto);
                    }
                case "GetStates":
                    {
                        return GetStates(dto);
                    }
                case "GetTerritory":
                    {
                        return GetTerritory(dto);
                    }
                case "GetCustomerGroup":
                    {
                        return GetCustomerGroup(dto);
                    }
                case "GetCollectors":
                    {
                        return GetCollectors(dto);
                    }
                case "GetBizOperations":
                    {
                        return GetBizOperations(dto);
                    }
                case "GetLocations":
                    {
                        return GetLocations();
                    }
                case "GetSellers":
                    {
                        return GetSellers(dto);
                    }
                case "QueryCACust":
                    {
                        return QueryCACust(dto);
                    }
                case "GetBanks":
                    {
                        return GetBanks(dto);
                    }
                case "SaveCreditApp":
                    {
                        return SaveCreditApp(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult SaveCreditApp(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.SavedCust == null)
                {
                    return BadRequest("Invalid credit application.");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid update type.");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid query key");
                }


                var db0 = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db0 == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var svrAdr = !string.IsNullOrWhiteSpace(db0.PostSvrAdressPort) ? db0.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}CreditApplication/{db0.COMPANYID}/{dto.UpdateType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");

                var body = JsonConvert.SerializeObject(dto.SavedCust);

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                var replied = JsonConvert.DeserializeObject<Dto_CreAppReplied>(response.Content);
                if (response.IsSuccessful)
                {
                    return Ok(replied);
                }

                // else 
                return BadRequest(replied.ErrorMessage);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetBanks(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                var qr_sp = @$"SELECT * FROM {db.SAPDB}..ODSC WITH (NOLOCK)  
                                    WHERE ISNULL(U_INACTIVE,'') <> 'Y'";

                var banks = conn.Query<ODSC>(qr_sp).ToList();

                if (banks.Count == 0) return NotFound();

                return Ok(banks);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult QueryCACust(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
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

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                var qr_sp = @"exec sp_QueryCACust @webDb, @startDt, @endDt ";
                var list = conn.Query<CUST>(qr_sp, new
                {
                    webDb = db.WEBDB,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

                if (list.Count == 0) return NotFound();

                // load it line 
                for (int d = 0; d < list.Count; d++)
                {
                    var qr_sp_line = @"exec sp_QueryCACustLines @webDb, @docEntry";
                    list[d].Sellers = conn.Query<CUST1>(qr_sp_line, new
                    {
                        webDb = db.WEBDB,
                        docEntry = list[d].DOCENTRY
                    }).ToList();


                    var qr_sp_files = @"exec sp_QueryCACustAttachments @webDb, @docEntry";
                    list[d].Attachments = conn.Query<CUST3>(qr_sp_files, new
                    {
                        webDb = db.WEBDB,
                        docEntry = list[d].DOCENTRY
                    }).ToList();
                }

                return Ok(list);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetSellers(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetSellers @webDb ";

                using var conn = new SqlConnection(_commDbConnStr);
                var sellers = conn.Query<Seller>(sp_qr, new
                {
                    webDb = db.WEBDB
                }).ToList();

                if (sellers.Count == 0) return NotFound();
                return Ok(sellers);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetLocations()
        {
            try
            {
                var local = new Loc
                {
                    Name = "LOCAL"
                };

                var outStation = new Loc
                {
                    Name = "OUT STATION"
                };

                var list = new List<Loc>();
                list.Add(local);
                list.Add(outStation);

                return Ok(list);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetBizOperations(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetBizOperation @webDb, @userCode";

                using var conn = new SqlConnection(_commDbConnStr);
                var bizOps = conn.Query<BizOpr>(sp_qr, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode
                }).ToList();

                if (bizOps.Count == 0) return NotFound();
                return Ok(bizOps);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCollectors(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetCollectors @sapDb";
                using var conn = new SqlConnection(_commDbConnStr);
                var collectors = conn.Query<Collector>(sp_qr, new
                {
                    sapDb = db.SAPDB
                }).ToList();

                if (collectors.Count == 0) return NotFound();
                return Ok(collectors);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }

        IActionResult GetCustomerGroup(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetCustomerGroup @sapDb";
                using var conn = new SqlConnection(_commDbConnStr);
                var custGroups = conn.Query<CustGroup>(sp_qr, new
                {
                    sapDb = db.SAPDB
                }).ToList();

                if (custGroups.Count == 0) return NotFound();
                return Ok(custGroups);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTerritory(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetTerritories @sapDb";
                using var conn = new SqlConnection(_commDbConnStr);
                var territories = conn.Query<Territory>(sp_qr, new
                {
                    sapDb = db.SAPDB
                }).ToList();

                if (territories.Count == 0) return NotFound();
                return Ok(territories);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult GetStates(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetStates @sapDb, @country";
                using var conn = new SqlConnection(_commDbConnStr);
                var states = conn.Query<State>(sp_qr, new
                {
                    sapDb = db.SAPDB,
                    country = "MY"
                }).ToList();

                if (states.Count == 0) return NotFound();
                return Ok(states);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCountries(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetCountries @sapDb";
                using var conn = new SqlConnection(_commDbConnStr);
                var countries = conn.Query<Country>(sp_qr, new
                {
                    sapDb = db.SAPDB
                }).ToList();

                if (countries.Count == 0) return NotFound();
                return Ok(countries);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTypeOfSecurity(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetCreditApplicationStandardCode @webDb, @codeType";
                using var conn = new SqlConnection(_commDbConnStr);
                var secureCodes = conn.Query<SecurityCode>(sp_qr, new
                {
                    webDb = db.WEBDB,
                    codeType = "CR3"
                }).ToList();

                if (secureCodes.Count == 0) return NotFound();
                return Ok(secureCodes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTypeOfFeqCall(Dto_CreditApplication dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi, please try again later.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi, please try again later.");
                }

                var sp_qr = @"exec sp_GetCreditApplicationStandardCode @webDb, @codeType";
                using var conn = new SqlConnection(_commDbConnStr);
                var freqCalles = conn.Query<FreqCall>(sp_qr, new
                {
                    webDb = db.WEBDB,
                    codeType = "CR4"
                }).ToList();

                if (freqCalles.Count == 0) return NotFound();
                return Ok(freqCalles);
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
