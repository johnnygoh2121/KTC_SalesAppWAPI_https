using Dapper;
using KTC_SalesAppWAPI.DTOs.COG;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
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
using System.Xml.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class COGController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        //readonly string APP_JSON = "application/json";
        readonly ILogger<COGController> _logger;
        string WebHostAddrEndPoint = "";

        string LastError { get; set; } = string.Empty;

        string _commDbConnStr { get; set; } = string.Empty;

        public COGController(IConfiguration configuration, ILogger<COGController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult Post(Dto_Cog dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetCOG":
                    {
                        return GetCOG(dto);
                    }
                case "GetCOGLines":
                    {
                        return GetCOGLines(dto);
                    }
                case "ReasonCodes":
                    {
                        return ReasonCodes(dto);
                    }

                case "ReasonCodes_Delivery": // for delivery
                    {
                        return ReasonCodes_Delivery(dto);
                    }
                case "VerifyCogItemCode":
                    {
                        return VerifyCogItemCode(dto);
                    }
                case "CreateCog":
                    {
                        return CreateCog1(dto);
                    }
                case "CreateDirectCN":
                    {
                        return CreateDirectCN1(dto); // return cog end point
                    }
                case "GetRTN":
                    {
                        return GetRTN(dto); // direct cn table
                    }
                case "GetRTNs":
                    {
                        return GetRTNs(dto); // direct cn table
                    }
                case "GetRTNLines":
                    {
                        return GetRTNLines(dto); // direct cn table
                    }
                case "GetCogDocs":
                    {
                        return GetCogDocs(dto);
                    }
                case "UpdateCogSignDocfiles":
                    {
                        return UpdateCogSignDocfiles(dto);
                    }
                case "InvItemHistory":
                    {
                        return InvItemHistory(dto);
                    }
                case "GetCogAndLines":
                    {
                        return GetCogAndLines(dto);
                    }
                case "GetBoxContents":
                    {
                        return GetBoxContents(dto);
                    }
                //case "GetCogReturnDocs":
                //    {
                //        return GetCogReturnDocs(dto); // RTN
                //    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetCogAndLines(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid cog doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var qr = @$"select * 
                            from {db.WEBDB}..COG with (nolock)
                            Where DocEntry = @DocEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var cog = conn.Query<COG_Doc>(qr, new { dto.DocEntry }).FirstOrDefault();
                if (cog == null)
                {
                    return NotFound();
                }

                // get the cog line 
                qr = @$"select 
                           t1.SuppCatNum [SuppCatNum]
                        ,  t2.Name [REASONDesc]
                        ,  t0.* 
                        from {db.WEBDB}..COG1 t0 with (nolock) 
                        left join {db.SAPDB}..OITM t1 with (nolock) on t1.ItemCode = t0.ItemCode
                        left join {db.SAPDB}..[@CSUS_REASON_GL_AR] t2 with (nolock) on t2.Code = t0.REASON
                        where t0.DocEntry = @DocEntry";

                cog.LINES = conn.Query<COG_Line>(qr, new { dto.DocEntry }).ToList();
                cog.DocTotal = cog.LINES.Sum(l => l.LINETOTAL);
                cog.SubSi = db.COMPANYNAME;

                return Ok(cog);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetBoxContents(Dto_Cog dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company name.");
                }
                if (dto.BoxGuid == default)
                {
                    return BadRequest("Invalid box guid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var query = @$"Select * from {db.WEBDB}..FTAPP_BOX1 Where convert(nvarchar(50), BoxGuid) = @boxGuid";
                using var conn = new SqlConnection(_commDbConnStr);
                var boxContents = conn.Query<FTAPP_Box1>(query, new
                {
                    boxGuid = $"{dto.BoxGuid}"
                }).ToList();
                if (boxContents.Count == 0) return NotFound();

                return Ok(boxContents);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult InvItemHistory(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("Invalid item code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Invalid card code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sp_query = @"exec sp_Cog_QueryLastOrderItem_List @webDb, @itemCode, @cardCode, @startDt, @ednDt";

                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<CogItem>(sp_query, new
                {
                    webDb = db.WEBDB,
                    itemCode = dto.ItemCode,
                    cardCode = dto.CardCode,
                    startDt = dto.StartDt,
                    ednDt = dto.EndDt
                }).ToList();

                if (results.Count == 0) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        int UserCodeGracePeriod(Dto_Cog dto)
        {

            // for tp ware return use zero grace period 
            // 20240914
            if (dto.Line != null)
            {
                var module = $"{dto.Line.AppModule}";
                if (module.Equals("TPRetTrCn")) return 0;
            }

            // check app setting json file for the grace period value
            // if fail, hard code to 7 days                                                  
            var defaultGracePeriodVal = _configuration.GetSection("AppSettings").GetSection("DefaultDiCnGracePeriod").Value;
            var isNumeric = int.TryParse($"{defaultGracePeriodVal}", out int defaultGracePeriod);
            if (!isNumeric)
            {
                defaultGracePeriod = 4; //7;
            }

            try
            {
                // query the app config table
                // for checking the default grace period for all

                string sql_DefaultGracePeriod = "";
                if (dto.IsStandAloneTrcn) // 20230614
                {
                    sql_DefaultGracePeriod = @"Select SetupValue 
                                                from FTApp_Config with (nolock) 
                                                Where SetupName= 'AppDiCnReturnGracePeriod_Driver'";

                    var conn = new SqlConnection(_commDbConnStr);
                    defaultGracePeriod = conn.ExecuteScalar<int>(sql_DefaultGracePeriod);

                    return defaultGracePeriod;
                }
                else // foloow seller grace period 
                {
                    sql_DefaultGracePeriod = @"Select SetupValue 
                                                from FTApp_Config with (nolock) 
                                                Where SetupName= 'AppDiCnReturnGracePeriod_Seller'";

                    var conn = new SqlConnection(_commDbConnStr);
                    defaultGracePeriod = conn.ExecuteScalar<int>(sql_DefaultGracePeriod);

                    if (string.IsNullOrWhiteSpace(dto.Subsi))
                    {
                        return defaultGracePeriod;
                    }

                    if (string.IsNullOrWhiteSpace(dto.UserCode))
                    {
                        return defaultGracePeriod;
                    }

                    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                    if (db == null)
                    {
                        return defaultGracePeriod;
                    }

                    // get the default grace period 
                    var sp_sql = @"exec sp_SelectGracePeriodByUserCode @webDb, @userCode";

                    var result = conn.ExecuteScalar<int>(sp_sql, new { webDb = db.WEBDB, dto.UserCode });
                    if (result <= 0) return defaultGracePeriod;
                    return result;
                }

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return defaultGracePeriod;
            }
        }

        IActionResult UpdateCogSignDocfiles(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.SignDoc))
                {
                    return BadRequest("Invalid files names");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }
                // query to get the prevous file, and update will later
                // 2021-07-18 add and update file from app 
                // check the exiting append in the file name behind

                var sqlCogFiles = $@"SELECT SIGNDOC
                                          FROM [{db.WEBDB}].[dbo].[RTN] with (nolock)  
                                          WHERE DocEntry = @DocEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var existingFiles = conn.ExecuteScalar<string>(sqlCogFiles, new { DocEntry = dto.DocEntry });
                if (!string.IsNullOrWhiteSpace(existingFiles))
                {
                    existingFiles += "," + dto.SignDoc;
                }
                else
                {
                    existingFiles = dto.SignDoc;
                }


                var sql = @$"UPDATE [{db.WEBDB}].[dbo].[RTN] 
                                SET SIGNDOC = @SignDoc
                                WHERE DocEntry = @DocEntry";

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    var result = conn.ExecuteScalar<int>(sql,
                        new
                        {
                            DocEntry = dto.DocEntry,
                            SignDoc = existingFiles
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
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //IActionResult GetCogReturnDocs (Dto_Cog dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("Sub si is empty");
        //        }
        //        if (dto.EndDt == default)
        //        {
        //            return BadRequest("invalid end date");
        //        }
        //        if (dto.StartDt == default)
        //        {
        //            return BadRequest("invalid start date");
        //        }

        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("Db info query error");
        //        }

        //        var sp_query = @"exec sp_SelectCogReturnDocs @webDb, @startDate, @endDate";
        //        var conn = new SqlConnection(_commDbConnStr);
        //        var res = conn.Query<Return_Doc>(sp_query, new
        //        {
        //            webDb = db.WEBDB,
        //            startDate = dto.StartDt,
        //            endDate = dto.EndDt
        //        }).ToList();

        //        if (res == null) return NotFound();
        //        if (res.Count == 0) return NotFound();

        //        return Ok(res);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult GetCogDocs(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }

                if (dto.UserCode == default)
                {
                    return BadRequest("invalid start date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Db info query error");
                }

                var sp_query = @"exec sp_SelectCogDocs @webDb, @subSi, @startDate, @endDate, @userCode ";
                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<COG_Doc>(sp_query, new
                {
                    webDb = db.WEBDB,
                    subSi = db.COMPANYNAME,
                    startDate = dto.StartDt,
                    endDate = dto.EndDt,
                    userCode = dto.UserCode
                }).ToList();

                if (res == null) return NotFound();
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

        IActionResult GetRTNLines(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectRTNLines @webDb, @subsi, @docEntry";
                //@webDb as nvarchar(100), 
                //@cardCode as nvarchar(100), 
                //@startDt as datetime, 
                //@endDt as datetime

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<Return_Line>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        subsi = db.COMPANYNAME,
                        docEntry = dto.DocEntry
                    }).ToList();

                if (res != null && res.Count > 0)
                {
                    for (int i = 0; i < res.Count; i++)
                    {
                        var item = res[i];
                        if (item == null) continue;

                        if (item.UOMQTY == 0)
                        {
                            var sql1 = @$"select U_CSUS_UOM from {db.SAPDB}..OITM with (nolock) Where ItemCode = @itemCode ";
                            var uomQty = conn.ExecuteScalar<decimal>(sql1, new { itemCode = item.ITEMCODE });
                            res[i].UOMQTY = (uomQty == 0) ? 1 : uomQty;
                        }
                    }

                    return Ok(res);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for whs app
        IActionResult GetRTNs(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }

                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start query date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end query date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectRTN2 @webDb, @erpDb, @startDt, @endDt, @subsi";

                //@webDb as nvarchar(100), 
                //@cardCode as nvarchar(100), 
                //@startDt as datetime, 
                //@endDt as datetime

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<Return_Doc>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        startDt = dto.StartDt,
                        endDt = dto.EndDt,
                        subsi = db.COMPANYNAME
                    }).ToList();

                if (res != null && res.Count > 0)
                {
                    return Ok(res);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }

        IActionResult GetRTN(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Card code is empty");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start query date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end query date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectRTN @webDb, @erpDb, @startDt, @endDt, @subsi, @userCode";

                //@webDb as nvarchar(100), 
                //@cardCode as nvarchar(100), 
                //@startDt as datetime, 
                //@endDt as datetime

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<Return_Doc>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        startDt = dto.StartDt,
                        endDt = dto.EndDt,
                        subsi = db.COMPANYNAME,
                        userCode = dto.UserCode
                    }).ToList();

                if (res != null && res.Count > 0)
                {
                    return Ok(res);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }

        IActionResult CreateDirectCN(Dto_Cog dto) // RTN in web db
        {
            try
            {
                if (dto.NewDirectCn == null)
                {
                    return BadRequest("Direct DN doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid query key");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid company id");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid Direct CN Doc company id");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("UserCode is empty");
                }
                //  dto.NewDirectCn.Docentry 
                if (dto.NewDirectCn.Docentry == 0)
                {
                    return BadRequest("docentry can not be zero from draft");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // get the grace period
                dto.NewDirectCn.Graceperiod = UserCodeGracePeriod(dto);

                //20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}ReturnCOG/{dto.QueryCompanyID}/Submit");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.NewDirectCn);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content = response.Content; //await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ReturnCogResult>(content);
                    result.updateDocType = dto.UpdateType;
                    result.docType = dto.Request;

                    // -----------------------------------------
                    // try to duplicate the draft to RTN 
                    // -----------------------------------------
                    // try to duplicate the draft to RTN 
                    using var conn_check = new SqlConnection(_commDbConnStr);
                    var checkRtnCreated = @$"Select * from {db.WEBDB}..RTN Where DocEntry = @docentry";
                    var found_check = conn_check.Query<RTN>(checkRtnCreated,
                        new
                        {
                            docentry = result.actionResult
                        }).FirstOrDefault();

                    // if save draft no found, then return error 
                    if (found_check == null)
                    {
                        return BadRequest("Saved RTN Fail, please try again");
                    }

                    // if found .. 
                    // duplicate to log table 
                    // FTAPP_RTN
                    // 
                    found_check.IS_WHS_RECEIPT = 0;
                    var sp_insertDeleteLog = $@"Delete from {db.WEBDB}..FTAPP_RTN Where DocEntry = @DocEntry;
                                                Delete from {db.WEBDB}..FTAPP_RTN1 Where DocEntry = @DocEntry; 
                                                Delete from {db.WEBDB}..FTAPP_RTN2 Where DocEntry = @DocEntry;  

                                              Insert into {db.WEBDB}..FTAPP_RTN 
                                                 (
                                                      DOCENTRY  , DOCNUM  , BASEDOCNUM  , DOCDATE  , DOCSTATUS  
                                                    , DOCTYPE  , CARDCODE  , CARDNAME  , SHIPADD  , COLTYPE  , COGNO 
                                                   , REFNO  , REMARKS  , CMENTRY  , UCREATED  , UMODIFIED  , DCREATED 
                                                   , DMODIFIED  , LASTINVREM  , GSTREM  , SIGNDOC  , ITEMDOC , SALESPERSON 
                                                   , GRACEPERIOD  , DRIVER  , LORRYNO , TRANSPORTER 
                                                   , IS_WHS_RECEIPT 
                                                   , WHS_RECEIPT_DT 
                                                   , WHS_USER_CODE 
                                                   , CNDOCNUM 
                                                   , CNENTRY 
                                                   , ITDOCNUM 
                                                 )
                                                 Select  DOCENTRY  , DOCNUM  , BASEDOCNUM  , DOCDATE  , DOCSTATUS  
                                                    , DOCTYPE  , CARDCODE  , CARDNAME  , SHIPADD  , COLTYPE  , COGNO 
                                                   , REFNO  , REMARKS  , CMENTRY  , UCREATED  , UMODIFIED  , DCREATED 
                                                   , DMODIFIED  , LASTINVREM  , GSTREM  , SIGNDOC  , ITEMDOC , SALESPERSON 
                                                   , GRACEPERIOD  , DRIVER  , LORRYNO , TRANSPORTER 
                                                   , 0     [IS_WHS_RECEIPT] 
                                                   , null  [WHS_RECEIPT_DT]
                                                   , null  [WHS_USER_CODE] 
                                                   , null  [CNDOCNUM] 
                                                   , null  [CNENTRY] 
                                                   , null  [ITDOCNUM]
                                                     from {db.WEBDB}..RTN 
                                                     Where DocEntry =  @DocEntry;
                                             
                                                Insert into {db.WEBDB}..FTAPP_RTN1 Select * from {db.WEBDB}..RTN1 
                                                Where DocEntry = @DocEntry;

                                                Insert into {db.WEBDB}..FTAPP_RTN2 Select * from {db.WEBDB}..RTN2 
                                                Where DocEntry = @DocEntry;";

                    if (conn_check.State == System.Data.ConnectionState.Closed) conn_check.Open();
                    using var checkTrans = conn_check.BeginTransaction();
                    var checkRes = conn_check.Execute(sp_insertDeleteLog,
                        new
                        {
                            DocEntry = result.actionResult
                        }, checkTrans);

                    if (checkRes <= 0) // update fail
                    {
                        checkTrans.Rollback();
                    }

                    // else then commit
                    checkTrans.Commit();
                    // end duplicate the logging 


                    // 20240906
                    // test select the CN from SAP 
                    using var connCheck1 = new SqlConnection(_commDbConnStr);
                    connCheck1.Open();
                    var transCheck1 = connCheck1.BeginTransaction();
                    try
                    {
                        var sp_QueryCn = $@"Select * from {db.SAPDB}..ORIN with (nolock) where U_SOID = @rtnDocEntry ;";
                        var foundSapCn = connCheck1
                            .Query<ORIN>(sp_QueryCn, new { rtnDocEntry = dto.NewDirectCn.Docentry }, transCheck1)
                            .FirstOrDefault();

                        if (foundSapCn == null)
                        {
                            transCheck1.Rollback();
                            return BadRequest("Server busy at creation of TRCN, please try again. Thanks.");
                        }

                        // reupdate the cn docntry to the RTN table
                        var sp_UpdateRTN_Entry = @$"Update {db.WEBDB}..RTN Set CMENTRY = @CMENTRY 
                                                Where DocEntry = @DocEntry; ";

                        var reUpdateResult = connCheck1.Execute(sp_UpdateRTN_Entry, new
                        {
                            CMENTRY = foundSapCn.DocEntry,
                            DocEntry = dto.NewDirectCn.Docentry
                        }, transCheck1);

                        if (reUpdateResult <= 0)
                        {
                            transCheck1.Rollback();
                            return BadRequest("Error reupdate the RTN table, Please try again.");
                        }

                        transCheck1.Commit();
                    }
                    catch (Exception ex_recheck)
                    {
                        transCheck1.Rollback();
                        return BadRequest($"Error reupdate the RTN table, " +
                            $"Please try again. [Isses]{ex_recheck.Message} {ex_recheck.StackTrace}");
                    }



                    // query the procedure to get the credit number 
                    using var conn = new SqlConnection(_commDbConnStr);
                    var sp = @"sp_SelectCogDirectCN_No @webDb, @erpDb, @returnCogDocEntry ";
                    //@webDb as nvarchar(100), 
                    //@erpDb as nvarchar(100), 
                    //@returnCogDocEntry as nvarchar(100)


                    var creditMemoNum = conn.ExecuteScalar<string>(sp, new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        returnCogDocEntry = result.actionResult
                    });

                    if (string.IsNullOrWhiteSpace(creditMemoNum))
                    {
                        // add in the post success log 
                        if (dto.Line != null)
                        {
                            dto.Line.PostResult = result.actionResult;
                            dto.Line.Details = "Success, Submit";
                            new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                        }

                        return Ok(result);
                    }

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result.actionResult;
                        dto.Line.Details = "Success, Submit CN# " + creditMemoNum;
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    result.CreditMemoDocNum = creditMemoNum;

                    // update the link table with cn doc num
                    // create cn from invoice 
                    if (dto.DlbNum > 0)
                    {
                        //isReupdateRTN_UCreatedUser = true;

                        // update the link table for DLB docentry 
                        // check duplicated doc 
                        var check_sql = @$"select * from {db.WEBDB}..FTAPP_DriverTrcnLink 
                                           where DLbEntry = @DLbEntry 
                                           and BaseDocNum = @BaseDocNum
                                           and BaseDocType = @BaseDocType
                                           and RtnEntry = @RtnEntry ";

                        var found = conn.Query<FTAPP_DriverTrcnLink>(check_sql, new
                        {
                            DLbEntry = dto.DlbNum,
                            BaseDocNum = dto.BasedDocNum,
                            BaseDocType = dto.BasedDoctype,
                            RtnEntry = dto.NewDirectCn.Docentry
                        }).FirstOrDefault();

                        if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                        using var trans = conn.BeginTransaction();
                        try
                        {
                            if (found != null)
                            {
                                var update_qr = @$"update {db.WEBDB}..FTAPP_DriverTrcnLink 
                                            set CnDocNum = @CnDocNum 
                                            where id = @id";

                                conn.Execute(update_qr, new { CnDocNum = creditMemoNum, id = found.id }, trans);
                            }
                            else
                            {
                                // insert the link driver information
                                var newLink = new FTAPP_DriverTrcnLink
                                {
                                    DLbEntry = dto.DlbNum,
                                    BaseDocNum = dto.BasedDocNum,
                                    BaseDocType = dto.BasedDoctype,
                                    TransDt = DateTime.Now,
                                    CnDocNum = int.Parse(creditMemoNum),
                                    RtnEntry = (int)dto.NewDirectCn.Docentry
                                };

                                var insert_link = @$"INSERT INTO {db.WEBDB}..FTAPP_DriverTrcnLink (
                                             DLbEntry
                                           , BaseDocNum
                                           , BaseDocType
                                           , TransDt 
                                           , CnDocNum
                                           , RtnEntry
                                        ) values ( 
                                             @DLbEntry
                                            ,@BaseDocNum
                                            ,@BaseDocType
                                            ,GETDATE() 
                                            ,@CnDocNum 
                                            ,@RtnEntry  )";

                                conn.Execute(insert_link, newLink, trans);
                            }
                            trans.Commit();
                        }
                        catch (Exception e)
                        {
                            trans.Rollback();
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                        }
                    }

                    // 20230613
                    // for stand alone trcn 
                    var isReupdateRTN_UCreatedUser = false;
                    if (dto.IsStandAloneTrcn)
                    {
                        isReupdateRTN_UCreatedUser = true;

                        var check_sql = @$"Select * from  {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink 
                                            Where RtnEntry = @RtnEntry ";

                        using var conn1 = new SqlConnection(_commDbConnStr);
                        var found1 = conn1.Query<FTAPP_DriverStandAloneTrcnLink>(check_sql, new
                        {
                            RtnEntry = (int)dto.NewDirectCn.Docentry
                        }).FirstOrDefault();

                        if (conn1.State == System.Data.ConnectionState.Closed) conn1.Open();
                        using var trans1 = conn1.BeginTransaction();

                        try
                        {
                            if (found1 != null) // if save draft before, then perform update
                            {
                                var update_sql = @$"update {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink 
                                                    set CnDocNum = @CnDocNum
                                                    Where Id = @Id ";

                                conn1.Execute(update_sql, new { Id = found1.id, CnDocNum = creditMemoNum }, trans1);
                            }
                            else
                            {
                                // perform the insert

                                var insertNewLink = new FTAPP_DriverStandAloneTrcnLink
                                {
                                    DriverName = dto.DriverName,
                                    PlateNum = dto.PlateNum,
                                    RtnEntry = (int)dto.NewDirectCn.Docentry,
                                    CnDocNum = int.Parse(creditMemoNum),
                                    TransDt = DateTime.Now
                                };

                                var insert_sql = $@"INSERT INTO  {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink (
                                                      TransDt
                                                    , CnDocNum 
                                                    , RtnEntry 
                                                    , DriverName
                                                    , PlateNum
                                                ) values ( 
                                                      GETDATE()
                                                    , @CnDocNum
                                                    , @RtnEntry 
                                                    , @DriverName
                                                    , @PlateNum 
                                                )";

                                conn1.Execute(insert_sql, insertNewLink, trans1);
                            }

                            // perform the commit
                            trans1.Commit();
                        }
                        catch (Exception e)
                        {
                            trans1.Rollback();
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                        }
                    }

                    // 20240719
                    if (isReupdateRTN_UCreatedUser == true)
                    {
                        ReUpdateTheUCreateUser_RTN(dto, db, result.actionResult, creditMemoNum);
                    }

                    return Ok(result);
                }
                else
                {
                    var content = response.Content;  //await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                    if (result == null)
                    {
                        return BadRequest("Error when posting to web portal");
                    }

                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = "Fail";
                        dto.Line.Details = result.errorMessage;
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var newReplied = new ReturnCogResult
                {
                    actionSuccess = false,
                    actionResult = "fail",
                    errorMessage = $"Exception:{LastError}",
                    documentStatus = ""
                };

                return BadRequest(newReplied);
            }
        }

        void ReUpdateTheUCreateUser_RTN(Dto_Cog dto, DbInfo db,
            string rtn_docEntry, string cdDocNum)
        {
            try
            {
                // 20240718
                // check agency code and get the seller user code
                if (!string.IsNullOrWhiteSpace(dto.NewDirectCn.AgencyCode))
                {
                    // query the seller code and update                     
                    //var spGetSellerCode = @"exec sp_QuerySellerCodeFromLastRouteSchedule @webDb, 
                    //                            @customerCardCode , @agencyCardCode ";

                    var spGetSellerCode = @"exec sp_QuerySellerCodeFromLastInvoice @webDb, 
                                                @customerCardCode , @agencyCardCode ";

                    using (var connSellerCodr = new SqlConnection(_commDbConnStr))
                    {
                        var sellerCode = connSellerCodr.Query<UserCodeProp>(spGetSellerCode, new
                        {
                            webDb = db.WEBDB,
                            customerCardCode = dto.NewDirectCn.Cardcode,
                            agencyCardCode = dto.NewDirectCn.AgencyCode
                        }).FirstOrDefault();

                        if (sellerCode == null)
                        {
                            sellerCode = new UserCodeProp
                            {
                                UserCode = "Manager",
                                UserName = "Manager",
                                SlpCode = "-1"
                            };

                            //// check last invoice 
                            //spGetSellerCode = @"exec sp_QuerySellerCodeFromLastInvoice @webDb, 
                            //                    @customerCardCode , @agencyCardCode ";

                            //sellerCode = connSellerCodr.Query<UserCodeProp>(spGetSellerCode, new
                            //{
                            //    webDb = db.WEBDB,
                            //    customerCardCode = dto.NewDirectCn.Cardcode,
                            //    agencyCardCode = dto.NewDirectCn.AgencyCode
                            //}).FirstOrDefault();

                            //if (sellerCode == null)
                            //{
                            //    sellerCode = new UserCodeProp
                            //    {
                            //        UserCode = "Manager",
                            //        UserName = "Manager",
                            //        SlpCode = "-1"
                            //    };
                            //}
                        }

                        var sp_update = @$"Update {db.WEBDB}..RTN 
                                            Set SALESPERSON = @UserName
                                            Where DocEntry  = @rtn_docEntry; 

                                            update {db.SAPDB}..ORIN 
                                            Set SLPCODE = @SlpCode 
                                            Where DocNum = @DocNum; ";

                        if (connSellerCodr.State == System.Data.ConnectionState.Closed) connSellerCodr.Open();
                        using (var trans = connSellerCodr.BeginTransaction())
                        {
                            var res = connSellerCodr.Execute(sp_update, new
                            {
                                UserCode = sellerCode.UserCode,
                                UserName = sellerCode.UserName,
                                rtn_docEntry = rtn_docEntry,
                                SlpCode = sellerCode.SlpCode,
                                DocNum = cdDocNum
                            }, trans);

                            if (res <= 0)
                            {
                                trans.Rollback();
                                LastError = $"{db.COMPANYNAME}, RTN# {rtn_docEntry} " +
                                                $"Error update RTN UCREATED, SALESPERSON and ORIN";
                                _logger.LogError(LastError);
                                return;
                            }

                            trans.Commit();
                        } // trans

                    } // connection

                } // end if 
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        // entry point
        IActionResult CreateDirectCN1(Dto_Cog dto) // RTN in web db
        {
            try
            {
                if (dto.NewDirectCn == null)
                {
                    return BadRequest("Direct DN doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid query key");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid company id");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid Direct CN Doc company id");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("UserCode is empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // get the grace period
                dto.NewDirectCn.Graceperiod = UserCodeGracePeriod(dto);

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // always save draft
                var client = new RestClient($"{svrAdr}ReturnCOG/{dto.QueryCompanyID}/draft");

                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.NewDirectCn);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content = response.Content; // await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ReturnCogResult>(content);
                    result.updateDocType = dto.UpdateType;
                    result.docType = dto.Request;

                    // to be commit                   
                    // save the dlb if 
                    if (dto.DlbNum > 0)
                    {
                        // check duplicated doc 
                        var check_sql = @$"select * from {db.WEBDB}..FTAPP_DriverTrcnLink 
                                           where DLbEntry = @DLbEntry 
                                           and   BaseDocNum = @BaseDocNum
                                           and   BaseDocType = @BaseDocType
                                           and   RtnEntry = @RtnEntry ";

                        var conn = new SqlConnection(_commDbConnStr);
                        var found = conn.Query<FTAPP_DriverTrcnLink>(check_sql, new
                        {
                            DLbEntry = dto.DlbNum,
                            BaseDocNum = dto.BasedDocNum,
                            BaseDocType = dto.BasedDoctype,
                            RtnEntry = result.actionResult
                        }).FirstOrDefault();

                        if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                        using var trans = conn.BeginTransaction();
                        try
                        {
                            if (found != null)
                            {
                                var delete_sql = @$"delete from {db.WEBDB}..FTAPP_DriverTrcnLink where id = @id";
                                conn.Execute(delete_sql, new { id = found.id }, trans);
                            }

                            var newLink = new FTAPP_DriverTrcnLink
                            {
                                DLbEntry = dto.DlbNum,
                                BaseDocNum = dto.BasedDocNum,
                                BaseDocType = dto.BasedDoctype,
                                TransDt = DateTime.Now,
                                RtnEntry = int.Parse(result.actionResult)
                            };

                            var insert_link = @$"INSERT INTO {db.WEBDB}..FTAPP_DriverTrcnLink (
                                             DLbEntry
                                           , BaseDocNum
                                           , BaseDocType
                                           , TransDt 
                                           , RtnEntry
                                        ) values ( 
                                             @DLbEntry
                                            ,@BaseDocNum
                                            ,@BaseDocType
                                            ,GETDATE()
                                            ,@RtnEntry
                                        )";

                            conn.Execute(insert_link, newLink, trans);
                            trans.Commit();
                        }
                        catch (Exception e)
                        {
                            trans.Rollback();
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                        }
                    }

                    // 20230613
                    // check and update the link
                    if (dto.IsStandAloneTrcn)
                    {
                        var check_sql = @$"Select * from  {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink 
                                            Where RtnEntry = @RtnEntry ";

                        using var conn1 = new SqlConnection(_commDbConnStr);
                        var found1 = conn1.Query<FTAPP_DriverStandAloneTrcnLink>(check_sql, new
                        {
                            RtnEntry = result.actionResult
                        }).FirstOrDefault();

                        if (conn1.State == System.Data.ConnectionState.Closed) conn1.Open();
                        using var trans1 = conn1.BeginTransaction();

                        try
                        {
                            if (found1 != null)
                            {
                                var delete_sql = @$"Delete from {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink                                         
                                                    Where Id = @Id ";

                                conn1.Execute(delete_sql, new { Id = found1.id }, trans1);
                            }

                            // performt the insert

                            var newLink = new FTAPP_DriverStandAloneTrcnLink
                            {
                                DriverName = dto.DriverName,
                                PlateNum = dto.PlateNum,
                                RtnEntry = int.Parse(result.actionResult),
                                TransDt = DateTime.Now
                            };

                            var insert_sql = $@"INSERT INTO  {db.WEBDB}..FTAPP_DriverStandAloneTrcnLink (
                                                      TransDt
                                                    , RtnEntry 
                                                    , DriverName
                                                    , PlateNum
                                                ) values ( 
                                                      GETDATE()
                                                    , @RtnEntry 
                                                    , @DriverName
                                                    , @PlateNum )";

                            conn1.Execute(insert_sql, newLink, trans1);
                            trans1.Commit();
                        }
                        catch (Exception e)
                        {
                            trans1.Rollback();
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                        }
                    }

                    if ($"{dto.UpdateType}".ToLower().Equals("submit"))
                    {
                        dto.NewDirectCn.Docentry = int.Parse(result.actionResult);
                        return CreateDirectCN(dto);
                    }

                    // return as draft saved
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result.actionResult;
                        dto.Line.Details = "Success, save draft";
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return Ok(result);
                }
                else
                {
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
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var newReplied = new ReturnCogResult
                {
                    actionSuccess = false,
                    actionResult = "fail",
                    errorMessage = $"Exception:{LastError}",
                    documentStatus = ""
                };

                return BadRequest(newReplied);
            }
        }

        /// <summary>
        ///  post to portal create cog 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult CreateCog(Dto_Cog dto)
        {
            try
            {
                if (dto.NewCog == null)
                {
                    return BadRequest("COG doc is null");
                }

                //  dto.NewCog.DOCENTRY
                //  dto.NewCog.DOCENTRY
                if (dto.NewCog.DOCENTRY == 0)
                {
                    return BadRequest("cog doc entry is zero");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid company id");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid COG Doc company id");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ?
                    db.PostSvrAdressPort : WebHostAddrEndPoint;

                // 20220409
                // using rest client 
                // post with rest client 
                // 20220409
                var client = new RestClient($"{svrAdr}COG/{dto.QueryCompanyID}/{dto.UpdateType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.NewCog);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content = response.Content; //await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<COGResult>(content);
                    result.updateDocType = dto.UpdateType;
                    result.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result.actionResult;
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return Ok(result);
                }
                else
                {
                    var content = response.Content; //await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                    if (result == null)
                    {
                        return BadRequest("Error when posting to web portal");
                    }

                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = "Fail";
                        dto.Line.Details = result.errorMessage;
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        ///  post to portal create cog 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult CreateCog1(Dto_Cog dto)
        {
            try
            {
                if (dto.NewCog == null)
                {
                    return BadRequest("COG doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid company id");
                }

                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid COG Doc company id");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // 20220409
                // using rest client 
                // post with rest client 
                // always save draft first
                // 20220409
                var client = new RestClient($"{svrAdr}COG/{dto.QueryCompanyID}/draft");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.NewCog);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var content = response.Content; // await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<COGResult>(content);
                    result.updateDocType = dto.UpdateType;
                    result.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = result.actionResult;
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    if ($"{dto.UpdateType}".ToLower() == "submit")
                    {
                        var actionResult = result.actionResult; // doc entry for draft
                        dto.NewCog.DOCENTRY = int.Parse(actionResult);
                        return CreateCog(dto); // post again for submit 
                    }

                    return Ok(result);
                }
                else
                {
                    var content = response.Content; //await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                    if (result == null)
                    {
                        return BadRequest("Error when posting to web portal");
                    }

                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = "Fail";
                        dto.Line.Details = result.errorMessage;
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult VerifyCogItemCode(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("The card code invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    return BadRequest("The query code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("The usercode is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name as info");
                }

                //get number of month of pass invoice from setup table
                var sql_query = @"select SetupValue 
                                  from [KTCW_COMMON]..[FTApp_Config] WITH (NOLOCK) 
                                  where SetupName = 'COGQueryNumOfMonthOfPastInvoiceLowerPrice' ";

                var conn = new SqlConnection(_commDbConnStr); // open the db connection
                var numOfMonth = conn.ExecuteScalar<int>(sql_query);
                if (numOfMonth <= 0)
                {
                    var value_ = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
                    var isNumeric = int.TryParse(value_, out int parserRes);
                    numOfMonth = isNumeric ? parserRes : 6;
                }

                var checkCode = dto.Code.TrimStart('0');                                     
                var sq_item = @"exec sp_Cog_QueryOItm  @erpDb, @code";
                var items = conn.Query<OITM_Ext>(sq_item, new
                {
                    erpDb = db.SAPDB,
                    code = checkCode
                }).ToList();

                if (items.Count == 0)
                {
                    return BadRequest($"Scan code: {dto.Code}\nNot found in current setup.");
                }

                if (items.Count > 1)
                {
                    // return the list for user selection
                    var newDto4 = new Dto_TrcnItem
                    {
                        Message = "MultipleItemFound",
                        Items = items,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto4);
                }

                // if only one item found
                OITM_Ext item = items[0];

                // condition for item checking 
                // check the item valid in sap 
                if (item == null)
                {
                    return BadRequest($"Scan code: {dto.Code}\nNot found in current setup.");
                }

                if (item.frozenFor == "Y")
                {
                    var message = $"Scan code: {dto.Code}\n" +
                                 $"For {item.ItemCode}\n" +
                                 $"{item.ItemName}\nwas set FROZEN in system.\n\nNo return allowed.";

                    var availItems = GetAvailableItems(db);
                    if (availItems.Count == 0)
                    {
                        return BadRequest(message);
                    }

                    var newDto3 = new Dto_TrcnItem
                    {
                        Message = message,
                        Items = availItems,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto3);
                }

                if (item.validFor == "N")
                {
                    var message = $"Scan code: {dto.Code}\n" +
                                 $"for {item.ItemCode}\n" +
                                 $"{item.ItemName}\nwas set INVALID in system.\n\nNo return allowed.";

                    var availItems = GetAvailableItems(db);
                    if (availItems.Count == 0)
                    {
                        return BadRequest(message);
                    }

                    var newDto2 = new Dto_TrcnItem
                    {
                        Message = message,
                        Items = availItems,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto2);
                }

                // check the item last order - by the store
                var sq_lastOrder = @"exec sp_Cog_QueryLastOrderItem @erpDb, @itemCode, @cardCode";
                var itemLastOrder = conn.Query<CogItem>(sq_lastOrder, new
                {
                    erpDb = db.SAPDB,
                    itemCode = item.ItemCode,
                    cardCode = dto.CardCode
                }).FirstOrDefault();

                if (itemLastOrder == null) // no order at all
                {
                    // get the current price list

                    sq_lastOrder = @"exec sp_Cog_QueryCurrentItem @erpDb, @itemCode, @cardCode";
                    var itemOrder = conn.Query<CogItem>(sq_lastOrder, new
                    {
                        erpDb = db.SAPDB,
                        itemCode = item.ItemCode,
                        cardCode = dto.CardCode
                    }).FirstOrDefault();

                    if (itemOrder == null) // current price list is null
                    {
                        // build the new object for zero price 
                        // follow last order price

                        // 20240323 
                        // query the item with zero price 
                        var zeroPriceItem_sp = @$"exec sp_Cog_QueryCurrentItem_ZeroPrice @erpDb, @itemCode, @cardCode";
                        var zeroPriceItem = conn.Query<CogItem>(zeroPriceItem_sp, new
                        {
                            erpDb = db.SAPDB,
                            itemCode = item.ItemCode,
                            cardCode = dto.CardCode
                        }).FirstOrDefault();

                        if (zeroPriceItem == null)
                        {
                            return BadRequest("There is error query the item for zero price," +
                                " please contact system support for help. Thanks.");
                        }

                        var newDto5 = new Dto_TrcnItem
                        {
                            Message = "ZeroPriceList",
                            Items = null,
                            IsSuccess = true,
                            Item = zeroPriceItem
                        };

                        return Ok(newDto5); // return based on current price list
                    }

                    // follow last order price
                    var newDto4 = new Dto_TrcnItem
                    {
                        Message = "CurrentPriceList",
                        Items = null,
                        IsSuccess = true,
                        Item = itemOrder
                    };

                    return Ok(newDto4); // return based on current price list
                }

                // if there is order
                // check the order is last 6 month

                var duration = GetMonthDiff(itemLastOrder.LastInvDate, DateTime.Now);
                if (duration > numOfMonth) // more than 6 mth
                {
                    // get the store latest price list
                    sq_lastOrder = @"exec sp_Cog_QueryCurrentItem @erpDb, @itemCode, @cardCode";
                    var itemOrder = conn.Query<CogItem>(sq_lastOrder, new
                    {
                        erpDb = db.SAPDB,
                        itemCode = item.ItemCode,
                        cardCode = dto.CardCode
                    }).FirstOrDefault();

                    if (itemOrder == null)
                    {
                        var newDto2 = new Dto_TrcnItem
                        {
                            Message = "CurrentPriceList",
                            Items = null,
                            IsSuccess = true,
                            Item = itemLastOrder
                        };

                        return Ok(newDto2); // return based on currentprice list

                        //var message = $"Scan code: {dto.Code}\n" +
                        //         $"for {item.ItemCode}\n" +
                        //         $"{item.ItemName}\n having zero price list setup for Store: {dto.CardCode} " +
                        //         $"in system.\n\nNo return allowed.";

                        //return BadRequest(message);
                    }

                    var newDto1 = new Dto_TrcnItem
                    {
                        Message = "CurrentPriceList",
                        Items = null,
                        IsSuccess = true,
                        Item = itemOrder
                    };

                    return Ok(newDto1); // return based on current price list
                }

                // return the within 6 month price
                var newDto = new Dto_TrcnItem
                {
                    Message = "LastOrderPriceList",
                    Items = null,
                    IsSuccess = true,
                    Item = itemLastOrder
                };

                return Ok(newDto); // return based on current price list                
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        static int GetMonthDiff(DateTime startDate, DateTime endDate)
        {
            int monthsApart = 12 * (startDate.Year - endDate.Year) + startDate.Month - endDate.Month;
            return Math.Abs(monthsApart);
        }

        List<OITM_Ext> GetAvailableItems(DbInfo db)
        {
            try
            {
                var sql = $@"SELECT * 
                            FROM {db.SAPDB}..OITM with (nolock)
                            WHERE INVNTITEM = 'Y' 
                                AND ISNULL(U_NONSTK,'') = 'Y' 
                                AND SellItem = 'Y' 
                                AND FROZENFOR = 'N'";

                return new SqlConnection(_commDbConnStr).Query<OITM_Ext>(sql).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult ReasonCodes(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User Code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectReasonCode @erpDb, @webDb, @userCode, @subsi";

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<ReasonCode>(sql,
                    new
                    {
                        erpDb = db.SAPDB,
                        webDb = db.WEBDB,
                        userCode = dto.UserCode,
                        subsi = db.COMPANYNAME
                    }).ToList();

                if (res != null && res.Count > 0) return Ok(res);
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ReasonCodes_Delivery(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectReasonCode_D @erpDb ";

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<ReasonCode>(sql,
                    new
                    {
                        erpDb = db.SAPDB
                    }).ToList();

                if (res.Count == 0) // if delivery app reason code not found // the use normal reason code 
                {
                    sql = "exec sp_SelectReasonCode @erpDb, @webDb, @userCode, @subsi";
                    conn = new SqlConnection(_commDbConnStr);
                    res = conn.Query<ReasonCode>(sql,
                       new
                       {
                           erpDb = db.SAPDB,
                           webDb = db.WEBDB,
                           userCode = dto.UserCode,
                           subsi = db.COMPANYNAME
                       }).ToList();

                    if (res.Count == 0) return NotFound();
                    return Ok(res);
                }

                return Ok(res); // return the delivery reason code
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCOG(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Card code is empty");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start query date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end query date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectCog @webDb, @cardCode, @startDt, @endDt, @subsi";
                //@webDb as nvarchar(100), 
                //@cardCode as nvarchar(100), 
                //@startDt as datetime, 
                //@endDt as datetime

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<COG_Doc>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        cardCode = dto.CardCode,
                        startDt = dto.StartDt,
                        endDt = dto.EndDt,
                        subsi = db.COMPANYNAME
                    }).ToList();

                if (res != null && res.Count > 0) return Ok(res);
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCOGLines(Dto_Cog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Sub si is empty");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectCogLines @webDb, @subsi, @docEntry";
                //@webDb as nvarchar(100), 
                //@cardCode as nvarchar(100), 
                //@startDt as datetime, 
                //@endDt as datetime

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Query<COG_Line>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        subsi = db.COMPANYNAME,
                        docEntry = dto.DocEntry
                    }).ToList();

                if (res != null && res.Count > 0) return Ok(res);
                return NotFound();
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



//IActionResult COGOldItem (Dto_Cog dto)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(dto.CompanyID))
//        {
//            return BadRequest("Company id is empty");
//        }
//        if (string.IsNullOrWhiteSpace(dto.QueryKeys))
//        {
//            return BadRequest("token is empty");
//        }

//        var client = new RestClient($"{WebHostAddrEndPoint}/master/{dto.CompanyID}/COGOldItem");
//        client.Timeout = -1;
//        var request = new RestRequest(Method.GET);
//        request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
//        IRestResponse response = client.Execute(request);

//        if (response.IsSuccessful)
//        {
//            var result = JsonConvert.DeserializeObject<List<OldCode>>(response.Content);
//            return Ok(result);
//        }
//        return NotFound();
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}

//IActionResult ReasonCodes_WebApi (Dto_Cog dto)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(dto.CompanyID))
//        {
//            return BadRequest("Company id is empty");
//        }
//        if (string.IsNullOrWhiteSpace(dto.QueryKeys))
//        {
//            return BadRequest("token is empty");
//        }

//        var client = new RestClient($"{WebHostAddrEndPoint}/master/{dto.CompanyID}/COGReason");
//        client.Timeout = -1;
//        var request = new RestRequest(Method.GET);
//        request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
//        IRestResponse response = client.Execute(request);

//        if (response.IsSuccessful)
//        {
//            var result = JsonConvert.DeserializeObject<List<ReasonCode>>(response.Content);
//            return Ok(result);
//        }
//        return NotFound();
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}

//async Task<IActionResult> COGITEM(Dto_Cog dto)
//{
//    try
//    {
//        if (dto.QueryItems == null)
//        {
//            return BadRequest("Request item master is empty");

//        }
//        if (dto.QueryKeys == null)
//        {
//            return BadRequest("Request keys is empty");
//        }

//        if (dto.QueryCompanyID == null)
//        {
//            return BadRequest("Request company id is empty");
//        }

//        using (var httpclient = new HttpClient())
//        {
//            var json = JsonConvert.SerializeObject(dto.QueryItems);
//            var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

//            //http://10.0.0.12:82/Master/Z14/ItemMaster
//            var uri = new Uri($"{WebHostAddrEndPoint}Master/{dto.QueryCompanyID}/{dto.Request}");

//            httpclient.DefaultRequestHeaders.Authorization =
//                    new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

//            var response = await httpclient.PostAsync(uri, stringContent);
//            var isSuccessStatusCode = response.IsSuccessStatusCode;
//            var lastStatusCode = response.StatusCode;

//            if (isSuccessStatusCode)
//            {
//                var content = await response.Content.ReadAsStringAsync();
//                if (content == null) return NotFound();

//                var item = JsonConvert.DeserializeObject<CogItem>(content);
//                return Ok(item);
//            }

//            return NotFound();
//        }
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}