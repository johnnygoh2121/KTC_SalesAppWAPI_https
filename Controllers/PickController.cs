using Dapper;
using KTC_SalesAppWAPI.DTOs.Pick;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Pick_IBT;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PickController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<PickController> _logger;

        string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        static bool IsBusyNextOrder = false;
        static bool IsBusyGetQueueSO3 = false;

        static bool IsSupportIBT = true;
        static bool IsByPassSO = false;

        static bool IsPickHoldBusy { get; set; } = false; // 20230113
        public PickController(IConfiguration configuration, ILogger<PickController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);            
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_Pick dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetQueueSO":
                    {
                        return GetQueueSO(dto);
                    }
                case "GetQueueSOByDocEntyWhildCard":
                    {
                        return GetQueueSOByDocEntyWhildCard(dto);
                    }
                case "GetQueueSOLines":
                    {
                        return GetQueueSOLines(dto);
                    }
                case "NextAvailOrder":
                    {
                        return GetQueueSO(dto, true); // NextAvailOrder, selete the first one
                    }
                case "GetQueueSO3": // 20210922 query all user order by date time
                    {
                        return GetQueueSO3(dto);
                    }
                case "GetWhsUserAgency":
                    {
                        return GetWhsUserAgency(dto);
                    }
                case "ReleaseSoInPicking":
                    {
                        return ReleaseSoInPicking(dto); // from app client
                    }
                case "PostPicked":
                    {
                        return PostPicked(dto);
                    }
                case "CheckSoLineDraftExist":
                    {
                        return CheckSoLineDraftExist(dto);
                    }
                case "DeletePickedDraft":
                    {
                        return DeletePickedDraft(dto);
                    }
                case "SwitchPickToSubmit":
                    {
                        return SwitchPickToSubmit(dto);
                    }
                case "SwitchPickToCancel":
                    {
                        return SwitchPickToCancel(dto);
                    }
                case "AddPickLog":
                    {
                        return AddPickLog(dto);
                    }
                case "ItemInFreezed":
                    {
                        return ItemInFreezed(dto);
                    }
                case "VerifyPickMissingSupervisor":
                    {
                        return VerifyPickMissingSupervisor(dto);
                    }
                case "GetCustomer": // read the address from SAP
                    {
                        return GetCustomer(dto);
                    }
                case "SaveSecBoxDraft":
                    {
                        return SaveSecBoxDraft(dto);
                    }
                case "GetSecBoxDraft":
                    {
                        return GetSecBoxDraft(dto);
                    }
                case "GetSectionBatches":
                    {
                        return GetSectionBatches(dto);
                    }
                case "ClearSecBoxLine":
                    {
                        return ClearSecBoxLine(dto);
                    }
                case "ClearSecBoxLine_IBT":
                    {
                        return ClearSecBoxLine_IBT(dto);
                    }
                case "ClearSecBoxLine_WithPickingMode":              // 20250418
                    {
                        return ClearSecBoxLine_WithPickingMode(dto); // 20250418
                    }
                case "ClearDraft":
                    {
                        return ClearDraft(dto);
                    }
                case "RecheckInvoice":
                    {
                        return RecheckInvoice(dto);
                    }
                case "SetSOOutStock":
                    {
                        return SetSOOutStock(dto);
                    }

                case "UpdateLineBarCodeFromSap":
                    {
                        return UpdateLineBarCodeFromSap(dto);
                    }
                case "FinalCheckOnHold":
                    {
                        return FinalCheckOnHold(dto);
                    }
                case "BoxSizes":
                    {
                        return BoxSizes();
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult BoxSizes()
        {
            try
            {
                var sql = "SELECT * FROM [FTAPP_BoxSize] with (NOLOCK) " +
                          "Where IsActive = 1";

                using var conn = new SqlConnection(_commDbConnStr);
                return Ok(conn.Query<FTAPP_BoxSize>(sql).ToList());
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// to check the sap invoice were created 
        /// if created delete the onhold for picking 
        /// replied app with posted.
        /// 
        /// if not created sap invoice. 
        /// check onhold record exist
        /// if exist then ok 
        /// else create the onhold for this document
        /// trigger app to process picking 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult FinalCheckOnHold(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db");
                }

                // start of the transaction scope
                using var conn = new SqlConnection(_commDbConnStr);

                // check the doc is created invoice 
                // 20212728
                var sql = @$"SELECT DocNum , DocEntry , u_soid
                                 FROM {db.SAPDB}..OINV WITH (NOLOCK) 
                                 WHERE U_SOID = @docEntry ";

                var sapInv = conn.Query<OINV>(sql, new { docEntry = dto.DocEntry }).FirstOrDefault();
                if (sapInv == null)
                {
                    // no applied no lock in sql
                    // as need insert statetment to lock the table, before select checking
                    var sq_select = @$"Select * 
                                       from {db.WEBDB}..FTAPP_OnHoldSoInPicking 
                                       Where HoldDocEntry = @HoldDocEntry ";

                    var results = conn.Query<FTAPP_OnHoldSoInPicking>
                            (sq_select, new { HoldDocEntry = dto.DocEntry }).FirstOrDefault();

                    if (results == null) /// no lock no invoice 
                    {
                        goto CreateHoldDoc; // then hold it 
                    }

                    if (results != null && results.HoldByUserCode == dto.UserCode)
                    {
                        return Ok();
                    }

                    if (results != null) // if locked , and sap invoice is null
                    {
                        int invDocNum = -1;
                        var isInvoiceCreated = CheckInvCreated(db, dto.DocEntry, out invDocNum);
                        if (isInvoiceCreated == false)
                        {
                            if (results.HoldByUserCode == dto.UserCode)
                            {
                                return Ok();
                            }

                            var message = $"{db.COMPANYNAME} SO #{results.HoldDocEntry} " +
                                          $"currently picking by {results.HoldByUserCode}, " +
                                      $"{results.HoldByUserName}\n[PE1]";

                            return BadRequest(message);
                        }
                        else
                        {
                            return BadRequest($"{db.COMPANYNAME} , SO# {dto.DocEntry} " +
                                              $"Picked Invoice #{invDocNum} posted");
                        }
                    }
                }

                // SAP invoice created
                // update the SO table 
                if (sapInv != null)
                {
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try // transaction scope
                        {
                            var update_soWithInv = $@"UPDATE {db.WEBDB}..SO  
                                            SET INVNO = @InvNo ,  
                                                INVENTRY = @InvEntry ,  
                                                DOCSTATUS = @DocStatus  
                                            WHERE DOCENTRY = @DocEntry ";

                            var result = conn.Execute(update_soWithInv,
                                new
                                {
                                    InvNo = sapInv.DocNum,
                                    InvEntry = sapInv.DocEntry,
                                    DocStatus = 'I',
                                    DocEntry = dto.DocEntry
                                }, trans);

                            if (result <= 0)
                            {
                                trans.Rollback();
                                return BadRequest($"Error manual update SO for {db.COMPANYNAME}, SO#{dto.DocEntry}");
                            }

                            // delete the onhold record for pick
                            var deleteOnHold = @$"DELETE FROM {db.WEBDB}..FTAPP_OnHoldSoInPicking 
                                                WHERE HoldDocEntry = @DocEntry ";

                            result = conn.Execute(deleteOnHold,
                               new
                               {
                                   DocEntry = dto.DocEntry
                               }, trans);

                            //if (result < 0) // just delete a
                            //{
                            //    trans.Rollback();
                            //    return BadRequest($"Error manual delete FTAPP_OnHoldSoInPicking " +
                            //        $"for {db.COMPANYNAME}, SO#{dto.DocEntry}");
                            //}

                            trans.Commit();
                            return BadRequest($"Picked / Invoice #{sapInv.DocNum} posted");
                        }
                        catch (Exception e) // transaction scope
                        {
                            trans.Rollback();
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                            return BadRequest($"{LastError}");
                        }
                    } // end of // transaction scope
                }

            CreateHoldDoc:
                // put the doc onhold
                var newDto = new Dto_Pick
                {
                    Subsi = dto.Subsi,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    DocEntry = dto.DocEntry,
                    HoldReason = "Picking"
                };

                return OnHoldSoInPicking(newDto); // hold from checking
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        bool CheckInvCreated(DbInfo db, int docEntry, out int invDocNum)
        {
            invDocNum = -1;
            var sql = @$"Select DocEntry, DocNum  
                        from {db.SAPDB}..OINV with (nolock)
                        Where U_SOID = @docEntry ";

            using var conn = new SqlConnection(_commDbConnStr);
            var inv = conn.Query<OINV>(sql, new { docEntry }).FirstOrDefault();
            if (inv == null) return false;

            // update the SO 
            // remove the onhold 
            invDocNum = inv.DocNum;
            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sqlUpdate = $@"Update {db.WEBDB}..SO Set DocStatus ='I', INVENTRY = @invDocEntry, INVNO = @invDocNum
                                Where DocEntry = @docEntry";

                var res = conn.Execute(sqlUpdate, new
                {
                    invDocEntry = inv.DocEntry,
                    invDocNum = inv.DocNum,
                    docEntry = docEntry
                }, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    _logger.LogError($"Update SO error for {db.COMPANYNAME}, SO#{docEntry}");
                    return false;
                }

                // remote the onhold
                var removeOnholdQuery = $@"Delete from {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                        Where DocEntry = @docEntry";

                res = conn.Execute(sqlUpdate, new
                {
                    docEntry = docEntry
                }, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    _logger.LogError($"Delete FTAPP_OnHoldSoInPicking for {db.COMPANYNAME}, SO#{docEntry}");
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

        // oos = out of stock 
        void BatchUpdateForOOS(List<SO> OosSos, string appVersion)
        {
            try
            {
                for (int x = 0; x < OosSos.Count; x++)
                {
                    var so = OosSos[x];
                    if (so == null) continue;
                    if (so.LinesCount > 0) continue; // check again for the line total

                    var dto = new Dto_Pick
                    {
                        Subsi = so.SubSi,
                        DocEntry = (int)so.DOCENTRY,
                        CardCode = so.CARDCODE,
                        UserCode = "SVR",
                        AppVersion = appVersion
                    };
                    SetSOOutStock(dto); // for each so 
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }


        IActionResult SetSOOutStock(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi name empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info empty");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

                using var trans = conn.BeginTransaction();
                try
                {
                    var sql_delete = @"exec sp_DeleteSoDraft @webDb, @docEntry";
                    var res = conn.Execute(sql_delete, new { webDb = db.WEBDB, docEntry = dto.DocEntry }, trans);
                    trans.Commit();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    var excepLog1 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = dto.Subsi,
                        Details = $"#{dto.DocEntry}, No Picked Qty, Set OutofStock, Excep: {LastError}",
                        PostResult = "Exception",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };

                    AppPostLogging(excepLog1);
                    return BadRequest($"request not handler.\n{LastError}");
                }

                var successLog = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"#{dto.DocEntry}, No Picked Qty, Set OutofStock",
                    PostResult = "Success",
                    AppVersion = $"ServerUpdate, {dto.AppVersion}"
                };

                AppPostLogging(successLog);
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var excepLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = dto.Subsi,
                    Details = $"#{dto.DocEntry}, No Picked Qty, Set OutofStock, Excep: {LastError}",
                    PostResult = "Exception",
                    AppVersion = $"ServerUpdate, {dto.AppVersion}"
                };

                AppPostLogging(excepLog1);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult RecheckInvoice(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
                {
                    return BadRequest("Invalid checking invoice number");
                }
                if (dto.DocEntry <= 0) // sales order doc entry
                {
                    return BadRequest("Invalid SO Doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // double check the inv # iin sap
                using var conn = new SqlConnection(_commDbConnStr);
                var sql = @$"SELECT DocNum 
                             FROM [{db.SAPDB}].[dbo].[OINV] WITH (NOLOCK) 
                             WHERE DocNum = @SO_InvNo 
                                   AND U_SOID = @docEntry ";

                var result = conn.ExecuteScalar<string>(sql, new
                {
                    SO_InvNo = dto.InvoiceNo,
                    docEntry = dto.DocEntry
                });

                if (string.IsNullOrWhiteSpace(result))
                {
                    return BadRequest("Invalid invalid invoice number");
                }

                if (result.Equals(dto.InvoiceNo))
                {
                    var checkedLog = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = db.COMPANYNAME,
                        Details = $"#{dto.DocEntry}, INV# {dto.InvoiceNo}",
                        PostResult = "Real",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };

                    AppPostLogging(checkedLog);

                    // save the box content                    
                    //SaveBoxes(dto.Boxes, db, dto.DocEntry); // confirm invoice
                    //RemoveDraftSo1BoxBox1(db, (int)dto.DocEntry); // success from post invoice

                    // remove onhold 
                    var release_sql = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_OnHoldSoInPicking]
                                        Where HoldDocEntry = @HoldDocEntry ";

                    var result1 = conn.Execute(release_sql,
                         new
                         {
                             HoldDocEntry = dto.DocEntry,
                         });

                    RemoveSecBoxBox1_Draft(db, dto.DocEntry); // success post invoice
                    return Ok();
                }

                // log fake invoice
                var fakeLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = db.COMPANYNAME,
                    Details = $"#{dto.DocEntry}, INV# {dto.InvoiceNo}",
                    PostResult = "Fake",
                    AppVersion = $"ServerUpdate, {dto.AppVersion}"
                };

                AppPostLogging(fakeLog1);
                return BadRequest("Invalid invoice number");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var excepLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = dto.Subsi,
                    Details = $"#{dto.DocEntry}, INV# {dto.InvoiceNo}, Excep: {LastError}",
                    PostResult = "Fake",
                    AppVersion = $"ServerUpdate, {dto.AppVersion}"
                };

                AppPostLogging(excepLog1);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearDraft(Dto_Pick dto) // to clear pick 
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc num");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, $"{dto.Subsi}");
            if (db == null)
            {
                return BadRequest("Invalid subsi no able to continue without subsi name");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // to clear the FTAPP_Box, FTAPP_Box and SecBox SecBox1
                    var deleteSql = @$"Delete from {db.WEBDB}..FTAPP_Box_DRAFT Where BaseEntry=@DocEntry; 
                                       Delete from {db.WEBDB}..FTAPP_Box1_DRAFT Where BaseEntry=@DocEntry;
                                       Delete from {db.WEBDB}..FTAPP_SecBox_DRAFT Where BaseEntry=@DocEntry;
                                       Delete from {db.WEBDB}..FTAPP_SecBox1_DRAFT Where BaseEntry=@DocEntry;
                                       Delete from {db.WEBDB}..FTAPP_SecBatch_Draft where docentry = @docEntry;
                                       Delete from {db.WEBDB}..FTAPP_SO1_DRAFT Where DocEntry=@DocEntry;
                                       Delete from {db.WEBDB}..FTAPP_Batch_Draft Where DocEntry=@DocEntry; ";

                    var res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    trans.Commit();
                    // create log for clear log
                    // 20210819 

                    var excepLog1 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = $"{dto.Subsi}",
                        Details = $"{dto.DocEntry}",
                        PostResult = "Cleared Picked",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };

                    AppPostLogging(excepLog1);
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
        }

        IActionResult ClearSecBoxLine_IBT(Dto_Pick dto)
        {
            // to clear the sec box 
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Sub si invalid");
            }
            if (dto.DocEntry < 0)
            {
                return BadRequest("doc entry invalid");
            }
            if (dto.LineNum < 0)
            {
                return BadRequest("line number invalid");
            }
            if (string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                return BadRequest("item code invalid");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db info invalid");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())

                try
                {
                    // delete the box content
                    var deleteBox_Draft = @$"
                                Delete from {db.WEBDB}..FTAPP_IBTSecBox1_DRAFT  WHERE BaseEntry = @DocEntry; 
                                Delete from {db.WEBDB}..FTAPP_IBTSecBox_DRAFT   WHERE BaseEntry = @DocEntry;
                                Delete from {db.WEBDB}..FTAPP_IBTSecBatch_Draft WHERE DocEntry  = @DocEntry; ";

                    var result = conn.Execute(deleteBox_Draft,
                        new
                        {
                            dto.DocEntry
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

        IActionResult ClearSecBoxLine_WithPickingMode(Dto_Pick dto)
        {
            // to clear the sec box 
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Sub si invalid");
            }
            if (dto.DocEntry < 0)
            {
                return BadRequest("doc entry invalid");
            }
            if (dto.LineNum < 0)
            {
                return BadRequest("line number invalid");
            }
            if (string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                return BadRequest("item code invalid");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db info invalid");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())

            try
            {
                // delete the box content
                var deleteBox_Draft =
                        @$"Delete from {db.WEBDB}..FTAPP_SecBox1_Draft  WHERE BaseEntry = @DocEntry; 
                        Delete from {db.WEBDB}..FTAPP_SecBox_Draft   WHERE BaseEntry = @DocEntry;
                        Delete from {db.WEBDB}..FTAPP_SecBatch_Draft 
                                where DocEntry  = @DocEntry
                                and PickingMode = @PickingMode ; ";

                var result = conn.Execute(deleteBox_Draft,
                    new
                    {
                        dto.DocEntry,
                        dto.PickingMode
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

        IActionResult ClearSecBoxLine(Dto_Pick dto)
        {
            // to clear the sec box 
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Sub si invalid");
            }
            if (dto.DocEntry < 0)
            {
                return BadRequest("doc entry invalid");
            }
            if (dto.LineNum < 0)
            {
                return BadRequest("line number invalid");
            }
            if (string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                return BadRequest("item code invalid");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db info invalid");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())

                try
                {
                    // delete the box content
                    var deleteBox_Draft =
                         @$"Delete from {db.WEBDB}..FTAPP_SecBox1_Draft  WHERE BaseEntry = @DocEntry; 
                            Delete from {db.WEBDB}..FTAPP_SecBox_Draft   WHERE BaseEntry = @DocEntry;
                            Delete from {db.WEBDB}..FTAPP_SecBatch_Draft WHERE DocEntry  = @DocEntry; ";

                    var result = conn.Execute(deleteBox_Draft,
                        new
                        {
                            dto.DocEntry
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

        // 20250417
        IActionResult GetSectionBatches(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi invalid");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Doc entry invalid");
                }
                if (dto.LineNum < 0)
                {
                    return BadRequest("Doc line num invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("item code invalid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Subsi name for db info invalid");
                }

                var sql = @$"SELECT distinct * 
                            FROM {db.WEBDB}..FTAPP_SecBatch_Draft WITH (NOLOCK)
                            WHERE DocEntry = @DocEntry 
                                AND ItemCode  = @ItemCode                            
                                AND BaseLine  = @LineNum";

                using var conn = new SqlConnection(_commDbConnStr);
                var batches = conn.Query<FTAPP_Batch>(sql, new
                {
                    DocEntry = dto.DocEntry,
                    ItemCode = dto.ItemCode,
                    LineNum = dto.LineNum

                }).ToList();

                if (batches.Count == 0) return NotFound();
                return Ok(batches);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetSecBoxDraft(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code invalid");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Doc entry invalid");
                }
                if (dto.LineNum < 0)
                {
                    return BadRequest("Doc line num invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("item code invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.Packaging))
                {
                    return BadRequest("packaging invalid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Subsi name for db info invalid");
                }

                var sql = @$"SELECT distinct * 
                            FROM [{db.WEBDB}].[DBO].[FTAPP_SecBox_Draft] WITH (NOLOCK)
                            WHERE BaseEntry = @DocEntry 
                            AND PickerCode = @PickerCode";

                using var conn = new SqlConnection(_commDbConnStr);
                var boxes = conn.Query<FTAPP_Box>(sql, new
                {
                    dto.DocEntry,
                    PickerCode = dto.UserCode
                }).ToList();

                if (boxes == null) return NotFound();
                if (boxes.Count == 0) return NotFound();

                var returnBoxes = new List<FTAPP_Box>();
                for (int id = 0; id < boxes.Count; id++)
                {
                    var sql_boxContent = $@"SELECT distinct * FROM [{db.WEBDB}].[dbo].[FTAPP_SecBox1_Draft] WITH (NOLOCK)
                                         WHERE BoxGuid = @BoxGuid";

                    var contents = conn.Query<FTAPP_Box1>(sql_boxContent, new
                    {
                        BoxGuid = boxes[id].BoxGuid
                    }).ToList();

                    if (contents == null) continue;
                    if (contents.Count == 0) continue;

                    boxes[id].Contents = contents;
                    returnBoxes.Add(boxes[id]);
                }

                if (returnBoxes.Count == 0) return NotFound();
                return Ok(returnBoxes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveSecBoxDraft(Dto_Pick dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Db info reading error");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())

                try
                {
                    // check boxes content
                    var sp_check_boxes_content = $@"select * 
                                    from {db.WEBDB}..FTAPP_SecBox1_Draft 
                                    Where BaseEntry = @DocEntry ";

                    var contentFounds = conn.Query<FTAPP_Box1>(sp_check_boxes_content,
                        new
                        {
                            DocEntry = dto.DocEntry
                        }, trans).ToList();

                    if (contentFounds.Count > 0)
                    {
                        var deleteBoxContent = @$"Delete from {db.WEBDB}..FTAPP_SecBox1_Draft
                                                WHERE  Id = @Id ";

                        var result = conn.Execute(deleteBoxContent, contentFounds, trans);
                        if (result <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"Error remove the section box content for pick entry #{dto.DocEntry}.");
                        }
                    }

                    // check the box head 
                    var sp_Check_Box = @$"select * from {db.WEBDB}..FTAPP_SecBox_Draft
                                        WHERE BaseEntry = @DocEntry";

                    var boxFounds = conn.Query<FTAPP_Box>(sp_Check_Box,
                        new
                        {
                            DocEntry = dto.DocEntry
                        }, trans).ToList();


                    if (boxFounds.Count > 0)
                    {
                        var deleteBox = @$"Delete from {db.WEBDB}..FTAPP_SecBox_Draft
                                        WHERE Id = @Id";

                        var result = conn.Execute(deleteBox, boxFounds, trans);
                        if (result <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"Error remove the section box for pick entry #{dto.DocEntry}.");
                        }
                    }

                    // 20250417 
                    // check the batches

                    // check the box head 
                    var sp_Check_Batch = @$"select * from {db.WEBDB}..FTAPP_SecBatch_Draft
                                        WHERE DocEntry = @DocEntry ";

                    var batchFounds = conn.Query<FTAPP_Batch>(sp_Check_Batch,
                        new
                        {
                            DocEntry = dto.DocEntry
                        }, trans).ToList();

                    if (batchFounds.Count > 0)
                    {
                        var deleteBatches = @$"delete from {db.WEBDB}..FTAPP_SecBatch_Draft 
                                                   WHERE id = @Id";

                        var result = conn.Execute(deleteBatches, batchFounds, trans);
                        if (result <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"Error remove the section batches for pick entry #{dto.DocEntry}.");
                        }
                    }


                    #region insert the boxes as draft
                    // insert box 
                    var insert_draft = @$"INSERT INTO  {db.WEBDB}..FTAPP_SecBox_DRAFT  (
                                    BoxId
                                , PickerCode
                                , PickerName
                                , PickDt                                   
                                , BaseEntry
                                , BoxGuid
                                , IsLooseBox
                                , CreatedDt
                                , TimeStampSeq 
                                , AppVersion
                                , BoxSize , LabelConsistTotalBoxes, PickMode
                                ) VALUES (
                                    @BoxId                                    
                                , @PickerCode
                                , @PickerName
                                , @PickDt                                   
                                , @BaseEntry
                                , @BoxGuid
                                , @IsLooseBox
                                , GETDATE()
                                , @TimeStampSeq 
                                , @AppVersion
                                , @BoxSize, @LabelConsistTotalBoxes, @PickMode
                                )";

                    var insert_res = conn.Execute(insert_draft, dto.Boxes, trans);
                    if (insert_res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Error insert the section box content for pick entry #{dto.DocEntry}.");
                    }

                    #endregion

                    // insert the box content 
                    #region insert box content as draft
                    var boxContents = new List<FTAPP_Box1>();
                    dto.Boxes.ForEach(c =>
                    {
                        if (c.Contents != null)
                        {
                            boxContents.AddRange(c.Contents);
                        }
                    });

                    if (boxContents.Count > 0)
                    {
                        insert_draft = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_SecBox1_DRAFT] 
                                        ( ItemCode
                                        , ItemName
                                        , Qty
                                        , Packaging
                                        , BoxGuid
                                        , ContentGuid
                                        , BaseEntry
                                        , BaseLine
                                        ) values (
                                            @ItemCode
                                        ,@ItemName
                                        ,@Qty
                                        ,@Packaging
                                        ,@BoxGuid
                                        ,@ContentGuid
                                        ,@BaseEntry
                                        ,@BaseLine) ";

                        // insert all box content 
                        insert_res = conn.Execute(insert_draft, boxContents, trans);
                        if (insert_res <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"Error insert the section box content for pick entry #{dto.DocEntry}.");
                        }
                    }
                    #endregion for box content draft

                    // 20250417
                    // insert the save batch section
                    var insertBatches = @$"INSERT INTO {db.WEBDB}..FTAPP_SecBatch_Draft (
                                                DocEntry
                                               ,BaseLine
                                               ,LineNum
                                               ,ItemCode
                                               ,ItemName
                                               ,WhsCode
                                               ,WhsName
                                               ,BatchNo
                                               ,BatchQty
                                               ,CsQty
                                               ,PcQty
                                               ,OBTQ_Abs
                                               ,OBTN_Abs
                                               ,UomQty
                                               ,PickedQty
                                               ,PickedCsQty
                                               ,PickedPcQty
                                               ,BoxId
                                               ,AppVersion
                                               ,TransDt  
                                               ,PickingMode
                                            ) values (
                                                  @DocEntry
                                                 ,@BaseLine
                                                 ,@LineNum
                                                 ,@ItemCode
                                                 ,@ItemName
                                                 ,@WhsCode
                                                 ,@WhsName
                                                 ,@BatchNo
                                                 ,@BatchQty
                                                 ,@CsQty
                                                 ,@PcQty
                                                 ,@OBTQ_Abs
                                                 ,@OBTN_Abs
                                                 ,@UomQty
                                                 ,@PickedQty
                                                 ,@PickedCsQty
                                                 ,@PickedPcQty
                                                 ,@BoxId
                                                 ,@AppVersion
                                                 ,GETDATE() 
                                                 ,@PickingMode);";

                    var insert_batch_res = conn.Execute(insertBatches, dto.Batches, trans);
                    if (insert_res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Error insert the section batches content for pick entry #{dto.DocEntry}.");
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

        // return the customer address information label printing
        IActionResult GetCustomer(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Subsi is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db information retrieve error");
                }

                var sql_getcard = @"exec sp_SelectOCRD @erpDB, @cardCode";
                using var conn = new SqlConnection(_commDbConnStr);
                var card = conn.Query<OCRD_Ext>(sql_getcard, new { erpDB = db.SAPDB, cardCode = dto.CardCode }).FirstOrDefault();

                if (card == null) return NotFound();

                var sql_address = @"exec sp_SelectCRD1 @erpDB, @cardCode, @adresType";
                var ship_address = conn.Query<CRD1>(sql_address,
                        new
                        {
                            erpDB = db.SAPDB,
                            cardCode = dto.CardCode,
                            adresType = 'S'
                        }).FirstOrDefault();

                if (ship_address == null)
                {
                    return Ok(card);
                }

                // get the ship address;
                card.ShipAdd = ship_address.GetAddress();
                return Ok(card);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult VerifyPickMissingSupervisor(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserPCode))
                {
                    return BadRequest("Invalid user p-code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // login with user login name
                // 20210825
                var sp_query = @"exec sp_VerifyWhsAuthUsrV1 @webDb, @userCode ";
                var found = new SqlConnection(_commDbConnStr).Query<USERS>
                            (sp_query, new { webDb = db.WEBDB, userCode = dto.UserCode }).FirstOrDefault();

                if (found != null) return Ok();
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ItemInFreezed(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("Invalid item code");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("WhsCode is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }
                var sql = @"exec sp_SelectFreezedItems @webDb, @itemCode, @whsCode";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<FTAPP_FreezedItems>(sql, new
                {
                    webDb = db.WEBDB,
                    itemCode = dto.ItemCode,
                    whsCode = dto.WhsCode
                }).FirstOrDefault();

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult AddPickLog(Dto_Pick dto)
        {
            if (dto.Log == null)
            {
                return BadRequest("log is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.Log.Subsi))
            {
                return BadRequest("the subsi is empty");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Log.Subsi);
            if (db == null)
            {
                return BadRequest("The db info reading error, please try again.");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var insert_sql = @$"INSERT INTO {db.WEBDB}..FTAPP_PickLog (
                                        TransDt
                                       ,UserCode
                                       ,UserName
                                       ,AuthUserCode
                                       ,AuthUserName
                                       ,ItemCode
                                       ,ItemName
                                       ,CodeBars
                                       ,NeededQty
                                       ,NeededQtyInPc
                                       ,NeededQtyInCs
                                       ,PickedQty
                                       ,PickedQtyInPcs
                                       ,PickedQtyInCs
                                       ,Uom
                                       ,ReportAs
                                       ,WhsName
                                       ,Subsi
                                       ,Branch
                                       ,BranchCode
                                       ,AgencyName
                                       ,AgencyCode
                                       ,BaseEntry
                                       ,BaseLine
                                       ,DocNum
                                       ,StickerNum
                                       ,AppVersion , LabelConsistTotalBoxes
                                      ) VALUES (
                                           GETDATE()
                                         , @UserCode
                                         , @UserName
                                         , @AuthUserCode
                                         , @AuthUserName
                                         , @ItemCode
                                         , @ItemName
                                         , @CodeBars
                                         , @NeededQty
                                         , @NeededQtyInPc
                                         , @NeededQtyInCs
                                         , @PickedQty
                                         , @PickedQtyInPcs
                                         , @PickedQtyInCs
                                         , @Uom
                                         , @ReportAs
                                         , @WhsName
                                         , @Subsi
                                         , @Branch
                                         , @BranchCode
                                         , @AgencyName
                                         , @AgencyCode
                                         , @BaseEntry
                                         , @BaseLine
                                         , @DocNum
                                         , @StickerNum 
                                         , @AppVersion, @LabelConsistTotalBoxes
                                )";


                conn.Execute(insert_sql, dto.Log, trans);
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

        IActionResult SwitchPickToSubmit(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }

                // get the SO 
                // get the so line 

                var sql = @$"SELECT * FROM [{db.WEBDB}].[dbo].[SO] WITH (NOLOCK)
                            WHERE DOCENTRY = @DocEntry ";

                using var conn = new SqlConnection(_commDbConnStr);
                var salesOrder = conn.Query<SO>(sql, new { DocEntry = dto.DocEntry }).FirstOrDefault();
                if (salesOrder == null) return NotFound();

                // query the line. 
                sql = @$"SELECT * FROM [{db.WEBDB}].[dbo].[SO1] WITH (NOLOCK)
                            WHERE DOCENTRY = @DocEntry";

                salesOrder.Lines = conn.Query<SO1>(sql, new { DocEntry = dto.DocEntry }).ToList();

                // 2022 04 06
                // using rest client to post 

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                var address = @$"{svrAdr}SalesOrder/{db.COMPANYID}/submit";
                var client = new RestClient(address);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");

                var body = JsonConvert.SerializeObject(salesOrder);
                request.AddParameter("application/json", body, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                var content = response.Content;
                var isValidContent = IsValidJson(content);

                if (response.IsSuccessful)
                {
                    if (isValidContent)
                    {
                        var result = JsonConvert.DeserializeObject<SoDocResult>(content);
                        result.updateDocType = "Submit";
                        result.docType = dto.Request;
                        return Ok(result);
                    }

                    return BadRequest($"Error when posting to web portal, [Response Success] " +
                                    $"\n\n [Message]\n{content}");
                }
                else // if not sucess
                {
                    if (isValidContent)
                    {
                        var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                        return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                    }

                    return BadRequest($"Error when posting to web portal, " +
                        $"\n\n [Response Fail] [Message]\n{content}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SwitchPickToCancel(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }

                // get the SO 
                // get the so line 

                var sql = @$"SELECT * FROM [{db.WEBDB}].[dbo].[SO] WITH (NOLOCK)
                            WHERE DOCENTRY = @DocEntry 
                            AND DOCSTATUS ='Q'";

                using var conn = new SqlConnection(_commDbConnStr);

                var salesOrder = conn.Query<SO>(sql, new { DocEntry = dto.DocEntry }).FirstOrDefault();
                if (salesOrder == null) return NotFound();

                // query the line. 
                sql = @$"SELECT * FROM [{db.WEBDB}].[dbo].[SO1] WITH (NOLOCK)
                            WHERE DOCENTRY = @DocEntry";

                salesOrder.Lines = conn.Query<SO1>(sql, new { DocEntry = dto.DocEntry }).ToList();
                if ($"{salesOrder.DOCSTATUS}".Equals("I"))
                {
                    return BadRequest($"Doc# {dto.DocEntry} already invoiced, cancel doc void / call off");
                }

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // 2022 04 06
                // using rest client to post 
                var address = $"{svrAdr}SalesOrder/{db.COMPANYID}/cancel";
                var client = new RestClient(address);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");

                var body = JsonConvert.SerializeObject(salesOrder);
                request.AddParameter("application/json", body, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);

                var content = response.Content;
                var isValidContent = IsValidJson(content);

                if (response.IsSuccessful)
                {
                    if (isValidContent)
                    {
                        var result = JsonConvert.DeserializeObject<SoDocResult>(content);
                        result.updateDocType = "Cancel";
                        result.docType = dto.Request;

                        var releasedto = new Dto_Pick
                        {
                            Subsi = dto.Subsi,
                            DocEntry = dto.DocEntry
                        };

                        RemoveDraftSo1BoxBox1(db, dto.DocEntry); // Human set cancel
                        ReleaseSoInPicking(releasedto); // cancel success and release on hold 

                        // set release onhold success
                        var successLog = new FTAPP_AppPostLog
                        {
                            AppModule = "Picked, cancel SO",
                            UserCode = $"{dto.UserCode}",
                            CardCode = $"{dto.CardCode}",
                            SubSi = $"{dto.Subsi}",
                            Details = $"#{dto.DocEntry}, No Picked Qty, Cancel order",
                            PostResult = "Success",
                            AppVersion = $"ServerUpdate, {dto.AppVersion}"
                        };

                        AppPostLogging(successLog);
                        return Ok(result);
                    }

                    return BadRequest($"Error when posting to web portal, [Response Success] " +
                                    $"\n\n [Message]\n{content}");
                }
                else // if not sucess
                {
                    if (isValidContent)
                    {
                        var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                        return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                    }

                    return BadRequest($"Error when posting to web portal, " +
                        $"\n\n [Response Fail] [Message]\n{content}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DeletePickedDraft(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }

                RemoveDraftSo1BoxBox1(db, dto.DocEntry); // delete picked draft
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateLineBarCodeFromSap(Dto_Pick dto)
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

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                // load the barcode table if any 
                var conn = new SqlConnection(_commDbConnStr);

                var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                var BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                        new { erpDb = db.SAPDB, itemCode = dto.ItemCode }).ToList();

                // query the SAP table to the codebar value
                // 20210721
                // for add in the SAP update of the barcode
                var sql_getSAPCodeBar = @$"SELECT TOP 1 *
                                           FROM {db.SAPDB}..OITM WITH (NOLOCK)  
                                           WHERE ItemCode = @itemCode ";

                var line = conn.Query<OITM_Ext>(sql_getSAPCodeBar, new { itemCode = dto.ItemCode }).FirstOrDefault();

                if (line != null)
                {
                    if (BarCodes == null) BarCodes = new List<OBCD_Ext>();

                    var codebar = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.CodeBars,
                        BcdName = string.Empty,
                        ItemCode = dto.ItemCode,
                        UomEntry = -1,
                        DataSource = string.Empty,
                        UserSign = -1,
                        LogInstanc = -1,
                        UserSign2 = -1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    BarCodes.Add(codebar);

                    var itemcode = new OBCD_Ext
                    {
                        BcdEntry = -2,
                        BcdCode = line.ItemCode,
                        BcdName = string.Empty,
                        ItemCode = dto.ItemCode,
                        UomEntry = -1,
                        DataSource = string.Empty,
                        UserSign = -1,
                        LogInstanc = -1,
                        UserSign2 = -1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    BarCodes.Add(itemcode);

                    var suppcatnum = new OBCD_Ext
                    {
                        BcdEntry = -3,
                        BcdCode = line.SuppCatNum,
                        BcdName = string.Empty,
                        ItemCode = dto.ItemCode,
                        UomEntry = -1,
                        DataSource = string.Empty,
                        UserSign = -1,
                        LogInstanc = -1,
                        UserSign2 = -1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    BarCodes.Add(suppcatnum);
                }

                return Ok(BarCodes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CheckSoLineDraftExist(Dto_Pick dto)
        {
            // 20230102
            // remove the with no lock from the draft select statement. 

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The SUBSI name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }

                // check got save draft
                var sql_DraftExist = @$"SELECT distinct 
                                               DOCENTRY
                                              , LINENUM
                                              , ITEMCODE
                                              , ITEMNAME
                                              , CODEBARS
                                              , UOMQTY
                                              , STOCKQTY
                                              , PRICE
                                              , QUANTITY
                                              , QUANTITYCS
                                              , QTY
                                              , DISC
                                              , SUPP
                                              , DISCSUM
                                              , LINETOTAL
                                              , PENTRY
                                              , PLINE
                                              , PTYPE
                                              , SUGGESTQTY
                                              , DOCNUM
                                              , BORNE
                                              , SUPPSUM
                                              , INVQTY
                                              , INVPRICE
                                              , INVTOTAL
                                              , ITEMCOST
                                              , DIM1
                                              , DIM2
                                              , DIM3
                                              , MBID
                                              , SUPPCODE
                                              , QUANTITYPC
                                              , REFNO
                                              , REFITEM
                                              , UOM
                                              , BATCHID
                                              , COKEPROMO
                                              , SUPPCATNUM
                                              , TAXCODE
                                              , PRICE2
                                              , NONIM
                                              , PROMOCOUNT
                                              , NPENTRY
                                              , NPID
                                              , NPLINE
                                              , PROMOPACKAGE
                                              , PICKEDQTY
                                              , REFLINE
                                              , PickedPcs
                                              , PickedCase
                                              , NeededCase
                                              , NeededPcs
                                              , ContentDesc
                                              , SubSi
                                              , IsMissing
                                              , IsMissingCs
                                              , IsMissingPc
                                              , IsAvailableForPick
                                              , AgencyName
                                              , AgencyCode
                                              , QUANTITYPC_Orig
                                              , QUANTITYCS_Orig
                                              , ManBtchNum
                                              , LineRemark
                                              , IsSwitchToPcs
                                              , U_MustCase
                                            FROM {db.WEBDB}..FTAPP_SO1_DRAFT  with (nolock)
                                        Where 
                                            DOCENTRY= @DocEntry order by ITEMNAME asc";

                using var conn = new SqlConnection(_commDbConnStr);

                // read the distinct line only
                // double line ignore
                var results = conn.Query<SO1>(sql_DraftExist, new { dto.DocEntry }).Distinct().ToList();
                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                var query_soPick1 = @$"select ID
                                        ,  DOCENTRY
                                        ,  LINENUM
                                        ,  ITEMCODE
                                        ,  REFITEM
                                        ,  PICKLISTNO
                                        ,  BIN
                                        ,  EXPIRED
                                        ,  QUANTITY
                                        ,  WEIGHT
                                from {db.WEBDB}..SOPICK1   with (nolock)
                                Where DOCENTRY = @DocEntry ";

                var soPick1s = conn.Query<SOPICK1>(query_soPick1, new { DocEntry = dto.DocEntry }).ToList();

                // massge the so line 
                //var newlines = new List<SO1>();
                for (int i = 0; i < results.Count; i++)
                {
                    //var line = results[i];
                    if (results[i] == null) continue;

                    if (results[i].QUANTITY == 0) continue;

                    // load the batch 
                    // if batch manage
                    if (results[i].ManBtchNum == "Y")
                    {
                        var queryBatch = @$"select * from {db.WEBDB}..FTAPP_Batch_Draft 
                                            Where docentry = @docentry
                                            and baseline = @baseline
                                            and itemCode = @itemCode ";

                        results[i].FTAPP_Batches = conn.Query<FTAPP_Batch>(queryBatch, new
                        {
                            docentry = results[i].DOCENTRY,
                            baseline = results[i].LINENUM,
                            itemcode = results[i].ITEMCODE
                        }).ToList();
                    }

                    // avoid division by zero
                    var uomQty = results[i].UOMQTY == 0 ? 1 : results[i].UOMQTY;
                    if (uomQty == 1)
                    {

                        if (results[i].U_MustCase == "Y")
                        {
                            results[i].QUANTITYPC = 0;;
                            results[i].QUANTITYCS = results[i].QUANTITY;
                            results[i].UOM = "CS";
                        }
                        else
                        {
                            results[i].QUANTITYPC = results[i].QUANTITY;
                            results[i].QUANTITYCS = 0;
                            results[i].UOM = "PC";
                        }
                        goto ContinueLoad;
                    }

                    // recalculate the cs and pc
                    results[i].QUANTITYCS = (int)Math.Floor(results[i].QUANTITY / uomQty);
                    results[i].QUANTITYPC = (int)(results[i].QUANTITY % uomQty);

                    // determine the UOM
                    if (results[i].QUANTITYPC == 0)
                    {
                        results[i].UOM = "CS";
                    }
                    else if (results[i].QUANTITYCS == 0)
                    {
                        results[i].UOM = "PC";
                    }
                    else if (results[i].QUANTITYCS > 0 && results[i].QUANTITYPC > 0) // both had value
                    {
                        results[i].UOM = "";
                    }
                    else if (results[i].QUANTITYCS > 0 && results[i].QUANTITYPC == 0)
                    {
                        results[i].UOM = "CS";
                    }
                    else if (results[i].QUANTITYCS == 0 && results[i].QUANTITYPC > 0)
                    {
                        results[i].UOM = "PC";
                    }
                ContinueLoad:

                    // load the barcode table if any 
                    var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                    results[i].BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                            new { erpDb = db.SAPDB, itemCode = results[i].ITEMCODE }).ToList();

                    // load the draft batch is any
                    // 20211121T1637
                    var query_linebatches = @$"Select * from {db.WEBDB}..FTAPP_Batch_Draft with (nolock)
                                                Where DocEntry = @DocEntry and
                                                      BaseLine = @BaseLine and 
                                                      ItemCode = @ItemCode
                                                order by boxid ";

                    results[i].FTAPP_Batches = conn.Query<FTAPP_Batch>(query_linebatches, new
                    {
                        DocEntry = results[i].DOCENTRY,
                        BaseLine = results[i].LINENUM,
                        ItemCode = results[i].ITEMCODE
                    }).ToList();

                    // 20230216
                    // load in the SO pick 1 data                     
                    if (soPick1s.Count > 0)
                    {
                        results[i].SoPick1s = soPick1s
                            .Where(x => x.ITEMCODE == results[i].ITEMCODE &&
                                        x.LINENUM == results[i].LINENUM).ToList();
                    }
                }

                // query the box 
                var sql = @$"SELECT * FROM {db.WEBDB}..FTAPP_Box_DRAFT with (nolock)
                                Where BaseEntry = @DocEntry";

                var boxes = conn.Query<FTAPP_Box>(sql, new { dto.DocEntry }).ToList();
                if (boxes != null && boxes.Count > 0)
                {
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var sql_bContent = $@"SELECT * FROM {db.WEBDB}..FTAPP_Box1_DRAFT  with (nolock)
                                                  Where BaseEntry = @DocEntry 
                                                  AND BoxGuid = @BoxGuid";

                        boxes[b].Contents = conn.Query<FTAPP_Box1>(sql_bContent,
                            new
                            {
                                DocEntry = dto.DocEntry,
                                BoxGuid = boxes[b].BoxGuid
                            }).ToList();
                    }
                }


                var replied = new { DraftLines = results, Boxes = boxes };
                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult PostPicked(Dto_Pick dto)
        {
            try
            {
                // prevent double post 
                // UserTransToken_PostPick

                // check the user in dlb creation 
                if (string.IsNullOrWhiteSpace(dto.UserToken))
                {
                    goto ByPassTransCheck;
                    //return BadRequest("bad user login, please log out app, " +
                    //    "and login again to refresh login token. Thanks");
                }

                // check the memory for the key exist
                if (Program.UserTransToken_PostPick == null) Program.UserTransToken_PostPick = new Dictionary<string, bool>();

                // check user token in list
                var isListed = Program.UserTransToken_PostPick.ContainsKey(dto.UserToken);

                if (isListed) // yes in 
                {
                    bool inTran = Program.UserTransToken_PostPick[dto.UserToken];
                    if (inTran)
                    {
                        return BadRequest("Post picking in process, please wait for moment. Thanks.");
                    }
                    else
                    {
                        Program.UserTransToken_PostPick[dto.UserToken] = true;
                    }
                }
                else // no then add in and set true 
                {
                    Program.UserTransToken_PostPick.Add(dto.UserToken, true); // add and set to intrans
                }

            ByPassTransCheck:

                #region checking 
                if (string.IsNullOrWhiteSpace(dto.RequestName))
                {
                    return BadRequest("The request name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("The company id is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.DocUpdateType))
                {
                    return BadRequest("The company update type is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("The query key is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveAsDraft))
                {
                    return BadRequest("The save draft option is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }

                // 20220325 remove the save draft from pick post 
                if (dto.SaveAsDraft.Equals("Y"))
                {
                    return HandlerSaveLineAsDaft(dto);
                }

                // check SO current status from 
                var db0 = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db0 == null)
                {
                    return BadRequest("Not able to read db info, pls try again.");
                }
                #endregion end 
                var sql_SettingSaveTrace = "Select SetupValue from FTApp_Config Where SetupName  = 'SavePickBeforeAfter'";
                var trcaeSteupValue = new SqlConnection(_commDbConnStr).ExecuteScalar<string>(sql_SettingSaveTrace);

                // 20230129
                // add in save when received from app 
                if (!string.IsNullOrWhiteSpace(trcaeSteupValue) && trcaeSteupValue == "Y")
                {
                    SaveLineFromReceived(db0, dto.PickedDoc.Lines);
                }


                // --------------------------------------------------------------------
                // check invoice existed 
                // double check the inv # in sap
                // 20210728

                #region double check the inv # in sap
                using (var connCheckInv = new SqlConnection(_commDbConnStr))
                {
                    var sql = @$"SELECT DocNum , DocEntry , u_soid
                                 FROM {db0.SAPDB}..OINV WITH (NOLOCK) 
                                 WHERE U_SOID = @docEntry ";

                    var sapInv = connCheckInv.Query<OINV>(sql, new { docEntry = dto.DocEntry }).FirstOrDefault();
                    if (sapInv != null && sapInv.U_SOID.Equals(dto.DocEntry))
                    {
                        if (connCheckInv.State == System.Data.ConnectionState.Closed) connCheckInv.Open();
                        var transInvUpdate = connCheckInv.BeginTransaction();
                        using (transInvUpdate)
                        {
                            try
                            {
                                var update_soWithInv = $@"UPDATE {db0.WEBDB}..SO  
                                        SET INVNO = @InvNo ,  
                                            INVENTRY = @InvEntry ,  
                                            DOCSTATUS = @DocStatus  
                                        WHERE DOCENTRY = @DocEntry ";

                                var result = connCheckInv.Execute(update_soWithInv,
                                                        new
                                                        {
                                                            InvNo = sapInv.DocNum,
                                                            InvEntry = sapInv.DocEntry,
                                                            DocStatus = 'I',
                                                            DocEntry = dto.DocEntry
                                                        }, transInvUpdate);

                                var deleteOnHold = @$"DELETE FROM {db0.WEBDB}..FTAPP_OnHoldSoInPicking
                                                      WHERE HoldDocEntry = @DocEntry ";

                                result = connCheckInv.Execute(deleteOnHold,
                                   new
                                   {
                                       DocEntry = dto.DocEntry
                                   }, transInvUpdate);

                                var newReplied = new SoDocResult
                                {
                                    updateDocType = dto.DocUpdateType,
                                    docType = dto.Request,
                                    INVNO = $"{sapInv.DocNum}"
                                };

                                // prepare replied app                       
                                var newLog1 = new FTAPP_AppPostLog
                                {
                                    AppModule = "Picked to Invoiced",
                                    UserCode = $"{dto.UserCode}",
                                    CardCode = $"{dto.PickedDoc.CARDCODE}",
                                    SubSi = db0.COMPANYNAME,
                                    Details = $"#{dto.PickedDoc.DOCENTRY}, INV# {sapInv.DocNum}, [CODE:CNU]",
                                    PostResult = "2nd Success",
                                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                                };

                                AppPostLogging(newLog1);
                                transInvUpdate.Commit();
                                return Ok(newReplied);
                            }
                            catch (Exception)
                            {
                                transInvUpdate.Rollback();
                                return BadRequest($"Fail auto update created invoice. " +
                                                  $"{db0.COMPANYNAME}, SO #{dto.DocEntry} ");
                            }
                        }
                    }
                } // close database connection

                #endregion
                // --------------------------------------------------------------------
                // check before post
                using (var connCurSo = new SqlConnection(_commDbConnStr))
                {
                    var getCurSo = $@"SELECT * 
                                      FROM {db0.WEBDB}..SO with (nolock)
                                      Where DocEntry = @DocEntry";

                    var soDoc = connCurSo.Query<SO>(getCurSo, new { dto.DocEntry }).FirstOrDefault();
                    if (soDoc == null)
                    {
                        return BadRequest($"{db0.COMPANYNAME}, SO# {dto.DocEntry} no found, no able to perform post to invoice.\n" +
                            $"Please skip the order, and continue next.");
                    }

                    if (!$"{soDoc.DOCSTATUS}".Equals("Q"))
                    {
                        return BadRequest($"{db0.COMPANYNAME}, SO # {dto.DocEntry} [Status] {soDoc.DOCSTATUS}, please process next.");
                    }
                } // close the db sql connection


                // 20220115 
                // try to adjust the pick qty tally with the posting line
                var lines = dto.PickedDoc.Lines;
                using (var pickQtyConn = new SqlConnection(_commDbConnStr))
                {
                    for (int id = 0; id < lines.Count; id++)
                    {
                        var pline = lines[id];
                        if (pline == null) continue;

                        if (pline.QUANTITY != pline.PICKEDQTY)
                        {
                            var sp_query_picked = @"exec sp_GetPickedQty @webDb, @docEntry, @itemCode,  @linenum";
                            var draftboxPickedInPcs = pickQtyConn.ExecuteScalar<decimal>(sp_query_picked, new
                            {
                                webDb = db0.WEBDB,
                                docEntry = pline.DOCENTRY,
                                itemCode = pline.ITEMCODE,
                                linenum = pline.LINENUM
                            });

                            if (pline.PICKEDQTY != draftboxPickedInPcs)
                            {
                                dto.PickedDoc.Lines[id].PICKEDQTY = draftboxPickedInPcs;
                            }
                        }
                    }
                } // close the db sql connection

                // 20210831
                // a combine checked all So1 item 
                if (dto.PickedDoc.Lines != null && dto.PickedDoc.Lines.Count > 0)
                {
                    var whsCode = dto.PickedDoc.WHSCODE;
                    var combinedPickedQty = dto.PickedDoc.Lines.GroupBy(i => i.ITEMCODE).Select(item => new
                    {
                        ItemCode = item.First().ITEMCODE,
                        ItemName = item.First().ITEMNAME,
                        CODEBARS = item.First().CODEBARS,
                        UomQty = item.First().UOMQTY,
                        SUPPCATNUM = item.First().SUPPCATNUM,
                        SumPickedQty = item.Sum(i => i.PICKEDQTY)
                    }).ToList();

                    // check each line picked qty is enough with on hand whs store
                    var message = "";
                    var inSuficient = false;
                    using (var connCheckOITW = new SqlConnection(_commDbConnStr))
                    {
                        combinedPickedQty.ForEach(i =>
                        {
                            var sql_check = $@"select OnHand 
                                            from {db0.SAPDB}..OITW
                                            Where ItemCode = @ItemCode 
                                            and WhsCode = @WhsCode";

                            var onHand = connCheckOITW.ExecuteScalar<decimal>(sql_check, new { ItemCode = i.ItemCode, whsCode = whsCode });
                            if (onHand < i.SumPickedQty)
                            {
                                inSuficient = true;

                                var lines = string.Join(", ",
                                    dto.PickedDoc.Lines
                                            .Where(item => item.ITEMCODE.Equals(i.ItemCode)).Select(line => line.LINENUM).ToList());

                                message += $"Item {i.ItemCode}" +
                                           $"\n{i.ItemName}" +
                                           $"\n{i.CODEBARS}" +
                                           $"\n{i.SUPPCATNUM} " +
                                           $"\n\n#{dto.DocEntry}, " +
                                           $"\nUOM Qty {i.UomQty:N0}\n[Sys. Qty insufficient]" +
                                                 $"\n[Combined Picked Qty] > [Sys. Qty]" +
                                                 $"\n{i.SumPickedQty:N0} > {onHand:N0}" +
                                                 $"\nPls look the item in line(s) {lines}, " +
                                                 $"clear and adjust picked qty to meet {onHand:N0}, then re-post.\n";
                                return;
                            }
                        });
                    } // close the db sql connection

                    if (inSuficient)
                    {
                        return BadRequest(message);
                    }
                }

                // -------------------------------------------------------------
                // check line with period qty on hand
                using (var connPeriodOnHandQty = new SqlConnection(_commDbConnStr))
                {
                    var periodLineQty = $@"SELECT                                         
                                       T1.LINENUM, T1.ITEMCODE, T1.QUANTITY, T1.PICKEDQTY, t1.CODEBARS, t1.ITEMNAME, 
                                       T2.OnHand [STOCKQTY], T3.[U_CSUS_UOM] [UOMQTY]
                                       FROM  [{db0.WEBDB}].dbo.SO  T0 with (nolock) 
		                                        INNER JOIN 
                                             [{db0.WEBDB}].dbo.SO1 T1 with (nolock) ON T1.DOCENTRY = T0.DOCENTRY
		                                        INNER JOIN 
                                             [{db0.SAPDB}].dbo.OITW T2 with (nolock) ON T2.ItemCode = T1.ITEMCODE 
                                                           AND T2.WhsCode = T0.WHSCODE
                                                INNER JOIN 
                                             [{db0.SAPDB}].dbo.OITM T3 with (nolock) ON T2.ItemCode = T3.ITEMCODE 
                                       WHERE T0.DOCENTRY = @DocEntry ";

                    // ---------------------------------------------------------
                    // get all line with period on hand qty
                    var soAvailQtyLine = connPeriodOnHandQty.Query<SO1>(periodLineQty, new { DocEntry = dto.DocEntry }).ToList();
                    if (soAvailQtyLine != null && soAvailQtyLine.Count > 0)
                    {
                        for (int oi = 0; oi < dto.PickedDoc.Lines.Count; oi++)
                        {
                            var postingline = dto.PickedDoc.Lines[oi];
                            if (postingline == null) continue;

                            var found = soAvailQtyLine.FirstOrDefault(x =>
                                                                x.ITEMCODE.Equals(postingline.ITEMCODE) &&
                                                                x.LINENUM.Equals(postingline.LINENUM) &&
                                                                x.STOCKQTY < postingline.PICKEDQTY);
                            if (found != null)
                            {
                                if (found.UOMQTY == 0) found.UOMQTY = 1;

                                var message = $"Item {postingline.ITEMCODE}" +
                                           $"\n{postingline.ITEMNAME}" +
                                           $"\n{postingline.CODEBARS}" +
                                           $"\n{postingline.SUPPCATNUM} " +
                                           $"\n\n#{dto.DocEntry}, Orig. Line # {postingline.LINENUM}, " +
                                           $"\nUOM Qty {found.UOMQTY:N0}\n[Sys. Qty insufficient]" +
                                                 $"\n[Picked Qty] > [Sys. Qty]" +
                                                 $"\n{postingline.PICKEDQTY:N0} > {found.STOCKQTY:N0}" +
                                                 $"\nPls picked {found.STOCKQTY:N0} to this line, and re-post.";

                                return BadRequest(message);
                            }
                        }
                    }
                }

                //20211013T2339
                //recheck the box and lines is tally with pick qty
                //var boxLineChecker = new PickInvoicePostChecker();
                //var message1 = boxLineChecker.CheckBeforePost(dto.PickedDoc.Lines, dto.Boxes);
                //if (!string.IsNullOrWhiteSpace(message1))
                //{
                //    return BadRequest(message1);
                //}

                // --------------------------------------------
                // masssge the SO line with orig SO line 
                using (var connMassageSoLines = new SqlConnection(_commDbConnStr))
                {
                    var sql_getSOLines = $@"SELECT * 
                                            FROM {db0.WEBDB}..SO1 with (nolock) 
                                            WHERE DocEntry = @DocEntry ";

                    var SOLines = new SqlConnection(_commDbConnStr).Query<SO1>(sql_getSOLines, new { dto.DocEntry }).ToList();
                    for (int id = 0; id < SOLines.Count; id++)
                    {
                        var so1 = SOLines[id];
                        SOLines[id].PICKEDQTY = 0; // reset to zero first 

                        if (so1 == null) continue;
                        var appSoLine = dto.PickedDoc.Lines.FirstOrDefault(x => x.ITEMCODE.Equals(so1.ITEMCODE) &&
                                                                                x.LINENUM.Equals(so1.LINENUM));

                        if (appSoLine != null) // copy the picked qty into the orig line 
                        {
                            SOLines[id].PICKEDQTY = appSoLine.PICKEDQTY;
                            SOLines[id].FTAPP_Batches = appSoLine.FTAPP_Batches;
                            SOLines[id].ManBtchNum = appSoLine.ManBtchNum;
                            SOLines[id].LineRemark = appSoLine.LineRemark; // 20231029
                        }
                    }

                    dto.PickedDoc.Lines = new List<SO1>(SOLines);  // 20230129   
                    if (!string.IsNullOrWhiteSpace(trcaeSteupValue) && $"{trcaeSteupValue}".ToLower().Equals("Y")) // mean no 
                    {
                        SaveLineFromPick(db0, dto.PickedDoc.Lines); // before massage
                        SaveLineForPost(db0, SOLines); // after massage                         
                    }
                } // close the sql connection

                // ----------------------------------------------
                // remove the box with no content 
                // filter out the box with no content 
                // v 45 / 46
                // 2021 07 24
                if (dto.Boxes != null)
                {
                    // filter the box
                    var boxes = dto.Boxes.Where(b => b.Contents != null && b.Contents.Count > 0).ToList();

                    // read the setting 
                    // BypassBoxPickedDtMassage
                    var sql_Setting = "Select SetupValue from FTApp_Config Where SetupName  = 'BypassBoxPickedDtMassage'";
                    var isByPass = new SqlConnection(_commDbConnStr).ExecuteScalar<string>(sql_Setting);

                    // turn off the box massage 
                    //if (!string.IsNullOrWhiteSpace(isByPass) && isByPass == "N")
                    //{                        
                    //20210928 check the time of the box
                    //for (int b = 0; b < boxes.Count; b++)
                    //{
                    //    boxes[b].PickDt = BoxPickDtMassage(boxes[b]);
                    //}
                    //}

                    // 20220902
                    // check the picked qty = 0
                    // check the box contain the qty, if yes set the line pickedqty based on box 

                    for (int x = 0; x < dto.PickedDoc.Lines.Count; x++)
                    {
                        var line = dto.PickedDoc.Lines[x];
                        if (line == null) continue;
                        if (line.PICKEDQTY > 0) continue;

                        // only for picked qty = zero
                        for (int b = 0; b < boxes.Count; b++)
                        {
                            var box = boxes[b];
                            if (box == null) continue;

                            var foundContent = box.Contents.Where(x => x.ItemCode == line.ITEMCODE &&
                                                                       x.BaseLine == line.LINENUM).ToList();

                            if (foundContent.Count == 0) continue;

                            decimal sumOfQty = 0;
                            foundContent.ForEach(s =>
                            {
                                if (s.Packaging == "CS")
                                {
                                    sumOfQty += line.UOMQTY * s.Qty;
                                }
                                else
                                {
                                    sumOfQty += s.Qty;
                                }
                            });

                            if (sumOfQty > 0)
                            {
                                dto.PickedDoc.Lines[x].PICKEDQTY = sumOfQty;
                            }
                        }
                    }

                    // 20240905
                    // massage the content qty 
                    // when box qty modulus qty uon equal to zero 
                    // then save the box content to box 
                    // update the box head label consist to box content
                    //for (int b = 0; b < boxes.Count; b++)
                    //{
                    //    var box = boxes[b];
                    //    if (box == null) continue;
                    //    if (box.Contents == null) continue;

                    //    if (box.Contents.Count == 1)
                    //    {
                    //        var content = box.Contents[0];
                    //        if (content.Packaging == "CS") continue; // ignore all CS packaging 
                    //        var itemCode = content.ItemCode;

                    //        var uomQty = dto.PickedDoc.Lines.Where(c => c.ITEMCODE == itemCode).FirstOrDefault()?.UOMQTY;
                    //        if (uomQty == 0) continue; // avoid div by zero

                    //        var moduleRes = box.Contents[0].Qty % uomQty;
                    //        if (moduleRes == 0 &&
                    //            boxes[b].Contents[0].Packaging == "PC")
                    //        {
                    //            var noOfBox = box.Contents[0].Qty / uomQty;
                    //            boxes[b].Contents[0].Qty = (decimal)noOfBox;
                    //            boxes[b].Contents[0].Packaging = "CS";
                    //            boxes[b].LabelConsistTotalBoxes = (int)noOfBox;
                    //        }
                    //    }                      
                    //}

                    dto.Boxes = boxes; // modified boxes 

                    // save the box 
                    var result = SaveBoxes(boxes, db0, dto.DocEntry); // save before post
                    if (!result)
                    {
                        return BadRequest($@"Pick post invoice , 
                                          Error saving box and it content, please try again [SSJ2023]
                                          \n{db0.COMPANYNAME}\nSO# {dto.DocEntry}");
                    }
                }

                // batch add on
                // 2021 11 28 Save the batch for line 
                for (int id = 0; id < dto.PickedDoc.Lines.Count; id++)
                {
                    // copy the details batch into line for posting 
                    var line = dto.PickedDoc.Lines[id];
                    if (line == null) continue;

                    if (line.FTAPP_Batches == null) continue;
                    if (line.FTAPP_Batches.Count == 0) continue;

                    dto.PickedDoc.Lines[id].Batches = new List<BatchNo>();

                    var batches = line.FTAPP_Batches;
                    for (int b = 0; b < batches.Count; b++)
                    {
                        var readBatches = batches[b];
                        if (readBatches == null) continue;
                        var batch = new BatchNo
                        {
                            Docentry = readBatches.DocEntry,
                            Linenum = readBatches.BaseLine,
                            Linenum2 = readBatches.LineNum,
                            Batchno = readBatches.BatchNo,
                            Quantity = readBatches.PickedQty,
                        };

                        dto.PickedDoc.Lines[id].Batches.Add(batch); // temp hide ***************
                                                                    // reset the App batch tp null
                        dto.PickedDoc.Lines[id].FTAPP_Batches = null;
                    }
                }

                // massage the del date 
                // 20121221 incase of miss delivery date
                if (dto.PickedDoc.DELDATE == default)
                {
                    dto.PickedDoc.DELDATE = dto.PickedDoc.DOCDATE.AddDays(2);
                }

                // massage the ship to address 
                // read the current SAP card code address 
                // before posting
                // 20211231
                using (var connMsgShipAddr = new SqlConnection(_commDbConnStr))
                {
                    var sp_query_ShiptoAddres = @"exec sp_SelectAddress @erpDb, @cardCode, @addrssType";
                    var shipTo_cardAddress = connMsgShipAddr.ExecuteScalar<string>(sp_query_ShiptoAddres, new
                    {
                        erpDb = db0.SAPDB,
                        cardCode = dto.PickedDoc.CARDCODE,
                        addrssType = "S" // ship address
                    });

                    if (!string.IsNullOrWhiteSpace(shipTo_cardAddress))
                    {
                        dto.PickedDoc.SHIPTOADD = shipTo_cardAddress;
                        dto.PickedDoc.SHIPTO = shipTo_cardAddress;
                    }

                    // update the bill to address
                    //  20240905
                    var sp_query_BilltoAddres = @"exec sp_SelectAddress @erpDb, @cardCode, @addrssType";
                    var billTo_cardAddress = connMsgShipAddr.ExecuteScalar<string>(sp_query_BilltoAddres, new
                    {
                        erpDb = db0.SAPDB,
                        cardCode = dto.PickedDoc.CARDCODE,
                        addrssType = "B" // bill address
                    });

                    if (!string.IsNullOrWhiteSpace(billTo_cardAddress))
                    {
                        dto.PickedDoc.BILLTOADD = billTo_cardAddress;
                        dto.PickedDoc.BILLTO = billTo_cardAddress;
                    }
                }

                // post with redsharp
                //var client = new RestClient($"{WebHostAddrEndPoint}{dto.RequestName}/{dto.CompanyId}/{dto.DocUpdateType}");
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db0.PostSvrAdressPort) ? db0.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}{dto.RequestName}/{dto.CompanyId}/{dto.DocUpdateType}");

                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.PickedDoc);

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var INVNO = GetInvNo(dto, db0);

                    // handler success
                    var content = response.Content; // await response.Content.ReadAsStringAsync();
                    var isValidJson0 = IsValidJson(content);

                    if (isValidJson0)
                    {
                        var result = JsonConvert.DeserializeObject<SoDocResult>(content);
                        result.updateDocType = dto.DocUpdateType;
                        result.docType = dto.Request;
                        result.INVNO = INVNO;

                        var newLog1 = new FTAPP_AppPostLog
                        {
                            AppModule = "Picked to Invoiced",
                            UserCode = $"{dto.UserCode}",
                            CardCode = $"{dto.PickedDoc.CARDCODE}",
                            SubSi = db0.COMPANYNAME,
                            Details = $"#{dto.PickedDoc.DOCENTRY}, INV# {INVNO}, received: {content}",
                            PostResult = "1st Success",
                            AppVersion = $"ServerUpdate {dto.AppVersion}"
                        };
                        AppPostLogging(newLog1);

                        // 20230527
                        // repair the box 
                        //var sp_repairboxes = @"exec sp_BoxesRemoveDuplicateBoxAndResetPackIds @webDb, @SoDocEntry";
                        //using var conn_repair = new SqlConnection(_commDbConnStr);
                        //conn_repair.Execute(sp_repairboxes, new { webDb = db0.WEBDB, SoDocEntry = dto.PickedDoc.DOCENTRY });
                        // update for active call for this cardcall 

                        return Ok(result);
                    }

                    // no valid json received
                    var newLog0 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.PickedDoc.CARDCODE}",
                        SubSi = db0.COMPANYNAME,
                        Details = $"#{dto.PickedDoc.DOCENTRY}, received: {content}",
                        PostResult = $"1st Post Fail",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    AppPostLogging(newLog0);
                    var newReplied1 = new PortalReplied
                    {
                        actionSuccess = false,
                        errorMessage = $"Received null response\n{content}",
                        actionResult = $"Fail post, #{dto.PickedDoc.DOCENTRY}",
                        documentStatus = dto.PickedDoc.DOCSTATUS
                    };
                    return BadRequest(newReplied1);
                }

                // if not success
                var content1 = response.Content;// await response.Content.ReadAsStringAsync();
                var isValidJson = IsValidJson(content1);
                if (!isValidJson)
                {
                    var newLog0 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.PickedDoc.CARDCODE}",
                        SubSi = $"{dto.PickedDoc.SubSi}",
                        Details = $"{dto.PickedDoc.DOCENTRY}, {content1}",
                        PostResult = $"1st Post Fail",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    AppPostLogging(newLog0);

                    var newReplied1 = new PortalReplied
                    {
                        actionSuccess = false,
                        errorMessage = content1,
                        actionResult = "Fail, Error when posting to web portal",
                        documentStatus = dto.PickedDoc.DOCSTATUS
                    };

                    return BadRequest(newReplied1);
                }

                // try convert
                var result1 = JsonConvert.DeserializeObject<PortalReplied>(content1);
                if (result1 == null) // convert fail
                {
                    var newLog1 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.PickedDoc.CARDCODE}",
                        SubSi = db0.COMPANYNAME,
                        Details = $"{dto.PickedDoc.DOCENTRY}, {content1}",
                        PostResult = $"1st Post Fail",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };
                    AppPostLogging(newLog1);

                    var newReplied1 = new PortalReplied
                    {
                        actionSuccess = false,
                        errorMessage = content1,
                        actionResult = "Fail, Error when posting to web portal",
                        documentStatus = dto.PickedDoc.DOCSTATUS
                    };
                    return BadRequest(newReplied1);
                }

                // else
                var newLog = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.PickedDoc.CARDCODE}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"#{dto.PickedDoc.DOCENTRY}" + result1.errorMessage,
                    PostResult = $"1st Post Fail",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                AppPostLogging(newLog);

                // 20220708
                // check the batch item line to replied the user
                // let user know the line item having the issue
                if (!string.IsNullOrWhiteSpace(result1.errorMessage) &&
                    result1.errorMessage.ToLower().Contains("Cannot add row without complete selection of batch/serial numbers".ToLower()))
                {
                    using (var execSpConn = new SqlConnection(_commDbConnStr))
                    {
                        var sp_CheckPickedQtyOverBatchQtyList = "exec sp_CheckBatchQtyVsPickedQty @webDb, @docEntry";
                        var overedBatchQtys = execSpConn.Query<PickQtyVsBathQty>(sp_CheckPickedQtyOverBatchQtyList, new
                        {
                            webDb = db0.WEBDB,
                            docEntry = dto.DocEntry
                        }).ToList();

                        // if there line or picked over batch
                        if (overedBatchQtys.Count > 0)
                        {
                            var repliedMsg = $"Doc #{dto.DocEntry}\n\n";
                            for (int m = 0; m < overedBatchQtys.Count; m++)
                            {
                                var line = overedBatchQtys[m];
                                if (line == null) continue;
                                repliedMsg += $"Line # {line.LineNum}\n{line.ItemCode}\n{line.ItemName}" +
                                    $"\nPicked qty {line.PickedQty:N2} not equal to {line.BatchQty:N2} batch qty." +
                                    $"\nPlease pick qty as {line.BatchQty:N2} pcs for line # {line.LineNum}";
                            }

                            repliedMsg += "\n\nPlease try post again.";
                            return BadRequest(repliedMsg);
                        }
                    }

                    // 20220708
                    // check batch information before posting 
                    // -------------------------------
                    return BadRequest(result1);
                }

                return BadRequest(result1);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var newLog = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to Invoiced",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.PickedDoc.CARDCODE}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"#{dto.PickedDoc.DOCENTRY}, Excep, " + LastError,
                    PostResult = $"1st Post Fail",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };
                AppPostLogging(newLog);

                var newReplied = new PortalReplied
                {
                    actionSuccess = false,
                    errorMessage = LastError,
                    actionResult = "Fail",
                    documentStatus = dto.PickedDoc.DOCSTATUS
                };

                return BadRequest(newReplied);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(dto.UserToken) && Program.UserTransToken_PostPick.Count > 0)
                {
                    Program.UserTransToken_PostPick.Remove(dto.UserToken);
                }
            }
        }

        DateTime BoxPickDtMassage(FTAPP_Box box)
        {
            try
            {
                var dt = box.PickDt;
                if (dt == default) return DateTime.Now;

                const string _Am = "AM";
                const string _Pm = "PM";
                var hr = dt.Hour;

                var pickAmPm = dt.ToString("tt", CultureInfo.InvariantCulture);
                var svrAmPm = DateTime.Now.ToString("tt", CultureInfo.InvariantCulture);

                switch (hr)
                {
                    case 1:
                    case 13:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 13, dt.Minute, dt.Second);
                            //if (hr == 1 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 13, dt.Minute, dt.Second);
                            //if (hr == 13 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 1, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 2:
                    case 14:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 14, dt.Minute, dt.Second);
                            //if (hr == 2 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 14, dt.Minute, dt.Second);
                            //if (hr == 14 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 2, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 3:
                    case 15:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 15, dt.Minute, dt.Second);
                            //if (hr == 3 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 15, dt.Minute, dt.Second);
                            //if (hr == 15 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 3, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 4:
                    case 16:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 16, dt.Minute, dt.Second);
                            //if (hr == 4 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 16, dt.Minute, dt.Second);
                            //if (hr == 16 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 4, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 5:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 17, dt.Minute, dt.Second);
                            //if (hr == 5 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 17, dt.Minute, dt.Second);
                            //if (hr == 17 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 5, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 6:
                    case 18:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 18, dt.Minute, dt.Second);
                            if (hr == 6 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 18, dt.Minute, dt.Second);
                            if (hr == 18 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 6, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 7:
                    case 19:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 19, dt.Minute, dt.Second);
                            if (hr == 7 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 19, dt.Minute, dt.Second);
                            if (hr == 19 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 7, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 8:
                    case 20:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 8, dt.Minute, dt.Second);
                            if (hr == 8 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 20, dt.Minute, dt.Second);
                            if (hr == 20 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 8, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 9:
                    case 21:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 9, dt.Minute, dt.Second);
                            if (hr == 9 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 21, dt.Minute, dt.Second);
                            if (hr == 21 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 9, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 10:
                    case 22:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 10, dt.Minute, dt.Second);
                            if (hr == 10 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 22, dt.Minute, dt.Second);
                            if (hr == 22 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 10, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 11:
                    case 23:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 11, dt.Minute, dt.Second);
                            if (hr == 11 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 23, dt.Minute, dt.Second);
                            if (hr == 23 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 11, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 0:
                    case 12:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 12, dt.Minute, dt.Second);
                            if (hr == 0 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 12, dt.Minute, dt.Second);
                            if (hr == 12 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 0, dt.Minute, dt.Second);
                            return dt;
                        }
                    default:
                        return dt;
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return box.PickDt;
            }
        }

        void SaveLineFromReceived(DbInfo db, List<SO1> lines)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sql_insert = $@"INSERT INTO {db.WEBDB}..[FTAPP_SO1_OnReceived] ( 
                                     DOCENTRY
                                   , LINENUM
                                   , ITEMCODE
                                   , ITEMNAME
                                   , CODEBARS
                                   , UOMQTY
                                   , STOCKQTY
                                   , PRICE
                                   , QUANTITY
                                   , QUANTITYCS
                                   , QTY
                                   , DISC
                                   , SUPP
                                   , DISCSUM
                                   , LINETOTAL
                                   , PENTRY
                                   , PLINE
                                   , PTYPE
                                   , SUGGESTQTY
                                   , DOCNUM
                                   , BORNE
                                   , SUPPSUM
                                   , INVQTY
                                   , INVPRICE
                                   , INVTOTAL
                                   , ITEMCOST
                                   , DIM1
                                   , DIM2
                                   , DIM3
                                   , MBID
                                   , SUPPCODE
                                   , QUANTITYPC
                                   , REFNO
                                   , REFITEM
                                   , UOM
                                   , BATCHID
                                   , COKEPROMO
                                   , SUPPCATNUM
                                   , TAXCODE
                                   , PRICE2
                                   , NONIM
                                   , PROMOCOUNT
                                   , NPENTRY
                                   , NPID
                                   , NPLINE
                                   , PROMOPACKAGE
                                   , PICKEDQTY
                                   , REFLINE
                                   , REFORDER
                                   , REFUOM
                                   , TPBRANCH , LineRemark
                                ) VALUES (
                                    @DOCENTRY
                                   ,@LINENUM
                                   ,@ITEMCODE
                                   ,@ITEMNAME
                                   ,@CODEBARS
                                   ,@UOMQTY
                                   ,@STOCKQTY
                                   ,@PRICE
                                   ,@QUANTITY
                                   ,@QUANTITYCS
                                   ,@QTY
                                   ,@DISC
                                   ,@SUPP
                                   ,@DISCSUM
                                   ,@LINETOTAL
                                   ,@PENTRY
                                   ,@PLINE
                                   ,@PTYPE
                                   ,@SUGGESTQTY
                                   ,@DOCNUM
                                   ,@BORNE
                                   ,@SUPPSUM
                                   ,@INVQTY
                                   ,@INVPRICE
                                   ,@INVTOTAL
                                   ,@ITEMCOST
                                   ,@DIM1
                                   ,@DIM2
                                   ,@DIM3
                                   ,@MBID
                                   ,@SUPPCODE
                                   ,@QUANTITYPC
                                   ,@REFNO
                                   ,@REFITEM
                                   ,@UOM
                                   ,@BATCHID
                                   ,@COKEPROMO
                                   ,@SUPPCATNUM
                                   ,@TAXCODE
                                   ,@PRICE2
                                   ,@NONIM
                                   ,@PROMOCOUNT
                                   ,@NPENTRY
                                   ,@NPID
                                   ,@NPLINE
                                   ,@PROMOPACKAGE
                                   ,@PICKEDQTY
                                   ,@REFLINE
                                   ,@REFORDER
                                   ,@REFUOM
                                   ,@TPBRANCH , @LineRemark
                            )";
                var result = conn.Execute(sql_insert, lines, trans);
                if (result <= 0)
                {
                    trans.Rollback();
                    return;
                }

                trans.Commit();
                return;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                LastError = $"{ex.Message}\n{ex.StackTrace}";
                _logger.LogError(LastError);
                return;
            }
        }

        void SaveLineFromPick(DbInfo db, List<SO1> lines)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sql_insert = $@"INSERT INTO {db.WEBDB}..[FTAPP_SO1_FROMPICK] ( 
                                     DOCENTRY
                                   , LINENUM
                                   , ITEMCODE
                                   , ITEMNAME
                                   , CODEBARS
                                   , UOMQTY
                                   , STOCKQTY
                                   , PRICE
                                   , QUANTITY
                                   , QUANTITYCS
                                   , QTY
                                   , DISC
                                   , SUPP
                                   , DISCSUM
                                   , LINETOTAL
                                   , PENTRY
                                   , PLINE
                                   , PTYPE
                                   , SUGGESTQTY
                                   , DOCNUM
                                   , BORNE
                                   , SUPPSUM
                                   , INVQTY
                                   , INVPRICE
                                   , INVTOTAL
                                   , ITEMCOST
                                   , DIM1
                                   , DIM2
                                   , DIM3
                                   , MBID
                                   , SUPPCODE
                                   , QUANTITYPC
                                   , REFNO
                                   , REFITEM
                                   , UOM
                                   , BATCHID
                                   , COKEPROMO
                                   , SUPPCATNUM
                                   , TAXCODE
                                   , PRICE2
                                   , NONIM
                                   , PROMOCOUNT
                                   , NPENTRY
                                   , NPID
                                   , NPLINE
                                   , PROMOPACKAGE
                                   , PICKEDQTY
                                   , REFLINE
                                   , REFORDER
                                   , REFUOM
                                   , TPBRANCH , LineRemark
                                ) VALUES (
                                    @DOCENTRY
                                   ,@LINENUM
                                   ,@ITEMCODE
                                   ,@ITEMNAME
                                   ,@CODEBARS
                                   ,@UOMQTY
                                   ,@STOCKQTY
                                   ,@PRICE
                                   ,@QUANTITY
                                   ,@QUANTITYCS
                                   ,@QTY
                                   ,@DISC
                                   ,@SUPP
                                   ,@DISCSUM
                                   ,@LINETOTAL
                                   ,@PENTRY
                                   ,@PLINE
                                   ,@PTYPE
                                   ,@SUGGESTQTY
                                   ,@DOCNUM
                                   ,@BORNE
                                   ,@SUPPSUM
                                   ,@INVQTY
                                   ,@INVPRICE
                                   ,@INVTOTAL
                                   ,@ITEMCOST
                                   ,@DIM1
                                   ,@DIM2
                                   ,@DIM3
                                   ,@MBID
                                   ,@SUPPCODE
                                   ,@QUANTITYPC
                                   ,@REFNO
                                   ,@REFITEM
                                   ,@UOM
                                   ,@BATCHID
                                   ,@COKEPROMO
                                   ,@SUPPCATNUM
                                   ,@TAXCODE
                                   ,@PRICE2
                                   ,@NONIM
                                   ,@PROMOCOUNT
                                   ,@NPENTRY
                                   ,@NPID
                                   ,@NPLINE
                                   ,@PROMOPACKAGE
                                   ,@PICKEDQTY
                                   ,@REFLINE
                                   ,@REFORDER
                                   ,@REFUOM
                                   ,@TPBRANCH , @LineRemark
                            )";

                var result = conn.Execute(sql_insert, lines, trans);
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                LastError = $"{ex.Message}\n{ex.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        // for actual data sent to post 
        // for later verification check 
        // 
        void SaveLineForPost(DbInfo db, List<SO1> lines)
        {

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sql_insert = $@"INSERT INTO {db.WEBDB}..[FTAPP_SO1_POSTED] ( 
                                     DOCENTRY
                                   , LINENUM
                                   , ITEMCODE
                                   , ITEMNAME
                                   , CODEBARS
                                   , UOMQTY
                                   , STOCKQTY
                                   , PRICE
                                   , QUANTITY
                                   , QUANTITYCS
                                   , QTY
                                   , DISC
                                   , SUPP
                                   , DISCSUM
                                   , LINETOTAL
                                   , PENTRY
                                   , PLINE
                                   , PTYPE
                                   , SUGGESTQTY
                                   , DOCNUM
                                   , BORNE
                                   , SUPPSUM
                                   , INVQTY
                                   , INVPRICE
                                   , INVTOTAL
                                   , ITEMCOST
                                   , DIM1
                                   , DIM2
                                   , DIM3
                                   , MBID
                                   , SUPPCODE
                                   , QUANTITYPC
                                   , REFNO
                                   , REFITEM
                                   , UOM
                                   , BATCHID
                                   , COKEPROMO
                                   , SUPPCATNUM
                                   , TAXCODE
                                   , PRICE2
                                   , NONIM
                                   , PROMOCOUNT
                                   , NPENTRY
                                   , NPID
                                   , NPLINE
                                   , PROMOPACKAGE
                                   , PICKEDQTY
                                   , REFLINE
                                   , REFORDER
                                   , REFUOM
                                   , TPBRANCH , LineRemark
                                ) VALUES (
                                    @DOCENTRY
                                   ,@LINENUM
                                   ,@ITEMCODE
                                   ,@ITEMNAME
                                   ,@CODEBARS
                                   ,@UOMQTY
                                   ,@STOCKQTY
                                   ,@PRICE
                                   ,@QUANTITY
                                   ,@QUANTITYCS
                                   ,@QTY
                                   ,@DISC
                                   ,@SUPP
                                   ,@DISCSUM
                                   ,@LINETOTAL
                                   ,@PENTRY
                                   ,@PLINE
                                   ,@PTYPE
                                   ,@SUGGESTQTY
                                   ,@DOCNUM
                                   ,@BORNE
                                   ,@SUPPSUM
                                   ,@INVQTY
                                   ,@INVPRICE
                                   ,@INVTOTAL
                                   ,@ITEMCOST
                                   ,@DIM1
                                   ,@DIM2
                                   ,@DIM3
                                   ,@MBID
                                   ,@SUPPCODE
                                   ,@QUANTITYPC
                                   ,@REFNO
                                   ,@REFITEM
                                   ,@UOM
                                   ,@BATCHID
                                   ,@COKEPROMO
                                   ,@SUPPCATNUM
                                   ,@TAXCODE
                                   ,@PRICE2
                                   ,@NONIM
                                   ,@PROMOCOUNT
                                   ,@NPENTRY
                                   ,@NPID
                                   ,@NPLINE
                                   ,@PROMOPACKAGE
                                   ,@PICKEDQTY
                                   ,@REFLINE
                                   ,@REFORDER
                                   ,@REFUOM
                                   ,@TPBRANCH , @LineRemark
                            )";
                var result = conn.Execute(sql_insert, lines, trans);
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                LastError = $"{ex.Message}\n{ex.StackTrace}";
                _logger.LogError(LastError);
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

        string GetInvNo(Dto_Pick dto, DbInfo db)
        {
            try
            {
                var sql = $"SELECT INVNO " +
                          $"FROM {db.WEBDB}..SO WITH (NOLOCK) " +
                          $"WHERE DOCENTRY= @docEntry ";

                using var conn = new SqlConnection(_commDbConnStr);
                var SO_InvNo = conn.ExecuteScalar<string>(sql, new { docEntry = dto.PickedDoc.DOCENTRY });

                if (!string.IsNullOrWhiteSpace(SO_InvNo)) return SO_InvNo;
                return string.Empty;

                // double check the inv # iin sap
                //sql = @$"SELECT DocNum 
                //         FROM [{db.SAPDB}].[dbo].[OINV] WITH (NOLOCK) 
                //         WHERE DocNum = @SO_InvNo 
                //               AND U_SOID = @docEntry ";

                //return conn.ExecuteScalar<string>(sql, new { SO_InvNo = SO_InvNo, docEntry = docEntry });
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return string.Empty;
            }
        }

        bool SaveBoxes(List<FTAPP_Box> boxes, DbInfo db, int docEntry)
        {
            if (boxes == null) return true;
            if (boxes.Count == 0) return true;

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                //---------------------------------
                var checkDuplicate_sql = @$"Select top 2 Boxid 
                                            from  {db.WEBDB}..FTAPP_Box with (nolock)
                                            Where BaseEntry = @DocEntry ";

                var boxListExist = conn.Query<FTAPP_Box>(checkDuplicate_sql, new { DocEntry = docEntry }, trans).ToList();
                if (boxListExist.Count > 0) // if found box delete
                {
                    // remove the old save boxes 
                    var sql_removeOldBox = @$"Delete from {db.WEBDB}..FTAPP_Box 
                                            Where BaseEntry = @DocEntry ";

                    var res = conn.Execute(sql_removeOldBox, new { DocEntry = docEntry }, trans, commandTimeout: 0);
                    if (res <= 0)
                    {
                        trans.Rollback();
                        _logger.LogError($"Error delete BOX for {db.COMPANYNAME}, SO #{docEntry}");
                        return false;
                    }
                }

                // check box 1 ---------------------------------
                checkDuplicate_sql = $"Select top 2 boxguid from  [{db.WEBDB}]..[FTAPP_Box1] with (nolock) " +
                                       " Where BaseEntry = @DocEntry ";

                var box1List = conn.Query<FTAPP_Box>(checkDuplicate_sql, new { DocEntry = docEntry }, trans).ToList();
                if (box1List.Count > 0)
                {
                    var sql_removeOldBox1 = $"Delete from  [{db.WEBDB}].[dbo].[FTAPP_Box1] " +
                                       " Where BaseEntry= @DocEntry ";

                    var res = conn.Execute(sql_removeOldBox1, new { DocEntry = docEntry }, trans, commandTimeout: 0);
                    if (res <= 0)
                    {
                        trans.Rollback();
                        _logger.LogError($"Error delete BOX for {db.COMPANYNAME}, SO #{docEntry}");
                        return false;
                    }
                }

                // 20220509
                var insert_box = @$"INSERT INTO {db.WEBDB}..FTAPP_Box (
                                          BoxId
                                        , PickerCode
                                        , PickerName
                                        , PickDt
                                        , BaseEntry 
                                        , BoxGuid
                                        , TimeStampSeq 
                                        , AppVersion
                                        , BoxSize, LabelConsistTotalBoxes, PickMode
                                        ) VALUES ( 
                                          @BoxId
                                        , @PickerCode
                                        , @PickerName
                                        , @PickDt
                                        , @BaseEntry 
                                        , @BoxGuid
                                        , @TimeStampSeq 
                                        , @AppVersion
                                        , @BoxSize, @LabelConsistTotalBoxes, @PickMode
                                        )";

                var insertRes = conn.Execute(insert_box, boxes.Distinct().ToList(), trans, commandTimeout: 0);
                if (insertRes <= 0)
                {
                    trans.Rollback();
                    _logger.LogError($"Error insert BOX for {db.COMPANYNAME}, SO #{docEntry}");
                    return false;
                }

                // massage the box 
                var boxContents = new List<FTAPP_Box1>();
                boxes.ForEach(b =>
                  {
                      if (b != null && b.Contents != null)
                      {
                          boxContents.AddRange(b.Contents);
                      }
                  });


                if (boxContents.Count == 0)
                {
                    trans.Commit();
                    return true;
                }

                var insert_boxContent = @$"INSERT INTO {db.WEBDB}..FTAPP_Box1 (
                                        ItemCode
                                        , ItemName
                                        , Qty
                                        , Packaging
                                        , BoxGuid
                                        , ContentGuid
                                        , BaseEntry
                                        , BaseLine
                                    ) VALUES (
                                        @ItemCode
                                        , @ItemName
                                        , @Qty
                                        , @Packaging
                                        , @BoxGuid
                                        , @ContentGuid
                                        , @BaseEntry
                                        , @BaseLine
                                    )";

                var insertBoxContentsRes = conn.Execute(insert_boxContent, boxContents.Distinct().ToList(),
                                                        trans, commandTimeout: 0);
                if (insertBoxContentsRes <= 0)
                {
                    trans.Rollback();
                    _logger.LogError($"Error insert BOX for {db.COMPANYNAME}, SO #{docEntry}");
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

        IActionResult HandlerSaveLineAsDaft(Dto_Pick dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("The company name is empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Db info reading error");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // remove daft so1 -------------------
                var check_sp = $@"Select * from {db.WEBDB}..FTAPP_SO1_DRAFT  where DocEntry = @DocEntry ";
                var founds = conn.Query<SO1>(check_sp, new { DocEntry = dto.DocEntry }, transaction).ToList();
                if (founds.Count > 0)
                {
                    var delete_draft = @$"Delete from {db.WEBDB}..FTAPP_SO1_DRAFT
                                     where DOCENTRY = @DOCENTRY ";

                    var deleteres = conn.Execute(delete_draft, founds, transaction);
                    if (deleteres <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error delete FTAPP_SO1_DRAFT for {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }

                // ---------------------------
                var sp_checkBox = $@"Select *  from {db.WEBDB}..FTAPP_Box_DRAFT
                                     where BaseEntry = @DocEntry";
                var foundBoxes = conn.Query<FTAPP_Box>(sp_checkBox, new { DocEntry = dto.DocEntry }, transaction).ToList();
                if (foundBoxes.Count > 0)
                {
                    // remove daft box -------------------
                    var delete_draft = @$"Delete from {db.WEBDB}..FTAPP_Box_DRAFT
                                      where BaseEntry = @BaseEntry ";
                    var deleteres = conn.Execute(delete_draft, foundBoxes, transaction);
                    if (deleteres <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error delete FTAPP_Box_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }

                // ---------------------------
                var sp_checkBox1 = $@"Select * from {db.WEBDB}..FTAPP_Box1_DRAFT
                                     where BaseEntry = @DocEntry ";

                var foundBox1s = conn.Query<FTAPP_Box1>(sp_checkBox1, new { DocEntry = dto.DocEntry }, transaction).ToList();
                if (foundBox1s.Count > 0)
                {
                    // remove draft box line -------------------
                    var delete_draft = @$"Delete from {db.WEBDB}..FTAPP_Box1_DRAFT
                                          where BaseEntry = @BaseEntry ";
                    var deleteres = conn.Execute(delete_draft, foundBox1s, transaction);
                    if (deleteres <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error delete FTAPP_Box1_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }


                // remove the doc entry batch 
                // delete the line 
                // 20240521
                var sp_check_batch = @$"select * from  {db.WEBDB}..FTAPP_Batch_Draft 
                                       Where DocEntry = @docEntry ";

                var batchesFound = conn.Query<FTAPP_Batch>(sp_check_batch, new { docEntry = dto.DocEntry }, transaction).ToList();
                if (batchesFound.Count > 0)
                {
                    var delete_draft = $@"Delete from {db.WEBDB}..FTAPP_Batch_Draft 
                                      Where Id = @id ";

                    var deleteres = conn.Execute(delete_draft, batchesFound, transaction);
                    if (deleteres <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error delete FTAPP_Batch_Draft for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }

                #region insert so1 draft
                // insert back 
                var insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_SO1_DRAFT (
                                              DOCENTRY
                                            , LINENUM
                                            , ITEMCODE
                                            , ITEMNAME
                                            , CODEBARS
                                            , UOMQTY
                                            , STOCKQTY
                                            , PRICE
                                            , QUANTITY
                                            , QUANTITYCS
                                            , QTY
                                            , DISC
                                            , SUPP
                                            , DISCSUM
                                            , LINETOTAL
                                            , PENTRY
                                            , PLINE
                                            , PTYPE
                                            , SUGGESTQTY
                                            , DOCNUM
                                            , BORNE
                                            , SUPPSUM
                                            , INVQTY
                                            , INVPRICE
                                            , INVTOTAL
                                            , ITEMCOST
                                            , DIM1
                                            , DIM2
                                            , DIM3
                                            , MBID
                                            , SUPPCODE
                                            , QUANTITYPC
                                            , REFNO
                                            , REFITEM
                                            , UOM
                                            , BATCHID
                                            , COKEPROMO
                                            , SUPPCATNUM
                                            , TAXCODE
                                            , PRICE2
                                            , NONIM
                                            , PROMOCOUNT
                                            , NPENTRY
                                            , NPID
                                            , NPLINE
                                            , PROMOPACKAGE
                                            , PICKEDQTY                                            
                                            , PickedPcs
                                            , PickedCase
                                            , NeededCase
                                            , NeededPcs
                                            , ContentDesc
                                            , SubSi 
                                            , REFLINE
                                            , IsMissing
                                            , IsMissingCs
                                            , IsMissingPc
                                            , IsAvailableForPick
                                            , AgencyName
                                            , AgencyCode
                                            , QUANTITYPC_Orig
                                            , QUANTITYCS_Orig
                                            , ManBtchNum 
                                            , IsSwitchToPcs
                                            , LineRemark
                                            , U_MustCase 

                                            ) VALUES ( 
                                             @DOCENTRY
                                            ,@LINENUM
                                            ,@ITEMCODE
                                            ,@ITEMNAME
                                            ,@CODEBARS
                                            ,@UOMQTY
                                            ,@STOCKQTY
                                            ,@PRICE
                                            ,@QUANTITY
                                            ,@QUANTITYCS
                                            ,@QTY
                                            ,@DISC
                                            ,@SUPP
                                            ,@DISCSUM
                                            ,@LINETOTAL
                                            ,@PENTRY
                                            ,@PLINE
                                            ,@PTYPE
                                            ,@SUGGESTQTY
                                            ,@DOCNUM
                                            ,@BORNE
                                            ,@SUPPSUM
                                            ,@INVQTY
                                            ,@INVPRICE
                                            ,@INVTOTAL
                                            ,@ITEMCOST
                                            ,@DIM1
                                            ,@DIM2
                                            ,@DIM3
                                            ,@MBID
                                            ,@SUPPCODE
                                            ,@QUANTITYPC
                                            ,@REFNO
                                            ,@REFITEM
                                            ,@UOM
                                            ,@BATCHID
                                            ,@COKEPROMO
                                            ,@SUPPCATNUM
                                            ,@TAXCODE
                                            ,@PRICE2
                                            ,@NONIM
                                            ,@PROMOCOUNT
                                            ,@NPENTRY
                                            ,@NPID
                                            ,@NPLINE
                                            ,@PROMOPACKAGE
                                            ,@PICKEDQTY                                            
                                            ,@PickedPcs
                                            ,@PickedCase
                                            ,@NeededCase
                                            ,@NeededPcs
                                            ,@ContentDesc
                                            ,@SubSi
                                            ,@REFLINE
                                            ,@IsMissing
                                            ,@IsMissingCs
                                            ,@IsMissingPc
                                            ,@IsAvailableForPick
                                            ,@AgencyName
                                            ,@AgencyCode
                                            ,@QUANTITYPC_Orig
                                            ,@QUANTITYCS_Orig
                                            ,@ManBtchNum
                                            ,@IsSwitchToPcs
                                            ,@LineRemark
                                            ,@U_MustCase
                                            )";

                var insert_res = conn.Execute(insert_draft, dto.PickedDoc.Lines.Distinct().ToList(), transaction);
                if (insert_res <= 0)
                {
                    transaction.Rollback();
                    return BadRequest($@"Error insert FTAPP_SO1_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                }
                #endregion

                #region insert the boxes as draft
                // insert box 
                insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_Box_DRAFT (
                                     BoxId
                                   , PickerCode
                                   , PickerName
                                   , PickDt                                   
                                   , BaseEntry
                                   , BoxGuid
                                   , IsLooseBox
                                   , CreatedDt
                                   , TimeStampSeq 
                                   , AppVersion
                                   , BoxSize  , LabelConsistTotalBoxes
                                    ) VALUES (
                                     @BoxId                                    
                                   , @PickerCode
                                   , @PickerName
                                   , @PickDt                                   
                                   , @BaseEntry
                                   , @BoxGuid
                                   , @IsLooseBox
                                   , GETDATE()
                                   , @TimeStampSeq 
                                   , @AppVersion
                                   , @BoxSize , @LabelConsistTotalBoxes
                                    )";
                insert_res = conn.Execute(insert_draft, dto.Boxes, transaction);
                if (insert_res <= 0)
                {
                    transaction.Rollback();
                    return BadRequest($@"Error insert FTAPP_Box_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                }
                #endregion

                // insert the box content 
                #region insert box content as draft
                var boxContents = new List<FTAPP_Box1>();
                dto.Boxes.ForEach(c =>
                {
                    if (c.Contents != null)
                    {
                        boxContents.AddRange(c.Contents);
                    }
                });

                if (boxContents.Count > 0)
                {
                    insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_Box1_DRAFT   
                                            ( ItemCode
                                            , ItemName
                                            , Qty
                                            , Packaging
                                            , BoxGuid
                                            , ContentGuid
                                            , BaseEntry
                                            , BaseLine
                                            ) values (
                                             @ItemCode
                                            ,@ItemName
                                            ,@Qty
                                            ,@Packaging
                                            ,@BoxGuid
                                            ,@ContentGuid
                                            ,@BaseEntry
                                            ,@BaseLine) ";

                    insert_res = conn.Execute(insert_draft, boxContents, transaction);
                    if (insert_res <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error insert FTAPP_Box1_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }
                #endregion for box content draft

                // if batch exist
                // insert batch 
                var batches = new List<FTAPP_Batch>();
                for (int b = 0; b < dto.PickedDoc.Lines.Count; b++)
                {
                    var line = dto.PickedDoc.Lines[b];
                    if (line == null) continue;
                    if (line.FTAPP_Batches == null) continue;

                    batches.AddRange(line.FTAPP_Batches);
                }

                // insert all batch 
                //InsertSoLineBatch(db, conn, transaction, line.FTAPP_Batches, dto.DocEntry, "FTAPP_Batch_Draft");
                if (batches.Count > 0)
                {
                    var insert_sql = $@"INSERT INTO {db.WEBDB}..FTAPP_Batch_Draft (
                                         DocEntry
                                       , BaseLine
                                       , LineNum
                                       , ItemCode
                                       , ItemName
                                       , WhsCode
                                       , WhsName
                                       , BatchNo
                                       , BatchQty
                                       , CsQty
                                       , PcQty
                                       , OBTQ_Abs
                                       , OBTN_Abs
                                       , UomQty
                                       , PickedQty
                                       , PickedCsQty
                                       , PickedPcQty     
                                       , BoxId
                                       , AppVersion
                                       , TransDt
                                       , PickingMode
                                ) VALUES ( 
                                           @DocEntry
                                          ,@BaseLine
                                          ,@LineNum
                                          ,@ItemCode
                                          ,@ItemName
                                          ,@WhsCode
                                          ,@WhsName
                                          ,@BatchNo
                                          ,@BatchQty
                                          ,@CsQty
                                          ,@PcQty
                                          ,@OBTQ_Abs
                                          ,@OBTN_Abs
                                          ,@UomQty
                                          ,@PickedQty
                                          ,@PickedCsQty
                                          ,@PickedPcQty
                                          ,@BoxId
                                          ,@AppVersion
                                          ,GETDATE()) 
                                          ,@PickingMode";

                    var insertBatchRes = conn.Execute(insert_sql, batches, transaction);
                    if (insertBatchRes <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest($@"Error insert FTAPP_Batch_Draft for {db.COMPANYNAME} SO# {dto.DocEntry} ");
                    }
                }

                var replied = new SoDocResult
                {
                    actionSuccess = true,
                    errorMessage = "",
                    actionResult = $"{dto.DocEntry}",
                    documentStatus = "draft",
                    updateDocType = "draft",
                    docType = "draft"
                };
                transaction.Commit();
                return Ok(replied);
            }
            catch (Exception e)
            {
                transaction.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //void InsertSoLineBatch(DbInfo db, SqlConnection conn,
        //                        SqlTransaction trans, List<FTAPP_Batch> list, int docEntry, string tableName)
        //{
        //    try
        //    {
        //        var insert_sql = $@"INSERT INTO {db.WEBDB}..{tableName} (
        //                                 DocEntry
        //                               , BaseLine
        //                               , LineNum
        //                               , ItemCode
        //                               , ItemName
        //                               , WhsCode
        //                               , WhsName
        //                               , BatchNo
        //                               , BatchQty
        //                               , CsQty
        //                               , PcQty
        //                               , OBTQ_Abs
        //                               , OBTN_Abs
        //                               , UomQty
        //                               , PickedQty
        //                               , PickedCsQty
        //                               , PickedPcQty     
        //                               , BoxId
        //                               , AppVersion
        //                               , TransDt
        //                        ) VALUES ( 
        //                                   @DocEntry
        //                                  ,@BaseLine
        //                                  ,@LineNum
        //                                  ,@ItemCode
        //                                  ,@ItemName
        //                                  ,@WhsCode
        //                                  ,@WhsName
        //                                  ,@BatchNo
        //                                  ,@BatchQty
        //                                  ,@CsQty
        //                                  ,@PcQty
        //                                  ,@OBTQ_Abs
        //                                  ,@OBTN_Abs
        //                                  ,@UomQty
        //                                  ,@PickedQty
        //                                  ,@PickedCsQty
        //                                  ,@PickedPcQty
        //                                  ,@BoxId
        //                                  ,@AppVersion
        //                                  ,GETDATE()) ";
        //        conn.Execute(insert_sql, list, trans);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //    }
        //}

        void RemoveSecBoxBox1_Draft(DbInfo db, int DocEntry)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // remove daft box
                var delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_SecBox_DRAFT]
                                     where BaseEntry = @DocEntry ";
                var deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);

                // remove draft box line
                delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_SecBox1_DRAFT]
                                     where BaseEntry = @DocEntry ";
                deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);

                trans.Commit();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        void RemoveDraftSo1BoxBox1(DbInfo db, int DocEntry)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // remove daft so1
                var delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_SO1_DRAFT]
                                     where DocEntry = @DocEntry ";
                var deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);

                // remove daft box
                delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_Box_DRAFT]
                                     where BaseEntry = @DocEntry ";
                deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);

                // remove draft box line
                delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_Box1_DRAFT]
                                     where BaseEntry = @DocEntry ";
                deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);

                delete_draft = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_Batch_Draft]
                                         where DocEntry = @DocEntry ";

                deleteres = conn.Execute(delete_draft, new { DocEntry }, trans);
                trans.Commit();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                trans.Rollback();
            }
        }

        IActionResult ReleaseSoInPicking(Dto_Pick dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Company / subsi name empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("User code is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                return BadRequest("User code is empty");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db infor return empty");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the onhold entry
                var release_sql = @$"Delete from {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                     Where HoldDocEntry = @HoldDocEntry ";
                var result = conn.Execute(release_sql,
                     new
                     {
                         HoldDocEntry = dto.DocEntry,
                     }, trans);

                if (result >= 0)
                {
                    trans.Commit();
                    // set release onhold success
                    var successLog = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked, release onhold",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = dto.Subsi,
                        Details = $"#{dto.DocEntry}, No Picked Qty, Release onhold",
                        PostResult = "Success",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };

                    AppPostLogging(successLog);
                    return Ok();
                }

                trans.Rollback();
                return BadRequest("Release on hold doc error, please try again.");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult OnHoldSoInPicking(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company / subsi name empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("User name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.HoldReason))
                {
                    return BadRequest("Hold reason is empty");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db infor return empty");
                }

                // check duplicated doc entry 
                // no applied no lock in sql
                // as need insert statetment to lock the table, before select checking
                var sql_checkDupl = @$"SELECT * 
                                        FROM {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                        WHERE HoldDocEntry = @HoldDocEntry ";

                using var conn = new SqlConnection(_commDbConnStr);
                var checkDupResult = conn.Query<FTAPP_OnHoldSoInPicking>(sql_checkDupl,
                       new
                       {
                           HoldDocEntry = dto.DocEntry
                       }).ToList();

                if (checkDupResult.Count > 0)
                {
                    int invDocNum = -1;
                    var isInvoiceCreated = CheckInvCreated(db, dto.DocEntry, out invDocNum);
                    if (isInvoiceCreated == false)
                    {
                        var message = "";
                        checkDupResult.ForEach(u =>
                        {
                            message += $"{db.COMPANYNAME} , SO# {u.HoldDocEntry} " +
                                       $"currently picking by {u.HoldByUserCode}, {u.HoldByUserName}[PE2]\n";
                        });

                        return BadRequest(message);
                    }
                    else
                    {
                        return BadRequest($"{db.COMPANYNAME} SO# {dto.DocEntry} Picked and Invoice #{invDocNum} posted");
                    }
                }

                // start on hold the SO from other 
                // start transaction
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    // perform insert
                    var insert_sql = @$"INSERT INTO {db.WEBDB}..FTAPP_OnHoldSoInPicking  ( 
                                         HoldDocEntry
                                       , HoldByUserCode
                                       , HoldByUserName
                                       , HoldStartDt
                                       , HoldReason
                                      ) VALUES (
                                         @HoldDocEntry
                                       , @HoldByUserCode
                                       , @HoldByUserName
                                       , GETDATE()
                                       , @HoldReason
                                      )";

                    var insert_result = conn.Execute(insert_sql,
                        new
                        {
                            HoldDocEntry = dto.DocEntry,
                            HoldByUserCode = dto.UserCode,
                            HoldByUserName = dto.UserName,
                            HoldReason = dto.HoldReason
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

        IActionResult GetWhsUserAgency(Dto_Pick dto)
        {
            try
            {
                // sp_SelectWhsUserAgency
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company / subsi name empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db infor return empty");
                }

                var sql = "exec sp_SelectWhsUserAgency @webDb, @erpDb, @userCode, @subsi";
                using var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<OCRD_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    userCode = dto.UserCode,
                    subsi = db.COMPANYNAME
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

        IActionResult GetQueueSOLines(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("The warehouse code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The compay info is empty");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var sql_so = @$"SELECT * FROM {db.WEBDB}..SO with (nolock) Where DocEntry = @DocEntry";
                var so = conn.Query<SO>(sql_so, new { DocEntry = dto.DocEntry }).FirstOrDefault();
                if (so == null)
                {
                    return BadRequest($"The docentry {dto.DocEntry} no found");
                }

                // query predefine bin and exp dates
                // if any 
                // 20230216
                var query_soPick1 = @$"select ID
                                        ,  DOCENTRY
                                        ,  LINENUM
                                        ,  ITEMCODE
                                        ,  REFITEM
                                        ,  PICKLISTNO
                                        ,  BIN
                                        ,  EXPIRED
                                        ,  QUANTITY
                                        ,  WEIGHT
                                from {db.WEBDB}..SOPICK1  with (nolock)
                                Where DOCENTRY = @DocEntry ";

                var soPick1s = conn.Query<SOPICK1>(query_soPick1, new { DocEntry = dto.DocEntry }).ToList();

                // query each lines
                //var sql = "exec sp_SelectQueueSOLine @webDB, @erpDb, @subsi, @docEntry, @whsCode ";

                // sp_SelectQueueSOLine_TobeLive 
                var sql = "exec sp_SelectQueueSOLine @webDB, @erpDb, @subsi, @docEntry, @whsCode ";
                var lines = conn.Query<SO1>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    subsi = db.COMPANYNAME,
                    docEntry = dto.DocEntry,
                    whsCode = dto.WhsCode
                }).Distinct().ToList();

                // 20220412
                // if found not line 
                if (lines.Count == 0) // mean no store
                {
                    var dto1 = new Dto_Pick
                    {
                        Subsi = dto.Subsi,
                        DocEntry = (int)so.DOCENTRY,
                        CardCode = so.CARDCODE,
                        UserCode = "SVR",
                        AppVersion = "Latest"
                    };

                    SetSOOutStock(dto1);

                    var replied1 = new { Lines = new List<SO1>() }; // trigger app to auto start next
                    return Ok(replied1);
                }

                // massage the pcs and pc qty to pick 
                // filter unwanted line , fixing all pc cs presentation

                // sort the list by item name alphatibet order
                //var Orderedlines = lines.OrderBy(n => n.ITEMNAME).Distinct().ToList();

                // for showing orig line
                // 20211203

                var Orderedlines = (so.REFTYPE == "TPWARE") ?
                     lines.OrderBy(n => n.ITEMNAME).ToList() :
                     lines.OrderBy(n => n.ITEMNAME).Distinct().ToList();

                var newlines = new List<SO1>();
                for (int i = 0; i < Orderedlines.Count; i++)
                {
                    var line = Orderedlines[i];
                    if (line == null) continue;

                    if (line.QUANTITY == 0) continue;

                    var uomQty = line.UOMQTY == 0 ? 1 : line.UOMQTY; // avoid zero devision

                    if (uomQty == 1)
                    {
                        if ($"{line.U_MustCase}".ToLower().Equals("y"))
                        {
                            line.QUANTITYPC = 0;
                            line.QUANTITYCS = line.QUANTITY;
                            line.UOM = "CS";
                        }
                        else
                        {
                            line.QUANTITYPC = line.QUANTITY;
                            line.QUANTITYCS = 0;
                            line.UOM = "PC";
                            //newlines.Add(line);
                        }
                        goto Continue_Query;
                    }

                    // recalculate the cs and pc
                    line.QUANTITYCS = (int)Math.Floor(line.QUANTITY / uomQty);
                    line.QUANTITYPC = (int)(line.QUANTITY % uomQty);

                    // determine the UOM
                    if (line.QUANTITYPC == 0)
                    {
                        line.UOM = "CS";
                    }
                    else if (line.QUANTITYCS == 0)
                    {
                        line.UOM = "PC";
                    }
                    else if (line.QUANTITYCS > 0 && line.QUANTITYPC > 0) // both had value
                    {
                        line.UOM = "";
                    }
                    else if (line.QUANTITYCS > 0 && line.QUANTITYPC == 0)
                    {
                        line.UOM = "CS";
                    }
                    else if (line.QUANTITYCS == 0 && line.QUANTITYPC > 0)
                    {
                        line.UOM = "PC";
                    }

                Continue_Query:
                    // load the barcode table if any 
                    var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                    line.BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                        new
                        {
                            erpDb = db.SAPDB,
                            itemCode = line.ITEMCODE
                        }).ToList();

                    // add more barcode 
                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.ITEMCODE,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    line.BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.CODEBARS,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    line.BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.SUPPCATNUM,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    line.BarCodes.Add(supcatnum);
                    newlines.Add(line);

                    // 20230216
                    // load in the SO pick 1 data                     
                    if (soPick1s.Count > 0)
                    {
                        line.SoPick1s = soPick1s
                            .Where(x => x.ITEMCODE == line.ITEMCODE &&
                                        x.LINENUM == line.LINENUM).ToList();
                    }
                }

                // load the box content as well
                // query the box 
                sql = @$"SELECT * FROM {db.WEBDB}..FTAPP_Box_DRAFT with (nolock)
                        Where BaseEntry = @DocEntry";

                var boxes = conn.Query<FTAPP_Box>(sql, new { dto.DocEntry }).ToList();
                if (boxes != null && boxes.Count > 0)
                {
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var sql_bContent = $@"SELECT * FROM {db.WEBDB}..FTAPP_Box1_DRAFT with (nolock)
                                                  Where BaseEntry = @DocEntry 
                                                  AND BoxGuid = @BoxGuid";

                        boxes[b].Contents = conn.Query<FTAPP_Box1>(sql_bContent,
                            new
                            {
                                DocEntry = dto.DocEntry,
                                BoxGuid = boxes[b].BoxGuid
                            }).ToList();
                    }
                }

                // 20240712
                // query the original SO line 
                // for bundler and promotion picking 
                var sp_QueryOrigSoLines = @$"select * from {db.WEBDB}..SO1 Where docEntry  = @docEntry ";
                var origSoLines = conn.Query<SO1>(sp_QueryOrigSoLines, new { docEntry = dto.DocEntry }).ToList();

                var replied = new { Lines = newlines, Boxes = boxes, OrigLines = origSoLines };
                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetQueueSOByDocEntyWhildCard(Dto_Pick dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.DocStatus))
                {
                    return BadRequest("The doc status is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.WildCardDocEntry))
                {
                    return BadRequest("The doc entry wildcard is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("The user code is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The compay info is empty");
                }

                //var sql = @"exec sp_SelectQueueSO_ByDocEntryWildCard @webDB, @erpDb, @subsi, @docStatus, @docEntryWhildCard, @subSiId";
                var sql = @"exec sp_SelectQueueSO_ByDocEntryWildCard_Version2 
                                        @webDb, @erpDb, @subSi, @subSiId, @userCode, @docEntryWhildCard";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<SO>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    subsi = db.COMPANYNAME,
                    subSiId = db.COMPANYID,
                    userCode = dto.UserCode,
                    docEntryWhildCard = dto.WildCardDocEntry
                }).FirstOrDefault();

                if (result != null)
                {
                    return Ok(result);
                }

                // query the onhold doc and add into the list 
                // based on user code query 

                var sql_queryOnhold = "exec sp_SelectHoldSO_ByUserCode @webDB, @erpDb , @subsi, @userCode , @docStatus, @SubSiID";

                var onholdDocs = conn.Query<SO>(sql_queryOnhold, new
                {
                    webDB = db.WEBDB,
                    erpDb = db.SAPDB,
                    subsi = db.COMPANYNAME,
                    userCode = dto.UserCode,
                    docStatus = "Q",
                    SubSiID = db.COMPANYID
                }).ToList();

                if (onholdDocs.Count > 0)
                {
                    var foundDoc = onholdDocs.Where(d => $"{d.DOCENTRY}".Contains(dto.WildCardDocEntry) && d.LinesCount > 0)
                                             .FirstOrDefault();
                    if (foundDoc == null) return NotFound();
                    return Ok(foundDoc);
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

        IActionResult GetQueueSO(Dto_Pick dto, bool selectSingleSo = false)
        {
            try
            {
                if (IsBusyNextOrder)
                {
                    return BadRequest("Please try again, another picker in auto starting");
                }

                IsBusyNextOrder = true;

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.DocStatus))
                {
                    return BadRequest("The doc status is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code can not empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("User name can not empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The compay info is empty");
                }

                if (dto.DocStatus == "H")
                {
                    // query the SO doc in FTAPP_OnHOLD
                    return GetQueueSO_Onhold(dto, db);
                }

                // remove the onhold doc where is already invoice 
                using var conn = new SqlConnection(_commDbConnStr);
                conn.Open();
                using (var trans0 = conn.BeginTransaction())
                {
                    var sql_deleteInvoicedONhold = @$"DELETE t0 
                                        FROM  {db.WEBDB}..FTAPP_OnHoldSoInPicking t0 
                                                INNER JOIN
	                                            {db.WEBDB}..SO t1 ON t1.DOCENTRY = t0.HoldDocEntry 
	                                    WHERE (t1.DOCSTATUS = 'I' or 
                                               t1.DOCSTATUS = 'L' or 
                                               t1.DOCSTATUS = 'R' or 
                                               t1.DOCSTATUS = 'C' )";

                    var cmd = conn.CreateCommand();
                    cmd.Transaction = trans0;
                    cmd.Connection = conn;
                    cmd.CommandText = sql_deleteInvoicedONhold;
                    cmd.ExecuteNonQuery();

                    // 20230617
                    // delete the ibt on hold 
                    var sql_deleteIBTONhold = @$"DELETE t0 
                                        FROM    {db.WEBDB}..FTAPP_OnHold_IBT_InPicking t0 INNER JOIN
	                                            {db.WEBDB}..IBT t1 ON t1.DOCENTRY = t0.HoldDocEntry 
	                                     WHERE (t1.DOCSTATUS = 'T' )";

                    cmd.CommandText = sql_deleteIBTONhold;
                    int runRes = cmd.ExecuteNonQuery();
                    trans0.Commit();
                }

                // doc statuc with Q and I go flow below
                //var sql = @"exec sp_SelectQueueSO @webDB, @erpDb, @subsi, @docStatus, @SubSiID";

                // 2021 07 15 new query script 
                var sql = @"exec sp_SelectQueueSO_Version2 @webDB, @erpDb, @userCode, @subSi, @subSiId";
                var results = conn.Query<SO>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    userCode = dto.UserCode,
                    subsi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID
                }).ToList();

                // 20230509
                // load in the ibt and match in
                if (IsSupportIBT)
                {
                    var sqlIbt = @"exec sp_SelectQueueIBT @webDB, @userCode";
                    var ibtRes = conn.Query<SO>(sqlIbt, new
                    {
                        webDb = db.WEBDB,
                        userCode = dto.UserCode
                    }).ToList();

                    if (ibtRes.Count > 0)
                    {
                        results.AddRange(ibtRes);
                    }
                }


                // remove the checking of zero line 
                // show the line with zero to picker 
                // 2021 0806
                var sapNoOOSs = results.Where(x => x.LinesCount == 0 && x.IsIBT == false).ToList(); // set OOS from web api
                if (sapNoOOSs != null && sapNoOOSs.Count > 0) // intial own transaction
                {
                    BatchUpdateForOOS(sapNoOOSs, $"{dto.AppVersion}");
                }

                var returnList = results.Where(x => x.LinesCount > 0).ToList();

                // query the onhold doc and add into the list 
                // based on user code query 
                var sql_queryOnhold =
                            @$"exec sp_SelectHoldSO_ByUserCode @webDB, @erpDb, @subsi,
                                                               @userCode, @docStatus, @SubSiID";

                var onholdDocs = conn.Query<SO>(sql_queryOnhold, new
                {
                    webDB = db.WEBDB,
                    erpDb = db.SAPDB,
                    subsi = db.COMPANYNAME,
                    userCode = dto.UserCode,
                    docStatus = "Q",
                    SubSiID = db.COMPANYID
                }).ToList();

                // 20230617
                // add in the ibt query on hold 
                var sql_IBT_queryOnhold = "exec sp_SelectHoldIBT_ByUserCode @webDB, @userCode";
                var onholdDocs_IBT = conn.Query<SO>(sql_IBT_queryOnhold, new
                {
                    webDB = db.WEBDB,
                    userCode = dto.UserCode,
                }).ToList();

                if (onholdDocs_IBT.Count > 0)
                {
                    onholdDocs.AddRange(onholdDocs_IBT); // add in the ibt together
                }


                // add in the onhold doc based on the user code
                if (onholdDocs.Count > 0)
                {
                    if (returnList == null) returnList = new List<SO>();
                    // reverse insert the doc
                    for (int h = onholdDocs.Count - 1; h >= 0; h--)
                    {
                        if (onholdDocs[h].LinesCount == 0) continue; // remove all line is zero
                        returnList.Insert(0, onholdDocs[h]);
                    }
                }

                if (returnList == null) return NotFound();
                if (returnList.Count == 0) return NotFound();

                if (!selectSingleSo)
                {
                    return Ok(returnList);
                }

                // loop each doc to see any hold, by other person
                // get the next new doc
                for (int d = 0; d < returnList.Count; d++)
                {
                    var doc = returnList[d];
                    if (doc == null) continue;
                    if (doc.LinesCount == 0) continue;

                    // check does this doc hold by some one not me
                    // remove the select no lock
                    // let db manage the table when some one may insert data
                    // select statement can wait and the insert process
                    string checkDocHoldBySomeOne = "";
                    if (!doc.IsIBT)
                    {
                        checkDocHoldBySomeOne = @$"SELECT * 
                                                   FROM  {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                                   WHERE HoldDocEntry = @HoldDocEntry ";
                    }
                    else // for ibt onhold checking 
                    {
                        checkDocHoldBySomeOne = @$"SELECT * 
                                                   FROM  {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
                                                   WHERE HoldDocEntry = @HoldDocEntry ";
                    }

                    var holded = conn.Query<FTAPP_OnHoldSoInPicking>(checkDocHoldBySomeOne,
                                                new
                                                {
                                                    HoldDocEntry = doc.DOCENTRY
                                                })
                                                .FirstOrDefault();

                    if (holded == null) // no hold by some one 
                    {
                        if (doc.IsIBT)
                        {
                            var newHold = new FTAPP_OnHold_IBT_InPicking
                            {
                                HoldDocEntry = (int)doc.DOCENTRY,
                                HoldByUserCode = dto.UserCode,
                                HoldByUserName = dto.UserName,
                                HoldStartDt = DateTime.Now,
                                HoldReason = "Picking"
                            };
                            PutIbtOrderOnHold(newHold, db);
                        }
                        else
                        {
                            var newHold = new Dto_Pick // put the doc hold at server
                            {
                                Subsi = dto.Subsi,
                                UserCode = dto.UserCode,
                                UserName = dto.UserName,
                                HoldReason = "Picking",
                                DocEntry = (int)doc.DOCENTRY
                            };

                            OnHoldSoInPicking(newHold); // by server when user press start
                            return Ok(doc);
                        }
                    }

                    if (holded.HoldByUserCode.Equals(dto.UserCode)) // it hold by me 
                    {
                        return Ok(doc);
                    }
                }

                return BadRequest("No new available pick order");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                IsBusyNextOrder = false; // open the entry for next person 
            }
        }

        // get SO by earlier date time 
        // return single avail order
        // auto start no support of testing database auto order
        // 20210922
        IActionResult GetQueueSO3(Dto_Pick dto)
        {
            try
            {
                if (IsBusyGetQueueSO3)
                {
                    return BadRequest("Please try again, server busy at the moment.");
                }

                IsBusyGetQueueSO3 = true;

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid user name");
                }

                // get all user company
                var sql_userlogons = @"select * from FTAPP_SSO with (nolock)
                                        where UserCode = @UserCode
                                        and left(UserCompanyID, 1) <> 'Z'";

                if (!string.IsNullOrWhiteSpace(dto.IsAutoStartSOTestDb))
                {
                    // get only the test database
                    sql_userlogons = @"select * from FTAPP_SSO with (nolock)
                                        where UserCode = @UserCode
                                        and left(UserCompanyID, 1) = 'Z'";
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var companies = conn.Query<FTAPP_SSO>(sql_userlogons, new { UserCode = dto.UserCode }).ToList();
                if (companies == null) return BadRequest($"{dto.UserCode} no setup / config properly, No new available pick order");

                var AllCompSOs_Q = new List<SO>();
                var AllCompSOs_H = new List<SO>();

                // geth all SO from the company
                var dbhelper = new DbNameHelper();
                for (int c = 0; c < companies.Count; c++)
                {
                    var company = companies[c];
                    var db = dbhelper.GetDbInfo(_commDbConnStr, company.UserCompany);

                    // remove thos completed SO
                    // with trans
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.Connection = conn;

                    using (var trans = conn.BeginTransaction())
                    {
                        var sql_deleteInvoicedONhold = @$"DELETE t0 
                                        FROM    {company.UserCompanyRef}..FTAPP_OnHoldSoInPicking t0 
                                                INNER JOIN
	                                            {company.UserCompanyRef}..SO t1 ON t1.DOCENTRY = t0.HoldDocEntry 
	                                     WHERE (t1.DOCSTATUS = 'I' or 
                                               t1.DOCSTATUS = 'L' or 
                                               t1.DOCSTATUS = 'R' or 
                                               t1.DOCSTATUS = 'C' or
                                               t1.DOCSTATUS = 'O' )";

                        cmd.Transaction = trans;
                        cmd.CommandText = sql_deleteInvoicedONhold;
                        cmd.ExecuteNonQuery();

                        // 20230617
                        // delete the ibt on hold 
                        var sql_deleteIBTONhold = @$"DELETE t0 
                                        FROM    {company.UserCompanyRef}..FTAPP_OnHold_IBT_InPicking t0 INNER JOIN
	                                            {company.UserCompanyRef}..IBT t1 ON t1.DOCENTRY = t0.HoldDocEntry 
	                                     WHERE (t1.DOCSTATUS = 'T' )";

                        cmd.CommandText = sql_deleteIBTONhold;
                        cmd.ExecuteNonQuery();
                        trans.Commit();
                    }

                    var results = new List<SO>();

                    // query all the Q / unholded SO
                    if (!IsByPassSO)
                    {
                        var sql = @"exec sp_SelectQueueSO_Version2 @webDB, @erpDb, @userCode, @subSi, @subSiId";
                        results = conn.Query<SO>(sql, new
                        {
                            webDb = company.UserCompanyRef,
                            erpDb = company.UserCompanyErpRef,
                            userCode = company.UserCode,
                            subsi = company.UserCompany,
                            SubSiID = company.UserCompanyID
                        }).ToList();

                        // show the line with zero to picker 
                        // remove the zero line 
                        // 2021 0806
                        var sapNoOOSs = results.Where(x => x.LinesCount == 0).ToList(); // set OOS from web api
                        if (sapNoOOSs.Count > 0)
                        {
                            BatchUpdateForOOS(sapNoOOSs, $"{dto.AppVersion}");
                        }
                    }

                    // 20230512
                    if (IsSupportIBT)
                    {
                        var sqlIbt = @"exec sp_SelectQueueIBT @webDB, @userCode";
                        var ibtRes = conn.Query<SO>(sqlIbt, new
                        {
                            webDb = db.WEBDB,
                            userCode = dto.UserCode
                        }).ToList();

                        if (ibtRes.Count > 0)
                        {
                            results.AddRange(ibtRes);
                        }
                    }


                    var returnList = results.Where(x => x.LinesCount > 0).ToList();
                    if (returnList.Count == 0)
                    {
                        goto CheckOnHoldList;
                    }

                    AllCompSOs_Q.AddRange(returnList); // combine all sales order into a list

                CheckOnHoldList:
                    // query the onhold doc and add into the list 
                    // based on user code query 
                    var sql_queryOnhold = "sp_SelectHoldSO_ByUserCode  @webDB, @erpDb , @subsi, @userCode , @docStatus, @SubSiID";
                    var onholdDocs = conn.Query<SO>(sql_queryOnhold, new
                    {
                        webDB = company.UserCompanyRef,
                        erpDb = company.UserCompanyErpRef,
                        subsi = company.UserCompany,
                        userCode = company.UserCode,
                        docStatus = "Q",
                        SubSiID = company.UserCompanyID
                    }).ToList();

                    if (onholdDocs.Count > 0)
                    {
                        AllCompSOs_H.AddRange(onholdDocs);
                    }

                    // 20230617
                    // add in the ibt query on hold 
                    var sql_IBT_queryOnhold = "exec sp_SelectHoldIBT_ByUserCode @webDB, @userCode";
                    var onholdDocs_IBT = conn.Query<SO>(sql_IBT_queryOnhold, new
                    {
                        webDB = db.WEBDB,
                        userCode = dto.UserCode,
                    }).ToList();

                    if (onholdDocs_IBT.Count > 0)
                    {
                        AllCompSOs_H.AddRange(onholdDocs_IBT); // add in the ibt together
                    }


                } // all companies loop

                // prepare check each onhold order
                if (AllCompSOs_H.Count > 0)
                {
                    var sortedEalierDtSo = AllCompSOs_H.OrderBy(x => x.SortedDate).ToList();
                    for (int holdSOIndex = 0; holdSOIndex < sortedEalierDtSo.Count; holdSOIndex++)
                    {
                        var order = sortedEalierDtSo[holdSOIndex];
                        if (order == null) continue;

                        var db = dbhelper.GetDbInfo(_commDbConnStr, order.SubSi);
                        if (db == null) continue;

                        // ensure the onhold still there 
                        // check does this doc hold by some one not me
                        // remove the select no lock
                        // let db manage the table when some one may insert data
                        // select statement can wait and the insert process

                        string checkDocHoldBySomeOne = "";
                        if (order.IsIBT)
                        {
                            checkDocHoldBySomeOne = @$"SELECT * 
                                                   FROM  {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
                                                   WHERE HoldDocEntry = @HoldDocEntry ";
                        }
                        else // for ibt onhold checking 
                        {
                            checkDocHoldBySomeOne = @$"SELECT * 
                                                   FROM  {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                                   WHERE HoldDocEntry = @HoldDocEntry ";
                        }

                        var holded = conn.Query<FTAPP_OnHoldSoInPicking>(checkDocHoldBySomeOne,
                                                    new
                                                    {
                                                        HoldDocEntry = order.DOCENTRY
                                                    })
                                                    .FirstOrDefault();

                        if (holded == null) // no hold by some one 
                        {
                            if (order.IsIBT)
                            {
                                var newHold = new FTAPP_OnHold_IBT_InPicking
                                {
                                    HoldDocEntry = (int)order.DOCENTRY,
                                    HoldByUserCode = dto.UserCode,
                                    HoldByUserName = dto.UserName,
                                    HoldStartDt = DateTime.Now,
                                    HoldReason = "Picking"
                                };

                                PutIbtOrderOnHold(newHold, db);
                            }
                            else
                            {
                                var newHold_so = new Dto_Pick // put the doc hold at server
                                {
                                    Subsi = order.SubSi,
                                    UserCode = dto.UserCode,
                                    UserName = dto.UserName,
                                    HoldReason = "Picking",
                                    DocEntry = (int)order.DOCENTRY
                                };
                                OnHoldSoInPicking(newHold_so); // by server when user press start
                            }

                            return Ok(order); // jump out all queue
                        }

                        if (holded.HoldByUserCode.Equals(dto.UserCode)) // it hold by me 
                        {
                            return Ok(order); // jump out all queue
                        }
                    }
                }

                // after check no onhold order .. check new Q order
                if (AllCompSOs_Q.Count > 0)
                {
                    var sortedEarlierQSos = AllCompSOs_Q.Where(x => x.LinesCount > 0)
                                                        .OrderBy(x => x.SortedDate)
                                                        .ToList();

                    for (int q = 0; q < sortedEarlierQSos.Count; q++)
                    {
                        var order = sortedEarlierQSos[q];
                        if (order == null) continue;

                        var db = dbhelper.GetDbInfo(_commDbConnStr, order.SubSi);
                        if (db == null) continue;

                        if (order.IsIBT)
                        {
                            var newHold = new FTAPP_OnHold_IBT_InPicking // put the doc hold at server
                            {
                                HoldDocEntry = (int)order.DOCENTRY,
                                HoldByUserCode = dto.UserCode,
                                HoldByUserName = dto.UserName,
                                HoldStartDt = DateTime.Now,
                                HoldReason = "Picking"
                            };

                            PutIbtOrderOnHold(newHold, db);
                            return Ok(order);
                        }

                        // ensure the onhold still there 
                        // check does this doc hold by some one not me
                        // remove the select no lock
                        // let db manage the table when some one may insert data
                        // select statement can wait and the insert process
                        var checkDocHoldBySomeOne = @$"SELECT * 
                                                   FROM  {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                                   WHERE HoldDocEntry = @HoldDocEntry ";

                        var holded = conn.Query<FTAPP_OnHoldSoInPicking>(checkDocHoldBySomeOne,
                                                    new
                                                    {
                                                        HoldDocEntry = order.DOCENTRY
                                                    })
                                                    .FirstOrDefault();

                        if (holded == null) // no hold by some one 
                        {
                            var newHold = new Dto_Pick // put the doc hold at server
                            {
                                Subsi = order.SubSi,
                                UserCode = dto.UserCode,
                                UserName = dto.UserName,
                                HoldReason = "Picking",
                                DocEntry = (int)order.DOCENTRY
                            };

                            OnHoldSoInPicking(newHold); // by server when user press start
                            return Ok(order); // jump out all queue
                        }

                        if (holded.HoldByUserCode.Equals(dto.UserCode)) // it hold by me 
                        {
                            return Ok(order); // jump out all queue
                        }
                    } // end of the Q order loop
                }

                return BadRequest("No new available pick order");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                IsBusyGetQueueSO3 = false;
            }
        }



        // 20230515
        // put ibt order on hold at pick order selection 
        bool PutIbtOrderOnHold(FTAPP_OnHold_IBT_InPicking dto, DbInfo db)
        {
            try
            {
                using var conn = new SqlConnection(_commDbConnStr);

                // check onhold duplicate 
                var checkDuplSql = $"Select * from {db.WEBDB}..FTAPP_OnHold_IBT_InPicking Where HoldDocEntry = @HoldDocEntry ";
                var found = conn.Query<FTAPP_OnHold_IBT_InPicking>(checkDuplSql, new { dto.HoldDocEntry }).FirstOrDefault();
                if (found != null) return true;

                // if no found then create the record
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    var insertSql = $@"Insert into {db.WEBDB}..FTAPP_OnHold_IBT_InPicking (
                                     HoldDocEntry 
                                   , HoldByUserCode
                                   , HoldByUserName
                                   , HoldStartDt
                                   , HoldReason 
                                ) values (
                                      @HoldDocEntry 
                                    , @HoldByUserCode
                                    , @HoldByUserName
                                    , GETDATE()
                                    , @HoldReason 
                                )";

                    conn.Execute(insertSql, dto, trans);
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
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        IActionResult GetQueueSO_Onhold(Dto_Pick dto, DbInfo db)
        {
            try
            {
                var sql = @"exec sp_SelectHoldSO @webDB, @erpDb, @subsi, @docStatus, @SubSiID";
                //var sql = @"exec sp_SelectHoldSO @webDB, @erpDb, @subsi, @startDate, @endDate, @docStatus";
                /* @webDB as nvarchar(100), 
                    @subsi as nvarchar(100), 
                    @warehouse as nvarchar(100),
                    @startDate as datetime, 
                    @endDate as datetime*/

                using var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<SO>(sql, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    subsi = db.COMPANYNAME,
                    docStatus = "Q",
                    SubSiID = db.COMPANYID
                }).ToList();

                if (results == null) return NotFound(); // save time cut off checking 
                if (results.Count == 0) return NotFound();

                // sub query the list with user agency 
                var queryUserAgency = "exec sp_SelectWhsUserAgency2 @webDb, @UserCode";
                var userAgencies = conn.Query<USERCARD>(queryUserAgency, new
                {
                    webDb = db.WEBDB,
                    UserCode = dto.UserCode
                }).ToList();

                if (userAgencies == null) return Ok(results);
                if (userAgencies.Count == 0) return Ok(results);

                // procee the new list 
                var returnList = results.Where(x => userAgencies.Any(y => y.CARDCODE == x.AgencyCode)).ToList();
                if (returnList == null) return NotFound();
                if (returnList.Count == 0) return NotFound();

                return Ok(returnList);
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
