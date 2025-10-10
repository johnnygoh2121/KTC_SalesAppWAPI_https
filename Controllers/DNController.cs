using Dapper;
using KTC_SalesAppWAPI.DTOs.DN;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DNController : ControllerBase
    {
        readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<DNController> _logger;
        string CommDbConnStr = string.Empty;
        string LastError = string.Empty;
        string WebHostAddrEndPoint = "";

        public DNController(IConfiguration configuration, ILogger<DNController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            CommDbConnStr = _configuration.GetConnectionString("MasterConn");
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(Dn_Dto dto)
        {
            switch (dto.Request)
            {
                case "GetAgencyDNType":
                    {
                        return GetAgencyDNType(dto);
                    }
                case "GetDNAgency":
                    {
                        return GetDNAgency(dto);
                    }
                case "GetDN":
                    {
                        return GetDN(dto);
                    }
                case "GetDNwCg":
                    {
                        return GetDNwCg(dto);
                    }
                case "GetAgencyBrand":
                    {
                        return GetAgencyBrand(dto);
                    }
                case "GetDNTermsConditions":
                    {
                        return GetDNTermsConditions();
                    }
                case "DebitNote":
                    {
                        return await DebitNote(dto);
                    }
                case "ArInvoice":
                    {
                        return ArInvoice(dto);
                    }
                case "DnCg_Cns":
                    {
                        return DnCg_Cns(dto);
                    }
                case "DnCg_CnsDetail":
                    {
                        return DnCg_CnsDetail(dto);
                    }
                case "DebitNoteChargeCode":
                    {
                        return await DebitNoteChargeCode(dto);
                    }
                case "SelectPafDnChargeCodeLine":
                    {
                        return SelectPafDnChargeCodeLine(dto);
                    }
                case "ArInvoiceWCg":
                    {
                        return ArInvoiceWCg(dto);
                    }
                case "UpdateDnfiles":
                    {
                        return UpdateDnfiles(dto);
                    }
                case "UpdateDnfilesWcg":
                    {
                        return UpdateDnfilesWcg(dto);
                    }
                //case "UpdatePafDnfiles":
                //    {
                //        return UpdatePafDnfiles(dto);
                //    }
                default:
                    {
                        return BadRequest("no such request");
                    }
            }
        }

        //IActionResult UpdatePafDnfiles (Dn_Dto dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        //        {
        //            return BadRequest("Company name is empty");
        //        }
        //        if (dto.DnDocEntry < 0)
        //        {
        //            return BadRequest("Invalid doc entry");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.DnFiles))
        //        {
        //            return BadRequest("Invalid files names");
        //        }
        //        var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
        //        if (db == null)
        //        {
        //            return BadRequest("Invalid company name, db info is empty");
        //        }

        //        var sql = @$"UPDATE [{db.WEBDB}].[dbo].[DN] 
        //                        SET PAFFILE = @GonFiles
        //                        WHERE DocEntry = @GonDocEntry";

        //        using (var conn = new SqlConnection(CommDbConnStr))
        //        {
        //            var result = conn.ExecuteScalar<int>(sql, new { dto.DnDocEntry, dto.DnFiles });
        //            return Ok();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult UpdateDnfilesWcg(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DnDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.DnFiles))
                {
                    return BadRequest("Invalid files names");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var sql = @$"UPDATE [{db.WEBDB}].[dbo].[PAFDN] 
                                SET PAFFILE = @DnFiles
                                WHERE DocEntry = @DnDocEntry";

                using var conn = new SqlConnection(CommDbConnStr);
                conn.Open();
                using var trans = conn.BeginTransaction();
                var result = conn.Execute(sql, new { dto.DnDocEntry, dto.DnFiles }, trans);
                if (result <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error update UPDATE PAFDN, subsi {db.COMPANYNAME}," +
                        $" DN Entry: {dto.DnDocEntry} ");
                }

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateDnfiles(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DnDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.DnFiles))
                {
                    return BadRequest("Invalid files names");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var sql = @$"UPDATE {db.WEBDB}..DN
                                SET PAFFILE = @DnFiles
                                WHERE DocEntry = @DnDocEntry";

                using var conn = new SqlConnection(CommDbConnStr);
                conn.Open();
                using var trans = conn.BeginTransaction();

                var result = conn.Execute(sql, new { dto.DnDocEntry, dto.DnFiles }, trans);
                if (result < 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error update file path DN, subsi {db.COMPANYNAME}, " +
                        $"DN Entry:{dto.DnDocEntry} ");
                }

                trans.Commit();
                return Ok();                
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SelectPafDnChargeCodeLine(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("company name is empty");
                }

                if (dto.PafDnDocEntry < 0)
                {
                    return BadRequest("request doc entry invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("company name provided invalid");
                }

                var sql = "exec sp_SelectPafDnChargeCodeLine @webDb, @docEntry";
                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<CNsDetail>(sql, new { webDb = db.WEBDB, docEntry = dto.PafDnDocEntry }).FirstOrDefault();
                if (results == null) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        async Task<IActionResult> DebitNoteChargeCode(Dn_Dto dto) // PAFDN
        {
            try
            {
                if (dto.dn == null)
                {
                    return BadRequest("DNCG doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid DNCG doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid DNCG Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid DNCG Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid DNCG Doc company name");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid Db info");
                }

                // 20220210

                if ($"{dto.UpdateType}".ToLower().Equals("submit") && string.IsNullOrWhiteSpace(dto.LastDocEntry))
                {
                    var sql = $@"select top 1 DOCENTRY from {db.WEBDB}..PAFDN t0
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

                // get the sales order line from portal
                using (var httpclient = new HttpClient())
                {
                    // post to portal                     
                    var json = JsonConvert.SerializeObject(dto.dn);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    // 20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                    var uri = new Uri($"{svrAdr}{dto.Request}/{dto.QueryCompanyID}/{dto.UpdateType}");

                    // /Paf/{CompanyId}/{UpdateType}

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);



                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<DnCgResult>(content);
                        result.updateDocType = dto.UpdateType;
                        result.docType = dto.Request;

                        // add in the post success log 
                        if (dto.Line != null)
                        {
                            dto.Line.PostResult = result.actionResult;
                            dto.Line.Details = "Success";
                            new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                        }

                        return Ok(result);
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
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
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DnCg_CnsDetail(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.docNumber <= 0)
                {
                    return BadRequest("invalid doc number");
                }
                if (dto.pafDocEntry <= 0)
                {
                    return BadRequest("invalid pad doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company dn info retrieve in error");
                }
                var sql = @"exec [sp_SelectcDnChargeCodeCn_Detail] @webDb, @erpDb, @docNumber, @pafDocEntry";
                var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<CNsDetail>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    docNumber = dto.docNumber,
                    pafDocEntry = dto.pafDocEntry
                }).FirstOrDefault();

                if (results == null) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DnCg_Cns(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Agency code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Card code is empty");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company dn info retrieve in error");
                }
                var sql = @"exec sp_SelectcDnChargeCodeCn @webDb, @erpDb, @cardCode, @agencyCode";
                var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<CNs>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    cardCode = dto.CardCode,
                    agencyCode = dto.AgencyCode
                }).ToList();

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

        IActionResult ArInvoiceWCg(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.InvNo <= 0)
                {
                    return BadRequest("Invalid dn number");
                }

                if (dto.DnDocEntry <= 0)
                {
                    return BadRequest("Invalid dn entry number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                var sql = @"exec sp_SelecArInvoice @sapDb, @DocNum";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<OINV>(sql, new { sapDb = db.SAPDB, DocNum = dto.InvNo }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = @"exec [sp_SelectArIvoiceLine] @sapDb, @docEntry";

                result.Lines = conn.Query<INV1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // get approval
                sql = "exec sp_SelectArInv_Approval_Wcg @webDb, @docEntry";
                result.Approval = conn.Query<PafApproval>(sql, new { webDb = db.WEBDB, docEntry = dto.DnDocEntry }).FirstOrDefault();

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ArInvoice(Dn_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.InvNo <= 0)
                {
                    return BadRequest("Invalid dn number");
                }

                if (dto.DnDocEntry <= 0)
                {
                    return BadRequest("Invalid dn entry number");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                var sql = @"exec sp_SelecArInvoice @sapDb, @DocNum";
                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<OINV>(sql, new { sapDb = db.SAPDB, DocNum = dto.InvNo }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = @"exec sp_SelectArIvoiceLine @sapDb, @docEntry";
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

                // get approval
                sql = "exec sp_SelectArInv_Approval @webDb, @docEntry";
                result.Approval = conn.Query<PafApproval>(sql, new { webDb = db.WEBDB, docEntry = dto.DnDocEntry }).FirstOrDefault();

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> DebitNote(Dn_Dto dto)
        {
            try
            {
                if (dto.dn == null)
                {
                    return BadRequest("DN doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid DN doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid DN Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid PAF Doc company id");
                }

                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid company name");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                // check the last doc entry 
                if ($"{dto.UpdateType}".ToLower() == "submit" && !string.IsNullOrWhiteSpace(dto.LastDocEntry))
                {
                    var sql = $@"select top 1 DOCENTRY from {db.WEBDB}..DN t0
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

                // get the sales order line from portal
                using (var httpclient = new HttpClient())
                {
                    // post to portal                     
                    var json = JsonConvert.SerializeObject(dto.dn);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    //20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                    var uri = new Uri($"{svrAdr}{dto.Request}/{dto.QueryCompanyID}/{dto.UpdateType}");

                    // /Paf/{CompanyId}/{UpdateType}

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;



                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<PAFDnResult>(content);
                        result.updateDocType = dto.UpdateType;
                        result.docType = dto.Request;

                        // add in the post success log 
                        if (dto.Line != null)
                        {
                            dto.Line.PostResult = result.actionResult;
                            dto.Line.Details = "Success";
                            new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                        }

                        return Ok(result);
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
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
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetDNTermsConditions()
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

        IActionResult GetAgencyBrand(Dn_Dto dto)
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

                var sp_sql = "EXEC sp_SelectAgencyBrand @companyName, @agencyName, @agencyCode, @sapDb";

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

        IActionResult GetDNAgency(Dn_Dto dto)

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
                    //@CompanyName as nvarchar(120), 
                    //@SapDb as nvarchar(120), 
                    //@WebDb as nvarchar(120), 
                    //@CardType as nvarchar(1), 
                    //@ValidFor as nvarchar(1)
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

                // base on user code

                var sql1 = " Exec sp_SelectCdnAgency2 @CompanyName,@SapDb,@WebDb, @CardType, @ValidFor, @UserCode ";

                using var conn1 = new SqlConnection(CommDbConnStr);
                var result1 = conn1.Query<OCRD_Ext>(sql1,
                    new
                    {
                        CompanyName = dto.CompanyName,
                        SapDb = dbInfo.SAPDB,
                        WebDb = dbInfo.WEBDB,
                        dto.CardType,
                        dto.ValidFor,
                        UserCode = dto.UserCode
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

        IActionResult GetAgencyDNType(Dn_Dto dto)
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
                return Ok(conn.Query<DNType>(sp_sql, new { codeType = dto.CodeType, webDb = dbInfo.WEBDB }).ToList());
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetDNwCg(Dn_Dto dto)
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
                var sp_query = "exec sp_SelectDNWithChargeCode @webDb, @sapDb, @companyName, @cardCode, @startDt, @endDt";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<DebitNote>(sp_query,
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

                        var newList = new List<DebitNote>();
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
        IActionResult GetDN(Dn_Dto dto)
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
                var sp_query = "exec sp_SelectDN @webDb, @sapDb, @companyName, @cardCode, @startDt, @endDt";

                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var result = conn.Query<DebitNote>(sp_query,
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

                        var newList = new List<DebitNote>();
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
    }
}



