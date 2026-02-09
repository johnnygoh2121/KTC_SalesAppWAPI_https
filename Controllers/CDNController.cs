using Dapper;
using KTC_SalesAppWAPI.DTOs;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CDNController : ControllerBase
    {
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<PaymentController> _logger;
        string CommDbConnStr = string.Empty;
        string LastError = string.Empty;
        string WebHostAddrEndPoint = "";

        public CDNController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            CommDbConnStr = _configuration.GetConnectionString("MasterConn");
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult Post(Cdn_Dto dto)
        {
            switch (dto.Request)
            {
                case "ArCnWithChargeCodeListing":
                    {
                        return ArCnWithChargeCodeListing(dto);
                    }
                case "GetAgencyCdnType":
                    {
                        return GetAgencyCdnType(dto);
                    }
                case "GetPAF":
                    {
                        return GetPAF(dto);
                    }
                case "GetPAFwChargeCode":
                    {
                        return GetPAFwChargeCode(dto);
                    }
                case "GetPafAgency":
                    {
                        return GetPafAgency(dto);
                    }
                case "GetAgencyBrand":
                    {
                        return GetAgencyBrand(dto);
                    }
                case "Paf":
                    {
                        return Paf1(dto);
                    }
                case "GetCDNTermsConditions":
                    {
                        return GetCDNTermsConditions();
                    }
                case "GetGondola": // gondola
                    {
                        return GetGondola(dto);
                    }
                case "Gondola":
                    {
                        return Gondola1(dto);
                    }
                case "CreditMemo": // use to query the customer crdit memo
                    {
                        return CreditMemo(dto);
                    }
                case "CreditMemo_Aging":
                    {
                        return CreditMemo_Aging(dto);
                    }
                case "Invoice_Aging":
                    {
                        return Invoice_Aging(dto);
                    }
                case "Invoice_AgingV2": // for packing module to print invoice 
                    {
                        // for packing module to print invoice 
                        // as the user display name should be seller, not the packer
                        // 20210911
                        return Invoice_AgingV2(dto);
                    }
                case "GetChargeCode":
                    {
                        return GetChargeCode(dto);
                    }
                case "LoadPafChargeCodeLine":
                    {
                        return LoadPafChargeCodeLine(dto);
                    }
                case "LoadDgpCatergories": // 20241218
                    {
                        return LoadDgpCatergories(dto);
                    }
                case "UpdateCDNPaffiles":
                    {
                        return UpdateCDNPaffiles(dto);
                    }
                case "UpdateGONfiles":
                    {
                        return UpdateGONfiles(dto);
                    }
                case "GetBrandAndSku":
                    {
                        return GetBrandAndSku(dto);
                    }
                case "GetUserLastDocEntry":
                    {
                        return GetUserLastDocEntry(dto);
                    }
                case "GetUserBudget":
                    {
                        return GetUserBudget(dto);
                    }
                case "UpdatePAFFiles":
                    {
                        return UpdatePAFFiles(dto);
                    }
                default:
                    return BadRequest("Reques not recognised");
            }
        }

        IActionResult UpdatePAFFiles(Cdn_Dto dto)
        {
            if (dto.PafDocEntry <= 0)
            {
                return BadRequest("Invalid PAF doc entry.");
            }

            var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("invalid db info");
            }

            using var conn = new SqlConnection(CommDbConnStr);

            var sp_query = $"Select * from  {db.WEBDB}..PAF " +
                   $"Where DocEntry = @docEntry  ";

            var found = conn.Query<PAF>(sp_query, new { docEntry = dto.PafDocEntry }).FirstOrDefault();
            if (found == null)
            {
                return BadRequest($"PAF record not found , docentry {dto.PafDocEntry}");
            }

            // paf files
            //if (!string.IsNullOrWhiteSpace(found.PAFFILE))
            //{
            //    found.PAFFILE += "," + dto.PafFiles1;
            //}
            //else
            //{
            //    found.PAFFILE = dto.PafFiles1;
            //}

            //// ref files
            //if (!string.IsNullOrWhiteSpace(found.REFFILE))
            //{
            //    found.REFFILE += "," + dto.RefFiles1;
            //}
            //else
            //{
            //    found.REFFILE = dto.RefFiles1;
            //}


            // 20240922
            // over write the file  
            if (!string.IsNullOrWhiteSpace(dto.PafFiles1))
            {
                found.PAFFILE = dto.PafFiles1;
            }
            if (!string.IsNullOrWhiteSpace(dto.RefFiles1))
            {
                found.REFFILE = dto.RefFiles1;
            }

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var updateSql = @$"Update {db.WEBDB}..PAF Set PAFFILE = @PafFiles , 
                                    REFFILE = @RefFiles Where DocEntry = @docEntry ";
                var res = conn.Execute(updateSql, new
                {
                    PafFiles = found.PAFFILE,
                    RefFiles = found.REFFILE,
                    docEntry = found.DOCENTRY
                }, trans);

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ArCnWithChargeCodeListing(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid company name");
                }
                if (dto.StartDate == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDate == default)
                {
                    return BadRequest("Invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var conn = new SqlConnection(CommDbConnStr);
                var sp = @"exec sp_ArCnWithChargeCodeListing @webDb , @startDt, @endDt";
                var res = conn.Query<ArCnCdnChargeCodeListing>(sp, new
                {
                    webDb = db.WEBDB,
                    startDt = dto.StartDate,
                    endDt = dto.EndDate
                }).ToList();

                if (res.Count == 0) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetUserBudget(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("UserCode is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("UserCode is empty");
                }
                var sql = $@"Exec sp_GetUserPafBudget @wedDb, @userCode";

                var conn = new SqlConnection(CommDbConnStr);
                var budget = conn.Query<CdnBudget>(sql, new { wedDb = db.WEBDB, userCode = dto.UserCode }).FirstOrDefault();
                if (budget == null) return NotFound();

                return Ok(budget);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetUserLastDocEntry(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("UserCode is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.TableName))
                {
                    return BadRequest("Table name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("UserCode is empty");
                }
                var sql = $@"select top 1 DOCENTRY from {db.WEBDB}..{dto.TableName} t0
                               Where t0.UCREATED = @UserCode 
                               order by t0.DOCENTRY desc";

                var conn = new SqlConnection(CommDbConnStr);
                var lastDocEntry = conn.ExecuteScalar<string>(sql, new { UserCode = dto.UserCode });
                return Ok(lastDocEntry);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetBrandAndSku(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                var sp_query = @"exec Sp_GetBrandAndSku @webDb";

                var result = new SqlConnection(CommDbConnStr).Query<BrandAndSku>(sp_query, new
                {
                    webDb = db.WEBDB
                });

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateGONfiles(Cdn_Dto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.GonDocEntry < 0)
            {
                return BadRequest("Invalid doc entry");
            }
            if (string.IsNullOrWhiteSpace(dto.GonFiles))
            {
                return BadRequest("Invalid files names");
            }
            var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("Invalid company name, db info is empty");
            }

            using var conn = new SqlConnection(CommDbConnStr);

            try
            {
                var sql = @$"UPDATE [{db.WEBDB}].[dbo].[GON] 
                                SET GONFILE = @GonFiles
                                WHERE DocEntry = @GonDocEntry";

                var result = conn.ExecuteScalar<int>(sql, new { dto.GonDocEntry, dto.GonFiles });
                return Ok();
            }
            catch (Exception e)
            {

                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateCDNPaffiles(Cdn_Dto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.PafDocEntry < 0)
            {
                return BadRequest("Invalid doc entry");
            }
            if (string.IsNullOrWhiteSpace(dto.SIGNFILE))
            {
                return BadRequest("Invalid files names");
            }
            var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("Invalid company name, db info is empty");
            }
            // query to get the prevous file, and update will later
            // 2021-07-18 add and update file from app 
            // check the exiting append in the file name behind
            var sqlpafFiles = $@"SELECT SIGNFILE
                                FROM [{db.WEBDB}].[dbo].[PAF] with (nolock)  
                                WHERE DocEntry = @PafDocEntry";

            using var conn = new SqlConnection(CommDbConnStr);
            var existingFiles = conn.ExecuteScalar<string>(sqlpafFiles, new { PafDocEntry = dto.PafDocEntry });
            if (!string.IsNullOrWhiteSpace(existingFiles))
            {
                existingFiles += "," + dto.SIGNFILE;
            }
            else
            {
                existingFiles = dto.SIGNFILE;
            }

            try
            {
                var sql = @$"UPDATE [{db.WEBDB}].[dbo].[PAF] 
                                SET SIGNFILE = @SIGNFILE
                                WHERE DocEntry = @PafDocEntry";

                var result = conn.ExecuteScalar<int>(sql, new { dto.PafDocEntry, SIGNFILE = existingFiles });

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadPafChargeCodeLine(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("company name is empty");
                }

                if (dto.PafDocEntry < 0)
                {
                    return BadRequest("request doc entry invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("company name provided invalid");
                }

                var sql = "exec sp_SelectPafChargeCodeLine @webDb, @docEntry";
                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<PAF1>(sql, new { webDb = db.WEBDB, docEntry = dto.PafDocEntry }).ToList();
                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadDgpCatergories(Cdn_Dto dto)
        {
            try
            {
                //if (string.IsNullOrWhiteSpace(dto.CompanyName))
                //{
                //    return BadRequest("company name is empty");
                //}

                //if (dto.PafDocEntry < 0)
                //{
                //    return BadRequest("request doc entry invalid");
                //}

                //var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                //if (db == null)
                //{
                //    return BadRequest("company name provided invalid");
                //}

                //var sql = "exec sp_SelectPafChargeCodeLine @webDb, @docEntry";
                //using var conn = new SqlConnection(CommDbConnStr);
                //var results = conn.Query<PAF1>(sql, new { webDb = db.WEBDB, docEntry = dto.PafDocEntry }).ToList();
                //if (results == null) return NotFound();
                //if (results.Count == 0) return NotFound();

                //return Ok(results);
                var testList = new List<string>() { "AIRCARE", "HAIRCARE", "SKINCARE", "ORALCARE" };

                return Ok(testList);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetChargeCode(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("request agency code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.StoreCard))
                {
                    return BadRequest("request agency code is empty");
                }
                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest($"Company {dto.CompanyName} not found");
                }

                var sql = "exec sp_SelectChargeCodePaf @webDb, @sapDb, @storeCard, @agencyCard, @subsi";

                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<Charge_Code>(sql, new
                {
                    webDb = dbInfo.WEBDB,
                    sapDb = dbInfo.SAPDB,
                    storeCard = dto.StoreCard,
                    agencyCard = dto.AgencyCode,
                    subsi = dto.CompanyName
                }).ToList();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreditMemo(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CNNumber <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                if (dto.PafDocEntry <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                //var sql = @"exec sp_SelecCreditMemo @sapDb, @docNum";"
                var sql = @"exec sp_SelecCreditMemoV1 @sapDb, @docEntry";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<ORIN>(sql, new
                {
                    sapDb = db.SAPDB,
                    docEntry = dto.PafDocEntry
                }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = "exec sp_SelecCreditMemoLine @sapDb, @docEntry";

                result.Lines = conn.Query<RIN1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 14 // credit memo line
                    }).ToList();
                }

                // get approvale 
                sql = "exec sp_SelectCreditMemo_Approval @webDb, @docEntry";
                result.Approval = conn.Query<PafApproval>(sql, new { webDb = db.WEBDB, docEntry = dto.PafDocEntry }).FirstOrDefault();

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Invoice_Aging(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CNNumber <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                var sql = @"exec sp_SelectInvoice_Aging @sapDb, @DocNum";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<OINV>(sql, new { sapDb = db.SAPDB, DocNum = dto.CNNumber }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = "exec sp_SelectInvoiceLine @sapDb, @docEntry";
                result.Lines = conn.Query<INV1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 13 // credit memo line
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for cater the packer print invoice request
        // read the user display name as seller from the portal and sap setup. 
        // no from the app login id
        IActionResult Invoice_AgingV2(Cdn_Dto dto) // for packing invoice printing 
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CNNumber <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                //var sql = @"exec sp_SelectInvoice_Aging @sapDb, @DocNum";

                var sql = @"exec sp_SelectInvoice_AgingV2 @webDb,  @sapDb, @docNum";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<OINV>(sql, new
                {
                    webDb = db.WEBDB,
                    sapDb = db.SAPDB,
                    DocNum = dto.CNNumber
                }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = "exec sp_SelectInvoiceLine @sapDb, @docEntry";
                result.Lines = conn.Query<INV1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load line batch is any 
                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 13 // invoice type
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreditMemo_Aging(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CNNumber <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                var sql = @"exec [sp_SelecCreditMemo] @sapDb, @DocNum";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<ORIN>(sql, new { sapDb = db.SAPDB, DocNum = dto.CNNumber }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = "exec sp_SelecCreditMemoLine @sapDb, @docEntry";
                result.Lines = conn.Query<RIN1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 14 // credit memo line
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        ///  gondola rental posting to web portal
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult Gondola(Cdn_Dto dto)
        {
            try
            {
                if (dto.Gon == null)
                {
                    return BadRequest("GON doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid GON doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid GON Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid GON Doc company id");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid db name");
                }

                // 20220413
                var svrAddress = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // post with rest client 
                // 20220409
                var client = new RestClient($"{svrAddress}{dto.Request}/{dto.QueryCompanyID}/{dto.UpdateType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.Gon);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content0 = response.Content; // await response.Content.ReadAsStringAsync();
                    var result0 = JsonConvert.DeserializeObject<GonResult>(content0);
                    result0.updateDocType = dto.UpdateType;
                    result0.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result0.actionResult;
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                    }

                    // 20211029 
                    // for PAF Cn created checking 
                    // check created CN number 
                    if (dto.UpdateType == "Submit")
                    {
                        var cnDOc = GetCNDoc(dto.QueryCompanyID, int.Parse(result0.actionResult), "GON");
                        if (cnDOc != null)
                        {
                            result0.CnDocNum = cnDOc.DocNum;
                            result0.CnDocEntry = cnDOc.DocEntry;
                        }
                    }

                    return Ok(result0);
                }

                var content = response.Content; // await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                if (result == null)
                {
                    return BadRequest("Error when posting to web portal");
                }

                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = result.errorMessage;
                    new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                }

                return BadRequest($"{result.errorMessage}\n{result.actionResult}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        ///  gondola rental posting to web portal
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult Gondola1(Cdn_Dto dto)
        {
            try
            {
                if (dto.Gon == null)
                {
                    return BadRequest("GON doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid GON doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid GON Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid GON Doc company id");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid db name");
                }

                // 20220209
                // double the last doc entry
                // 20220208
                // check the last doc before post 
                if (!string.IsNullOrWhiteSpace(dto.LastDocEntry))
                {
                    var sql = $@"select top 1 DOCENTRY from {db.WEBDB}..GON t0
                                Where UCREATED = @UserCode 
                                order by t0.DOCENTRY desc";

                    var conn = new SqlConnection(CommDbConnStr);
                    var lastDocEntry = conn.ExecuteScalar<string>(sql, new { UserCode = dto.UserCode });

                    if (!$"{lastDocEntry}".Equals($"{dto.LastDocEntry}"))
                    {
                        return BadRequest(" Please close this screen " +
                            "and refresh the list to check, does the doc created, while server was busy.");
                    }
                }

                //20220413
                var svrAddrs = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // post with rest client 
                // 20220409                
                // always save draft
                var client = new RestClient($"{svrAddrs}{dto.Request}/{dto.QueryCompanyID}/draft");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.Gon);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content0 = response.Content; // await response.Content.ReadAsStringAsync();
                    var result0 = JsonConvert.DeserializeObject<GonResult>(content0);
                    result0.updateDocType = dto.UpdateType;
                    result0.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result0.actionResult;
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                    }

                    // 20211029 
                    // for PAF Cn created checking 
                    // check created CN number 
                    if (dto.UpdateType == "Submit") // if 
                    {
                        var actionResult = result0.actionResult; // doc entry for draft
                        dto.Gon.DOCENTRY = int.Parse(actionResult);
                        return Gondola(dto); // post as submit
                    }

                    return Ok(result0);
                }

                var content = response.Content; // await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                if (result == null)
                {
                    return BadRequest("Error when posting to web portal");
                }

                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = result.errorMessage;
                    new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                }

                return BadRequest($"{result.errorMessage}\n{result.actionResult}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCDNTermsConditions()
        {
            try
            {
                var sql = @"SELECT * FROM FTAPP_CDNTandCView WITH (NOLOCK) ";
                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<FTAPP_PrintOutConfig>(sql).FirstOrDefault();
                    if (result == null) return NotFound();
                    return Ok(result.SectionContent);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetGondola(Cdn_Dto dto)
        {
            try
            {
                if (dto.StartDate.Equals(default))
                {
                    return BadRequest("request start date invalid");
                }
                if (dto.EndDate.Equals(default))
                {
                    return BadRequest("request end date invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("request card code is empty");
                }
                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("request company name invalid");
                }
                /*@webDb nvarchar(100), 
                @sapDb nvarchar(100),
                @companyName nvarchar(120), 
                @cardCode nvarchar(120),
                @agencyCode nvarchar(120), 
                @startDt datetime, 
                @endDt datetime*/
                var sp_query = "exec sp_SelectGondola @webDb, @sapDb, @companyName, @cardCode, @startDt, @endDt";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<Gondola>(sp_query,
                        new
                        {
                            webDb = dbInfo.WEBDB,
                            sapDb = dbInfo.SAPDB,
                            companyName = dbInfo.COMPANYNAME,
                            cardCode = dto.CardCode,
                            startDt = $"{dto.StartDate:yyyyMMdd}",
                            endDt = $"{dto.EndDate:yyyyMMdd}"
                        }).ToList();

                    if (result == null) return NotFound();
                    if (result.Count == 0) return NotFound();

                    // 20210625
                    // filter by request user agency 
                    // get list of the user agency 
                    if (!string.IsNullOrWhiteSpace(dto.UserCode))
                    {
                        // get list of the user code belong agency 
                        var sql_usrAgency = $@"select distinct CARDCODE 
                                            from [{dbInfo.WEBDB}].dbo.USERCARD with (nolock)
                                            where UserCode = @UserCode 
                                            and CARDTYPE = 'S' ";

                        var agencyCodes = conn.Query<string>(sql_usrAgency, new { dto.UserCode }).ToList();
                        if (agencyCodes?.Count == 0) return Ok(result);

                        var newList = new List<Gondola>();
                        agencyCodes.ForEach(x =>
                        {
                            var docs = result.Where(d => d.AGENCY.Equals(x)).ToList();
                            if (docs != null && docs.Count > 0)
                            {
                                newList.AddRange(docs);
                            }
                        });

                        return Ok(newList);
                    }

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


        IActionResult Paf1(Cdn_Dto dto)
        {
            try
            {
                if (dto.Paf == null)
                {
                    return BadRequest("PAF doc is null");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid PAF doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid PAF Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid PAF Doc company id");
                }

                var db = new DbNameHelper().GetDbInfoById(CommDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid PAF Doc company info [NULL]");
                }

                // prevent doc zero
                // 2021 07 28 
                if (dto.Paf.CHARGECODES != null && dto.Paf.CHARGECODES.Count > 0)
                {
                    dto.Paf.AMOUNT = dto.Paf.CHARGECODES.Sum(l => l.AMOUNT);   // recalculate the line amount
                                                                               // 
                                                                               // 20250207
                    for (int c = 0; c < dto.Paf.CHARGECODES.Count; c++)
                    {
                        var ccode = dto.Paf.CHARGECODES[c];
                        if (ccode == null) continue;
                        if (ccode.FROMDATE == default(DateTime))
                        {
                            dto.Paf.CHARGECODES[c].FROMDATE = null;
                        }

                        if (ccode.TODATE == default(DateTime))
                        {
                            dto.Paf.CHARGECODES[c].TODATE = null;
                        }
                    }
                }

                //20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // post with rest client 
                // 20220409
                var client = new RestClient($"{svrAdr}{dto.Request}/{dto.QueryCompanyID}/draft");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.Paf);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                // if post draft success
                if (response.IsSuccessful)
                {
                    var result0 = JsonConvert.DeserializeObject<PAFResult>(response.Content);
                    result0.updateDocType = dto.UpdateType;
                    result0.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result0.actionResult;
                        dto.Line.Details = $"Success, Draft";
                        new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                    }

                    // 20211029 
                    // for PAF Cn created checking 
                    // check created CN number 
                    if (dto.UpdateType == "Submit")
                    {
                        var actionResult = result0.actionResult; // doc entry for draft
                        dto.Paf.DOCENTRY = int.Parse(actionResult);
                        return Paf(dto);
                    }

                    UpdateDocStatusDToA(db); // 20220226
                    return Ok(result0);
                }

                // if fail
                var content = response.Content; //await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                if (result == null)
                {
                    return BadRequest("Error when posting to web portal");
                }

                // add in the post success log 
                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = result.errorMessage;
                    new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                }

                // update all submited doc 
                // 20220225              
                UpdateDocStatusDToA(db); // 20220226
                return BadRequest($"{result.errorMessage}\n{result.actionResult}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //private bool IsValidJson(string strInput)
        //{
        //    if (string.IsNullOrWhiteSpace(strInput)) return false;
        //    strInput = strInput.Trim();

        //    if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
        //        (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
        //    {
        //        try
        //        {
        //            var obj = JToken.Parse(strInput);
        //            return true;
        //        }
        //        catch (JsonReaderException jex)
        //        {
        //            LastError = $"{jex.Message}\n{jex.StackTrace}";
        //            _logger.LogError(LastError);
        //            return false;
        //        }
        //        catch (Exception ex) //some other exception
        //        {
        //            LastError = $"{ex.Message}\n{ex.StackTrace}";
        //            _logger.LogError(LastError);
        //            return false;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Post save the paf as draft or submit 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult Paf(Cdn_Dto dto)
        {
            try
            {
                if (dto.Paf == null)
                {
                    return BadRequest("PAF doc is null");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid PAF Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid PAF Doc company id");
                }

                if (dto.Paf.DOCENTRY == 0)
                {
                    return BadRequest("Invalid doc entry, entry can not be zero");
                }

                var db = new DbNameHelper().GetDbInfoById(CommDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid PAF Doc company info [NULL]");
                }

                // prevent doc zero
                // 2021 07 28 
                if (dto.Paf.CHARGECODES != null && dto.Paf.CHARGECODES.Count > 0)
                {
                    dto.Paf.AMOUNT = dto.Paf.CHARGECODES.Sum(l => l.AMOUNT);   // recalculate the line amount                 
                }

                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // post with rest client 
                var client = new RestClient($"{svrAdr}{dto.Request}/{dto.QueryCompanyID}/budget");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.Paf);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content0 = response.Content; //await response.Content.ReadAsStringAsync();
                    var result0 = JsonConvert.DeserializeObject<PAFResult>(content0);
                    result0.updateDocType = dto.UpdateType;
                    result0.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result0.actionResult;
                        dto.Line.Details = "Success, Budget";
                        new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                    }

                    // 20211029 
                    // for PAF Cn created checking 
                    // check created CN number 
                    if (dto.UpdateType == "Submit")
                    {
                        var cnDOc = GetCNDoc(dto.QueryCompanyID, int.Parse(result0.actionResult), "PAF");
                        if (cnDOc != null)
                        {
                            result0.CnDocNum = cnDOc.DocNum;
                            result0.CnDocEntry = cnDOc.DocEntry;
                        }
                    }

                    UpdateDocStatusDToA(db); // 20220226

                    // set the token to false
                    return Ok(result0);
                }

                var content = response.Content; // await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                if (result == null)
                {
                    return BadRequest("Error when posting to web portal");
                }

                // add in the post success log 
                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = result.errorMessage;
                    new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                }

                // update all submited doc 
                // 20220225                   
                UpdateDocStatusDToA(db); // 20220226
                return BadRequest($"{result.errorMessage}\n{result.actionResult}");
            }
            catch (Exception e)
            {

                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void UpdateDocStatusDToA(DbInfo db)
        {
            using var conn = new SqlConnection(CommDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            try
            {

                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandText = @$"Update {db.WEBDB}..PAF 
                                   Set DocStatus = 'A' 
                                   Where DOCSTATUS = 'D' and CNNO is not null";
                cmd.ExecuteNonQuery();
                trans.Commit();

                // update all submited doc 
                // 20220225
                //var res = conn.Execute(sqlupdate, trans);
                //trans.Commit();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        ORIN GetCNDoc(string companyId, int docEntry, string tableName)
        {
            try
            {
                var db = new DbNameHelper().GetDbInfoById(CommDbConnStr, companyId);

                var sql = $@"Select * from {db.WEBDB}..{tableName} with (nolock) 
                            Where docentry = @docentry";

                var conn = new SqlConnection(CommDbConnStr);
                var doc = conn.Query<PAF>(sql, new { docentry = docEntry }).FirstOrDefault();
                if (doc == null) return null;

                sql = $@"Select * from {db.SAPDB}..ORIN with (nolock) 
                        WHERE DocNum = @DocNum 
                        AND CardCode = @CardCode ";

                var cnDoc = conn.Query<ORIN>(sql, new
                {
                    DocNum = doc.CNNO,
                    CardCode = doc.CARDCODE
                }).FirstOrDefault();

                if (cnDoc == null) return null;

                if (cnDoc.U_CSUS_PAF == doc.DOCENTRY)
                {
                    return cnDoc; // verified CN created
                }
                return null;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult GetPafAgency(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("request company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.ValidFor))
                {
                    return BadRequest("request company name is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("Requested company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    var sql = " Exec sp_SelectCdnAgency @CompanyName,@SapDb,@WebDb, @CardType, @ValidFor";
                    using var conn = new SqlConnection(CommDbConnStr);
                    var result = conn.Query<OCRD_Ext>(sql,
                        new
                        {
                            CompanyName = dto.CompanyName,
                            SapDb = dbInfo.SAPDB,
                            WebDb = dbInfo.WEBDB,
                            dto.CardType,
                            dto.ValidFor
                        }).ToList();

                    if (result == null) return NotFound();
                    if (result.Count == 0) return NotFound();
                    return Ok(result);
                }

                // else query with user code
                var sql1 = " Exec sp_SelectCdnAgency2 @CompanyName,@SapDb,@WebDb, @CardType, @ValidFor, @UserCode";
                using var conn1 = new SqlConnection(CommDbConnStr);
                var result1 = conn1.Query<OCRD_Ext>(sql1,
                    new
                    {
                        CompanyName = dto.CompanyName,
                        SapDb = dbInfo.SAPDB,
                        WebDb = dbInfo.WEBDB,
                        dto.CardType,
                        dto.ValidFor,
                        dto.UserCode
                    }).ToList();

                if (result1 == null) return NotFound();
                if (result1.Count == 0) return NotFound();
                return Ok(result1);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetPAF(Cdn_Dto dto)
        {
            try
            {
                if (dto.StartDate.Equals(default))
                {
                    return BadRequest("request start date invalid");
                }
                if (dto.EndDate.Equals(default))
                {
                    return BadRequest("request end date invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("request card code is empty");
                }
                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("request company name invalid");
                }
                /*@webDb nvarchar(100), 
                @sapDb nvarchar(100),
                @companyName nvarchar(120), 
                @cardCode nvarchar(120),
                @agencyCode nvarchar(120), 
                @startDt datetime, 
                @endDt datetime*/
                var sp_query = "exec sp_SelectPaf @webDb, @sapDb, @companyName, @cardCode, @startDt, @endDt";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<PAF>(sp_query,
                        new
                        {
                            webDb = dbInfo.WEBDB,
                            sapDb = dbInfo.SAPDB,
                            companyName = dbInfo.COMPANYNAME,
                            cardCode = dto.CardCode,
                            startDt = $"{dto.StartDate:yyyyMMdd}",
                            endDt = $"{dto.EndDate:yyyyMMdd}"
                        }).ToList();

                    if (result == null) return NotFound();
                    if (result.Count == 0) return NotFound();

                    // filter by request user agency 
                    // get list of the user agency 
                    if (!string.IsNullOrWhiteSpace(dto.UserCode))
                    {
                        // get list of the user code belong agency 
                        var sql_usrAgency = $@"select distinct CARDCODE 
                                            from [{dbInfo.WEBDB}].dbo.USERCARD with (nolock)
                                            where UserCode = @UserCode 
                                            and CARDTYPE = 'S' ";

                        var agencyCodes = conn.Query<string>(sql_usrAgency, new { dto.UserCode }).ToList();
                        if (agencyCodes?.Count == 0) return Ok(result);

                        var newList = new List<PAF>();
                        agencyCodes.ForEach(x =>
                             {
                                 var docs = result.Where(d => d.AGENCY.Equals(x)).ToList();
                                 if (docs != null && docs.Count > 0)
                                 {
                                     newList.AddRange(docs);
                                 }
                             });

                        return Ok(newList);
                    }

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

        IActionResult GetPAFwChargeCode(Cdn_Dto dto)
        {
            try
            {
                if (dto.StartDate.Equals(default))
                {
                    return BadRequest("request start date invalid");
                }
                if (dto.EndDate.Equals(default))
                {
                    return BadRequest("request end date invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("request card code is empty");
                }
                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("request company name invalid");
                }
                /*@webDb nvarchar(100), 
                @sapDb nvarchar(100),
                @companyName nvarchar(120), 
                @cardCode nvarchar(120),
                @agencyCode nvarchar(120), 
                @startDt datetime, 
                @endDt datetime*/
                var sp_query = "exec sp_SelectPafwChargeCode @webDb, @sapDb, @companyName, @cardCode, @startDt, @endDt";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<PAF>(sp_query,
                        new
                        {
                            webDb = dbInfo.WEBDB,
                            sapDb = dbInfo.SAPDB,
                            companyName = dbInfo.COMPANYNAME,
                            cardCode = dto.CardCode,
                            startDt = $"{dto.StartDate:yyyyMMdd}",
                            endDt = $"{dto.EndDate:yyyyMMdd}"
                        }).ToList();

                    if (result == null) return NotFound();
                    if (result.Count == 0) return NotFound();

                    // massage the doc amount 

                    var conn1 = new SqlConnection(CommDbConnStr);
                    result.ForEach(r =>
                      {
                          if (r.AMOUNT == 0)
                          {
                              var sql_amt = @$"select sum(amount) [amt] 
                                                from [{dbInfo.WEBDB}]..PAF1 with (nolock) 
                                                Where  DOCENTRY = @DOCENTRY";
                              var r_amt = conn1.ExecuteScalar<decimal>(sql_amt, new { r.DOCENTRY });
                              if (r_amt > 0)
                              {
                                  r.AMOUNT = r_amt;
                              }
                          }

                          // load the charge code line 
                          var line_sp = "exec sp_SelectPafChargeCodeLine @webDb, @docEntry ";
                          r.CHARGECODES = conn.Query<PAF1>(line_sp,
                              new { webDb = dbInfo.WEBDB, docEntry = r.DOCENTRY }).ToList();

                      });


                    // 20210625
                    // filter by request user agency 
                    // get list of the user agency 
                    if (!string.IsNullOrWhiteSpace(dto.UserCode))
                    {
                        // get list of the user code belong agency 
                        var sql_usrAgency = $@"select distinct CARDCODE 
                                            from [{dbInfo.WEBDB}].dbo.USERCARD with (nolock)
                                            where UserCode = @UserCode 
                                            and CARDTYPE = 'S' ";

                        var agencyCodes = conn.Query<string>(sql_usrAgency, new { dto.UserCode }).ToList();
                        if (agencyCodes?.Count == 0) return Ok(result);

                        var newList = new List<PAF>();
                        agencyCodes.ForEach(x =>
                        {
                            var docs = result.Where(d => d.AGENCY.Equals(x)).ToList();
                            if (docs != null && docs.Count > 0)
                            {
                                newList.AddRange(docs);
                            }
                        });
                        return Ok(newList);
                    }

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

        IActionResult GetAgencyBrand(Cdn_Dto dto)
        {
            // load all brand before load item 
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request company is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Request company is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest($"Company {dto.CompanyName} not found");
                }

                dto.CompanyName = dto.CompanyName.Replace("'", "''");
                dto.AgencyName = dto.AgencyName.Replace("'", "''");
                dto.AgencyCode = dto.AgencyCode.Replace("'", "''");

                //create procedure sp_SelectAgencyBrand
                //@companyName as nvarchar(100), 
                //@agencyName as nvarchar(100), 
                //@agencyCode as nvarchar(100), 
                //@sapDb as nvarchar(100)

                var sp_sql = "exec sp_SelectAgencyBrand @companyName, @agencyName, @agencyCode, @sapDb";
                using var conn = new SqlConnection(CommDbConnStr);
                var brands = conn.Query<Brand>(sp_sql,
                    new
                    {
                        companyName = dbInfo.COMPANYNAME,
                        agencyName = dto.AgencyName,
                        agencyCode = dto.AgencyCode,
                        sapDb = dbInfo.SAPDB
                    }).ToList();
                return Ok(brands);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetAgencyCdnType(Cdn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("request company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.CodeType))
                {
                    return BadRequest("The request code type is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("request company name invalid");
                }

                //create procedure sp_SelectStaticCode
                //@codeType as nvarchar(100), 
                //@webDb as nvarchar(100)
                var sp_sql = "exec sp_SelectStaticCode @codeType, @webDb";

                using var conn = new SqlConnection(CommDbConnStr);
                return Ok(conn.Query<CdnType>(sp_sql, new { codeType = dto.CodeType, webDb = dbInfo.WEBDB }).ToList());
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
