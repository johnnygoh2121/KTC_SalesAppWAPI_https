using Dapper;
using KTC_SalesAppWAPI.DTOs.Delivery;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.Delivery;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Dispatch;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Transfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Delivery
{
    [Route("[controller]")]
    [ApiController]
    public class DispatchController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<DispatchController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";

        public DispatchController(IConfiguration configuration, ILogger<DispatchController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
        }


        [HttpPost]
        public IActionResult PostAsync(Dto_Dispatch dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadDlbSumm":
                    {
                        return LoadDlbSumm(dto);
                    }
                case "LoadDlbDoc_ByCardName":
                    {
                        return LoadDlbDoc_ByCardName(dto);
                    }
                case "LoadInvLines":
                    {
                        return LoadInvLines(dto);
                    }
                case "LoadInv":
                    {
                        return LoadInv(dto);
                    }
                case "LoadUnDeliveryReasonCodes":
                    {
                        return LoadUnDeliveryReasonCodes(dto);
                    }
                case "LoadInvBoxes":
                    {
                        return LoadInvBoxes(dto);
                    }
                case "LoadIBTBoxes":
                    {
                        return LoadIBTBoxes(dto);
                    }
                case "LoadTransferBoxes":
                    {
                        return LoadTransferBoxes(dto);
                    }
                case "VerifyAndUpdateDriverBox":
                    {
                        return VerifyAndUpdateDriverBox(dto);
                    }
                case "GetInvoiceSeller":
                    {
                        return GetInvoiceSeller(dto);
                    }
                case "GetCogSeller":
                    {
                        return GetCogSeller(dto);
                    }
                case "UpdateDriverLog":
                    {
                        return UpdateDriverLog(dto);
                    }
                case "ReturnThruInvoice":
                    {
                        return ReturnThruInvoice(dto);
                    }
                case "ClearReturnThruInvoice":
                    {
                        return ClearReturnThruInvoice(dto);
                    }
                case "LoadReturnThruInvoices":
                    {
                        return LoadReturnThruInvoices(dto);
                    }
                case "LoadInvAndLines":
                    {
                        return LoadInvAndLines(dto);
                    }
                case "LoadTrcn_RetDoc":
                    {
                        return LoadTrcn_RetDoc(dto);
                    }
                case "UpdateDocSign_Dispatch":
                    {
                        return UpdateDocSign_Dispatch(dto);
                    }

                case "UpdateDriverNRIC_Dispatch": // 20220620
                    {
                        return UpdateDriverNRIC_Dispatch(dto);
                    }
                case "ResetBoxOutStayInTruck":
                    {
                        return ResetBoxOutStayInTruck(dto);
                    }
                case "UpdateDlb1SignedDoc1":
                    {
                        return UpdateDlb1SignedDoc(dto); // for very first upload of file 
                    }
                case "UpdateDlb1SignedDoc":
                    {
                        return UpdateDlb1SignedDoc(dto); // for very first upload of file 
                    }
                case "UpdateDlb1SignedDoc2":
                    {
                        return UpdateDlb1SignedDoc2(dto); // for driver dlb list view attachment
                    }
                case "UpdateDlb1SignedDoc2_FromReUpload":
                    {
                        return UpdateDlb1SignedDoc2_FromReUpload(dto); // for driver reupload the invoice
                    }
                case "VerifyAndUpdateDriverDoc_Out":
                    {
                        return VerifyAndUpdateDriverDoc_Out(dto);
                    }
                case "LoadCogDocAndLines":
                    {
                        return LoadCogDocAndLines(dto);
                    }
                case "UpdateDriverStoreCheckedIn":
                    {
                        return UpdateDriverStoreCheckedIn(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult UpdateDriverStoreCheckedIn(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CardName))
            {
                return BadRequest("invalid card name");
            }
            if (string.IsNullOrWhiteSpace(dto.TruckNo))
            {
                return BadRequest("Invalid truck no");
            }

            var companies = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
            if (companies == null)
            {
                return BadRequest("delivery features no deployed");
            }
            if (companies.Count == 0)
            {
                return BadRequest("delivery features no deployed");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            // loop thru card name 
            // load the DLB 
            // update each DLB the tiem stamp 
            // checking 
            // exec KTCW_COMMON..[sp_GetDlbDoc_ByCardName] 'AVONWEB', 'KDW6661', 'SERVAY HYPERMARKET(LIKA'
            // cobine reading the dlb summary 


            var dlb1s = new List<DLB1>();
            for (int c = 0; c < companies.Count; c++)
            {
                var db = companies[c];
                if (db == null) continue;

                string newCardName = dto.CardName;
                if (dto.CardName.Contains("'"))
                {
                    newCardName = dto.CardName.Replace("'", "''");
                }

                var sp_query = @"exec sp_GetDlbDoc_ByCardName_Test1 @webDb, @truckNo, @storeName";
                var list = conn.Query<DLB1>(sp_query, new
                {
                    webDb = db.WEBDB,
                    truckNo = dto.TruckNo,
                    storeName = newCardName
                }, trans).ToList();

                if (list.Count == 0) continue;

                // massage the dlb with the cardname 
                var filterByCardName = list.Where(d => d.CARDNAME == dto.CardName).ToList();
                if (filterByCardName.Count == 0) continue;


                for (int i = 0; i < filterByCardName.Count; i++)
                {
                    var dlb1 = filterByCardName[i];
                    if (dlb1 == null) continue;


                    // loop the dlb to update the time 
                    var update_sp = @$"Update {db.WEBDB}..DLB1 
                                   Set CheckInDt = GETDATE() 
                                   Where DocEntry = @DOCENTRY 
                                     and LINENUM = @LINENUM ";

                    var updateCnt = conn.Execute(update_sp, new { dlb1.DOCENTRY, dlb1.LINENUM }, trans);
                }
            }


            // finally update the check in time 
            try
            {
                // purely insert the check in time.
                var insert_sq = @"insert into ktcw_common..FTAPP_StoreCheckedIn (
                                    CardName, TruckNo, CheckedInDt
                                ) values (  
                                    @CardName, @TruckNo, GETDATE() )";

                var result = conn.Execute(insert_sq, new
                {
                    CardName = dto.CardName,
                    TruckNo = dto.TruckNo
                }, trans);

                trans.Commit();
                var checkedInDt = new { checkedDateTime = DateTime.Now };
                return Ok(checkedInDt);
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadCogDocAndLines(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.CogNum <= 0)
                {
                    return BadRequest("invalid cog number");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var query_cog = @$"Select * 
                                   , (Select sum(LineTotal) 
                                        from {db.WEBDB}..COG1 with (nolock)
                                        where docentry = @docNum) [DocTotal]
                                   from {db.WEBDB}..COG with (nolock)
                                   Where DocEntry = @docNum";

                using var conn = new SqlConnection(_commDbConnStr);
                var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.CogNum }).FirstOrDefault();
                if (cog == null) return NotFound();

                var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = cog.DOCENTRY }).ToList();
                cog.SubSi = db.COMPANYNAME;

                return Ok(cog);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateDlb1SignedDoc2(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DlbEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (dto.DocNum < 0)
                {
                    return BadRequest("Invalid DLB doc number.");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var sqlCogFiles = $@"SELECT top 1 t1.* 
                                    from {db.WEBDB}..FTAPP_DLB t0 with (nolock)
                                    inner join {db.WEBDB}..FTAPP_DLB1 t1 with (nolock) on t1.HeadGuid = t0.HeadGuid
                                    where t0.DLBEntry = @DlbEntry
                                    and t1.DocNum =  @DocNum 
                                    and t1.DocType = @DocType order by id desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb1 = conn.Query<FTAPP_DLB1>(sqlCogFiles, new
                {
                    DlbEntry = dto.DlbEntry,
                    DocNum = dto.DocNum,
                    DocType = dto.DocType
                }).FirstOrDefault();

                if (dlb1 == null)
                {
                    // get the last dlb entry 
                    var dlbHelper = new DLBHelper(db);
                    dlbHelper.RecreteFTAPP_DLB_By_FTAPPDLB(dto.DlbEntry, db, _commDbConnStr);

                    // recheck 
                    dlb1 = conn.Query<FTAPP_DLB1>(sqlCogFiles, new
                    {
                        DlbEntry = dto.DlbEntry,
                        DocNum = dto.DocNum,
                        DocType = dto.DocType
                    }).FirstOrDefault();

                    return BadRequest($"DLB doc not found for file update, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum} " +
                            $"[NO_DLB1_REC_FOUND]");
                }

                // get the logging setup 
                var sp_IsLogging = @"select setupvalue 
                                 from KTCW_COMMON..FTApp_Config 
                                 where setupname = 'DriveAppSignedFileLoggingEnable' ";

                var isLog = conn.ExecuteScalar<string>(sp_IsLogging);
                bool logAct = $"{isLog}".ToLower().Equals("y");


                // start the database transaction
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                // perform a backup log 
                // do a loging to the last DLB1 
                // as backup
                if (logAct)
                {
                    var sp_ToLog_b4 = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                            @source , @action ";

                    var resTolog_b4 = conn.Execute(sp_ToLog_b4, new
                    {
                        webDb = db.WEBDB,
                        dlbEntry = dto.DlbEntry,
                        docNum = dto.DocNum,
                        docType = dto.DocType,
                        source = "DriverApp",
                        action = "ViewAttachChange_B4"
                    }, trans);
                }

                // continue the normal process
                //string existingFiles = dto.SignedFiles;                

                // update the dlb1 table row
                // 20240511
                var sql = @$"UPDATE {db.WEBDB}..FTAPP_DLB1
                                SET SignedFiles = @SignedFiles, 
                                SignedFileUploadDt = GetDate()
                                WHERE Id  = @Id";

                var res = conn.Execute(sql,
                   new
                   {
                       Id = dlb1.id,
                       SignedFiles = dto.SignedFiles // direct over write
                   }, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"DLB1 File path no update, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                        $", type: {dto.DocType}, doc no: {dto.DocNum}");
                }


                if (logAct)
                {
                    var sp_ToLog_after = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                            @source , @action ";

                    var resTolog_After = conn.Execute(sp_ToLog_after, new
                    {
                        webDb = db.WEBDB,
                        dlbEntry = dto.DlbEntry,
                        docNum = dto.DocNum,
                        docType = dto.DocType,
                        source = "DriverApp",
                        action = "ViewAttachChange_After"
                    }, trans);
                }

                // 20240513
                // check the record exit or not 
                var sp_check = @$"Select top 1 docEntry from {db.WEBDB}..InvoiceAttach 
                                    where docEntry = @docEntry";

                var found = conn.ExecuteScalar<int>(sp_check, new { docEntry = dlb1.DocEntry }, trans);
                if (found > 0) // then perform update 
                {
                    // 20231228
                    // update the InvoiceAttach table row
                    var sql_update_invoiceattach = @$"update {db.WEBDB}..InvoiceAttach 
                                                            set Attachments = @attachments, 
                                                                AttchmentStatus = @AttchmentStatus, 
                                                                UploadBy = @uploadBy, 
                                                                UploadDate = GETDATE(), 
                                                                ApprSts = null, 
                                                                ApprRem = null
                                                            where docentry = @docEntry ";

                    res = conn.Execute(sql_update_invoiceattach,
                      new
                      {
                          attachments = dto.SignedFiles,
                          AttchmentStatus = dto.AttchmentStatus,
                          uploadBy = string.IsNullOrWhiteSpace(dto.DriverName) ? "" : dto.DriverName,
                          docEntry = dlb1.DocEntry,
                      }, trans);

                    if (res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Update InvoiceAttach fails, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum}");
                    }

                    // 20240511
                    // insert record for InvoiceAttach1  for history 
                    var insert_InvoiceAttach1Hist =
                        @$"Insert into {db.WEBDB}..InvoiceAttach1 (
                            DocEntry
                            , Action
                            , ActionBy
                            , ActionDate  
                            , ActionSts
                            , ActionRem
                          ) values (
                            @DocEntry
                           ,@Action
                           ,@ActionBy
                           ,GETDATE() 
                           ,@ActionSts
                           ,@ActionRem
                        )";

                    res = conn.Execute(insert_InvoiceAttach1Hist, new
                    {
                        DocEntry = dlb1.DocEntry,
                        Action = dto.AttchmentStatus,
                        ActionBy = string.IsNullOrWhiteSpace(dto.DriverName) ? "" : dto.DriverName,
                        ActionSts = dto.AttchmentStatus,
                        ActionRem = dto.AttchmentStatus
                    }, trans);

                    if (res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Update InvoiceAttach fails, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum}");
                    }
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

        IActionResult UpdateDlb1SignedDoc2_FromReUpload(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DlbEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (dto.DocNum < 0)
                {
                    return BadRequest("Invalid DLB doc number.");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var sqlCogFiles = $@"SELECT top 1 t1.* 
                                    from {db.WEBDB}..FTAPP_DLB t0 with (nolock)
                                    inner join {db.WEBDB}..FTAPP_DLB1 t1 with (nolock) on t1.HeadGuid = t0.HeadGuid
                                    where t0.DLBEntry = @DlbEntry
                                    and t1.DocNum =  @DocNum 
                                    and t1.DocType = @DocType order by id desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb1 = conn.Query<FTAPP_DLB1>(sqlCogFiles, new
                {
                    DlbEntry = dto.DlbEntry,
                    DocNum = dto.DocNum,
                    DocType = dto.DocType
                }).FirstOrDefault();

                if (dlb1 == null)
                {
                    // get the last dlb entry 
                    var dlbHelper = new DLBHelper(db);
                    dlbHelper.RecreteFTAPP_DLB_By_FTAPPDLB(dto.DlbEntry, db, _commDbConnStr);

                    // recheck 
                    dlb1 = conn.Query<FTAPP_DLB1>(sqlCogFiles, new
                    {
                        DlbEntry = dto.DlbEntry,
                        DocNum = dto.DocNum,
                        DocType = dto.DocType
                    }).FirstOrDefault();

                    // second check
                    if (dlb1 == null)
                    {
                        return BadRequest($"DLB doc not found for file update, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum} " +
                            $"[NO_DLB1_REC_FOUND]");
                    }
                }

                // get the logging setup 
                var sp_IsLogging = @"select setupvalue 
                                 from KTCW_COMMON..FTApp_Config 
                                 where setupname = 'DriveAppSignedFileLoggingEnable' ";

                var isLog = conn.ExecuteScalar<string>(sp_IsLogging);
                bool logAct = $"{isLog}".ToLower().Equals("y");


                // start the database transaction
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                if (logAct)
                {
                    // perform a backup log 
                    // do a loging to the last DLB1 
                    // as backup
                    var sp_ToLog_b4 = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                            @source , @action ";

                    var resTolog_b4 = conn.Execute(sp_ToLog_b4, new
                    {
                        webDb = db.WEBDB,
                        dlbEntry = dto.DlbEntry,
                        docNum = dto.DocNum,
                        docType = dto.DocType,
                        source = "DriverApp",
                        action = "ViewAttachChange_ReUploadB4"
                    }, trans);
                }

                // continue the normal process
                //string existingFiles = dto.SignedFiles;                

                // update the dlb1 table row
                // 20240511
                var sql = @$"UPDATE {db.WEBDB}..FTAPP_DLB1
                                SET SignedFiles = @SignedFiles, 
                                SignedFileUploadDt = GetDate()
                                WHERE Id  = @Id";

                var res = conn.Execute(sql,
                   new
                   {
                       Id = dlb1.id,
                       SignedFiles = dto.SignedFiles // direct over write
                   }, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"DLB1 File path no update, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                        $", type: {dto.DocType}, doc no: {dto.DocNum}");
                }

                if (logAct)
                {
                    var sp_ToLog_after = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                            @source , @action ";

                    var resTolog_After = conn.Execute(sp_ToLog_after, new
                    {
                        webDb = db.WEBDB,
                        dlbEntry = dto.DlbEntry,
                        docNum = dto.DocNum,
                        docType = dto.DocType,
                        source = "DriverApp",
                        action = "ViewAttachChange_ReUploadAfter"
                    }, trans);
                }

                // 20240513
                // check the record exit or not 
                var sp_check = @$"Select top 1 docEntry from {db.WEBDB}..InvoiceAttach 
                                    where docEntry = @docEntry";

                var found = conn.ExecuteScalar<int>(sp_check, new { docEntry = dlb1.DocEntry }, trans);
                if (found > 0) // then perform update 
                {
                    // 20231228
                    // update the InvoiceAttach table row
                    var sql_update_invoiceattach = @$"update {db.WEBDB}..InvoiceAttach 
                                                            set Attachments = @attachments, 
                                                                AttchmentStatus = @AttchmentStatus, 
                                                                UploadBy = @uploadBy, 
                                                                UploadDate = GETDATE(), 
                                                                ApprSts = null, 
                                                                ApprRem = null
                                                            where docentry = @docEntry ";

                    res = conn.Execute(sql_update_invoiceattach,
                    new
                    {
                        attachments = dto.SignedFiles,
                        AttchmentStatus = dto.AttchmentStatus,
                        uploadBy = string.IsNullOrWhiteSpace(dto.DriverName) ? "" : dto.DriverName,
                        docEntry = dlb1.DocEntry,
                    }, trans);

                    if (res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Update InvoiceAttach fails, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum}");
                    }

                    // 20240511
                    // insert record for InvoiceAttach1  for history 
                    var insert_InvoiceAttach1Hist =
                        @$"Insert into {db.WEBDB}..InvoiceAttach1 (
                            DocEntry
                            , Action
                            , ActionBy
                            , ActionDate  
                            , ActionSts
                            , ActionRem
                          ) values (
                            @DocEntry
                           ,@Action
                           ,@ActionBy
                           ,GETDATE() 
                           ,@ActionSts
                           ,@ActionRem
                        )";

                    res = conn.Execute(insert_InvoiceAttach1Hist, new
                    {
                        DocEntry = dlb1.DocEntry,
                        Action = dto.AttchmentStatus,
                        ActionBy = string.IsNullOrWhiteSpace(dto.DriverName) ? "" : dto.DriverName,
                        ActionSts = dto.AttchmentStatus,
                        ActionRem = dto.AttchmentStatus
                    }, trans);

                    if (res <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"Update InvoiceAttach fails, subsi {db.COMPANYNAME}, dlb entry : {dto.DlbEntry}" +
                            $", type: {dto.DocType}, doc no: {dto.DocNum}");
                    }
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

        // 20230911
        //IActionResult UpdateDlb1SignedDoc1(Dto_Dispatch dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("Company name is empty");
        //        }
        //        if (dto.DlbEntry < 0)
        //        {
        //            return BadRequest("Invalid doc entry");
        //        }
        //        if (dto.DocNum < 0)
        //        {
        //            return BadRequest("Invalid DLB doc number.");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.DocType))
        //        {
        //            return BadRequest("Invalid doc type");
        //        }

        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("Invalid company name, db info is empty");
        //        }

        //        var sqlCogFiles = $@"SELECT t1.* 
        //                            from {db.WEBDB}..FTAPP_DLB t0 with (nolock)
        //                            inner join {db.WEBDB}..FTAPP_DLB1 t1 with (nolock) on t1.HeadGuid = t0.HeadGuid
        //                            where t0.DLBEntry = @DlbEntry
        //                            and t1.DocNum =  @DocNum 
        //                            and t1.DocType = @DocType";

        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var dlb1 = conn.Query<FTAPP_DLB1>(sqlCogFiles, new
        //        {
        //            DlbEntry = dto.DlbEntry,
        //            DocNum = dto.DocNum,
        //            DocType = dto.DocType
        //        }).FirstOrDefault();

        //        if (dlb1 == null)
        //        {
        //            return BadRequest($"DLB doc not found for file update. subsi: {db.COMPANYNAME}, " +
        //                $"DLBEntry :{dto.DlbEntry}, doc type: {dto.DocType}, doc no: {dto.DocNum} ");
        //        }

        //        // 20231114
        //        // check exits and delete the pre files 
        //        // save disk space
        //        try
        //        {
        //            if (!string.IsNullOrWhiteSpace(dlb1.SignedFiles))
        //            {
        //                var filesName = dlb1.SignedFiles.Split(",");
        //                for (int x = 0; x < filesName.Length; x++)
        //                {
        //                    var filenam = $"{filesName[x]}".Trim();
        //                    if (string.IsNullOrWhiteSpace(filenam)) continue;

        //                    var filePath = System.IO.Path.Combine(_localAttchPath, filenam);
        //                    if (System.IO.File.Exists(filePath))
        //                    {
        //                        System.IO.File.Delete(filePath);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception fileException)
        //        {
        //            LastError = $"{fileException.Message}\n{fileException.StackTrace}";
        //            _logger.LogError(LastError);
        //        }

        //        // continue the normal process
        //        if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
        //        using var trans = conn.BeginTransaction();

        //        string existingFiles = dto.SignedFiles;
        //        var sql = @$"UPDATE {db.WEBDB}..FTAPP_DLB1
        //                        SET SignedFiles = @SignedFiles, 
        //                        SignedFileUploadDt = GetDate()
        //                        WHERE Id  = @Id";

        //        var res = conn.Execute(sql,
        //           new
        //           {
        //               Id = dlb1.id,
        //               SignedFiles = existingFiles
        //           }, trans);

        //        if (res <= 0)
        //        {
        //            trans.Rollback();
        //            return BadRequest($"File path update for DLB fail. subsi: {db.COMPANYNAME}, " +
        //                              $"DLBEntry :{dto.DlbEntry}, doc type: {dto.DocType}, doc no: {dto.DocNum} ");
        //        }

        //        // for create the transfer trace 
        //        // invoice only 
        //        if (dto.DocType == "I")
        //        {
        //            var isdone = CreateTransferToStore(db, dlb1.DocNum, conn, trans);
        //            if (isdone == false)
        //            {
        //                trans.Rollback();
        //                return BadRequest("Error create transfer doc, please contact support for help. Thanks.");
        //            }
        //        }

        //        trans.Commit();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        // orig, append the file list
        // 20231109
        IActionResult UpdateDlb1SignedDoc(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.DlbEntry < 0)
            {
                return BadRequest("Invalid doc entry");
            }
            if (dto.DocNum < 0)
            {
                return BadRequest("Invalid DLB doc number.");
            }
            if (string.IsNullOrWhiteSpace(dto.DocType))
            {
                return BadRequest("Invalid doc type");
            }
            if (string.IsNullOrWhiteSpace(dto.SignedFiles))
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

            var checkDlb1_sp = $@"SELECT t1.* 
                                  from {db.WEBDB}..FTAPP_DLB t0 with (nolock)
                                  inner join {db.WEBDB}..FTAPP_DLB1 t1 with (nolock) on t1.HeadGuid = t0.HeadGuid
                                  where t0.DLBEntry = @DlbEntry
                                  and t1.DocNum =  @DocNum 
                                  and t1.DocType = @DocType; ";

            using var checkConn = new SqlConnection(_commDbConnStr);
            var dlb1 = checkConn.Query<FTAPP_DLB1>(checkDlb1_sp, new
            {
                DlbEntry = dto.DlbEntry,
                dto.DocNum,
                dto.DocType
            }).FirstOrDefault();

            if (dlb1 == null) // if the dlb1 no found, recreate from script repair
            {
                return BadRequest($"Error query the FTAPP_DLB1 Entry {dto.DlbEntry}, Doc {dto.DocNum}, Type: {dto.DocType}");
            }

            // check if the sign files is empty then return             
            if (string.IsNullOrWhiteSpace(dto.SignedFiles))
            {
                return Ok();
            }

            // get the logging setup 
            var sp_IsLogging = @"select setupvalue 
                                 from KTCW_COMMON..FTApp_Config 
                                 where setupname = 'DriveAppSignedFileLoggingEnable' ";

            var isLog = checkConn.ExecuteScalar<string>(sp_IsLogging);
            bool logAct = $"{isLog}".ToLower().Equals("y");
            if (logAct)
            {
                // 20240815
                // create backuplog - before
                var sp_ToLog = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                 @source , @action ";

                var res = checkConn.Execute(sp_ToLog, new
                {
                    webDb = db.WEBDB,
                    dlbEntry = dto.DlbEntry,
                    docNum = dto.DocNum,
                    docType = dto.DocType,
                    source = "DriverApp",
                    action = "UpDocSinged_FirstTime_B4"
                });
            }

            // always add up
            string existingFiles = dlb1.SignedFiles;
            if (!string.IsNullOrWhiteSpace(existingFiles))
            {
                existingFiles += "," + dto.SignedFiles;
            }
            else
            {
                existingFiles = dto.SignedFiles;
            }

            // start the update 
            var updateConn = new SqlConnection(_commDbConnStr);
            updateConn.Open();
            var updTrans = updateConn.BeginTransaction();
            var sql = @$"UPDATE {db.WEBDB}..FTAPP_DLB1
                            SET SignedFiles = @SignedFiles, 
                            SignedFileUploadDt = GetDate()
                            WHERE Id  = @Id";
            try
            {
                var upd_sqlRes = updateConn.Execute(sql,
                   new
                   {
                       Id = dlb1.id,
                       SignedFiles = existingFiles
                   }, updTrans);

                if (upd_sqlRes < 0)
                {
                    updTrans.Rollback();
                    return BadRequest($"{db.COMPANYNAME} Update DLB2 signed doc file fail, Please try again, " +
                                       $"Doc: {dto.DocNum}, Type: {dto.DocType}");
                }


                // 20240815
                // create backuplog - after
                if (logAct)
                {
                    var sp_ToLog_after = @$"exec Sp_SelectInsertDLb1_Log @webDb , @dlbEntry,  @docNum, @docType, 
                                 @source , @action ";

                    var res_after = updateConn.Execute(sp_ToLog_after, new
                    {
                        webDb = db.WEBDB,
                        dlbEntry = dto.DlbEntry,
                        docNum = dto.DocNum,
                        docType = dto.DocType,
                        source = "DriverApp",
                        action = "UpDocSinged_FirstTime_After"
                    }, updTrans);
                }

                // for create the transfer trace 
                // invoice only 
                if (dto.DocType == "I")
                {
                    var isDone = CreateTransferToStore(db, dlb1.DocNum, updateConn, updTrans);
                    if (isDone == false)
                    {
                        updTrans.Rollback();
                        return BadRequest($"{db.COMPANYNAME} Create transfer to store fail (after update signed doc path), " +
                                      $"Please try again, " +
                                      $"Doc: {dto.DocNum}, Type: {dto.DocType}");
                    }
                }

                updTrans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                updTrans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        bool CreateTransferToStore(DbInfo db, int InvNum, SqlConnection updateConn, SqlTransaction updateTrans)
        {
            try
            {
                using var selectConn = new SqlConnection(_commDbConnStr);
                var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                Models.Pick.OINV inv = selectConn.Query<Models.Pick.OINV>(query_inv,
                    new
                    {
                        docnum = InvNum
                    }).FirstOrDefault();

                if (inv == null)
                {
                    LastError = $"[CreateTransferToStore] Inv# {InvNum} no found in DB {db.COMPANYNAME}";
                    _logger.LogError(LastError);
                    return false;
                }

                // check does the transfer done to the store 
                var sp_check = @$"select t0.* 
                                    from {db.WEBDB}..FTAPP_Transfer t0 with (nolock) 
                                    inner join {db.WEBDB}..FTAPP_Transfer1 t1 with (nolock)  on t1.GroupGuid = t0.GroupGuid
                                    where t0.ReceiverCode = @CardCode
                                    and t1.InvNo = @InvNo ";

                var found = selectConn.Query<FTAPP_Transfer>(sp_check,
                    new
                    {
                        CardCode = inv.CardCode,
                        InvNo = InvNum
                    }).FirstOrDefault();

                if (found != null)
                {
                    LastError = $"[CreateTransferToStore] Inv# {InvNum} found in DB " +
                                        $"{db.COMPANYNAME}, no update needed.";
                    _logger.LogError(LastError);
                    return true;
                }

                var groupGuid = Guid.NewGuid();
                var tHead = new FTAPP_Transfer
                {
                    ReceiverCode = inv.CardCode,
                    ReceiverName = inv.CardName,
                    LocationCode = inv.ShipToCode,
                    LocationName = inv.ShipToCode,
                    TransDt = DateTime.Now,
                    DocStatus = "T",
                    DriverName = "NA",
                    GroupGuid = groupGuid,
                    Module = "ToStore"
                };

                var tLines = new List<FTAPP_Transfer1>();
                var tLinesBoxes = new List<FTAPP_Transfer2>();

                // get the box list from web portal
                var query_box = $@" select DISTINCT   t0.BoxId
                                                      , t0.PickerCode
                                                      , t0.PickerName
                                                      , t0.PickDt
                                                      , t0.PackId
                                                      , t0.PackDt
                                                      , t0.PackerCode
                                                      , t0.PackerName
                                                      , t0.BaseEntry
                                                      , t0.BoxGuid
                                                      , t0.TimeStampSeq
                                                      , t0.AppVersion
                                                      , t0.BoxSize
                                                      , t0.OrderProcessWeek
                                                      , t0.BusinessCenterCode
                                                      , t0.CurrentCartonNo
                                                      , t0.OrderNo
                                                      , t0.LabelConsistTotalBoxes

                                    from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                                    left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                    Where t0.BaseEntry = @baseentry 
                                    and t1.BoxGuid is not null";

                var boxes = selectConn.Query<FTAPP_Box>(query_box, new
                {
                    baseentry = inv.U_SOID
                }).ToList();

                //if (boxes.Count == 0)
                //{
                //    LastError = $"[CreateTransferToStore] No boxes found for Inv# {InvNum} @ DB {db.COMPANYNAME}";
                //    _logger.LogError(LastError);
                //    return false;
                //}

                // create the line
                var newT = new FTAPP_Transfer1
                {
                    InvNo = InvNum,
                    TransDt = DateTime.Now,
                    GroupGuid = groupGuid
                };

                tLines.Add(newT);

                if (boxes.Count > 0)
                {
                    // prepare the box 
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var box = new FTAPP_Transfer2
                        {
                            BoxId = boxes[b].BoxId,
                            InvNo = inv.DocNum,
                            TransDt = DateTime.Now,
                            GroupGuid = groupGuid
                        };
                        tLinesBoxes.Add(box);
                    }
                }

                var helper = new TransferHelper(db);
                var isCreated = helper.CreateTransfer(tHead, tLines, tLinesBoxes,
                                                    updateConn, updateTrans, out string errMsg);
                if (isCreated)
                {
                    return true;
                }

                LastError = $"[CreateTransferToStore] {errMsg} @ {db.COMPANYNAME}";
                _logger.LogError(LastError);
                return false;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        IActionResult UpdateDriverNRIC_Dispatch(Dto_Dispatch dto)
        {
            //if (string.IsNullOrWhiteSpace(dto.Subsi))
            //{
            //    return BadRequest("Invalid subsi");
            //}
            if (dto.HeadGuid == default)
            {
                return BadRequest("Invalid dlb guid");
            }
            if (string.IsNullOrWhiteSpace(dto.SignFiles))
            {
                return BadRequest("Invalid signed file");
            }

            var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
            for (int d = 0; d < dbs.Count; d++)
            {
                var db = dbs[d];
                if (db == null) continue;

                var qr_dlb = @$"Select * from {db.WEBDB}..FTAPP_DLB with (nolock)
                                Where HeadGuid = @headerGuid ";

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb = conn.Query<FTAPP_DLB>(qr_dlb, new
                {
                    headerGuid = dto.HeadGuid
                }).FirstOrDefault();

                if (dlb == null) continue;  //return BadRequest("DLB no found, pls try again");

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    var update_sql = $@"update {db.WEBDB}..FTAPP_DLB 
                                    set NRICFile = @NRICFile 
                                    where HeadGuid = @headguid";
                    var res = conn.Execute(update_sql, new
                    {
                        NRICFile = dto.SignFiles,
                        headguid = dlb.HeadGuid
                    }, trans);

                    trans.Commit();
                    continue;
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }
            }

            return Ok();
        }

        IActionResult UpdateDocSign_Dispatch(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.HeadGuid == default)
            {
                return BadRequest("Invalid dlb guid");
            }
            if (string.IsNullOrWhiteSpace(dto.SignFiles))
            {
                return BadRequest("Invalid signed file");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            var qr_dlb = @$"Select * from {db.WEBDB}..FTAPP_DLB with (nolock)
                                Where HeadGuid = @headerGuid ";

            using var conn = new SqlConnection(_commDbConnStr);
            var dlb = conn.Query<FTAPP_DLB>(qr_dlb, new
            {
                headerGuid = dto.HeadGuid
            }).FirstOrDefault();

            if (dlb == null) return BadRequest("DLB no found, pls try again");

            string existingFiles = $"{dlb.SignedFile}";
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
            try
            {
                var update_sql = $@"update {db.WEBDB}..FTAPP_DLB 
                                    set SignedFile = @signedFile 
                                    where HeadGuid = @headguid";
                var res = conn.Execute(update_sql, new
                {
                    signedFile = existingFiles,
                    headguid = dlb.HeadGuid
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

        IActionResult LoadTrcn_RetDoc(Dto_Dispatch dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty"); // default is manager
                }

                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("Truck is empty");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start query date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end query date");
                }

                var companies = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                var rtns = new List<Return_Doc>();
                for (int c = 0; c < companies.Count; c++)
                {
                    var db = companies[c];    //new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                    if (db == null)
                    {
                        return BadRequest("db info query error, or invalid subsi");
                    }

                    var sql = "exec sp_SelectRTN_Driver @webDb, @erpDb, @startDt, @endDt, @subsi, @userCode, @truckNo";

                    var conn = new SqlConnection(_commDbConnStr);
                    var res = conn.Query<Return_Doc>(sql,
                        new
                        {
                            webDb = db.WEBDB,
                            erpDb = db.SAPDB,
                            startDt = dto.StartDt,
                            endDt = dto.EndDt,
                            subsi = db.COMPANYNAME,
                            userCode = dto.UserCode,
                            truckNo = dto.TruckNo
                        }).ToList();

                    if (res.Count == 0) continue;
                    rtns.AddRange(res);
                }

                if (rtns.Count == 0) return NotFound();
                for (int i = 0; i < rtns.Count; i++) // assign a id 
                {
                    rtns[i].Id = i;
                }

                return Ok(rtns);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInvAndLines(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.InvNum))
                {
                    return BadRequest("Invalid invoice entry");
                }
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


                // return ok when all status 
                // get the invoice from sap                     
                var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @InvNum";
                Models.Pick.OINV inv = conn.Query<Models.Pick.OINV>(query_inv, new { dto.InvNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for sap invoice.");
                }

                // get the invoice line 
                var qr_invLines = @$"Select * from {db.SAPDB}..INV1 with (nolock) where DocEntry = @DocEntry";
                inv.Lines = conn.Query<INV1>(qr_invLines, new { inv.DocEntry }).ToList();

                // get the box list from web portal
                //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                //                    Where baseentry = @baseentry";

                // get the box list from web portal
                var query_box = $@"select DISTINCT   t0.BoxId
                                                      , t0.PickerCode
                                                      , t0.PickerName
                                                      , t0.PickDt
                                                      , t0.PackId
                                                      , t0.PackDt
                                                      , t0.PackerCode
                                                      , t0.PackerName
                                                      , t0.BaseEntry
                                                      , t0.BoxGuid
                                                      , t0.TimeStampSeq
                                                      , t0.AppVersion
                                                      , t0.BoxSize
                                                      , t0.OrderProcessWeek
                                                      , t0.BusinessCenterCode
                                                      , t0.CurrentCartonNo
                                                      , t0.OrderNo
                                                      , t0.LabelConsistTotalBoxes

                                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                Where t0.BaseEntry = @baseentry 
                                and t1.BoxGuid is not null";

                inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                if (inv.Boxes.Count == 0)
                {
                    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                }

                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;

                return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadReturnThruInvoices(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("Invalid plate no");
                }

                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }

                var rturnList = new List<FTAPP_RET_THR_INV>();
                var companies = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                for (int c = 0; c < companies.Count; c++)
                {
                    var db = companies[c];
                    if (db == null) continue;

                    //var qr_retInv = @$"select '{db.COMPANYNAME}' [SubSi]
                    //            , * 
                    //            from {db.WEBDB}..FTAPP_RET_Thr_Inv 
                    //                Where TruckNo = @TruckNo                                     
                    //                and convert(date, transdt) >= @StartDt
                    //                and convert(date, transdt) <= @EndDt";

                    var qr_retInv = @$"select t0.*, '{db.COMPANYNAME}' [SubSi], t1.Reason [Reason]
                                    from {db.WEBDB}..FTAPP_RET_Thr_Inv t0 with (nolock)
                                    left join {db.WEBDB}..FTAPP_DriverLog t1 with (nolock) 
                                        on t1.DlbEntry = t0.DlbEntry and t1.DocNum = t0.InvNum and t1.DocType = @docTYpe
                                    Where t0.TruckNo = @TruckNo
                                    and convert(date, t0.transdt) >= @StartDt
                                    and convert(date, t0.transdt) <= @EndDt";

                    using var conn = new SqlConnection(_commDbConnStr);
                    var list = conn.Query<FTAPP_RET_THR_INV>(qr_retInv, new
                    {
                        dto.TruckNo,
                        dto.StartDt,
                        dto.EndDt,
                        docTYpe = "I"
                    }).ToList();

                    if (list.Count == 0) continue;
                    rturnList.AddRange(list);
                }

                if (rturnList.Count == 0) return NotFound();
                return Ok(rturnList);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }



        IActionResult ReturnThruInvoice(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.RetInv == null)
            {
                return BadRequest("Invalid return inv");
            }
            if (dto.DlbEntry <= 0)
            {
                return BadRequest("Invalid dlb entry");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid inv num");
            }
            if (string.IsNullOrWhiteSpace(dto.TruckNo))
            {
                return BadRequest("Invalid plate no");
            }
            //if (string.IsNullOrWhiteSpace(dto.DriverName)) // commented for non checking 
            //{
            //    return BadRequest("Invalid driver name ");
            //}

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            // check duplication
            var check_dup_qr = $@"Select * from {db.WEBDB}..FTAPP_RET_THR_INV with (nolock)
                                    Where DlbEntry = @DlbEntry 
                                    and InvNum = @InvNum 
                                    and TruckNo = @TruckNo
                                    and DriverName = @DriverName";

            using var conn = new SqlConnection(_commDbConnStr);
            var found = conn.Query<FTAPP_RET_THR_INV>(check_dup_qr, new
            {
                dto.DlbEntry,
                dto.InvNum,
                dto.TruckNo,
                dto.DriverName
            }).FirstOrDefault();

            // if found save 
            if (found != null)
            {
                if (found.DocStatus.Equals("Returned"))
                {
                    return BadRequest("Invoice returned.");
                }
                else
                {
                    return BadRequest("Invoice added, await return.");
                }
            }

            // else no found any

            // get number of ite for this invoice
            // get the doc entry for this invoice 

            var ItemCount_qr = $@" select count(1) [ItemCount], t0.DocEntry [InvEntry]
                                    from {db.SAPDB}..oinv t0 with(nolock) inner join 
                                         {db.SAPDB}..inv1 t1 with(nolock) on t0.DocEntry = t1.DocEntry
                                    where t0.DocNum = @InvNum
                                    group by t0.DocEntry ";

            var newReturnInv = conn.Query<FTAPP_RET_THR_INV>(ItemCount_qr, new { dto.InvNum }).FirstOrDefault();
            if (newReturnInv != null)
            {
                newReturnInv.DocStatus = dto.RetInv.DocStatus;
                newReturnInv.DlbEntry = dto.RetInv.DlbEntry;
                newReturnInv.InvNum = dto.RetInv.InvNum;
                newReturnInv.BoxCount = dto.RetInv.BoxCount;
                //newReturnInv.InvEntry = dto.RetInv.InvEntry;
                //newReturnInv.ItemCount = dto.RetInv.ItemCount;
                newReturnInv.StoreCode = dto.RetInv.StoreCode;
                newReturnInv.StoreName = dto.RetInv.StoreName;
                newReturnInv.TruckNo = dto.RetInv.TruckNo;
                newReturnInv.DriverName = dto.RetInv.DriverName;
                newReturnInv.Transporter = dto.RetInv.Transporter;
            }
            else
            {
                newReturnInv = dto.RetInv;
            }

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            var insert_qr = $@" INSERT INTO {db.WEBDB}..FTAPP_RET_THR_INV (
                                         DocStatus
                                       , DlbEntry
                                       , InvNum
                                       , InvEntry
                                       , TransDt
                                       , ItemCount
                                       , BoxCount 
                                       , StoreCode
                                       , StoreName, TruckNo, DriverName, Transporter
                                    ) values ( 
                                      @DocStatus
                                     ,@DlbEntry
                                     ,@InvNum
                                     ,@InvEntry
                                     ,GETDATE()
                                     ,@ItemCount
                                     ,@BoxCount 
                                     ,@StoreCode
                                     ,@StoreName, @TruckNo, @DriverName, @Transporter )";
            try
            {
                var res = conn.Execute(insert_qr, newReturnInv, trans);
                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, DocNum: {dto.InvDocNum}, insert FTAPP_RET_THR_INV fail, please try again.");
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

        IActionResult ClearReturnThruInvoice(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DlbEntry <= 0)
            {
                return BadRequest("Invalid dlb entry");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid inv num");
            }
            if (string.IsNullOrWhiteSpace(dto.TruckNo))
            {
                return BadRequest("Invalid plate no");
            }
            //if (string.IsNullOrWhiteSpace(dto.DriverName))
            //{
            //    return BadRequest("Invalid driver name ");
            //}

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            // check duplication
            var check_dup_qr = $@"Select * from {db.WEBDB}..FTAPP_RET_THR_INV  with (nolock)
                                    Where DlbEntry = @DlbEntry 
                                    and InvNum = @InvNum 
                                    and TruckNo = @TruckNo
                                    and DriverName = @DriverName 
                                    and DocStatus = 'Intransit' ";

            using var conn = new SqlConnection(_commDbConnStr);
            var isFound = conn.Query<FTAPP_RET_THR_INV>(check_dup_qr, new
            {
                dto.DlbEntry,
                dto.InvNum,
                dto.TruckNo,
                dto.DriverName
            }).FirstOrDefault();

            // if not found
            if (isFound == null) return Ok();

            // if found
            var delete_qr = $"Delete from {db.WEBDB}..FTAPP_RET_THR_INV Where id = @id";
            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            var trans = conn.BeginTransaction();
            try
            {
                var res = conn.Execute(delete_qr, new { isFound.id }, trans);
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

        IActionResult UpdateDriverLog(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.Log == null)
            {
                return BadRequest("Invalid driver log");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);

            // check any loging duplicated
            var sp_checDup = @$"Select * from {db.WEBDB}..FTAPP_DriverLog 
                                where DocNum = @DocNum 
                                and DlbEntry = @DlbEntry 
                                and DocType = @DocType 
                                and TruckNo = @TruckNo 
                                and  CardCode = @CardCode
                                and DriverName = @DriverName ";

            var founds = conn.Query<FTAPP_DriverLog>(sp_checDup, new
            {
                DocNum = dto.Log.DocNum,
                DlbEntry = dto.Log.DlbEntry,
                DocType = dto.Log.DocType,
                TruckNo = dto.Log.TruckNo,
                CardCode = dto.Log.CardCode,
                DriverName = dto.Log.DriverName
            }).ToList();

            // if found duplicated
            if (founds != null && founds.Count > 0)
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }

                // delete all insert 1 
                var sp_delete = @$"delete from {db.WEBDB}..FTAPP_DriverLog 
                                where DocNum = @DocNum 
                                and DlbEntry = @DlbEntry 
                                and DocType = @DocType 
                                and TruckNo = @TruckNo 
                                and  CardCode = @CardCode
                                and DriverName = @DriverName ";

                using var trans1 = conn.BeginTransaction();
                try
                {
                    // delete the all log 
                    var deleteRes = conn.Execute(sp_delete, new
                    {
                        DocNum = dto.Log.DocNum,
                        DlbEntry = dto.Log.DlbEntry,
                        DocType = dto.Log.DocType,
                        TruckNo = dto.Log.TruckNo,
                        CardCode = dto.Log.CardCode,
                        DriverName = dto.Log.DriverName
                    }, trans1);

                    if (deleteRes <= 0)
                    {
                        trans1.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, Error delete DocNum: {dto.Log.DocNum}, " +
                                        $"DocType: {dto.Log.DocType}, DLB: {dto.Log.DlbEntry}, FTAPP_DriverLog");
                    }

                    // take the first record
                    var found = founds.FirstOrDefault();
                    found.Reason = dto.Log.Reason;

                    // insert back the first records
                    var insert_sql = $@"INSERT INTO {db.WEBDB}..FTAPP_DriverLog (
                                         ReportAs
                                       , TransDt
                                       , DlbEntry
                                       , DocNum
                                       , DocType
                                       , Reason
                                       , TruckNo
                                       , CardCode
                                       , CardName
                                       , DriverName 
                                    ) values (  
                                        @ReportAs
                                       ,GETDATE()
                                       ,@DlbEntry
                                       ,@DocNum
                                       ,@DocType
                                       ,@Reason
                                       ,@TruckNo
                                       ,@CardCode
                                       ,@CardName
                                       ,@DriverName ) ";

                    var res = conn.Execute(insert_sql, found, trans1);
                    if (res <= 0)
                    {
                        trans1.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, Error insert DocNum: {dto.Log.DocNum}, " +
                                     $"DocType: {dto.Log.DocType}, DLB: {dto.Log.DlbEntry}, FTAPP_DriverLog");
                    }

                    trans1.Commit();
                    return Ok();
                }
                catch (Exception cSql)
                {
                    trans1.Rollback();
                    LastError = $"{cSql.Message}\n{cSql.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"Update - Request not handler.\n{LastError}");
                }
            }

            // insert new 
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }

            using var trans = conn.BeginTransaction();
            try
            {
                var insert_sql = $@"INSERT INTO {db.WEBDB}..FTAPP_DriverLog (
                                         ReportAs
                                       , TransDt
                                       , DlbEntry
                                       , DocNum
                                       , DocType
                                       , Reason
                                       , TruckNo
                                       , CardCode
                                       , CardName
                                       , DriverName 
                                    ) values (  
                                        @ReportAs
                                       ,GETDATE()
                                       ,@DlbEntry
                                       ,@DocNum
                                       ,@DocType
                                       ,@Reason
                                       ,@TruckNo
                                       ,@CardCode
                                       ,@CardName
                                       ,@DriverName ) ";

                var res = conn.Execute(insert_sql, dto.Log, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, Error insert DocNum: {dto.Log.DocNum}, " +
                                 $"DocType: {dto.Log.DocType}, DLB: {dto.Log.DlbEntry}, FTAPP_DriverLog");
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

        IActionResult GetInvoiceSeller(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("Invalid doc docntry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                var query = $@"select t2.USERCODE [UserCode], t2.USERNAME [UserName]
                            from 
                            {db.SAPDB}..OINV t0  with (nolock) inner join 
                            {db.WEBDB}..SO t1    with (nolock) on t1.DOCENTRY = t0.U_SOID inner join                             
                            {db.WEBDB}..USERS t2 with (nolock) on t2.USERCODE = t1.UCREATED
                            Where t0.DocEntry = @InvDocEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var seller = conn.Query<FTAPP_SSO>(query, new
                {
                    dto.InvDocEntry
                }).FirstOrDefault();

                if (seller == null) // take manager as default account to allow process the trcn
                {
                    seller = new FTAPP_SSO
                    {
                        UserCode = "manager",
                        UserName = "manager"
                    };
                }

                return Ok(seller);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var seller = new FTAPP_SSO //return manager as default
                {
                    UserCode = "manager",
                    UserName = "manager"
                };

                return Ok(seller);
            }
        }

        IActionResult GetCogSeller(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.CogDocEntry < 0)
                {
                    return BadRequest("Invalid doc docntry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                //var query = $@"select t2.USERCODE [UserCode], t2.USERNAME [UserName]
                //            from 
                //            {db.SAPDB}..OINV t0  with (nolock) inner join 
                //            {db.WEBDB}..SO t1    with (nolock) on t1.DOCENTRY = t0.U_SOID inner join 
                //            {db.WEBDB}..USERS t2 with (nolock) on t2.USERCODE = t1.UCREATED
                //            Where t0.DocEntry = @InvDocEntry";

                var query = @$"select t1.USERCODE [UserCode], t1.USERNAME [UserName]
                            from 
                            {db.WEBDB}..COG t0   with (nolock) inner join 
                            {db.WEBDB}..USERS t1 with (nolock) on t0.UCREATED = t1.USERCODE 
                            Where t0.DOCENTRY = @CogDocEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var seller = conn.Query<FTAPP_SSO>(query, new
                {
                    dto.CogDocEntry
                }).FirstOrDefault();

                if (seller == null) return NotFound();
                return Ok(seller);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ResetBoxOutStayInTruck(Dto_Dispatch dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid invoice number");
            }
            if (dto.SoDocEntry < 0)
            {
                return BadRequest("Invalid so doc entry");
            }
            if (dto.DlbEntry < 0)
            {
                return BadRequest("Invalid dlb entry");
            }
            if (string.IsNullOrWhiteSpace(dto.DocType))
            {
                return BadRequest("Invalid doc type");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            // verify 
            // query the guid for the dlb 
            using var selectConn = new SqlConnection(_commDbConnStr);
            var query_dlb = $@"Select * from {db.WEBDB}..FTAPP_DLB with (nolock) Where DlbEntry = @DlbEntry";
            var dlb = selectConn.Query<FTAPP_DLB>(query_dlb, new { dto.DlbEntry }).FirstOrDefault();

            if (dlb == null)
            {
                return BadRequest($"DLB entry #{dto.DlbEntry} no found, " +
                    $"subsi: {db.COMPANYNAME}\n Type: {dto.DocType} Doc no: {dto.InvNum}");
            }

            // start update with trans
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {

                // update the FTAPP_DLB1 
                var update_dlb1 = $@"update {db.WEBDB}..FTAPP_DLB1 set OutDt = null 
                                    where HeadGuid = @HeadGuid 
                                    and DocNum = @DocNum
                                    and DocType = @DocType ";

                var updDLB1_res = conn.Execute(update_dlb1, new
                {
                    dlb.HeadGuid,
                    DocNum = dto.InvNum,
                    DocType = dto.DocType
                }, trans);

                if (updDLB1_res < 0)
                {
                    trans.Rollback();
                    return BadRequest($"DLB entry #{dto.DlbEntry} update DLB1 fail, " +
                        $"subsi: {db.COMPANYNAME}\n Type: {dto.DocType} Doc no: {dto.InvNum}");
                }

                // else perform update of the intransist date time 
                var update_sql = @$"Update {db.WEBDB}..FTAPP_DLB2                                    
                        set OutTransitDt = NUll
                        Where InvDocNum = @InvNum
                        and SoDocEntry = @SoDocEntry 
                        and DlbEntry = @DlbEntry ";

                var updDLB2_res = conn.Execute(update_sql, new { dto.InvNum, dto.SoDocEntry, dto.DlbEntry }, trans);
                if (updDLB2_res < 0)
                {
                    trans.Rollback();
                    return BadRequest($"DLB entry #{dto.DlbEntry} update DLB2 fail, " +
                        $"subsi: {db.COMPANYNAME}\n Type: {dto.DocType} Doc no: {dto.InvNum}");
                }

                // 2022 11 22
                // reset the delivery date at sap invoice 
                // set the delivery date to null by invoice doc num
                if (dto.DocType == "I")
                {
                    var update_invoice_deliveryDt = @$"Update {db.SAPDB}..OINV set U_DelDate = NULL 
                                                       where DocNum = @DocNum";
                    var updt_Sap_Deldt = conn.Execute(update_invoice_deliveryDt, new
                    {
                        DocNum = dto.InvNum
                    }, trans);

                    if (updt_Sap_Deldt < 0)
                    {
                        trans.Rollback();
                        return BadRequest($"DLB entry #{dto.DlbEntry} update SAP Inv Delivery DT fail, " +
                            $"subsi: {db.COMPANYNAME}\n Type: {dto.DocType} Doc no: {dto.InvNum}");
                    }
                }

                if (dto.DocType == "T")
                {
                    var update_transfer_deliveryDt = @$"Update {db.SAPDB}..OWTR set U_DelDate = NULL 
                                                where DocNum = @DocNum";
                    var updt_trnf_Deldt = conn.Execute(update_transfer_deliveryDt, new
                    {
                        DocNum = dto.InvNum
                    }, trans);

                    if (updt_trnf_Deldt < 0)
                    {
                        trans.Rollback();
                        return BadRequest($"DLB entry #{dto.DlbEntry} update SAP Transfer Delivery DT fail, " +
                            $"subsi: {db.COMPANYNAME}\n Type: {dto.DocType} Doc no: {dto.InvNum}");
                    }
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

        IActionResult VerifyAndUpdateDriverDoc_Out(Dto_Dispatch dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.DocType))
            {
                return BadRequest("Invalid Doc Type");
            }
            if (dto.DlbEntry < 0)
            {
                return BadRequest("Invalid Doc Type");
            }
            if (dto.DocNum < 0)
            {
                return BadRequest("Invalid Doc number");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            // check dlb exist 
            using var checkConn = new SqlConnection(_commDbConnStr);
            var query_dlb = @$"select * from {db.WEBDB}..FTAPP_DLB with (NOLOCK) 
                                        Where DlbEntry = @DlbEntry";

            var dlb = checkConn.Query<FTAPP_DLB>(query_dlb, new { dto.DlbEntry }).FirstOrDefault();
            if (dlb == null)
            {
                return BadRequest($"{db.COMPANYNAME}, " +
                     $"The query DLB records {dto.DlbEntry}, for doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                     $"please try again. Thanks.");
            }

            var query_dlb1 = @$" Select * 
                                from  {db.WEBDB}..FTAPP_DLB1   with (nolock)                                
                                where HeadGuid = @HeadGuid
                                and DocNum = @DocNum
                                and DocType = @DocType 
                                order by id desc ";

            var foundDoc = checkConn.Query<FTAPP_DLB1>(query_dlb1, new
            {
                HeadGuid = dlb.HeadGuid,
                DocNum = dto.DocNum,
                DocType = dto.DocType
            }).FirstOrDefault();

            if (foundDoc == null)
            {
                var sp_recreateDLb1 = $@"exec sp_RepairFTAPP_DLB1_SingleInvNo @webDb, @dlbEntry, @invNum, @docType ";
                using var reCreateConn = new SqlConnection(_commDbConnStr);
                var res = reCreateConn.Execute(sp_recreateDLb1, new
                {
                    webDb = db.WEBDB,
                    dlbEntry = dto.DlbEntry,
                    invNum = dto.DocNum,
                    docType = dto.DocType
                });

                foundDoc = checkConn.Query<FTAPP_DLB1>(query_dlb1, new
                {
                    HeadGuid = dlb.HeadGuid,
                    DocNum = dto.DocNum,
                    DocType = dto.DocType
                }).FirstOrDefault();

                //return BadRequest($"{db.COMPANYNAME}, " +
                //    $"The query DLB1 doc records {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                //    $"please try again. Thanks.");
            }

            // check line 

            var sp_checkLine = @$"select * from {db.WEBDB}..FTAPP_DLB2 with (nolock)                                 
                                    Where HeadGuid = @HeadGuid 
                                    and InvDocNum = @InvDocNum;";

            var lines = checkConn.Query<FTAPP_DLB2>(sp_checkLine, new
            {
                HeadGuid = dlb.HeadGuid,
                InvDocNum = dto.DocNum
            }).ToList();

            if (lines.Count == 0)
            {
                var sp_recreateDlb2 = $"exec sp_RepairFTAPP_DLB2_SingleInvNo @webDb, @dlbNum, @invNum ";
                using var reCreateConn_dlb2 = new SqlConnection(_commDbConnStr);
                var res = reCreateConn_dlb2.Execute(sp_recreateDlb2, new
                {
                    webDb = db.WEBDB,
                    dlbNum = dto.DlbEntry,
                    invNum = dto.DocNum
                });

                //return BadRequest($"{db.COMPANYNAME}, " +
                //  $"The query DLB2 doc records {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                //  $"please try again. Thanks.");
            }

            // start update 
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {

                // update all the box out time 
                // else perform update of the intransist date time 
                var update_sql1 = @$"Update {db.WEBDB}..FTAPP_DLB2                                    
                                    set OutTransitDt = GETDATE()
                                    Where HeadGuid = @HeadGuid 
                                    and InvDocNum = @InvDocNum; ";

                var res_UpdDLB2Dt = conn.Execute(update_sql1, new
                {
                    dlb.HeadGuid,
                    InvDocNum = dto.DocNum
                }, trans);

                if (res_UpdDLB2Dt <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, " +
                                    $"Error update DLB2 doc records {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                                    $"please try again. Thanks.");
                }

                // else perform update of the out transist date time (FTAPP DLB1)
                var update_sql = @$"Update {db.WEBDB}..FTAPP_DLB1
                        set OutDt = GETDATE()
                        Where Headguid = @id";

                var res_UpdDlb1Dt = conn.Execute(update_sql, new { id = foundDoc.HeadGuid }, trans);
                if (res_UpdDlb1Dt <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, " +
                                    $"Error update DLB1 doc records {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                                    $"please try again. Thanks.");
                }

                // 2022 11 22
                // update the sap invoice delivery datetime 
                if (dto.DocType == "I")
                {
                    var update_Inv_deliveryDt = @$"Update {db.SAPDB}..OINV set U_DelDate = @DateValue where DocNum = @DocNum";
                    var res_updSapInv_DeliveryDt = conn.Execute(update_Inv_deliveryDt, new
                    {
                        DateValue = $"{DateTime.Now:yyyy-MM-dd}",
                        DocNum = dto.DocNum
                    }, trans);

                    if (res_updSapInv_DeliveryDt <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, " +
                                        $"Error update Invoice {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                                        $"please try again. Thanks.");
                    }
                }


                // update transfer doc delivery date
                // 20230521
                if (dto.DocType == "T")
                {
                    var update_TrnsfDt = @$"Update {db.SAPDB}..OWTR set U_DelDate = @DateValue where DocNum = @DocNum";
                    var updateSAPTransfer_DeliveryDatRes = conn.Execute(update_TrnsfDt, new
                    {
                        DateValue = $"{DateTime.Now:yyyy-MM-dd}",
                        DocNum = dto.DocNum
                    }, trans);

                    if (updateSAPTransfer_DeliveryDatRes <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, " +
                                       $"Error update Transfer {dto.DlbEntry}, doc: {dto.DocNum}, type:{dto.DocType} no found, " +
                                       $"please try again. Thanks.");
                    }
                }

                // all good and commit 
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

        IActionResult VerifyAndUpdateDriverBox(Dto_Dispatch dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid invoice number");
            }
            if (string.IsNullOrWhiteSpace(dto.BoxId))
            {
                return BadRequest("Invalid box id");
            }
            if (dto.DlbEntry < 0)
            {
                return BadRequest("Invalid dlb entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            var query_box = @$" Select * from  {db.WEBDB}..FTAPP_DLB2 with (NOLOCK)
                                    Where InvDocNum = @InvDocNum
                                          and BoxId = @BoxId 
                                          and DLBEntry = @DlbEntry";

            using var conn = new SqlConnection(_commDbConnStr);
            var foundBx = conn.Query<FTAPP_DLB2>(query_box, new
            {
                InvDocNum = dto.InvNum,
                BoxId = dto.BoxId,
                DlbEntry = dto.DlbEntry
            }).FirstOrDefault();

            if (foundBx == null)
            {
                // 20251203
                var sp_repair = "exec sp_RepairFTAPP_DLB2_SingleInvNo @webDb , @dlbNum , @invNum ";
                var updated = conn.Execute(sp_repair, new
                {
                    webDb = db.WEBDB,
                    dlbNum = dto.DlbEntry,
                    invNum = dto.InvNum
                });

                foundBx = conn.Query<FTAPP_DLB2>(query_box, new
                {
                    InvDocNum = dto.InvNum,
                    BoxId = dto.BoxId,
                    DlbEntry = dto.DlbEntry
                }).FirstOrDefault();

                //return BadRequest($"Box {dto.BoxId}, no found for transfer, please try again.");
            }

            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }

            try
            {
                using var trans = conn.BeginTransaction();
                // else perform update of the intransist date time 
                // update based on id 
                var update_sql = @$"Update {db.WEBDB}..FTAPP_DLB2                                    
                        set OutTransitDt = GETDATE()
                        Where id = @id";

                var updateRes = conn.Execute(update_sql, new { id = foundBx.id }, trans);
                if (updateRes == 1)
                {
                    trans.Commit();
                    return Ok();
                }

                trans.Rollback();
                return BadRequest($"Update box id: {foundBx.BoxId} fail, please try again.");
            }
            catch (Exception e)
            {                
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadTransferBoxes(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.TransferDocNum))
                {
                    return BadRequest("invalid inv doc num");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                var transfer = conn.Query<OWTR_Ext>(query_transfer,
                    new
                    {
                        webDb = db.WEBDB,
                        transferDocNum = dto.TransferDocNum
                    }).FirstOrDefault();

                if (transfer == null) return NotFound();

                // 20251128
                // get the setup app config 
                var sp_setupConfig = $@"select SetupValue from KTCW_COMMON..FTAPP_Config 
                                      where SetupName= 'DeliveryAppUsedInputBoxQty_IBT' ";

                var isAllowInputBxWty_val = conn.ExecuteScalar<string>(sp_setupConfig);
                if ($"{isAllowInputBxWty_val}".ToLower().Equals("y"))
                {
                    transfer.IsAllowInputBoxQty = true;
                }

                // 20230517
                // query all the box with this ibt / transfer doc
                // get the box list from web portal
                var query_box = $@"select DISTINCT t0.BoxId
                                        , t0.PickerCode
                                        , t0.PickerName
                                        , t0.PickDt
                                        , t0.PackId
                                        , t0.PackDt
                                        , t0.PackerCode
                                        , t0.PackerName
                                        , t0.BaseEntry
                                        , t0.BoxGuid
                                        , t0.TimeStampSeq
                                        , t0.AppVersion
                                        , t0.BoxSize
                                        , t0.OrderProcessWeek
                                        , t0.BusinessCenterCode
                                        , t0.CurrentCartonNo
                                        , t0.OrderNo
                                        , t0.LabelConsistTotalBoxes

                                        from {db.WEBDB}..FTAPP_IBTBox t0 with (NOLOCK)
                                        left join {db.WEBDB}..IBT     t1 with (NOLOCK) on t1.DOCENTRY = t0.BaseEntry
                                        Where t1.TRANSITNO =  @IbtDocNum  ";

                // based on scan in transfer doc 

                transfer.Boxes = conn.Query<FTAPP_Box>(query_box, new { IbtDocNum = transfer.DocNum }).ToList();
                if (transfer.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                }

                transfer.SubSi = db.COMPANYNAME;
                transfer.SubsiId = db.COMPANYID;

                return Ok(transfer);

                // return ok when all status 
                // get the invoice from sap                     
                //var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                //Models.Pick.OINV inv = conn.Query<Models.Pick.OINV>(query_inv, new { docnum = dto.InvNum }).FirstOrDefault();
                //if (inv == null)
                //{
                //    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for sap invoice.");
                //}

                //// get the box list from web portal
                ////var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                ////                    Where baseentry = @baseentry";

                //// get the box list from web portal
                //var query_box = $@"select DISTINCT   t0.BoxId
                //                                      , t0.PickerCode
                //                                      , t0.PickerName
                //                                      , t0.PickDt
                //                                      , t0.PackId
                //                                      , t0.PackDt
                //                                      , t0.PackerCode
                //                                      , t0.PackerName
                //                                      , t0.BaseEntry
                //                                      , t0.BoxGuid
                //                                      , t0.TimeStampSeq
                //                                      , t0.AppVersion
                //                                      , t0.BoxSize
                //                                      , t0.OrderProcessWeek
                //                                      , t0.BusinessCenterCode
                //                                      , t0.CurrentCartonNo
                //                                      , t0.OrderNo
                //                                      , t0.LabelConsistTotalBoxes

                //                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                //                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                //                Where t0.BaseEntry = @baseentry 
                //                and t1.BoxGuid is not null";

                //inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                //if (inv.Boxes.Count == 0)
                //{
                //    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                //}

                //inv.Subsi = db.COMPANYNAME;
                //inv.SubsiId = db.COMPANYID;
                //return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInvBoxes(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvNum))
                {
                    return BadRequest("invalid inv doc num");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                var allowInputBoxQty_sp = "Select SetupValue from ktcw_common..FTAPP_Config " +
                    "where SetupName = 'DeliveryAppUsedInputBoxQty' ";

                var allowInputBoxQty = conn.ExecuteScalar<string>(allowInputBoxQty_sp);
                var isAllowInputBox = false;
                if (allowInputBoxQty.ToLower().Equals("y"))
                {
                    isAllowInputBox = true;
                }

                // return ok when all status 
                // get the invoice from sap                     
                var query_inv = @$"select '{isAllowInputBox}' [IsAllowInputBoxQty]                                             
                                            , t2.GlblLocNum [DROP_POINT_GEOCODE]  
                                            , t2.WhsCode [InterBranchWhsCode]
                                            , t2.WhsName  [InterBranchWhsName]
                                            , t0.*
                                        from {db.SAPDB}..OINV t0 with (NOLOCK) 
                                        left join {db.SAPDB}..OCRD t1 on t1.CardCode = t0.CardCode 
                                        left join {db.SAPDB}..OWHS t2 on t2.WhsCode = t1.U_DROPPOINT
                                        where DocNum = @DocNum";

                Models.Pick.OINV inv = conn.Query<Models.Pick.OINV>(query_inv, new { DocNum = dto.InvNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for sap invoice.");
                }

                // get the box list from web portal
                var query_box = $@"select DISTINCT    t0.PickerCode
                                                      , t0.PickerName
                                                      , t0.PickDt
                                                      , t0.PackId
                                                      , t0.PackDt
                                                      , t0.PackerCode
                                                      , t0.PackerName
                                                      , t0.BaseEntry
                                                      , t0.BoxGuid
                                                      , t0.TimeStampSeq
                                                      , t0.AppVersion
                                                      , t0.BoxSize
                                                      , t0.OrderProcessWeek
                                                      , t0.BusinessCenterCode
                                                      , t0.CurrentCartonNo
                                                      , t0.OrderNo
                                                      , t0.LabelConsistTotalBoxes
                                                      , t0.BoxId
                                from {db.WEBDB}..FTAPP_Box t0 with (NOLOCK)                                
                                Where t0.BaseEntry = @BaseEntry  ";

                var boxes = conn.Query<FTAPP_Box>(query_box, new { BaseEntry = inv.U_SOID }).ToList();
                boxes = boxes.Distinct().ToList();
                if (boxes.Count == 0)
                {
                    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                }

                inv.Boxes = new List<FTAPP_Box>(boxes);
                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;
                return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadIBTBoxes(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.TransferNum))
                {
                    return BadRequest("invalid transfer doc num");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                var transfer = conn.Query<OWTR_Ext>(query_transfer,
                    new
                    {
                        webDb = db.WEBDB,
                        transferDocNum = dto.TransferNum
                    }).FirstOrDefault();

                if (transfer == null) return NotFound();


                // 20230517
                // query all the box with this ibt / transfer doc
                // get the box list from web portal
                var query_box = $@"select DISTINCT t0.BoxId
                                                      , t0.PickerCode
                                                      , t0.PickerName
                                                      , t0.PickDt
                                                      , t0.PackId
                                                      , t0.PackDt
                                                      , t0.PackerCode
                                                      , t0.PackerName
                                                      , t0.BaseEntry
                                                      , t0.BoxGuid
                                                      , t0.TimeStampSeq
                                                      , t0.AppVersion
                                                      , t0.BoxSize
                                                      , t0.OrderProcessWeek
                                                      , t0.BusinessCenterCode
                                                      , t0.CurrentCartonNo
                                                      , t0.OrderNo
                                                      , t0.LabelConsistTotalBoxes

                                from {db.WEBDB}..FTAPP_IBTBox t0 with (nolock)
                                left join  {db.WEBDB}..FTAPP_IBTBox1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                left join {db.WEBDB}..IBT t3 on t3.DocEntry = t0.BaseEntry
                                Where t3.TRANSITNO = @IbtDocNum 
                                and t1.BoxGuid is not null";

                // based on scan in transfer doc 

                transfer.Boxes = conn.Query<FTAPP_Box>(query_box, new { IbtDocNum = transfer.DocNum }).ToList();
                if (transfer.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                }

                return Ok(transfer);

                //// return ok when all status 
                //// get the invoice from sap                     
                //var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                //Models.Pick.OINV inv = conn.Query<Models.Pick.OINV>(query_inv, new { docnum = dto.InvNum }).FirstOrDefault();
                //if (inv == null)
                //{
                //    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for sap invoice.");
                //}

                //// get the box list from web portal
                //var query_box = $@"select DISTINCT   t0.BoxId
                //                                      , t0.PickerCode
                //                                      , t0.PickerName
                //                                      , t0.PickDt
                //                                      , t0.PackId
                //                                      , t0.PackDt
                //                                      , t0.PackerCode
                //                                      , t0.PackerName
                //                                      , t0.BaseEntry
                //                                      , t0.BoxGuid
                //                                      , t0.TimeStampSeq
                //                                      , t0.AppVersion
                //                                      , t0.BoxSize
                //                                      , t0.OrderProcessWeek
                //                                      , t0.BusinessCenterCode
                //                                      , t0.CurrentCartonNo
                //                                      , t0.OrderNo
                //                                      , t0.LabelConsistTotalBoxes

                //                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                //                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                //                Where t0.BaseEntry = @baseentry 
                //                and t1.BoxGuid is not null";

                //inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                //if (inv.Boxes.Count == 0)
                //{
                //    return BadRequest($"Invoice #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                //}

                //inv.Subsi = db.COMPANYNAME;
                //inv.SubsiId = db.COMPANYID;
                //return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadUnDeliveryReasonCodes(Dto_Dispatch dto)
        {
            try
            {
                var query = @"Select * 
                             from FTAPP_DeliveryReasonCodes with (nolock)";

                using var conn = new SqlConnection(_commDbConnStr);
                var reasonCodes = conn.Query<UnDeliveredReasonCodes>(query).ToList();
                if (reasonCodes.Count == 0) return NotFound();

                return Ok(reasonCodes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInv(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.InvDocNum < 0)
                {
                    return BadRequest("invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var query_inv = $@"select * from {db.SAPDB}..OINV with (nolock) where docnum = @InvDocNum";
                using var conn = new SqlConnection(_commDbConnStr);
                var found = conn.Query<Models.Pick.OINV>(query_inv, new { dto.InvDocNum }).FirstOrDefault();

                if (found == null) return NotFound();
                return Ok(found);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInvLines(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.InvDocEntry < 0)
                {
                    return BadRequest("invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_InvLine = @$"Select * 
                                    from {db.SAPDB}..INV1 with (nolock) 
                                    where DocEntry = @InvDocEntry";
                using var conn = new SqlConnection(_commDbConnStr);
                var lines = conn.Query<INV1>(sp_InvLine, new
                {
                    dto.InvDocEntry
                }).ToList();

                if (lines.Count == 0) return NotFound();
                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadDlbDoc_ByCardName(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("invalid truck no.");
                }
                if (string.IsNullOrWhiteSpace(dto.CardName))
                {
                    return BadRequest("invalid store name");
                }
                if (dto.DlvryDate == default)
                {
                    return BadRequest("Invalid date");
                }

                // exec KTCW_COMMON..[sp_GetDlbDoc_ByCardName] 'AVONWEB', 'KDW6661', 'SERVAY HYPERMARKET(LIKA'
                // cobine reading the dlb summary 
                var companies = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);

                if (companies == null)
                {
                    return BadRequest("delivery features no deployed");
                }
                if (companies.Count == 0)
                {
                    return BadRequest("delivery features no deployed");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb1s = new List<DLB1>();
                for (int c = 0; c < companies.Count; c++)
                {
                    var db = companies[c];
                    if (db == null) continue;


                    string newCardName = dto.CardName;
                    if (dto.CardName.Contains("'"))
                    {
                        newCardName = dto.CardName.Replace("'", "''");
                    }

                    var sp_query = @"exec sp_GetDlbDoc_ByCardName_Test1 @webDb, @truckNo, @storeName";
                    var list = conn.Query<DLB1>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        truckNo = dto.TruckNo,
                        storeName = newCardName
                    }).ToList();

                    if (list.Count == 0) continue;

                    // filter again usong programming method to 
                    // to get the store name again from the list
                    var filterByCardName = list.Where(d => d.CARDNAME == dto.CardName).ToList();
                    if (filterByCardName.Count == 0) continue;

                    dlb1s.AddRange(filterByCardName);
                }

                if (dlb1s.Count == 0)
                {
                    return NotFound();
                }

                return Ok(dlb1s);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadDlbSumm(Dto_Dispatch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("invalid truck no.");
                }

                if (dto.DlvryDate == default)
                {
                    return BadRequest("Invalid date");
                }

                // cobine reading the dlb summary 
                var companies = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                if (companies.Count == 0)
                {
                    return BadRequest("delivery features no deployed");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var dlbs = new List<DLBSumm>();
                for (int c = 0; c < companies.Count; c++)
                {
                    var db = companies[c];
                    if (db == null) continue;

                    var sp_query = @"Exec sp_GetDlbDoc @webDb, @truckNo";
                    var list = conn.Query<DLBSumm>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        truckNo = dto.TruckNo
                    }).ToList();

                    if (list.Count == 0) continue;
                    dlbs.AddRange(list);
                }

                if (dlbs.Count == 0)
                {
                    return NotFound();
                }

                // group by customer name / store code from multiple company
                var groupByStore = dlbs
                    .GroupBy(u => u.CardName)
                    .Select(grp => new DLBSumm
                    {
                        CardName = grp.First().CardName,
                        CardCode = grp.First().CardCode,
                        Territory = grp.First().Territory,
                        GeoCode = grp.First().GeoCode,
                        Street = grp.First().Street,
                        Block = grp.First().Block,
                        ZipCode = grp.First().ZipCode,
                        City = grp.First().City,
                        County = grp.First().County,
                        Country = grp.First().County,
                        State = grp.First().State,
                        NumOfDoc = grp.Sum(invCount => invCount.NumOfDoc),
                        DriverName = grp.First().DriverName,
                        IsInterbranch = grp.First().IsInterbranch,
                    }).ToList();

                if (groupByStore.Count == 0) return NotFound();

                // check the check in status 
                for (int g = 0; g < groupByStore.Count; g++)
                {
                    var store = groupByStore[g];
                    if (store == null) continue;

                    var sql_checkIn = @$"select top 1 CheckedInDt 
                                      from KTCW_COMMON..FTAPP_StoreCheckedIn 
                                      Where CardName = @CardName 
                                      and TruckNo = @TruckNo 
                                      order by id desc";

                    // update the last check in time.
                    groupByStore[g].LastCheckedInDt = conn.ExecuteScalar<DateTime>(sql_checkIn,
                        new
                        {
                            CardName = store.CardName,
                            TruckNo = dto.TruckNo
                        });

                    if (store.IsInterbranch) // load in the invoice, drop point warehouse 
                    {
                        // get a invoice number (any)
                        // get the invoice drop point whs 

                        for (int u = 0; u < companies.Count; u++)
                        {
                            var db = companies[u];
                            if (db == null) continue;

                            var sp_Listinvoice = "exec sp_GetDlbDoc_ByCardName_Test1 @webDb, @truckNo, @storeName";
                            var dlb1s = conn.Query<DLB1>(sp_Listinvoice, new
                            {
                                webDb = db.WEBDB,
                                truckNo = dto.TruckNo,
                                storeName = store.CardName
                            }).ToList();

                            if (dlb1s.Count == 0) continue;

                            if (store.DocType == "I") // invoice 
                            {
                                // base on the dlb1
                                var invoice = dlb1s.Where(i => i.DOCTYPE == "I").FirstOrDefault();
                                if (invoice == null) continue;

                                var query_inv = @$"select t2.GlblLocNum [DROP_POINT_GEOCODE]                                           
                                                , t2.WhsCode [InterBranchWhsCode]
                                                , t2.WhsName  [InterBranchWhsName]
                                                , t0.*
                                 from {db.SAPDB}..OINV t0 with (NOLOCK) 
                                 left join {db.SAPDB}..OCRD t1 with (NOLOCK) on t1.CardCode = t0.CardCode 
                                 left join {db.SAPDB}..OWHS t2 with (NOLOCK) on t2.WhsCode = t1.U_DROPPOINT
                                 where DocNum = @DocNum";

                                var inv = conn.Query<Models.Pick.OINV>(query_inv, new
                                {
                                    DocNum = invoice.DOCNUM
                                }).FirstOrDefault();

                                if (inv != null)
                                {
                                    groupByStore[g].DROP_POINT_GEOCODE = inv.DROP_POINT_GEOCODE;
                                    groupByStore[g].WhsCode = inv.InterBranchWhsCode;
                                    groupByStore[g].WhsName = inv.InterBranchWhsName;
                                }
                            }
                            else // transfer
                            {
                                // base on the dlb1
                                var transfer = dlb1s.Where(i => i.DOCTYPE == "T").FirstOrDefault();
                                if (transfer == null) continue;

                                var sp_ibtWhs = $@"select t1.GlblLocNum [DROP_POINT_GEOCODE]
                                                , t1.WhsCode [InterBranchWhsCode]
                                                , t1.WhsName [InterBranchWhsName]
                                                from {db.WEBDB}..IBT t0 with (NOLOCK)
                                                left join {db.SAPDB}..OWHS t1 with (NOLOCK) on t1.WhsCode = t0.WhsCode
                                                Where t0.Transitno = @transferDocNum";

                                var trf = conn.Query<Models.Pick.OINV>(sp_ibtWhs, new
                                {
                                    transferDocNum = transfer.DOCNUM
                                }).FirstOrDefault();

                                if (trf != null)
                                {
                                    groupByStore[g].DROP_POINT_GEOCODE = trf.DROP_POINT_GEOCODE;
                                    groupByStore[g].WhsCode = trf.InterBranchWhsCode;
                                    groupByStore[g].WhsName = trf.InterBranchWhsName;
                                }

                            }
                        }
                    }
                }

                return Ok(groupByStore);
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
