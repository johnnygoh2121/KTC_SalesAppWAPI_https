using KTC_SalesAppWAPI.DTOs.COG;
using KTC_SalesAppWAPI.DTOs.WhsReturn;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.GRPO;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.WhsReturn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using OINV = KTC_SalesAppWAPI.Models.Pick.OINV;
using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Helpers.Delivery;
using System.Threading;
using System.Threading.Tasks;
using KTC_SalesAppWAPI.Models.COG;
using System.Data;
using KTC_SalesAppWAPI.Models.WebPortal;
using System.Reflection;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WhsReturnController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        //readonly string APP_JSON = "application/json";
        readonly ILogger<WhsReturnController> _logger;
        string WebHostAddrEndPoint = "";
        string _commDbConnStr = "";
        string LastError = "";

        public WhsReturnController(IConfiguration configuration, ILogger<WhsReturnController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(WhsRet_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetCnAndLines":
                    {
                        return GetCnAndLines(dto);
                    }
                case "GetInvAndLines":
                    {
                        return GetInvAndLines(dto);
                    }
                case "GetCnAndLines_Redo":
                    {
                        return GetCnAndLines(dto, true);
                    }
                case "GetInvAndLines_Redo":
                    {
                        return GetInvAndLines_Redo(dto, true);
                    }
                case "SaveWReturn":
                    {
                        return SaveWReturn(dto);
                    }
                case "SaveWReturn_Inv":
                    {
                        return SaveWReturn_Inv(dto);
                    }
                case "UpdateDocSign":
                    {
                        return UpdateDocSign(dto);
                    }
                case "GetWRtns":
                    {
                        return GetWRtns(dto);
                    }
                case "GetWRtns_Inv":
                    {
                        return GetWRtns_Inv(dto);
                    }
                case "GenerateSenderCode":
                    {
                        return GenerateSenderVerifyCode(dto);
                    }
                case "GetWhsRtnVerifyQrCode": // for sales app
                    {
                        return GetWhsRtnVerifyQrCode(dto);
                    }
                case "GetWhsRtnVerifyQrCode_Inv":
                    {
                        return GetWhsRtnVerifyQrCode_Inv(dto);
                    }
                case "CheckAllowReturn":
                    {
                        return CheckAllowReturn();
                    }
                case "TrCnTracking":
                    {
                        return TrCnTracking(dto);
                    }
                case "SaveDraft_RtnLines":
                    {
                        return SaveDraft_RtnLines(dto);
                    }
                case "SaveDraft_RtnLines_Inv":
                    {
                        return SaveDraft_RtnLines_Inv(dto);
                    }
                case "LoadDraftLines":
                    {
                        return LoadDraftLines(dto);
                    }
                case "LoadDraftLines_Inv":
                    {
                        return LoadDraftLines_Inv(dto);
                    }
                //case "RemoveRetDocDraft": // disable to prevent rtn being delete 
                //    {
                //        return RemoveRetDocDraft(dto);
                //    }
                default:
                    {
                        return BadRequest("no recognised request " + dto.Request);
                    }
            }
        }

        //IActionResult RemoveRetDocDraft(WhsRet_Dto dto)
        //{

        //    return Ok();

            //if (string.IsNullOrWhiteSpace(dto.SubSi))
            //{
            //    return BadRequest("Invalid subsi");
            //}
            //if (dto.RetDocEntry <= 0)
            //{
            //    return BadRequest("Invalid return draft doc entry");
            //}

            //var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            //if (db == null)
            //{
            //    return BadRequest("Invalid dbi");
            //}

            //using var conn = new SqlConnection(_commDbConnStr);
            //conn.Open();
            //using var trans = conn.BeginTransaction();

            //try
            //{
            //    var sql = $@"Delete from {db.WEBDB}..RTN Where DocEntry = @docEntry and DocStatus = 'D';
            //                 Delete from {db.WEBDB}..RTN1 Where DocEntry = @docEntry;
            //                 Delete from {db.WEBDB}..RTN2 Where DocEntry = @docEntry;  ";

            //    conn.Execute(sql, new { docEntry = dto.RetDocEntry }, trans);
            //    trans.Commit();
            //    return Ok();
            //}
            //catch (Exception e)
            //{
            //    trans.Rollback();
            //    LastError = $"{e.Message}\n{e.StackTrace}";
            //    _logger.LogError(LastError);
            //    return BadRequest($"request not handler.\n{LastError}");
            //}
        //}

        IActionResult LoadDraftLines_Inv(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("subsi is empty");
                }
                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("doc entry empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }
                var query = $@"Select *
                               From {db.WEBDB}..FTAPP_WRTN1_INV_DRAFT 
                               Where InvEntry = @InvEntry";

                var res = new SqlConnection(_commDbConnStr).Query<WhsRtn1_Inv>(query, new { InvEntry = dto.InvDocEntry }).ToList();

                if (res.Count == 0) return NotFound();

                res.ForEach(x =>
                {
                    if (x.LineGuid == default) x.LineGuid = Guid.NewGuid();
                });

                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadDraftLines(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("subsi is empty");
                }
                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("doc entry empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }
                var query = $@"Select *
                               From {db.WEBDB}..FTAPP_WRTN1_DRAFT 
                               Where CnEntry = @CnEntry";

                var res = new SqlConnection(_commDbConnStr).Query<WhsRtn1>(query, new { CnEntry = dto.CnDocEntry }).ToList();

                if (res == null) return NotFound();

                res.ForEach(x =>
                   {
                       if (x.LineGuid == default) x.LineGuid = Guid.NewGuid();
                   });

                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDraft_RtnLines_Inv(WhsRet_Dto dto)
        {

            if (dto.RtnLines_Inv_Draft == null)
            {
                return BadRequest("draft line empty");
            }
            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("subsi is empty");
            }
            if (dto.InvDocEntry < 0)
            {
                return BadRequest("doc entry empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("dbi is empty");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the draft
                var delete_sq = $@"delete from {db.WEBDB}..FTAPP_WRTN1_INV_DRAFT 
                                   Where InvEntry = @InvEntry";

                conn.Execute(delete_sq, new { InvEntry = dto.InvDocEntry }, trans);

                for (int x = 0; x < dto.RtnLines_Inv_Draft.Count; x++)
                {
                    var line = dto.RtnLines_Inv_Draft[x];
                    if (line == null) continue;

                    // insert the draft
                    var insert_sq = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN1_INV_DRAFT (
                                             InvEntry
                                           , InvLine
                                           , InvDocNum
                                           , CardCode
                                           , CardName
                                           , InvPrice
                                           , InvQty
                                           , UserCode
                                           , UserName
                                           , RtnDt
                                           , RtnQty
                                           , Subsi
                                           , SubsiID
                                           , ItemCode
                                           , ItemName
                                           , ScanInCode
                                           , LineGuid
                                           , LotNo                                          
                                           , Reason
                                           , WhsCode
                                           , BarcodeStr
                                           , InvIssueQty
                                           , ReceivedQty
                                           , VarianceQty
                                           , Remarks
                                           , Suppcatnum
                                           , UOMQTY
                                           , CodeBars
                                           , QtyInPcs
                                           , QtyPc
                                           , QtyCs 
                                           , ManBtchNum 
                                           , Remark ";

                    var sql_val = @") VALUES (
                                              @InvEntry
                                             ,@InvLine
                                             ,@InvDocNum
                                             ,@CardCode
                                             ,@CardName
                                             ,@InvPrice
                                             ,@InvQty
                                             ,@UserCode
                                             ,@UserName
                                             ,@RtnDt
                                             ,@RtnQty
                                             ,@Subsi
                                             ,@SubsiID
                                             ,@ItemCode
                                             ,@ItemName
                                             ,@ScanInCode
                                             ,@LineGuid
                                             ,@LotNo                                             
                                             ,@Reason
                                             ,@WhsCode
                                             ,@BarcodeStr
                                             ,@InvIssueQty
                                             ,@ReceivedQty
                                             ,@VarianceQty
                                             ,@Remarks
                                             ,@Suppcatnum
                                             ,@UOMQTY
                                             ,@CodeBars
                                             ,@QtyInPcs
                                             ,@QtyPc
                                             ,@QtyCs
                                             ,@ManBtchNum 
                                             ,@Remark ";


                    if (line.MfrDate != default)
                    {
                        insert_sq += ", MfrDate ";
                        sql_val += ", @MfrDate ";
                    }
                    if (line.ExpDate != default)
                    {
                        insert_sq += ", ExpDate ";
                        sql_val += ", @ExpDate ";
                    }

                    var final_insert = @$"{insert_sq}{sql_val} )";
                    var res = conn.Execute(final_insert, line, trans);
                }

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

        // for auto save the draft and load
        IActionResult SaveDraft_RtnLines(WhsRet_Dto dto)
        {
            if (dto.RtnLines_Draft == null)
            {
                return BadRequest("draft line empty");
            }
            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("subsi is empty");
            }
            if (dto.CnDocEntry < 0)
            {
                return BadRequest("doc entry empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("dbi is empty");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the draft
                var delete_sq = $@"delete from {db.WEBDB}..FTAPP_WRTN1_DRAFT 
                                   Where CnEntry = @CnEntry";
                conn.Execute(delete_sq, new { CnEntry = dto.CnDocEntry }, trans);

                for (int x = 0; x < dto.RtnLines_Draft.Count; x++)
                {
                    var line = dto.RtnLines_Draft[x];
                    if (line == null) continue;

                    // insert the draft
                    var insert_sq = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN1_DRAFT (
                                             CnEntry
                                           , CnLine
                                           , CnDocNum
                                           , CardCode
                                           , CardName
                                           , CnPrice
                                           , CnQty
                                           , UserCode
                                           , UserName
                                           , RtnDt
                                           , RtnQty
                                           , Subsi
                                           , SubsiID
                                           , ItemCode
                                           , ItemName
                                           , ScanInCode
                                           , LineGuid
                                           , LotNo                                          
                                           , Reason
                                           , WhsCode
                                           , BarcodeStr
                                           , CnIssueQty
                                           , ReceivedQty
                                           , VarianceQty
                                           , Remarks
                                           , Suppcatnum
                                           , UOMQTY
                                           , CodeBars
                                           , QtyInPcs
                                           , QtyPc
                                           , QtyCs 
                                           , ManBtchNum 
                                           , Remark
                                           , U_SRQTY 
                                           , ActAvailReturnQty
";
                    var sql_val = @") VALUES (
                                              @CnEntry
                                             ,@CnLine
                                             ,@CnDocNum
                                             ,@CardCode
                                             ,@CardName
                                             ,@CnPrice
                                             ,@CnQty
                                             ,@UserCode
                                             ,@UserName
                                             ,@RtnDt
                                             ,@RtnQty
                                             ,@Subsi
                                             ,@SubsiID
                                             ,@ItemCode
                                             ,@ItemName
                                             ,@ScanInCode
                                             ,@LineGuid
                                             ,@LotNo                                             
                                             ,@Reason
                                             ,@WhsCode
                                             ,@BarcodeStr
                                             ,@CnIssueQty
                                             ,@ReceivedQty
                                             ,@VarianceQty
                                             ,@Remarks
                                             ,@Suppcatnum
                                             ,@UOMQTY
                                             ,@CodeBars
                                             ,@QtyInPcs
                                             ,@QtyPc
                                             ,@QtyCs
                                             ,@ManBtchNum 
                                             ,@Remark
                                             ,@U_SRQTY 
                                             ,@ActAvailReturnQty ";

                    if (line.MfrDate != default)
                    {
                        insert_sq += ", MfrDate ";
                        sql_val += ", @MfrDate ";
                    }
                    if (line.ExpDate != default)
                    {
                        insert_sq += ", ExpDate ";
                        sql_val += ", @ExpDate ";
                    }

                    var final_insert = @$"{insert_sq}{sql_val} )";
                    var res = conn.Execute(final_insert, line, trans);
                }

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

        IActionResult TrCnTracking(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Invalid company id");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start query date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end query date");
                }
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.CompanyId);
                if (db == null)
                {
                    return BadRequest("Invalid company id");
                }

                var sp_query = @"exec sp_QueryTrCntracking @webDb, @starDt, @endDt";
                var results = new SqlConnection(_commDbConnStr).Query<TrCNTracking>(sp_query, new
                {
                    webDb = db.WEBDB,
                    starDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

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

        IActionResult CheckAllowReturn()
        {
            try
            {
                var query = @"Select SetupValue from KTCW_COMMON..FTApp_Config 
                              Where SetupName= 'WhsZeroTrCnDayInMonth' ";

                var conn = new SqlConnection(_commDbConnStr);
                var setupVal = conn.ExecuteScalar<string>(query);

                if (string.IsNullOrWhiteSpace(setupVal)) return Ok();
                if (setupVal == "-1") return Ok();

                var isNumeric = int.TryParse(setupVal, out int result);
                if (isNumeric)
                {
                    var today = DateTime.Now.Day;
                    if (today >= result) //20211215 for the day onward are not allow
                    {
                        return BadRequest($"Whs return zero day on {result:N0} every month, no return allowed");
                    }
                }
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // use by the sales app to get the qr code for whs app to scan
        IActionResult GetWhsRtnVerifyQrCode(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                //if (string.IsNullOrWhiteSpace(dto.UserCode))
                //{
                //    return BadRequest("Invalid whs user code");
                //}
                if (dto.CnDocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.CnDocNum))
                {
                    return BadRequest("Invalid doc num");
                }
                if (string.IsNullOrWhiteSpace(dto.Operation))
                {
                    return BadRequest("Invalid opr name");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var query = $@" Select * from {db.WEBDB}..FTAPP_SecretCodes  
                                 Where Subsi = @Subsi 
                                  --and SenderUserCode = @UserCode
                                  and DocEntry = @CnDocEntry 
                                  and DocNum = @CnDocNum
                                  and Operation = @Operation ";

                var res = new SqlConnection(_commDbConnStr).Query<FTAPP_SecretCodes>(query, new
                {
                    Subsi = dto.SubSi,
                    UserCode = dto.UserCode,
                    CnDocEntry = dto.CnDocEntry,
                    CnDocNum = dto.CnDocNum,
                    Operation = dto.Operation
                }).FirstOrDefault();

                if (res == null) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // use by the sales app to get the qr code for whs app to scan
        IActionResult GetWhsRtnVerifyQrCode_Inv(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid whs user code");
                }
                if (dto.InvDocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.InvDocNum))
                {
                    return BadRequest("Invalid doc num");
                }
                if (string.IsNullOrWhiteSpace(dto.Operation))
                {
                    return BadRequest("Invalid opr name");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var query = $@" Select * from {db.WEBDB}..FTAPP_SecretCodes  
                                 Where Subsi = @Subsi 
                                  and SenderUserCode = @UserCode
                                  and DocEntry = @InvDocEntry 
                                  and DocNum = @InvDocNum
                                  and Operation = @Operation ";

                var res = new SqlConnection(_commDbConnStr).Query<FTAPP_SecretCodes>(query, new
                {
                    Subsi = dto.SubSi,
                    UserCode = dto.UserCode,
                    InvDocEntry = dto.InvDocEntry,
                    InvDocNum = dto.InvDocNum,
                    Operation = dto.Operation
                }).FirstOrDefault();

                if (res == null) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GenerateSenderVerifyCode(WhsRet_Dto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid whs user code");
            }
            if (string.IsNullOrWhiteSpace(dto.ReturnSenderName))
            {
                return BadRequest("Invalid sender user name");
            }
            if (string.IsNullOrWhiteSpace(dto.ReturnSenderCode))
            {
                return BadRequest("Invalid sender user code");
            }
            if (dto.CnDocEntry < 0)
            {
                return BadRequest("Invalid doc entry");
            }
            if (string.IsNullOrWhiteSpace(dto.CnDocNum))
            {
                return BadRequest("Invalid doc num");
            }
            if (string.IsNullOrWhiteSpace(dto.Operation))
            {
                return BadRequest("Invalid opr name");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid db info");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 20220105 
                // delete the record by docentry
                // always maintain 1 rec 
                var delete_sql = @$"delete from {db.WEBDB}..FTAPP_SecretCodes 
                                  where DocEntry = @DocEntry                                  
                                  and Operation = @Operation ";

                conn.Execute(delete_sql, new
                {
                    DocEntry = dto.CnDocEntry,
                    Operation = dto.Operation
                }, trans);

                // update into the temp table
                var newSecret = new FTAPP_SecretCodes
                {
                    Subsi = dto.SubSi,
                    SenderUserCode = dto.ReturnSenderCode,
                    SenderUserName = dto.ReturnSenderName,
                    WhsUserCode = dto.UserCode,
                    DocEntry = dto.CnDocEntry,
                    DocNum = dto.CnDocNum,
                    Operation = dto.Operation,
                    SecretCode = Guid.NewGuid()
                };

                var insert_sql = $@"INSERT INTO {db.WEBDB}..[FTAPP_SecretCodes] ( 
                                         Subsi
                                       , WhsUserCode
                                       , SenderUserCode
                                       , SenderUserName
                                       , TransDt
                                       , DocEntry
                                       , DocNum
                                       , Operation
                                       , SecretCode
                                        ) VALUES (
                                        @Subsi
                                      , @WhsUserCode
                                      , @SenderUserCode
                                      , @SenderUserName
                                      , GETDATE()
                                      , @DocEntry
                                      , @DocNum
                                      , @Operation
                                      , @SecretCode 
                                     )";

                var res = conn.Execute(insert_sql, newSecret, trans);
                trans.Commit();
                return Ok(newSecret);
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetWRtns_Inv(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid query start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid query end date");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                // u are here
                // sp_SelectWRTN_Inv
                var sp_query = @"exec sp_SelectWRTN_Inv @webDb, @subsi, @subsiID, @startDt, @endDt, @userCode";
                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_WRTN_INV>(sp_query, new
                {
                    webDb = db.WEBDB,
                    subsi = db.COMPANYNAME,
                    subsiID = db.COMPANYID,
                    startDt = $"{dto.StartDt:yyyy-MM-dd}",
                    endDt = $"{dto.EndDt:yyyy-MM-dd}",
                    userCode = dto.UserCode
                }).ToList();

                if (results?.Count == 0) return NotFound();

                // load the doc line to app 
                for (int d = 0; d < results.Count; d++)
                {
                    var doc = results[d];
                    if (doc == null) continue;

                    // load it line from tabale 
                    var sql_loadLine = $"Select * from {db.WEBDB}..FTAPP_WRTN1_INV with (nolock) " +
                                       $"Where InvEntry = @InvDocEntry";

                    results[d].Lines = conn.Query<FTAPP_WRTN1_INV>(sql_loadLine, new { doc.InvDocEntry }).ToList();
                }

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetWRtns(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid query start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid query end date");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var sp_query = @"exec sp_SelectWRTN @webDb, @subsi, @subsiID, @startDt, @endDt";
                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_WRTN>(sp_query, new
                {
                    webDb = db.WEBDB,
                    subsi = db.COMPANYNAME,
                    subsiID = db.COMPANYID,
                    startDt = $"{dto.StartDt:yyyy-MM-dd}",
                    endDt = $"{dto.EndDt:yyyy-MM-dd}",
                }).ToList();

                if (results?.Count == 0) return NotFound();

                // load the doc line to app 
                for (int d = 0; d < results.Count; d++)
                {
                    var doc = results[d];
                    if (doc == null) continue;

                    // load it line from tabale 
                    var sql_loadLine = $"Select * from {db.WEBDB}..FTAPP_WRTN1 with (nolock) " +
                                       $"Where CnEntry = @CnDocEntry";

                    results[d].Lines = conn.Query<FTAPP_WRTN1>(sql_loadLine, new { doc.CnDocEntry }).ToList();
                }

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateDocSign(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.SignFiles))
                {
                    return BadRequest("Invalid files names");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                // query to get the prevous file, and update will later
                // 2021-07-18 add and update file from app 
                // check the exiting append in the file name behind
                var sqlWRtnFiles = $@"SELECT Signed
                                          FROM [{db.WEBDB}]..[FTAPP_WRTN] with (nolock)  
                                          WHERE CnDocEntry = @CnDocEntry";

                var existingFiles = conn.ExecuteScalar<string>(sqlWRtnFiles, new { CnDocEntry = dto.CnDocEntry });
                if (!string.IsNullOrWhiteSpace(existingFiles))
                {
                    existingFiles += "," + dto.SignFiles;
                }
                else
                {
                    existingFiles = dto.SignFiles;
                }

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                var sql = @$"UPDATE [{db.WEBDB}]..[FTAPP_WRTN] 
                                SET Signed = @Signed
                                WHERE CnDocEntry = @CnDocEntry";

                var result = conn.Execute(sql, new { CnDocEntry = dto.CnDocEntry, Signed = existingFiles }, trans);
                if (result <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error update FTAPP_WRTN Sign file, subsi {db.COMPANYNAME}, " +
                        $"CN Entry : {dto.CnDocEntry}");
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


        IActionResult SaveWReturn_Inv(WhsRet_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvDocNum))
                {
                    return BadRequest("invalid invoice num");
                }
                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("invalid invoice entry");
                }
                if (dto.RetMemo_ByInv == null)
                {
                    return BadRequest("Return doc is empty");
                }
                if (dto.InvHead == null)
                {
                    return BadRequest("Return doc (1) is empty");
                }
                if (dto.InvDetails == null)
                {
                    return BadRequest("Return doc (2) is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("invalid update type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // create Cn for the store
                //string errMessage = "";
                var IsCnCreateForStore = PostCreateCnForStore_Draft(db, dto, out string errMessage);
                if (IsCnCreateForStore == null)
                {
                    if (errMessage.ToLower().Contains("This Return already exist, please refer no".ToLower()))
                    {
                        var rtnNo = "";
                        var textArray = errMessage.Split(" ");
                        if (textArray.Length == 0)
                        {
                            return BadRequest($"{db.COMPANYNAME}, " +
                                            $"Save return invoice {dto.InvDocNum} fail, " +
                                            $"DLB: {dto.InvHead.DlbEntry} pls try again. [Message] \n{errMessage}");
                        }

                        rtnNo = textArray[textArray.Length - 1].Trim(); // get the last text
                        if (string.IsNullOrWhiteSpace(rtnNo))
                        {
                            return BadRequest($"{db.COMPANYNAME}, " +
                                            $"Save return invoice {dto.InvDocNum} fail, " +
                                            $"DLB: {dto.InvHead.DlbEntry} pls try again. [Message] \n{errMessage}");
                        }

                        var isNum = decimal.TryParse($"{rtnNo}".Trim(), out var num);
                        if (!isNum)
                        {
                            return BadRequest($"{db.COMPANYNAME}, " +
                                              $"Save return invoice {dto.InvDocNum} fail, " +
                                              $"DLB: {dto.InvHead.DlbEntry} pls try again. [Message] \n{errMessage}");
                        }

                        // requery the check for created RTN and Credit memo num
                        // 20240817
                        var sp_FindRtn = @$"select t0.DOCENTRY [actionResult], t1.DocNum [CreditMemoDocNum] 
                                                  from {db.WEBDB}..RTN t0 
                                            inner join {db.SAPDB}..ORIN t1 on t1.DocEntry = t0.CMENTRY
                                            where t0.DocEntry = @DocEntry ";

                        var connCheckRtn = new SqlConnection(_commDbConnStr);
                        IsCnCreateForStore = connCheckRtn.Query<ReturnCogResult>(sp_FindRtn,
                            new { DocEntry = num }).FirstOrDefault();

                        goto ProcessSuccess;
                    }

                    return BadRequest($"{db.COMPANYNAME}, " +
                                      $"Save return invoice {dto.InvDocNum} fail, " +
                                      $"DLB: {dto.InvHead.DlbEntry} pls try again. [Message] \n{errMessage}");
                }

            ProcessSuccess:

                if (dto.UpdateType == "S") // if save as submit // update the ret inv status
                {
                    // 20250910
                    if (IsCnCreateForStore == null)
                    {
                        return BadRequest("Inv return post create Cn fail, please try again. [10x]");
                    }

                    // update the return status to return
                    using (var conn = new SqlConnection(_commDbConnStr))
                    {
                        // =====================================
                        // 20240917
                        // check cn number by rtn docentry vs U_SOID

                        var sp_QuerySapCn = $@"Select top 1 * 
                                               From {db.SAPDB}..ORIN with (nolock) 
                                               Where U_SOID = @docEntry
                                               order by DocDate desc; ";

                        var found_Cn = conn.Query<ORIN>(sp_QuerySapCn, new
                        {
                            docEntry = IsCnCreateForStore.actionResult
                        }).FirstOrDefault();

                        if (found_Cn == null)
                        {
                            // post fail
                            return BadRequest("Inv return post create Cn fail, please try again. [11x]");
                        }
                        // =====================================

                        if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                var update_sp = @$"update {db.WEBDB}..FTAPP_RET_THR_INV
                                                    set DocStatus = @DocStatus
                                                    Where InvNum = @InvDocNum 
                                                    and InvEntry = @InvDocEntry 
                                                    and DlbEntry = @DlbEntry; ";

                                var UpdateRes = conn.Execute(update_sp, new
                                {
                                    DocStatus = "Returned",
                                    InvDocNum = dto.InvDocNum,
                                    InvDocEntry = dto.InvDocEntry,
                                    DlbEntry = dto.InvHead.DlbEntry
                                }, trans);

                                if (UpdateRes < 0)
                                {
                                    trans.Rollback();
                                    return BadRequest($"{db.COMPANYNAME} DocNum: {dto.InvDocNum}, DocType:I, " +
                                                     $"DLB:{dto.InvHead.DlbEntry}. Update FTAPP_RET_THR_INV status returned fail.");
                                }

                                // remove the draft line
                                var delete_draft_inv1 = @$"Delete from {db.WEBDB}..FTAPP_WRTN1_INV_DRAFT
                                                   Where InvDocNum = @InvDocNum 
                                                   and InvEntry = @InvDocEntry";

                                var delete_draft_res = conn.Execute(delete_draft_inv1, new
                                {
                                    InvDocNum = dto.InvDocNum,
                                    InvDocEntry = dto.InvDocEntry
                                }, trans);

                                if (delete_draft_res < 0)
                                {
                                    trans.Rollback();
                                    return BadRequest($"{db.COMPANYNAME} DocNum: {dto.InvDocNum}, DocType:I, " +
                                                     $"DLB:{dto.InvHead.DlbEntry}. Delete FTAPP_WRTN1_INV_DRAFT fail.");
                                }

                                // save the return invoice details and head
                                var isSave = SaveInvApp(dto, db, conn, trans);
                                if (!isSave)
                                {
                                    trans.Rollback();
                                    LastError = $@"Save return invoice app data error. Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}. Delete/Insert FTAPP_WRTN_INV + INV1 fail.";

                                    _logger.LogError(LastError);
                                    return BadRequest(LastError);
                                }

                                isSave = SaveHrCharge_Inv(dto, db, conn, trans);
                                if (!isSave)
                                {
                                    trans.Rollback();
                                    LastError = $@"Save return invoice hr charge data error. Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}. Delete/Insert FTAPP_InvSummary1 fail.";

                                    _logger.LogError(LastError);
                                    return BadRequest(LastError);
                                }

                                if (IsCnCreateForStore != null)
                                { // 20220617
                                  // create the link record for later listing loadin
                                    var newLink = new FTAPP_RtnInvCnDlbLink
                                    {
                                        DlbEntry = dto.InvHead.DlbEntry,
                                        InvNum = dto.InvHead.InvDocNum,
                                        RtnEntry = int.Parse(IsCnCreateForStore.actionResult),
                                        CnNum = int.Parse(IsCnCreateForStore.CreditMemoDocNum)
                                    };

                                    var insertLink_sql = $@"Insert into 
                                                        {db.WEBDB}..FTAPP_RtnInvCnDlbLink ( 
                                            DlbEntry 
                                            ,InvNum   
                                            ,RtnEntry 
                                            ,CnNum    
                                            ,TransDt 
                                        ) values (
                                             @DlbEntry 
                                            ,@InvNum   
                                            ,@RtnEntry 
                                            ,@CnNum    
                                            ,GETDATE() )";

                                    var insert_linked_res = conn.Execute(insertLink_sql, newLink, trans);
                                    if (insert_linked_res <= 0)
                                    {
                                        trans.Rollback();
                                        LastError = $@"Insert Dlb Invoice Return ref. link error. Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}. Insert FTAPP_RtnInvCnDlbLink fail.";

                                        _logger.LogError(LastError);
                                        return BadRequest(LastError);
                                    }
                                }

                                trans.Commit();
                            }
                            catch (Exception e)
                            {
                                trans.Rollback();
                                LastError = $"{e.Message}\n{e.StackTrace}";
                                _logger.LogError(LastError);
                                return BadRequest(LastError);
                            }
                        } // close of trans
                    } // close of conn


                    // 20220627
                    // check header got varient 
                    // and peform sap gi from the cn 

                    if (dto.InvHead.HasVarient && IsCnCreateForStore != null)
                    {
                        using var conn1 = new SqlConnection(_commDbConnStr); // intial new connection
                        var query_Charge = $@"Select * from {db.WEBDB}..FTAPP_InvSummary1 with (nolock)
                                              Where InvDocEntry = @InvDocEntry ";

                        var charge = conn1.Query<InvSummary>(query_Charge,
                            new
                            {
                                InvDocEntry = dto.InvHead.InvDocEntry
                            }).FirstOrDefault();

                        if (charge != null)
                        {
                            IsCnCreateForStore.HrChargeAmt = charge.ChargeAmt;
                            IsCnCreateForStore.ChargedUserCode = charge.UserCode;
                            IsCnCreateForStore.ChargedUserName = charge.UserName;
                        }

                        var queryInvHead = @$"select 
                                              '{db.COMPANYNAME}' [Subsi]
                                            , '{db.COMPANYID}' [SubsiId]
                                            , t0.* 
                                            , t1.InvNum 
                                            , t1.CnNum
                                            , t1.RtnEntry
                                            , t2.DocEntry [CnEntry]
                                            from      {db.WEBDB}..FTAPP_WRTN_INV t0 with (nolock)
                                            left join {db.WEBDB}..FTAPP_RtnInvCnDlbLink t1 with (nolock) on t1.InvNum = t0.InvDocNum 
						   				                                                                and t1.DlbEntry = t0.DlbEntry
                                            left join {db.SAPDB}..ORIN t2 with (nolock) on t2.DocNum = t1.CnNum										   
                                            Where InvDocEntry = '{dto.InvHead.InvDocEntry}'";

                        var invHead = conn1.Query<FTAPP_WRTN_INV>(queryInvHead).FirstOrDefault();
                        if (invHead == null)
                        {
                            return BadRequest($@"Error query the inv head for GI process, 
                                                  Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}.");
                        }

                        var queryInvLine = @$" select (t0.InvQty - t0.RtnQty) [VarientQty]  
                                            , t0.* 
                                            from {db.WEBDB}..FTAPP_WRTN1_INV t0 with (nolock) 
                                            Where InvEntry = '{dto.InvHead.InvDocEntry}'
                                            and   (t0.InvQty - t0.RtnQty) > 0";

                        var invLines = conn1.Query<FTAPP_WRTN1_INV>(queryInvLine).ToList();
                        if (invLines.Count == 0)
                        {
                            return BadRequest($@"Error query the inv lines for GI process,  Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}");
                        }

                        var giHelper = new DeliveryDiApi_RetThruInv(_commDbConnStr, db, invHead, invLines);
                        var giRes = giHelper.CreateGoodIssue();
                        if (!string.IsNullOrWhiteSpace(giRes))
                        {
                            return BadRequest($@"Post create GI based on Ret thru inv, with error : {giRes}, 
                                                  Info: {db.COMPANYNAME}
                                                  DocNum: {dto.InvDocNum}, DocType: I, 
                                                  DLB: {dto.InvHead.DlbEntry}.");
                        }
                        // else
                        IsCnCreateForStore.GIDocNum = $"{giHelper.PostedDocNum}"; // handler null GI
                    }
                }

                // need to update the create cn 

                // return the save result as draft or submit 
                return Ok(IsCnCreateForStore);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        ReturnCogResult PostCreateCnForStore_Draft(DbInfo db, WhsRet_Dto dto, out string errMessage)
        {
            errMessage = "";
            try
            {
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}ReturnCOG/{dto.QueryCompanyID}/draft");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.RetMemo_ByInv);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    var result1 = JsonConvert.DeserializeObject<ReturnCogResult>(response.Content);
                    errMessage = result1 == null ? response.Content : result1.errorMessage;
                    return null;
                }

                var content = response.Content;
                var result = JsonConvert.DeserializeObject<ReturnCogResult>(content);
                if (result != null)
                {
                    if (dto.UpdateType == "D")
                    {
                        result.updateDocType = "D";
                        return result;
                    }

                    // else perform the post submit
                    dto.RetMemo_ByInv.Docentry = long.Parse(result.actionResult);
                    dto.RetMemo_ByInv.Docnum = long.Parse(result.actionResult);
                    return PostCreateCnForStore_Submit(db, dto, out errMessage); // post again as submit
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        ReturnCogResult PostCreateCnForStore_Submit(DbInfo db, WhsRet_Dto dto, out string errMessage)
        {
            errMessage = "";
            try
            {
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}ReturnCOG/{dto.QueryCompanyID}/Submit");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.RetMemo_ByInv);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                if (!response.IsSuccessful)
                {
                    var result1 = JsonConvert.DeserializeObject<ReturnCogResult>(response.Content);
                    errMessage = result1 == null ? response.Content : result1.errorMessage;
                    return null;
                }

                var result = JsonConvert.DeserializeObject<ReturnCogResult>(response.Content);
                if (result == null)
                {
                    errMessage = "result is null/ empty";
                    return null;
                }

                // once success
                dto.RetMemo_ByInv.Docentry = long.Parse(result.actionResult);
                dto.RetMemo_ByInv.Docnum = long.Parse(result.actionResult);

                // query the procedure to get the credit number 
                var sp = @"sp_SelectCogDirectCN_No @webDb, @erpDb, @returnCogDocEntry ";

                var conn = new SqlConnection(_commDbConnStr);
                var creditMemoNum = conn.ExecuteScalar<string>(sp, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    returnCogDocEntry = result.actionResult
                });

                result.CreditMemoDocNum = creditMemoNum;

                var log = new FTAPP_AppPostLog
                {
                    AppModule = "WarehouseReceive_Inv",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.SubSi}",
                    Details = $"InvEntry# {dto.RetMemo_ByInv.Docentry}, Received: {response.Content}",
                    PostResult = $"{result.actionResult}",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                // get the base on 
                AppPostLogging(log);
                return result;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult SaveWReturn(WhsRet_Dto dto)
        {
            try
            {
                // check post to sap properties
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Invalid company id [CI]");
                }
                if (string.IsNullOrWhiteSpace(dto.Token))
                {
                    return BadRequest("Invalid company portal token [CPT]");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Invalid card code");
                }
                if (string.IsNullOrWhiteSpace(dto.AppVersion))
                {
                    return BadRequest("Invalid AppVersion");
                }
                if (dto.Doc1 == null)
                {
                    return BadRequest("Invalid Doc sent in");
                }

                // for portal saving property
                if (dto.Head == null)
                {
                    return BadRequest("Invalid head doc");
                }

                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                if (dto.RtnLines == null)
                {
                    return BadRequest("Invalid doc lines");
                }

                if (dto.RtnLines.Count == 0)
                {
                    return BadRequest("Invalid doc lines [ZR]");
                }

                // 20231226
                // check the retrun line and match cn line no tally from app
                if (dto.RtnLines.Count != dto.Doc1.Lines.Count)
                {
                    return BadRequest("Return line count no tally. [20231226]");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                // massage the lines
                for (int x = 0; x < dto.Doc1.Lines.Count; x++)
                {
                    if (dto.Doc1.Lines[x].ExpDate == new DateTime(1, 1, 1))
                    {
                        dto.Doc1.Lines[x].ExpDate = null;
                    }
                    if (dto.Doc1.Lines[x].MfrDate == new DateTime(1, 1, 1))
                    {
                        dto.Doc1.Lines[x].MfrDate = null;
                    }
                }

                // save the data first 
                SaveCnApp(dto, db);

                // add in check for varient 
                // double check any varient return
                if (dto.RtnLines != null)
                {
                    var hasVarient = dto.RtnLines.Any(c => c.CnQty != c.RtnQty);
                    if (hasVarient)
                    {
                        SaveHrCharge(dto, db);
                    }
                }

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var endpoint = $@"{svrAdr}WarehouseReceiveCN/{dto.CompanyId}";
                var json = JsonConvert.SerializeObject(dto.Doc1);

                // 20241128 
                // save the json send to pb api 
                var newJsonLog = new FTAPP_JsSentLog
                {
                    Endpoint = endpoint,
                    JSonValue = json,
                    Token = dto.Token,
                    Module = "CN_WHS_RET",
                    DocType = "CN",
                    DocNum = dto.Head.CnDocNum,
                    DocEntry = dto.Head.CnDocEntry,
                    TransDt = DateTime.Now
                };

                var insertLog = @$"INSERT INTO {db.WEBDB}..FTAPP_JsSentLog ( 
                                                Endpoint
                                               ,JSonValue
                                               ,Module
                                               ,DocType
                                               ,DocNum
                                               ,DocEntry
                                               ,Token
                                               ,TransDt ) values (
                                                @Endpoint
                                               ,@JSonValue
                                               ,@Module
                                               ,@DocType
                                               ,@DocNum
                                               ,@DocEntry
                                               ,@Token
                                               ,@TransDt ) ";

                using var saveLogCon = new SqlConnection(_commDbConnStr);
                var res_savelog = saveLogCon.Execute(insertLog, newJsonLog);

                // end ----- save json log codes



                // post to SAP for invetory transfer
                var client = new RestClient(endpoint);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.Token}");
                request.AddHeader("Content-Type", "application/json");
                
                request.AddParameter("application/json", json, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                if (response.IsSuccessful)
                {
                    var replied1 = JsonConvert.DeserializeObject<PortalReplied>(response.Content);

                    // query the rtn record 
                    using var connUpdate = new SqlConnection(_commDbConnStr);
                    var sp_queryRtn = @$"select * from {db.WEBDB}..RTN Where CMENTRY = @CMENTRY;";
                    var found_rtn = connUpdate.Query<RTN>(sp_queryRtn, new { CMENTRY = dto.Head.CnDocEntry }).FirstOrDefault();

                    if (found_rtn == null)
                    {
                        return BadRequest("RTN record no found for this CN, Please contact support for help. Thanks.[URTN0]");
                    }

                    // 20241030 
                    // update the logging file 
                    var sp_updateRTNLog = $@"update t1 set  IS_WHS_RECEIPT = 1
                                                    , WHS_RECEIPT_DT = GETDATE()
                                                    , WHS_USER_CODE = @whsUserCode
                                                    , CNDOCNUM = @cnDocNum
                                                    , CNENTRY = @cnDocEntry
                                                    , ITDOCNUM = @ItDocNuM

                                            from       {db.WEBDB}..RTN t0 
                                            inner join {db.WEBDB}..FTAPP_RTN t1 on t1.DOCENTRY = t0.DOCENTRY
                                            where t0.DocEntry = @docEntry; ";

                    if (connUpdate.State == System.Data.ConnectionState.Closed) connUpdate.Open();
                    using var transUpdate = connUpdate.BeginTransaction();

                    try
                    {
                        var itDocNum = string.IsNullOrWhiteSpace(replied1.actionResult) ? -1 : int.Parse(replied1.actionResult);
                        var cnDocNum = -1; // default value
                        if (dto.RtnLines != null && dto.RtnLines.Count > 0)
                        {
                            cnDocNum = dto.RtnLines.First().CnDocNum;
                        }

                        var res = connUpdate.Execute(sp_updateRTNLog, new
                        {
                            whsUserCode = dto.UserCode,
                            cnDocNum = cnDocNum,
                            cnDocEntry = found_rtn.CMENTRY,
                            docEntry = found_rtn.DOCENTRY,
                            ItDocNuM = itDocNum  // transferred IT doc number (sap)
                        }, transUpdate);

                        if (res <= 0)
                        {
                            transUpdate.Rollback();
                            return BadRequest("Error update RTN received at warehouse, please contact support for help. Thanks [URTN1]");
                        }

                        transUpdate.Commit();
                    }
                    catch (Exception err)
                    {
                        transUpdate.Rollback();
                        return BadRequest($"Error update RTN received at warehouse, please contact support for help. Thanks [URTN2] {err.Message} {err.StackTrace}");
                    }


                    // process as per normal
                    // else success but replied no in proper
                    var newLog = new FTAPP_AppPostLog
                    {
                        AppModule = "WarehouseReceiveCN",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = $"{dto.SubSi}",
                        Details = $"CNEntry# {dto.Doc1.DocEntry}, Received: {response.Content}",
                        PostResult = "Success",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    AppPostLogging(newLog);

                    // get the charge amt 
                    if (dto.Head.HasVarient)
                    {
                        using var conn1 = new SqlConnection(_commDbConnStr); // intial new connection
                        var query_Charge = $@"Select * from {db.WEBDB}..FTAPP_CnSummary1 with (nolock)
                                      Where CnDocEntry = @CnDocEntry ";

                        var charge = conn1.Query<CnSummary>(query_Charge,
                            new
                            {
                                CnDocEntry = dto.CnDocEntry
                            }).FirstOrDefault();

                        if (charge != null)
                        {
                            replied1.HrChargeAmt = charge.ChargeAmt;
                            replied1.ChargedUserCode = charge.UserCode;
                            replied1.ChargedUserName = charge.UserName;
                        }
                    }

                    return Ok(replied1);
                }

                // no success
                var newLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "WarehouseReceiveCN",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.SubSi}",
                    Details = $"CNEntry# {dto.Doc1.DocEntry}, Received: {response.Content}",
                    PostResult = "Fail",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                AppPostLogging(newLog1);
                var replied = JsonConvert.DeserializeObject<PortalReplied>(response.Content);
                return BadRequest($"SAP End point replied :" + replied?.errorMessage);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                var newLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "WarehouseReceiveCN",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.SubSi}",
                    Details = $"CNEntry# {dto.Doc1.DocEntry}, Received: {LastError}",
                    PostResult = "Exception",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for whs app return 
        // code detect variant 
        // create record into HR table - CnSummary
        bool SaveHrCharge(WhsRet_Dto dto, DbInfo db)
        {
            if (dto.Head == null) return true;
            if (dto.Head.HasVarient == false) return true;

            var cnEntry = dto.Doc1.DocEntry;
            using var conn = new SqlConnection(_commDbConnStr);
            var sp_query = @"exec sp_SelectCnWithRetButVarient1 @webDb, @cnDocEntry ";
            var cnSummary = conn.Query<CnSummary>(sp_query, new
            {
                webDb = db.WEBDB,
                cnDocEntry = cnEntry
            }).FirstOrDefault();

            if (cnSummary == null) return true;
            if ($"{cnSummary.ChargeAmt:N2}".Equals("0.00")) return true; //check the amt is zero then return

            // check duplicated 
            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var dupCharge = $@"Select * from {db.WEBDB}..FTAPP_CnSummary1
                                   Where CnDocEntry = @CnDocEntry ";

                var dupChargeCn = conn.Query<CnSummary>(dupCharge, new { CnDocEntry = cnEntry }, trans).FirstOrDefault();
                if (dupChargeCn != null)
                {
                    var delete_sql = $@"delete from {db.WEBDB}..FTAPP_CnSummary1 Where CnDocEntry = @CnDocEntry ";
                    var result = conn.Execute(delete_sql, new
                    {
                        CnDocEntry = cnEntry
                    }, trans);

                    if (result <= 0)
                    {
                        trans.Rollback();
                        LastError = $"Del-Upd CNSumm fail, subsi {db.COMPANYNAME}, CnEntry : {dto.CnDocEntry}";
                        _logger.LogError(LastError);
                        return false;
                    }
                }

                // insert the data
                var sqlinsert = $@" INSERT INTO  {db.WEBDB}..FTAPP_CnSummary1 (
                                         SubSi
                                       , CnDocEntry
                                       , CNNO
                                       , StoreCode
                                       , StoreName
                                       , DocDate
                                       , GraceExpiredDate
                                       , UserCode
                                       , UserName
                                       , ChargeAmt
                                       , Remarks
                                       , ChargeSource
                                       , EmployeeNo
                                       , CompanyCode
                                       , PayItem
                                       , Currency
                                       , DateFrom
                                       , DateTo
                                       , Amount
                                       , Fixed
                                       , Run1
                                       , ReportedDate 
                                    ) VALUES (
                                          @SubSi
                                         ,@CnDocEntry
                                         ,@CNNO
                                         ,@StoreCode
                                         ,@StoreName
                                         ,@DocDate
                                         ,@GraceExpiredDate
                                         ,@UserCode
                                         ,@UserName
                                         ,@ChargeAmt
                                         ,@Remarks
                                         ,@ChargeSource
                                         ,@EmployeeNo
                                         ,@CompanyCode
                                         ,@PayItem
                                         ,@Currency
                                         ,@DateFrom
                                         ,@DateTo
                                         ,@Amount
                                         ,@Fixed
                                         ,@Run1
                                         ,@ReportedDate );";

                var res = conn.Execute(sqlinsert, cnSummary, trans);
                if (res <= 0)
                {
                    trans.Rollback();
                    LastError = $"Insert CNSumm fail, subsi {db.COMPANYNAME}, CnEntry : {dto.CnDocEntry}";
                    _logger.LogError(LastError);
                    return false;
                }
                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        bool SaveHrCharge_Inv(WhsRet_Dto dto, DbInfo db, SqlConnection outter_conn, SqlTransaction trans)
        {
            if (dto.InvHead == null) return true;
            if (!dto.InvHead.HasVarient) return true;

            var invEntry = dto.InvHead.InvDocEntry;
            var sp_query = @"exec sp_SelectRetInvWithRetButVarient1 @webDb, @invDocEntry ";
            using var new_conn = new SqlConnection(_commDbConnStr);
            var invSummary = new_conn.Query<InvSummary>(sp_query, new
            {
                webDb = db.WEBDB,
                invDocEntry = invEntry
            }).FirstOrDefault();


            if (invSummary == null) return true;

            try
            {
                var dupCharge = $@"Select * from {db.WEBDB}..FTAPP_InvSummary1 
                                      Where InvDocEntry = @InvDocEntry ";

                var dupChargeCn = new_conn.Query<InvSummary>(dupCharge, new { InvDocEntry = invEntry }).FirstOrDefault();

                if (dupChargeCn != null)
                {
                    var delete_sql = $@"delete from {db.WEBDB}..FTAPP_InvSummary1
                                      Where InvDocEntry = @InvDocEntry ";
                    outter_conn.Execute(delete_sql, new
                    {
                        InvDocEntry = invEntry
                    }, trans);
                }

                // insert the data
                var sqlinsert = $@" INSERT INTO  {db.WEBDB}..FTAPP_InvSummary1 (
                                         SubSi
                                       , InvDocEntry
                                       , InvNO
                                       , StoreCode
                                       , StoreName
                                       , DocDate
                                       , GraceExpiredDate
                                       , UserCode
                                       , UserName
                                       , ChargeAmt
                                       , Remarks
                                       , ChargeSource
                                       , EmployeeNo
                                       , CompanyCode
                                       , PayItem
                                       , Currency
                                       , DateFrom
                                       , DateTo
                                       , Amount
                                       , Fixed
                                       , Run1
                                       , ReportedDate 
                                    ) VALUES (
                                          @SubSi
                                         ,@InvDocEntry
                                         ,@InvNO
                                         ,@StoreCode
                                         ,@StoreName
                                         ,@DocDate
                                         ,@GraceExpiredDate
                                         ,@UserCode
                                         ,@UserName
                                         ,@ChargeAmt
                                         ,@Remarks
                                         ,@ChargeSource
                                         ,@EmployeeNo
                                         ,@CompanyCode
                                         ,@PayItem
                                         ,@Currency
                                         ,@DateFrom
                                         ,@DateTo
                                         ,@Amount
                                         ,@Fixed
                                         ,@Run1
                                         ,@ReportedDate );";

                var res = outter_conn.Execute(sqlinsert, invSummary, trans);
                return true;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        // save for portal and hr usage 
        bool SaveCnApp(WhsRet_Dto dto, DbInfo db)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var sp_CheckHeadExist_query = $@"Select top 1 * from  {db.WEBDB}..FTAPP_WRTN 
                                                    Where CnDocEntry = @CnEntry";

                var found = conn.Query<FTAPP_WRTN>(sp_CheckHeadExist_query,
                    new { CnEntry = dto.Head.CnDocEntry }, trans).FirstOrDefault();

                if (found != null) // delete the head
                {
                    // head delete
                    var delete_query = $"delete from {db.WEBDB}..FTAPP_WRTN Where CnDocEntry = @CnEntry";
                    var result1 = conn.Execute(delete_query, new { CnEntry = dto.Head.CnDocEntry }, trans);

                    if (result1 == 0)
                    {
                        trans.Rollback();
                        LastError = $"Error remove the last save return whs CN head record, subsi {db.COMPANYNAME}, CnEntry : {dto.Head.CnDocEntry}";
                        _logger.LogError(LastError);
                        return false;
                    }
                }

                var checkLineExist_Query = $@" select top 1 * from {db.WEBDB}..FTAPP_WRTN1 Where CnEntry = @CnEntry";
                var isLineFound = conn.Query<FTAPP_WRTN1>(checkLineExist_Query, new { CnEntry = dto.Head.CnDocEntry }, trans).FirstOrDefault();

                if (isLineFound != null)
                {
                    // line delete
                    var delete_query = $"delete from {db.WEBDB}..FTAPP_WRTN1 Where CnEntry = @CnEntry";
                    var result2 = conn.Execute(delete_query, new { CnEntry = dto.Head.CnDocEntry }, trans);
                    if (result2 == 0)
                    {
                        trans.Rollback();
                        LastError = $"Error remove the last save return whs CN lines, subsi {db.COMPANYNAME}, CnEntry : {dto.Head.CnDocEntry}";
                        _logger.LogError(LastError);
                        return false;
                    }
                }


                // loop each line to save into table
                for (int r = 0; r < dto.RtnLines.Count; r++)
                {
                    var line = dto.RtnLines[r];
                    if (line == null) continue;

                    // perform insert if the line 
                    var sp_insert = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN1 (
                                     CnEntry
                                   , CnLine
                                   , CnDocNum
                                   , CardCode
                                   , CardName
                                   , CnPrice
                                   , CnQty
                                   , UserCode
                                   , UserName
                                   , RtnDt
                                   , RtnQty
                                   , Subsi
                                   , SubsiID 
                                   , ItemCode 
                                   , ItemName
                                   , Remarks
                                   , UomQty
                                   , ScanInCode
                                   , Reason
                                   , WhsCode
                                   , LotNo
                                   , ManBtchNum
                                   , Remark 
                                   , U_SRQTY
                                   , ActAvailReturnQty ";

                    var middle_part = @") VALUES ( 
                                       @CnEntry
                                     , @CnLine
                                     , @CnDocNum
                                     , @CardCode
                                     , @CardName
                                     , @CnPrice
                                     , @CnQty
                                     , @UserCode
                                     , @UserName
                                     , GetDate()
                                     , @RtnQty
                                     , @Subsi
                                     , @SubsiID 
                                     , @ItemCode 
                                     , @ItemName
                                     , @Remarks
                                     , @UomQty
                                     , @ScanInCode
                                     , @Reason
                                     , @WhsCode
                                     , @LotNo                                     
                                     , @ManBtchNum
                                     , @Remark 
                                     , @U_SRQTY
                                     , @ActAvailReturnQty ";

                    // massage the date 
                    if (line.ExpDate != default)
                    {
                        sp_insert += ", ExpDate";
                        middle_part += ",@ExpDate";
                    }

                    if (line.MfrDate != default)
                    {
                        sp_insert += ", MfrDate";
                        middle_part += ",@MfrDate";
                    }

                    var combine_sql = sp_insert + middle_part + ")";
                    var result3 = conn.Execute(combine_sql, line, trans);

                    if (result3 == 0)
                    {
                        trans.Rollback();
                        LastError = $"Error save return whs CN lines, " +
                            $"subsi {db.COMPANYNAME}, CnEntry : {dto.Head.CnDocEntry}, line# {line.CnLine}";
                        _logger.LogError(LastError);
                        return false;
                    }
                }

                // get user name 
                if (!string.IsNullOrWhiteSpace(dto.Head.AuthUserCode))
                {
                    var sql_readUserName = $@"select top 1 USERNAME from {db.WEBDB}..USERS with (nolock)
                                                    Where USERCODE = @AuthUserCode";

                    dto.Head.AuthUserName = conn.ExecuteScalar<string>(sql_readUserName,
                        new { AuthUserCode = dto.Head.AuthUserCode }, trans);
                }

                // insert the head
                var insertHead = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN ( 
                                        CnDocEntry
                                       ,CnDocNum
                                       ,Files
                                       ,TransDt  
                                       ,OwnerCode 
                                       ,OwnerName
                                       ,Remarks
                                       , StoreCode
                                       , StoreName
                                       , AuthUserCode
                                       , AuthUserName
                                       , HasVarient
                                       , Remark
                                      ) VALUES (
                                        @CnDocEntry
                                       ,@CnDocNum
                                       ,@Files
                                       ,GetDate() 
                                       ,@OwnerCode 
                                       ,@OwnerName
                                       ,@Remarks
                                       ,@StoreCode
                                       ,@StoreName
                                       ,@AuthUserCode
                                       ,@AuthUserName
                                       ,@HasVarient
                                       ,@Remark
                                      )";

                var result = conn.Execute(insertHead, dto.Head, trans);
                if (result == 0)
                {
                    trans.Rollback();
                    LastError = $"Error save return whs CN head, " +
                        $"subsi {db.COMPANYNAME}, CnEntry : {dto.Head.CnDocEntry}";
                    _logger.LogError(LastError);
                    return false;
                }

                trans.Commit(); // for all cn app and summary together un one trans
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        // save for portal and hr usage 
        bool SaveInvApp(WhsRet_Dto dto, DbInfo db, SqlConnection conn, SqlTransaction trans)
        {

            try
            {
                // line delete
                //var delete_query = @$"delete from {db.WEBDB}..FTAPP_WRTN_INV Where InvDocEntry = @InvEntry ;
                //                      delete from {db.WEBDB}..FTAPP_WRTN1_INV Where InvEntry = @InvEntry; ";

                //var result = conn.Execute(delete_query, new { InvEntry = dto.InvDocEntry }, trans);

                int result = -1;
                for (int r = 0; r < dto.InvDetails.Count; r++)
                {
                    var line = dto.InvDetails[r];
                    if (line == null) continue;

                    // perform insert if the line 
                    var sp_insert = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN1_INV (
                                     InvEntry
                                   , InvLine
                                   , InvDocNum
                                   , CardCode
                                   , CardName
                                   , InvPrice
                                   , InvQty
                                   , UserCode
                                   , UserName
                                   , RtnDt
                                   , RtnQty
                                   , Subsi
                                   , SubsiID 
                                   , ItemCode 
                                   , ItemName
                                   , Remarks
                                   , UomQty
                                   , ScanInCode
                                   , Reason
                                   , WhsCode
                                   , LotNo
                                   , ManBtchNum
                                   , Remark ";

                    var middle_part = @") VALUES ( 
                                       @InvEntry
                                     , @InvLine
                                     , @InvDocNum
                                     , @CardCode
                                     , @CardName
                                     , @InvPrice
                                     , @InvQty
                                     , @UserCode
                                     , @UserName
                                     , GetDate()
                                     , @RtnQty
                                     , @Subsi
                                     , @SubsiID 
                                     , @ItemCode 
                                     , @ItemName
                                     , @Remarks
                                     , @UomQty
                                     , @ScanInCode
                                     , @Reason
                                     , @WhsCode
                                     , @LotNo                                     
                                     , @ManBtchNum
                                     , @Remark ";

                    // massage the date 
                    if (line.ExpDate != default)
                    {
                        sp_insert += ", ExpDate";
                        middle_part += ",@ExpDate";
                    }

                    if (line.MfrDate != default)
                    {
                        sp_insert += ", MfrDate";
                        middle_part += ",@MfrDate";
                    }

                    var combine_sql = sp_insert + middle_part + ")";
                    result = conn.Execute(combine_sql, line, trans);
                }

                // get user name 
                if (!string.IsNullOrWhiteSpace(dto.InvHead.AuthUserCode))
                {
                    var sql_readUserName = $@"select top 1 USERNAME from {db.WEBDB}..USERS with (nolock)
                                                    Where USERCODE = @AuthUserCode";

                    // use new connection
                    using var new_conn = new SqlConnection(_commDbConnStr);
                    dto.InvHead.AuthUserName = new_conn.ExecuteScalar<string>(sql_readUserName,
                        new { AuthUserCode = dto.InvHead.AuthUserCode });
                }


                // insert the head
                var insertHead = @$"INSERT INTO {db.WEBDB}..FTAPP_WRTN_INV ( 
                                         InvDocEntry
                                       , InvDocNum
                                       , Files
                                       , TransDt  
                                       , OwnerCode 
                                       , OwnerName
                                       , Remarks
                                       , StoreCode
                                       , StoreName
                                       , AuthUserCode
                                       , AuthUserName
                                       , HasVarient
                                       , Remark
                                       , DlbEntry
                                      ) VALUES (
                                        @InvDocEntry
                                       ,@InvDocNum
                                       ,@Files
                                       ,GetDate() 
                                       ,@OwnerCode 
                                       ,@OwnerName
                                       ,@Remarks
                                       ,@StoreCode
                                       ,@StoreName
                                       ,@AuthUserCode
                                       ,@AuthUserName
                                       ,@HasVarient
                                       ,@Remark
                                       ,@DlbEntry
                                      )";

                result = conn.Execute(insertHead, dto.InvHead, trans);
                return true;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        void AppPostLogging(FTAPP_AppPostLog log)
        {
            try
            {
                new AppPostLogHelper().Create(_commDbConnStr, log);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        private bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            strInput = strInput.Trim();

            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    LastError = $"{jex.Message}\n{jex.StackTrace}";
                    _logger.LogError(LastError);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    LastError = $"{ex.Message}\n{ex.StackTrace}";
                    _logger.LogError(LastError);
                    return false;
                }
            }
            return false;
        }

        IActionResult GetInvAndLines(WhsRet_Dto dto, bool isRedo = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User Code is empty");
                }

                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                // remove the redo query
                // 20211231

                var sp_query = @"exec sp_SelctWhsRetInvoice @webDb, @erpDb, @invDocEntry, @SubSi, @SubSiID";

                var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<OINV>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    invDocEntry = dto.InvDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).FirstOrDefault();

                if (result == null) return NotFound();

                Console.WriteLine(result.Reason);

                // get the cn line for reference
                var sp_invLines = @"exec sp_SelctWhsRetInvLines @webDb, @erpDb, @invDocEntry, @SubSi, @SubSiID";
                var lines = conn.Query<INV1>(sp_invLines, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    invDocEntry = dto.InvDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).ToList();

                if (lines.Count == 0) return NotFound();
                // load in all related barcode to scan in 

                for (int i = 0; i < lines.Count; i++)
                {
                    var sql = $@"SELECT * FROM [{db.SAPDB}]..[OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var item = lines[i];
                    if (item == null) continue;

                    lines[i].UOMQTY = lines[i].UOMQTY == 0 ? 1 : lines[i].UOMQTY;

                    // load item code related barcode
                    lines[i].BarCodes = conn.Query<OBCD_Ext>(sql, new { itemcode = item.ItemCode }).ToList();
                    if (lines[i].BarCodes == null) lines[i].BarCodes = new List<OBCD_Ext>();

                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].ItemCode,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    lines[i].BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].CodeBars,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].SuppCatNum,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(supcatnum);
                    lines[i].LineGuid = Guid.NewGuid();

                    // prepare the batch if any 
                    var sq_batches = @$"select BatchNum, Quantity 
                                       from {db.SAPDB}..IBT1 with (nolock) 
                                       where BaseEntry = @baseEntry 
                                       and   BaseNum = @baseNum
                                       and   BaseLinNum =@baseLinNum        
                                       and   BaseType = @baseType";

                    lines[i].Batches = conn.Query<Batch>(sq_batches, new
                    {
                        baseEntry = result.DocEntry,
                        baseNum = result.DocNum,
                        baseLinNum = lines[i].LineNum,
                        baseType = 13
                    }).ToList();

                }

                result.Lines = lines;
                return Ok(result);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetInvAndLines_Redo(WhsRet_Dto dto, bool isRedo = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User Code is empty");
                }

                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                // remove the redo query
                // 20211231                
                var sp_query = @"exec sp_SelctWhsRetInvoice_Redo @webDb, @erpDb, @invDocEntry, @SubSi, @SubSiID";
                var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<OINV>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    invDocEntry = dto.InvDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).FirstOrDefault();

                if (result == null) return NotFound();

                // get the cn line for reference
                var sp_invLines = @"exec sp_SelctWhsRetInvLines @webDb, @erpDb, @invDocEntry, @SubSi, @SubSiID";
                var lines = conn.Query<INV1>(sp_invLines, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    invDocEntry = dto.InvDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).ToList();

                if (lines.Count == 0) return NotFound();
                // load in all related barcode to scan in 

                for (int i = 0; i < lines.Count; i++)
                {
                    var sql = $@"SELECT * FROM [{db.SAPDB}]..[OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var item = lines[i];
                    if (item == null) continue;

                    lines[i].UOMQTY = lines[i].UOMQTY == 0 ? 1 : lines[i].UOMQTY;

                    // load item code related barcode
                    lines[i].BarCodes = conn.Query<OBCD_Ext>(sql, new { itemcode = item.ItemCode }).ToList();
                    if (lines[i].BarCodes == null) lines[i].BarCodes = new List<OBCD_Ext>();

                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].ItemCode,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    lines[i].BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].CodeBars,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].SuppCatNum,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(supcatnum);
                    lines[i].LineGuid = Guid.NewGuid();
                }

                result.Lines = lines;
                return Ok(result);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCnAndLines(WhsRet_Dto dto, bool isRedo = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User Code is empty");
                }

                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                // remove the redo query
                // 20211231
                //var sp_query = @"exec sp_SelctWhsRetCn @erpDb, @CnDocEntry, @SubSi, @SubSiID";
                var sp_query = (isRedo) ? @"exec sp_SelctWhsRetCn_Redo " :
                                          @"exec sp_SelctWhsRetCn ";

                sp_query += @"@webDb, @erpDb, @commonDb, @cnDocEntry, @SubSi, @SubSiID";

                var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<ORIN>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    commonDb = "KTCW_COMMON",
                    CnDocEntry = dto.CnDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).FirstOrDefault();

                if (result == null)
                {
                    return NotFound();
                    // 20231217 
                    // add in check for trcn, ensure the return from sap was done, 
                    // if not allow to return again.
                    //try
                    //{
                    // add in another check to confirm the trcn done with transfer 
                    //var checkIsReturnSql = @$"select 
                    //                case when (Select count(1) 
                    //                   from {db.WEBDB}..FTAPP_WRTN w1 with (nolock) 
                    //                   inner join {db.SAPDB}..OWTR w2 with (nolock) on w1.CnDocEntry = w2.U_SOID
                    //                     Where w1.CnDocEntry = '{dto.CnDocEntry}') >= 1 then 1 else 0 end [IsReturned]";

                    //var isReturn = conn.ExecuteScalar<int>(checkIsReturnSql);
                    //if (isReturn == 1) // already transfered.
                    //{
                    //    return NotFound();
                    //}
                    //else
                    //{
                    //    // if not return then delete the wrtn record 
                    //    // delete the wrtn record for new 
                    //    var deleteWrtnSql = @$"Delete from {db.WEBDB}..FTAPP_WRTN Where CnDocEntry = '{dto.CnDocEntry}'";
                    //    conn.Execute(deleteWrtnSql);


                    //    var deleteWrtn1Sql = @$"Delete from {db.WEBDB}..FTAPP_WRTN1 Where CnEntry = '{dto.CnDocEntry}'";
                    //    conn.Execute(deleteWrtn1Sql);
                    //}
                    //}
                    //catch (Exception s)
                    //{
                    //    LastError = $"{s.Message}\n{s.StackTrace}";
                    //    _logger.LogError(LastError);
                    //    return BadRequest($"request not handler. rechecking trcn  fail. with error :\n{LastError}");
                    //}
                }

                // check is over grace period
                // 20210915
                var defaultGRPRD = UserCodeGracePeriod(new Dto_Cog
                {
                    Subsi = dto.SubSi,
                    UserCode = dto.UserCode
                });

                if (result.U_GRPRD <= 0)
                {
                    result.U_GRPRD = defaultGRPRD;
                }

                var expDt = result.DocDate.AddDays(result.U_GRPRD);
                var remainDay = expDt.Day - DateTime.Now.Day;
                if (remainDay <= 0)
                {
                    result.SvrMsg = $"CN# {result.DocNum}, dated:{result.DocDate:dd-MMM-yy}, " +
                                        $"return grace period expired on {expDt:dd-MMM-yy}, " +
                                        $"grace period > {result.U_GRPRD:N0} days";
                }

                // get the cn line for reference
                var sp_cnLines = @"exec sp_SelctWhsRetCnLines @webDb, @erpDb, @CnDocEntry, @SubSi, @SubSiID";
                var lines = conn.Query<RIN1>(sp_cnLines, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    CnDocEntry = dto.CnDocEntry,
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).ToList();

                if (lines?.Count == 0) return NotFound();
                // load in all related barcode to scan in 

                for (int i = 0; i < lines.Count; i++)
                {
                    var sql = $@"SELECT * FROM [{db.SAPDB}]..[OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var item = lines[i];
                    if (item == null) continue;

                    lines[i].UOMQTY = lines[i].UOMQTY == 0 ? 1 : lines[i].UOMQTY;

                    // load item code related barcode
                    lines[i].BarCodes = conn.Query<OBCD_Ext>(sql, new { itemcode = item.ItemCode }).ToList();
                    if (lines[i].BarCodes == null) lines[i].BarCodes = new List<OBCD_Ext>();

                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].ItemCode,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    lines[i].BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].CodeBars,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].SuppCatNum,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(supcatnum);

                    //var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    //lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    //{
                    //    sapDb = db.SAPDB,
                    //    baseEntry = result.DocEntry,
                    //    baseLineNum = lines[i].LineNum,
                    //    itemCode = lines[i].ItemCode,
                    //    docType = 14 // credit memo line
                    //}).ToList();
                }

                result.Lines = new List<RIN1>(lines);
                return Ok(result);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        int UserCodeGracePeriod(Dto_Cog dto)
        {
            var defaultGracePeriod = 7;
            try
            {
                var conn = new SqlConnection(_commDbConnStr);
                var sql_DefaultGracePeriod = @"Select SetupValue 
                                                from FTApp_Config with (nolock) 
                                                Where SetupName= 'AppDiCnReturnGracePeriod'";

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

                var setupGracePeriod = conn.ExecuteScalar<int>(sp_sql, new { webDb = db.WEBDB, dto.UserCode });
                if (setupGracePeriod < 0) return defaultGracePeriod;
                return setupGracePeriod;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return defaultGracePeriod;
            }
        }
    }
}

//void HandlerSendEmail(WhsRet_Dto dto)
//{
//    try
//    {
//        var lines = dto.RtnLines;
//        var head = dto.Head;

//        decimal totalChargedAmt = 0;
//        var bodydesc = $"Dear HR & Finance\n\n" +
//                       $"The usercode:{head.OwnerCode}, {head.OwnerName} had DiCn varient return charges on follow item(s)\n\n" +
//                       $"CN# {head.CnDocNum}\n" +
//                       $"Store: {head.StoreCode}\n" +
//                       $"Store name: {head.StoreName}\n" +
//                       $"Receipt date: {head.TransDt:dd-MMM-yy}\n\n\n";

//        for (int i = 0; i < lines.Count; i++)
//        {
//            var line = lines[i];
//            if (line == null) continue;

//            decimal lineChargeAmt = 0;
//            var varientQty = line.RtnQty - line.CnQty;
//            if (varientQty < 0)
//            {
//                lineChargeAmt = line.CnPrice * varientQty;
//                totalChargedAmt += lineChargeAmt;
//            }

//            bodydesc += $"{line.ItemCode}, {line.ItemName}\n";
//            bodydesc += $"Issued: {line.CnQty:N0}, Receipt:{line.RtnQty}, Varient: {varientQty:N0}, " +
//                                $"charged amt: {lineChargeAmt:N2}\n\n";
//        }

//        bodydesc += $"Total charges {totalChargedAmt:N2}";

//        bodydesc += $"\n\nBest Rgds,\nAdmin\n{DateTime.Now:dd-MMM-yy hh:mm tt}";

//        // prepare subject 
//        var subject = $"DiCn Varient Charged {head.OwnerCode}, {head.OwnerName}, Amt {totalChargedAmt:N2}";

//        // prepare to send email
//        var mailHelper = new SendMailHelper(_logger);
//        var server = _configuration.GetSection("AppSettings").GetSection("MailServerAddress").Value;
//        var sender = _configuration.GetSection("AppSettings").GetSection("MailSenderAccount").Value;
//        var senderPw = _configuration.GetSection("AppSettings").GetSection("MailSenderPw").Value;
//        var serverPort = _configuration.GetSection("AppSettings").GetSection("MailServerPort").Value;
//        var serverEncryt = _configuration.GetSection("AppSettings").GetSection("MailServerEncryption").Value;
//        var senderName = _configuration.GetSection("AppSettings").GetSection("MailSenderName").Value; //MailSenderName

//        //var to = new string[] { "goh.chongmin@gmail.com","tan.tzekok@kimteckcheong.com"};
//        //var to = new string[] { "goh.chongmin@gmail.com" };

//        // get mailing list 
//        var mailboxes = GetMailList(dto.SubSi, "DiCnVarient");
//        var to = mailboxes.Select(x => x.ReceiverEmailAddress).Distinct().ToArray();

//        mailHelper.CreateMessageWithAttachment(server, sender, senderPw, to, subject, bodydesc, "", serverPort, false);

//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//    }
//}

//List<FTApp_MailList> GetMailList(string companyName, string reportType)
//{
//    try
//    {
//        // get list if email address 
//        var sql = $"SELECT * " +
//            $"FROM FTAPP_DicnVarientMailList with (nolock) " +
//            $"WHERE Subsidairy = @Subsidairy " +
//            $"AND Report = @Report ";

//        var conn = new SqlConnection(_commDbConnStr);
//        return new SqlConnection(_commDbConnStr).Query<FTApp_MailList>(sql,
//                    new
//                    {
//                        Subsidairy = companyName,
//                        Report = reportType
//                    }).ToList();

//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return null;
//    }
//}