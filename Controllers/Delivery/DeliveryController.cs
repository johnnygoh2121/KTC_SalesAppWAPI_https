using Dapper;
using KTC_SalesAppWAPI.DTOs.Delivery;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.Delivery;
using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.TrcukInspection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Delivery
{
    [Route("[controller]")]
    [ApiController]
    public class DeliveryController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<DeliveryController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";

        public DeliveryController(IConfiguration configuration, ILogger<DeliveryController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_Delivery dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetListOfDlb":
                    {
                        return GetListOfDlb(dto);
                    }
                case "GetListOf_FTAPP_Dlb":
                    {
                        return GetListOf_FTAPP_Dlb(dto);
                    }
                case "GetSingle_FTAPP_Dlb":
                    {
                        return GetSingle_FTAPP_Dlb(dto);
                    }
                case "GetTrucks":
                    {
                        return GetTrucks(dto);
                    }
                case "VerifyInvoice_FreshAdd":
                    {
                        return VerifyInvoice_FreshAdd(dto);
                    }
                case "VerifyInvoice":
                    {
                        return VerifyInvoice(dto);
                    }
                case "VerifyCog":
                    {
                        return VerifyCog(dto);
                    }
                case "VerifyTransfer":
                    {
                        return VerifyTransfer(dto);
                    }
                case "SaveOutBox":
                    {
                        return SaveOutBox(dto);
                    }
                case "SaveDLB1":
                    {
                        return SaveDLB1(dto); // save dlb and dlb 1
                    }
                case "RemoveDoc":
                    {
                        return RemoveDoc(dto);
                    }
                case "RemoveDeliveryInv":
                    {
                        return RemoveDeliveryInv(dto);
                    }
                case "RemoveDeliveryDoc":
                    {
                        return RemoveDeliveryDoc(dto);
                    }
                case "RemoveDeliveryInv_ByDriver":
                    {
                        return RemoveDeliveryInv_ByDriver(dto);
                    }
                case "CheckDraftLine":
                    {
                        return CheckDraftLine(dto);
                    }
                case "ClearDraft":
                    {
                        return ClearDraft(dto);
                    }
                case "LoadCountedBoxes":
                    {
                        return LoadCountedBoxes(dto);
                    }
                case "DriverLogin":
                    {
                        return DriverLogin(dto);
                    }
                case "DriverCheckDoc":
                    {
                        return DriverCheckDoc(dto);
                    }
                case "VerifyAndUpdateDriverBox":
                    {
                        return VerifyAndUpdateDriverBox(dto);
                    }
                case "VerifyAndUpdateDriverDoc":
                    {
                        return VerifyAndUpdateDriverDoc(dto);
                    }
                case "SaveDLB_ByDriver":
                    {
                        return SaveDLB_ByDriver(dto);
                    }
                case "CheckDriverDlb":
                    {
                        return CheckDriverDlb(dto);
                    }
                case "CheckDriverDlb_ByGuid":
                    {
                        return CheckDriverDlb_ByGuid(dto);
                    }
                case "LoadDlbLines":
                    {
                        return LoadDlbLines(dto);
                    }
                case "LoadAppConfig":
                    {
                        return LoadAppConfig();
                    }
                case "SaveDBLToOut":
                    {
                        return SaveDBLToOut(dto);
                    }
                case "GetDriverDocs":
                    {
                        return GetDriverDocs(dto);
                    }
                case "GetDriverDocs_Complete":
                    {
                        return GetDriverDocs_Complete(dto);
                    }
                case "SaveDLB1_Line":
                    {
                        return SaveDLB1_Line(dto);
                    }
                case "RemoveDeliverySaveDraft":
                    {
                        return RemoveDeliverySaveDraft(dto);
                    }
                case "CheckDriveAvail":
                    {
                        return CheckDriveAvail(dto);
                    }
                case "CheckingDriverDrafts":
                    {
                        return CheckingDriverDrafts(dto);
                    }
                case "LoadCounted_IBTBoxes":
                    {
                        return LoadCounted_IBTBoxes(dto);
                    }
                case "GetAgedInvoice":
                    {
                        return GetAgedInvoice(dto);
                    }
                case "GetSubsiWarehouse":
                    {
                        return GetSubsiWarehouse(dto);
                    }
                default:
                    {
                        return BadRequest("no recognized request");
                    }
            }
        }

        IActionResult GetSubsiWarehouse(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid SUBSI");
                }

                var dbs = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbs == null)
                {
                    return BadRequest("Invalid SUBSI");
                }

                var query = $"select * from {dbs.SAPDB}..OWHS ";
                using var conn = new SqlConnection(_commDbConnStr);

                var whs = conn.Query<OWHS_Ext>(query).ToList();
                if (whs.Count == 0)
                {
                    return NotFound();
                }

                return Ok(whs);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetAgedInvoice(Dto_Delivery dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("Invalid whsCode");
                }

                var dbs = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbs == null)
                {
                    return BadRequest("Invalid subsi");
                }

                var agedInvsAll = new List<AgedDoc>();
                var agedInvs_sp = "exec sp_GetOldestAgedInv_v1 @webDb, @whsCode ";
                using var conn = new SqlConnection(_commDbConnStr);
                var agedInvs = conn.Query<AgedDoc>(agedInvs_sp, new
                {
                    webDb = dbs.WEBDB,
                    whsCode = dto.WhsCode
                }).ToList();

                if (agedInvs.Count > 0)
                {
                    agedInvsAll.AddRange(agedInvs);
                }

                // 20240316
                var sp_intransitWhs = "exec sp_GetOldestAgedInv_TransWhs_v1 @webDb, @whsCode ";
                var agedInvs_InsWhs = conn.Query<AgedDoc>(sp_intransitWhs, new
                {
                    webDb = dbs.WEBDB,
                    whsCode = dto.WhsCode
                }).ToList();

                if (agedInvs_InsWhs.Count > 0)
                {
                    agedInvsAll.AddRange(agedInvs_InsWhs);
                }

                if (agedInvsAll.Count == 0)
                {
                    return NotFound();
                }

                agedInvsAll = agedInvsAll.Distinct().ToList(); // remove duplicated invoice 

                return Ok(agedInvsAll);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CheckingDriverDrafts(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DriverName))
                {
                    return BadRequest("Invalid driver name");
                }
                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                var dlb_drafts = new List<FTAPP_DLB>();
                using var conn = new SqlConnection(_commDbConnStr);

                for (int d = 0; d < dbs.Count; d++)
                {
                    var db = dbs[d];
                    if (db == null) continue;

                    var sp_query = @"exec sp_QueryDriverDlbDrafts @webDb , @dlbStatus,  @truckNo, @driverName";
                    var db_dlb_drafts = conn.Query<FTAPP_DLB>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        dlbStatus = "D",
                        truckNo = dto.PlateNo,
                        driverName = dto.DriverName
                    }).ToList();

                    if (db_dlb_drafts.Count == 0) continue;

                    // 20230516
                    var zeroDocCountDrafts = db_dlb_drafts.Where(d => d.DocCount == 0).ToList();
                    if (zeroDocCountDrafts.Count > 0)
                    {
                        for (int i = 0; i < zeroDocCountDrafts.Count; i++)
                        {
                            // delete the draft head 
                            var deletesql = @$"delete from {db.WEBDB}..FTAPP_DLB where id = @id";
                            conn.Execute(deletesql, new { id = zeroDocCountDrafts[i].id });
                        }
                    }

                    var containDrafts = db_dlb_drafts.Where(d => d.DocCount > 0).ToList();
                    if (containDrafts.Count == 0) continue;

                    dlb_drafts.AddRange(containDrafts);
                }

                return Ok(dlb_drafts);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }



        IActionResult CheckDriveAvail(Dto_Delivery dto)
        {
            try
            {
                // need to swicth back when confirm the test from daniel
                // 20220818
                var sp_chckDelByPassSetting = $"Select SetupValue " +
                                        $"from FTApp_Config " +
                                        $"Where SetupName ='ByPass_DeliveryAppControlClearDLbWhenAddNewDlb'";

                // for testing enviroment
                //var sp_chckDelByPassSetting = $"Select SetupValue " +
                //                        $"from FTApp_Config " +
                //                        $"Where SetupName ='ByPass_DeliveryAppControlClearDLbWhenAddNewDlb_84'";

                using var conn = new SqlConnection(_commDbConnStr);
                var isByPass = conn.ExecuteScalar<string>(sp_chckDelByPassSetting);

                if (!string.IsNullOrWhiteSpace(isByPass) && isByPass.ToLower().Equals("y"))
                {
                    return Ok(); // is by pass checking, ok to add new dlb
                }

                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("Invalid truck no.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                // 20230519
                // check all inspection pass for today 
                var query_truckInsp = @$"select * from {db.WEBDB}..FTAPP_TruckInspection t0
                                        where CONVERT(date, t0.Date) <= convert(date, getdate())
                                        and TruckNo = @truckNo
                                        order by DocEntry desc";

                var inspHead = conn.Query<FTAPP_TruckInspection>(query_truckInsp, new { truckNo = dto.TruckNo }).FirstOrDefault();
                if (inspHead == null)
                {
                    return BadRequest($"Please conduct a inspection to truck, before load in DLB.");
                }

                // query the line 
                var query_truckInspLines = @$"Select * from {db.WEBDB}..FTAPP_TruckInspection1 Where DocEntry = @docentry";
                var inspLines = conn.Query<FTAPP_TruckInspection1>(query_truckInspLines, new { docentry = inspHead.DocEntry }).ToList();

                if (inspLines.Count == 0)
                {
                    return BadRequest($"Please conduct a inspection to truck, before load in DLB. [1]");
                }

                var Ngs = inspLines.Where(x => x.InspectionResult == 0).Select(c => $"NG -> {c.Inspection}").ToArray();
                if (Ngs.Length > 0)
                {
                    var combineString = string.Join("\n\n", Ngs);
                    return BadRequest($"Inspection result show contain NG, no DLB loading allowed. [Summary of NG]\n{combineString}");
                }

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                if (dbs.Count == 0)
                {
                    return BadRequest("Checking dlb on truck, invalid dbi");
                }

                // continue the checking 
                var message = string.Empty;
                for (int d = 0; d < dbs.Count; d++)
                {
                    var sp_query = @"exec sp_CheckDriveAvail @webDb, @truckNo";
                    var dlb1s = conn.Query<DLB1>(sp_query, new { webDb = dbs[d].WEBDB, truckNo = dto.PlateNo }).ToList();

                    if (dlb1s.Count == 0) continue;
                    var invs = string.Join("\n", dlb1s.Select(d => d.DOCNUM).Distinct().ToList());
                    if (!string.IsNullOrWhiteSpace(invs))
                    {
                        // accumulate message
                        message += $"{dbs[d].COMPANYNAME}\n{dto.PlateNo} occupied with - DLB Inv(s) - \n\n{invs}\n\n";
                    }
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return BadRequest(message);
                }

                return Ok(); // ok to add new dlb
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult RemoveDeliverySaveDraft(Dto_Delivery dto)
        {
            try
            {
                //if (string.IsNullOrWhiteSpace(dto.Subsi))
                //{
                //    return BadRequest("Invalid subsi");
                //}

                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid guid");
                }
                if (string.IsNullOrWhiteSpace(dto.DriverName))
                {
                    return BadRequest("Invalid driver name");
                }
                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                for (int i = 0; i < dbs.Count; i++)
                {
                    var db = dbs[i];
                    if (db == null)
                    {
                        return BadRequest("invalid dbi");
                    }

                    // query the dlb 
                    // if dlb is empty tehn by pass the the dlb
                    var query_dlb = @$"select * from {db.WEBDB}..FTAPP_DLB Where HeadGuid = @HeadeGuid";
                    using var conn = new SqlConnection(_commDbConnStr);
                    var dlb = conn.Query<FTAPP_DLB>(query_dlb, new
                    {
                        HeadeGuid = dto.SaveHeadGuid
                    }).FirstOrDefault();

                    if (dlb == null) continue; // continue next db
                    if (dlb.DLBStatus != "D") continue; // only draft can be delete
                    if (dlb.DLBEntry > 0) continue; // created DLB can not delete

                    // query list of doc num under this head guid
                    var query_docs = @$"select * from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @HeadeGuid";
                    var dlb1s = conn.Query<FTAPP_DLB1>(query_docs, new
                    {
                        HeadeGuid = dto.SaveHeadGuid
                    }).ToList();

                    // having doc in it 
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        // delete each doc 
                        for (int d = 0; d < dlb1s.Count; d++)
                        {
                            var doc = dlb1s[d];
                            if (doc == null) continue;


                            var delete_sql = @$"delete from {db.WEBDB}..FTAPP_DLB2 
                                                Where convert(nvarchar(50), HeadGuid) = @HeadGuid";

                            conn.Execute(delete_sql, new { HeadGuid = $"{doc.HeadGuid}" }, trans);


                            // 20221004
                            // check does this invoice status in DLB1 is O 
                            // if o then no delete .. if not then delete
                            var query_dlb1 = @$"select  * 
                                                from {db.WEBDB}..DLB1 with (nolock) 
                                                Where DocNum = @docnum 
                                                and docType = @doctype";

                            var dlb1s2 = conn.Query<DLB1>(query_dlb1, new { docnum = doc.DocNum, doctype = doc.DocType }, trans).ToList();
                            if (dlb1s2.Count == 0) // no dlb found, delete on hold
                            {
                                // delete the on hold docment 
                                var delete_onhold = @$"delete from {db.WEBDB}..FTAPP_HoldDlvryDocs 
                                                   where DocNum = @DocNum 
                                                   and DocType = @DocType 
                                                    and UserCode = @PlateNo
                                                    and UserName = @DriverName";

                                conn.Execute(delete_onhold, new
                                {
                                    DocNum = doc.DocNum,
                                    DocType = doc.DocType,
                                    Plateno = dto.PlateNo,
                                    DriverName = dto.DriverName
                                }, trans);
                            }
                            else // got dlb record 
                            {
                                // then check status as O 
                                var oStatusdlb1 = dlb1s2.Where(x => $"{x.STATUS}".ToLower() == "o").FirstOrDefault();
                                if (oStatusdlb1 == null)
                                {
                                    // delete the on hold docment 
                                    var delete_onhold = @$"delete from {db.WEBDB}..FTAPP_HoldDlvryDocs 
                                                   where DocNum = @DocNum 
                                                   and DocType = @DocType 
                                                    and UserCode = @PlateNo
                                                    and UserName = @DriverName";

                                    conn.Execute(delete_onhold, new
                                    {
                                        DocNum = doc.DocNum,
                                        DocType = doc.DocType,
                                        Plateno = dto.PlateNo,
                                        DriverName = dto.DriverName
                                    }, trans);
                                }
                                // else is O the leave it onhold
                            }
                        }

                        // delete the doc 
                        var delete_query_doc = @$"delete from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @HeadeGuid";
                        conn.Execute(delete_query_doc, new { HeadeGuid = dto.SaveHeadGuid }, trans);

                        // delete the head 
                        var delete_query_doc_head = @$"delete from {db.WEBDB}..FTAPP_DLB Where HeadGuid = @HeadeGuid";
                        conn.Execute(delete_query_doc_head, new { HeadeGuid = dto.SaveHeadGuid }, trans);

                        // create delete log 
                        var newLog = new FTAPP_DLB_DRAFT_DEL_LOG
                        {
                            HeadGuid = dto.SaveHeadGuid,
                            TransDt = DateTime.Now,
                            DriverName = dlb.DriverName,
                            TruckNo = dlb.TruckNo
                        };

                        var insert_log = @$"Insert into {db.WEBDB}..FTAPP_DLB_DRAFT_DEL_LOG 
                                            (HeadGuid, TransDt, DriverName, TruckNo )
                                            values 
                                            (@HeadGuid, GETDATE(), @DriverName, @TruckNo)";

                        conn.Execute(insert_log, newLog, trans);

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }
                    continue; // for company name loop
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

        IActionResult SaveDLB1_Line(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.SaveDLB1Line == null)
            {
                return BadRequest("Invalid dlb1 line");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sp_update = @$"UPDATE {db.WEBDB}..FTAPP_DLB1
                                    SET  RefNo = @RefNo
                                        , ConsigmentNo = @ConsigmentNo
                                    WHERE  id = @id";

                var res = conn.Execute(sp_update, dto.SaveDLB1Line, trans);
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

        IActionResult GetDriverDocs(Dto_Delivery dto)
        {
            try
            {
                // get the dlb 
                // get the dlb1 

                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid dlb guid");
                }

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);

                using var conn = new SqlConnection(_commDbConnStr);
                FTAPP_DLB dlbHead = null;


                for (int i = 0; i < dbs.Count; i++)
                {
                    var webDb = dbs[i];
                    if (webDb == null) continue;

                    // check is created flb 
                    var sp_isCreated = @$"select * from {webDb.WEBDB}..FTAPP_DLB with (nolock)
                                     where DLBEntry is not null 
                                     and DLBStatus = @DLBStatus
                                     and CONVERT(nvarchar(50), HeadGuid) = @HeadGuid ";

                    var isCreated = conn.Query<FTAPP_DLB>(sp_isCreated, new
                    {
                        HeadGuid = $"{dto.SaveHeadGuid}",
                        DLBStatus = "O"
                    }).FirstOrDefault();

                    if (isCreated != null)
                    {
                        continue;
                        //return BadRequest($"DLB #{isCreated.DLBEntry} was created for GUID : {dto.SaveHeadGuid}");
                    }

                    var sp_dlb = @$"select * from {webDb.WEBDB}..FTAPP_DLB with (nolock)
                                     where DLBEntry is null 
                                     and DLBStatus = @DLBStatus
                                     and CONVERT(nvarchar(50), HeadGuid) = @HeadGuid ";

                    var dlbCheck = conn.Query<FTAPP_DLB>(sp_dlb, new
                    {
                        HeadGuid = $"{dto.SaveHeadGuid}",
                        DLBStatus = "D"
                    }).FirstOrDefault();

                    if (dlbCheck == null) continue; // a quick check for dlb, id not then next

                    if (dlbHead == null) // assign the dlb data, first time assign only
                    {
                        dlbHead = dlbCheck;
                        dlbHead.Docs = new List<FTAPP_DLB1>();
                    }

                    // read the dlb docs
                    var sp_dlb1s = @$"select distinct
                                            t0.DocNum
                                           ,t0.StoreCode
                                           ,t0.StoreName
                                           ,t0.DocEntry
                                           ,t0.DocStatus
                                           ,t0.StatusDesc
                                           ,t0.HeadGuid
                                           ,t0.DocType
                                           ,t0.BoxStatusDesc
                                           ,t0.OutDt
                                           ,t0.TransInDt
                                           ,t0.DocDate
                                           ,t0.DocTotal
                                           ,t0.CartonNo
                                           ,t0.RefNo
                                           ,t0.ConsigmentNo
                                           ,t0.SignedFiles
                                           ,t0.Subsi
                                           ,t0.IsReScan
                                           ,t0.LastDlbEntry
                                           ,t0.SignedFileUploadDt
                                           ,t0.ToWhsCode
                                           ,t0.ToWhsName
                                           ,t0.IBTEntry
                                        , t1.Currency [Currency] 
                                        , '{webDb.COMPANYNAME}' [SubSi]
                                        , case when ISNULL(t0.CartonNo, 0) > 0 then 1 else 0 end [IsBoxLabelVis]
                                        , t0.CartonNo [BoxesCount]
                                        , t1.U_DROPPOINT [U_DROPPOINT]   
                                        , t0.App_Determined_IsInterbranch
                                             
                                        , t0.GeoCode
                                        , t0.GeoType
                                  from       {webDb.WEBDB}..FTAPP_DLB1 t0 with (NOLOCK)
                                  inner join {webDb.SAPDB}..OCRD       t1 with (NOLOCK) on t1.CardCode = t0.StoreCode
                                  where convert(NVARCHAR(50), HeadGuid) = @HeadGuid ";

                    var dlb1s = conn
                        .Query<FTAPP_DLB1>(sp_dlb1s, new { HeadGuid = $"{dto.SaveHeadGuid}" }).Distinct().ToList();

                    if (dlb1s.Count == 0) continue;

                    // 20250828 
                    // query the invoice warehouse 

                    for (int u = 0; u < dlb1s.Count; u++)
                    {
                        if (dlb1s[u].DocType != "I")
                        {
                            continue;
                        }

                        var sp_GetwarehouseCode = @$"select WhsCode from {webDb.WEBDB}..SO Where Invno = @invDocnum";
                        dlb1s[u].Warehouse = conn.ExecuteScalar<string>(sp_GetwarehouseCode, new
                        {
                            invDocnum = $"{dlb1s[u].DocNum}"
                        });
                    }


                    // group them for distinct 
                    // 2024 0506
                    dlb1s = dlb1s
                        .GroupBy(c => new { c.DocNum, c.DocType })
                        .Select(f => new FTAPP_DLB1
                        {
                            DocNum = f.First().DocNum,
                            StoreCode = f.First().StoreCode,
                            StoreName = f.First().StoreName,
                            DocEntry = f.First().DocEntry,
                            DocStatus = f.First().DocStatus,
                            StatusDesc = f.First().StatusDesc,
                            HeadGuid = f.First().HeadGuid,
                            DocType = f.First().DocType,
                            BoxStatusDesc = f.First().BoxStatusDesc,
                            OutDt = f.First().OutDt,
                            TransInDt = f.First().TransInDt,
                            DocDate = f.First().DocDate,
                            DocTotal = f.First().DocTotal,
                            CartonNo = f.First().CartonNo,
                            RefNo = f.First().RefNo,
                            ConsigmentNo = f.First().ConsigmentNo,
                            SignedFiles = f.First().SignedFiles,
                            SubSi = f.First().SubSi,
                            IsReScan = f.First().IsReScan,
                            LastDlbEntry = f.First().LastDlbEntry,
                            ToWhsCode = f.First().ToWhsCode,
                            ToWhsName = f.First().ToWhsName,
                            IBTEntry = f.First().IBTEntry,
                            Currency = f.First().Currency,
                            IsBoxLabelVis = f.First().IsBoxLabelVis,
                            BoxesCount = f.First().BoxesCount,
                            U_DROPPOINT = f.First().U_DROPPOINT,
                            Warehouse = f.First().Warehouse,
                            App_Determined_IsInterbranch = f.First().App_Determined_IsInterbranch, 
                            GeoCode = f.First().GeoCode,
                            GeoType = f.First().GeoType,
                        })
                        .ToList();

                    // 20240123
                    // modi the box count from 
                    dlbHead.Docs.AddRange(dlb1s);
                }

                return Ok(dlbHead);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetDriverDocs_Complete(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid dlb guid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // get the dlb 
                // get the dlb1 

                var sp_queryDlb = @$"select * , t1.BOP [SiteId] , t1.DMODIFIED [PostedDt]
                                    from {db.WEBDB}..FTAPP_DLB t0 with(nolock)
                                    left join {db.WEBDB}..DLB t1 with (nolock) on t1.DocEntry = t0.DLBEntry 
                                     where HeadGuid = @HeadGuid 
                                     and DLBStatus = @DLBStatus
                                     and DLBEntry is not null";

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb = conn.Query<FTAPP_DLB>(sp_queryDlb, new
                {
                    HeadGuid = dto.SaveHeadGuid,
                    DLBStatus = "O"
                }).FirstOrDefault();

                if (dlb == null) return NotFound();

                var sp_dlb1s = $@"select *, t1.Currency [Currency] , t2.NumAtCard [CustRef], t4.Territory [Territory]
                                    from {db.WEBDB}..FTAPP_DLB1 t0 with (nolock)
                                    left join {db.SAPDB}..OCRD t1 with (nolock) on t1.CardCode = t0.StoreCode
                                    left join {db.SAPDB}..OINV t2 with (nolock) on t2.DocNum = t0.DocNum
                                    left join {db.WEBDB}..FTAPP_DLB t3 with (nolock) on t3.HeadGuid = t0.HeadGuid
                                    left join {db.WEBDB}..DLB1 t4 with (nolock) on t4.DOCENTRY = t3.DLBEntry 
					                                    and t4.DOCNUM = t0.DocNum
					                                    and t4.DOCTYPE = t0.DocType
                                    where t0.HeadGuid = @HeadGuid ; ";

                var dlbs = conn.Query<FTAPP_DLB1>(sp_dlb1s, new { HeadGuid = dto.SaveHeadGuid }).ToList();
                for (int d = 0; d < dlbs.Count; d++)
                {
                    var dlb1 = dlbs[d];
                    if (dlb1 == null) continue;

                    // for invoice
                    if (dlb1.DocType == "I")
                    {
                        var soDocEntry_sp = @$"select docentry from {db.WEBDB}..SO Where INVNO = @invNo";
                        var soEntry = conn.Query<int>(soDocEntry_sp, new { invNo = dlb1.DocNum }).FirstOrDefault();
                        if (soEntry >= 0)
                        {
                            var csCount_sp = @$"select ISNULL( sum(qty), 0) [FullCnt]
                                            from {db.WEBDB}..FTAPP_box1 
                                            where BaseEntry = @soEntry
                                            and Packaging = 'CS'";

                            var ccCount = conn.Query<int>(csCount_sp, new { soEntry }).FirstOrDefault();

                            var psCount_sp = @$"select ISNULL( count( distinct BoxGuid), 0) [LoseCnt]
                                            from {db.WEBDB}..FTAPP_box1 
                                            where BaseEntry = @soEntry
                                            and Packaging = 'PC'";

                            var psCount = conn.Query<int>(psCount_sp, new { soEntry }).FirstOrDefault();

                            dlbs[d].CartonNo = ccCount + psCount;
                        }
                        continue;
                    }

                    // 20241111
                    // for ibt query
                    if (dlb1.DocType == "T")
                    {
                        var ibtDocEntry_sp = @$"select docentry from {db.WEBDB}..IBT Where TRANSITNO = @transNo ; ";
                        var ibtEntry = conn.Query<int>(ibtDocEntry_sp, new { transNo = dlb1.DocNum }).FirstOrDefault();
                        if (ibtEntry >= 0)
                        {
                            var csCount_sp = @$"select ISNULL(sum(qty), 0) [FullCnt]
                                                from {db.WEBDB}..FTAPP_IBTBox1 
                                                where BaseEntry = @ibtEntry
                                                and Packaging = 'CS' ; ";

                            var ccCount = conn.Query<int>(csCount_sp, new { ibtEntry }).FirstOrDefault();

                            var psCount_sp = @$"select ISNULL( count( distinct BoxGuid), 0) [LoseCnt]
                                                    from {db.WEBDB}..FTAPP_IBTBox1 
                                                    where BaseEntry = @ibtEntry
                                                    and Packaging = 'PC' ; ";

                            var psCount = conn.Query<int>(psCount_sp, new { ibtEntry }).FirstOrDefault();

                            dlbs[d].CartonNo = ccCount + psCount;
                            continue;
                        }
                    }
                }

                dlb.Docs = new List<FTAPP_DLB1>(dlbs);

                return Ok(dlb);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadAppConfig()
        {
            try
            {
                using var conn = new SqlConnection(_commDbConnStr);
                var qr = "Select * from FTApp_Config with (nolock)";

                var res = conn.Query<FTApp_Config>(qr).ToList();
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

        IActionResult LoadDlbLines(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid dlb guid");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                var qr_lines = @$"select * from {db.WEBDB}..FTAPP_DLB1 with (nolock)
                                 where HeadGuid = @SaveHeadGuid";

                var lines = conn.Query<FTAPP_DLB1>(qr_lines, new { dto.SaveHeadGuid }).ToList();
                if (lines.Count == 0) return NotFound();

                // for loop each bld line to get repective doc type 
                for (int d = 0; d < lines.Count; d++)
                {
                    if (lines[d] == null) continue;
                    dto.DocNum = $"{lines[d].DocNum}"; // reused the dto properties

                    // massage each lines for respective doc type 
                    switch (lines[d].DocType)
                    {
                        case "I":
                            {
                                // for driver app sreen display
                                // return ok when all status 
                                // get the invoice from sap                     
                                var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                                OINV inv = conn.Query<OINV>(query_inv, new { docnum = dto.DocNum }).FirstOrDefault();
                                if (inv == null)
                                {
                                    return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for sap invoice.");
                                }

                                // get the box list from web portal
                                //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                                //                Where baseentry = @baseentry";

                                // get the box list from web portal
                                var query_box = $@"select DISTINCT 
                                                        t0.BoxId
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
                                    return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for boxes.");
                                }

                                inv.Subsi = db.COMPANYNAME;
                                inv.SubsiId = db.COMPANYID;
                                lines[d].Invoice = inv;

                                lines[d].DocDate = inv.DocDate;
                                lines[d].DocTotal = inv.DocTotal;
                                lines[d].CartonNo = inv.Boxes.Count;
                                lines[d].BoxesCount = inv.Boxes.Count;
                                lines[d].IsBoxLabelVis = true;
                                break;
                            }
                        case "C":
                            {
                                var query_cog = @$"Select *
                                        , (Select sum(LineTotal) from {db.WEBDB}..COG1 where DocEntry = @docNum) [DocTotal] 
                                        from {db.WEBDB}..COG with (nolock)
                                        Where DocEntry = @docNum";

                                var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                                if (cog == null) return NotFound();

                                var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                                cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = dto.DocNum }).ToList();
                                cog.SubSi = db.COMPANYNAME;

                                lines[d].Cog = cog;
                                lines[d].DocDate = (DateTime)cog.DOCDATE;
                                lines[d].DocTotal = cog.DocTotal;
                                lines[d].CartonNo = 0;
                                lines[d].BoxesCount = 0;
                                lines[d].IsBoxLabelVis = false;
                                break;
                            }
                        case "T":
                            {
                                var query_transfer = @$"Select * from {db.SAPDB}..OWTR with (nolock)
                                   Where DocNum = @docNum";

                                var transfer = conn.Query<OWTR_Ext>(query_transfer, new { docNum = dto.DocNum }).FirstOrDefault();
                                if (transfer == null) return NotFound();

                                var query_cogLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                   Where DocEntry = @DocEntry";

                                transfer.Lines = conn.Query<WTR1_Ext>(query_cogLine, new { transfer.DocEntry }).ToList();
                                transfer.SubSi = db.COMPANYNAME;

                                lines[d].Transfer = transfer;
                                lines[d].DocDate = transfer.DocDate;
                                lines[d].DocTotal = transfer.DocTotal;
                                lines[d].CartonNo = 0;
                                lines[d].BoxesCount = 0;
                                lines[d].IsBoxLabelVis = false;
                                break;
                            }
                    }
                }

                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // loop thru the company and get the doc this driver scan in
        IActionResult CheckDriverDlb(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DriverName))
                {
                    return BadRequest("Invalid driver name");
                }
                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }

                var databs = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (databs == null)
                {
                    return BadRequest("invalid dbi");
                }

                var checkDlbGuid_sql = @$"Select * from {databs.WEBDB}..FTAPP_DLB 
                                      Where TruckNo = @PlateNo
                                      AND DriverName = @DriverName
                                      AND DLBEntry is null ";

                using var conn = new SqlConnection(_commDbConnStr);
                var found = conn.Query<FTAPP_DLB>(checkDlbGuid_sql, new
                {
                    dto.PlateNo,
                    dto.DriverName
                }).FirstOrDefault(); // found dlb

                if (found == null) return NotFound();

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);

                for (int i = 0; i < dbs.Count; i++) // loop the delovery db 
                {
                    var db = dbs[i];
                    if (db == null) continue;

                    // get all the doc 
                    var query_line = @$"Select *
                                    from {db.WEBDB}..FTAPP_DLB1 with (nolock) Where HeadGuid = @HeadGuid";

                    var foundDocs = conn.Query<FTAPP_DLB1>(query_line, new { HeadGuid = found.HeadGuid }).ToList();

                    if (foundDocs.Count == 0) continue;

                    // load in each docs information
                    for (int d = 0; d < foundDocs.Count; d++)
                    {
                        var foundDoc = foundDocs[d];
                        if (foundDoc == null) continue;

                        switch (foundDoc.DocType)
                        {
                            case "I":
                                {
                                    // for driver app sreen display
                                    // return ok when all status 
                                    // get the invoice from sap                     
                                    var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                                    OINV inv = conn.Query<OINV>(query_inv, new { docnum = foundDoc.DocNum }).FirstOrDefault();
                                    if (inv == null)
                                    {
                                        return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for sap invoice.");
                                    }

                                    // get the box list from web portal
                                    //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                                    //            Where baseentry = @baseentry";

                                    // get the box list from web portal
                                    var query_box = $@"select DISTINCT
                                                        t0.BoxId
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
                                        return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for boxes.");
                                    }

                                    inv.Subsi = db.COMPANYNAME;
                                    inv.SubsiId = db.COMPANYID;

                                    foundDoc.Invoice = inv;
                                    foundDoc.DocDate = inv.DocDate;
                                    foundDoc.DocTotal = inv.DocTotal;
                                    foundDoc.CartonNo = inv.Boxes.Count;
                                    foundDoc.BoxesCount = inv.Boxes.Count;
                                    foundDoc.IsBoxLabelVis = true;

                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                            case "C":
                                {
                                    var query_cog = @$"Select *
                                        , (Select sum(LineTotal) from {db.WEBDB}..COG1 where DocEntry = @docNum) [DocTotal] 
                                        from {db.WEBDB}..COG with (nolock)
                                        Where DocEntry = @docNum";

                                    var cog = conn.Query<COG_Doc>(query_cog, new { docNum = foundDoc.DocNum }).FirstOrDefault();
                                    if (cog == null) return NotFound();

                                    var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                                    cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = cog.DOCENTRY }).ToList();
                                    cog.SubSi = db.COMPANYNAME;

                                    foundDoc.Cog = cog;
                                    foundDoc.DocDate = (DateTime)cog.DOCDATE;
                                    foundDoc.DocTotal = cog.DocTotal;
                                    foundDoc.CartonNo = 0;

                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                            case "T":
                                {
                                    var query_transfer = @$"Select * from {db.SAPDB}..OWTR with (nolock)
                                   Where DocNum = @docNum";

                                    var transfer = conn.Query<OWTR_Ext>(query_transfer, new { docNum = foundDoc.DocNum }).FirstOrDefault();
                                    if (transfer == null) return NotFound();

                                    var query_cogLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                                        Where DocEntry = @DocEntry";

                                    transfer.Lines = conn.Query<WTR1_Ext>(query_cogLine, new { transfer.DocEntry }).ToList();
                                    transfer.SubSi = db.COMPANYNAME;

                                    foundDoc.Transfer = transfer;
                                    foundDoc.DocDate = transfer.DocDate;
                                    foundDoc.DocTotal = transfer.DocTotal;
                                    foundDoc.CartonNo = 0;

                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                        }
                    } // loop found doc
                } // loop db

                return Ok(found);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // loop thru the company and get the doc this driver scan in
        IActionResult CheckDriverDlb_ByGuid(Dto_Delivery dto)
        {
            try
            {
                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid save guid");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                var databs = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (databs == null)
                {
                    return BadRequest("invalid dbi");
                }

                var checkDlbGuid_sql = @$"Select * from {databs.WEBDB}..FTAPP_DLB with (nolock)
                                           Where HeadGuid = @SaveHeadGuid ";

                using var conn = new SqlConnection(_commDbConnStr);
                var found = conn.Query<FTAPP_DLB>(checkDlbGuid_sql, new
                {
                    SaveHeadGuid = dto.SaveHeadGuid
                }).FirstOrDefault(); // found dlb

                if (found == null) return NotFound();

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);

                for (int i = 0; i < dbs.Count; i++) // loop the delivery db 
                {
                    var db = dbs[i];
                    if (db == null) continue;

                    // get all the doc 
                    var query_line = @$"Select *
                                    from {db.WEBDB}..FTAPP_DLB1 with (nolock) 
                                    Where HeadGuid = @HeadGuid 
                                    order by id desc";

                    var foundDocs = conn.Query<FTAPP_DLB1>(query_line, new { HeadGuid = found.HeadGuid }).ToList();

                    if (foundDocs.Count == 0) continue;

                    // load in each docs information
                    for (int d = 0; d < foundDocs.Count; d++)
                    {
                        var foundDoc = foundDocs[d];
                        if (foundDoc == null) continue;

                        switch (foundDoc.DocType)
                        {
                            case "I":
                                {
                                    // for driver app sreen display
                                    // return ok when all status 
                                    // get the invoice from sap                     
                                    var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                                    OINV inv = conn.Query<OINV>(query_inv, new { docnum = foundDoc.DocNum }).FirstOrDefault();
                                    if (inv == null)
                                    {
                                        return BadRequest($"Invoice #{foundDoc.DocNum} from {db.COMPANYNAME}, Error query for sap invoice.");
                                    }

                                    // get the box list from web portal
                                    //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                                    //            Where baseentry = @baseentry";

                                    // get the box list from web portal
                                    var query_box = $@"select DISTINCT 
                                                        t0.BoxId
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
                                        return BadRequest($"Invoice #{foundDoc.DocNum} from {db.COMPANYNAME}, Error query for boxes.");
                                    }

                                    inv.Subsi = db.COMPANYNAME;
                                    inv.SubsiId = db.COMPANYID;

                                    foundDoc.Invoice = inv;
                                    foundDoc.DocDate = inv.DocDate;
                                    foundDoc.DocTotal = inv.DocTotal;
                                    foundDoc.CartonNo = inv.Boxes.Count;
                                    foundDoc.BoxesCount = inv.Boxes.Count;
                                    foundDoc.IsBoxLabelVis = true;

                                    // 20221014
                                    // query the added box 
                                    var query_addedBox = @$"Select count(1) 
                                                            from {db.WEBDB}..FTAPP_DLB2 with (nolock)
                                                            where headGuid = @headGuid
                                                            and InvDocNum = @invDocNum";

                                    var scanAddedBoxesCnt = conn.ExecuteScalar<int>(query_addedBox,
                                        new
                                        {
                                            headGuid = found.HeadGuid,
                                            invDocNum = foundDoc.DocNum
                                        });


                                    foundDoc.AddBoxInfo = new ScanAddBoxesInfo
                                    {
                                        ScanAddStatus = inv.Boxes.Count == scanAddedBoxesCnt ? "complete" : "partial",
                                        TotalInvBoxes = inv.Boxes.Count,
                                        ScanAddedBoxes = scanAddedBoxesCnt
                                    };

                                    foundDoc.IsCompleted_AddBoxes = (inv.Boxes.Count == scanAddedBoxesCnt);

                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                            case "C":
                                {
                                    var query_cog = @$"Select *
                                        , (Select sum(LineTotal) from {db.WEBDB}..COG1 where DocEntry = @docNum) [DocTotal] 
                                        from {db.WEBDB}..COG with (nolock)
                                        Where DocEntry = @docNum";

                                    var cog = conn.Query<COG_Doc>(query_cog, new { docNum = foundDoc.DocNum }).FirstOrDefault();
                                    if (cog == null) return NotFound();

                                    var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                                        Where DocEntry = @docEntry";

                                    cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = cog.DOCENTRY }).ToList();
                                    cog.SubSi = db.COMPANYNAME;

                                    foundDoc.Cog = cog;
                                    foundDoc.DocDate = (DateTime)cog.DOCDATE;
                                    foundDoc.DocTotal = cog.DocTotal;
                                    foundDoc.CartonNo = 0;

                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                            case "T":
                                {
                                    // 20230518
                                    // the transfer head
                                    var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                                    var transfer = conn.Query<OWTR_Ext>(query_transfer,
                                        new
                                        {
                                            webDb = db.WEBDB,
                                            transferDocNum = foundDoc.DocNum
                                        }).FirstOrDefault();

                                    if (transfer == null) return NotFound();

                                    // the transfer line
                                    var query_cogLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                                        Where DocEntry = @DocEntry";

                                    transfer.Lines = conn.Query<WTR1_Ext>(query_cogLine, new { transfer.DocEntry }).ToList();

                                    // transfer boxes 
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
                                        return BadRequest($"{db.COMPANYNAME}, Transfer #{dto.DocNum} from {dto.Subsi}, Error query for boxes.");
                                    }


                                    // 20221014
                                    // query the added box                                     
                                    var query_addedBox = @$"Select count(1) 
                                                            from {db.WEBDB}..FTAPP_DLB2 with (nolock)
                                                            where headGuid = @headGuid
                                                            and InvDocNum = @invDocNum";

                                    var scanAddedBoxesCnt = conn.ExecuteScalar<int>(query_addedBox,
                                        new
                                        {
                                            headGuid = found.HeadGuid,
                                            invDocNum = foundDoc.DocNum
                                        });

                                    foundDoc.AddBoxInfo = new ScanAddBoxesInfo
                                    {
                                        ScanAddStatus = transfer.Boxes.Count == scanAddedBoxesCnt ? "complete" : "partial",
                                        TotalInvBoxes = transfer.Boxes.Count,
                                        ScanAddedBoxes = scanAddedBoxesCnt
                                    };

                                    foundDoc.IsCompleted_AddBoxes = (transfer.Boxes.Count == scanAddedBoxesCnt);

                                    foundDoc.DocDate = transfer.DocDate;
                                    foundDoc.DocTotal = transfer.DocTotal;
                                    foundDoc.CartonNo = transfer.Boxes.Count;
                                    foundDoc.Transfer = transfer;


                                    if (found.Docs == null) found.Docs = new List<FTAPP_DLB1>();
                                    found.Docs.Add(foundDoc);
                                    break;
                                }
                        }
                    } // loop found doc
                } // loop db

                return Ok(found);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDLB_ByDriver(Dto_Delivery dto)
        {
            try
            {
                // check the user in dlb creation 
                if (string.IsNullOrWhiteSpace(dto.UserToken))
                {
                    goto ByPassTransCheck;
                    //return BadRequest("bad user login, please log out app, " +
                    //    "and login again to refresh login token. Thanks");
                }

                // check the memory for the key exist
                if (Program.UserTransToken_CreateDLB == null) Program.UserTransToken_CreateDLB = new Dictionary<string, bool>();

                // check user token in list
                var isListed = Program.UserTransToken_CreateDLB.ContainsKey(dto.UserToken);

                if (isListed) // yes in 
                {
                    bool inTran = Program.UserTransToken_CreateDLB[dto.UserToken];
                    if (inTran)
                    {
                        return BadRequest("Create DLB in process, please recheck the created DLB Thanks.");
                    }
                    else
                    {
                        Program.UserTransToken_CreateDLB[dto.UserToken] = true;
                    }
                }
                else // no then add in and set true 
                {
                    Program.UserTransToken_CreateDLB.Add(dto.UserToken, true); // add and set to intrans
                }

            ByPassTransCheck:

                if (dto.Dlb1 == null)
                {
                    return BadRequest("Invalid dlb lines");
                }
                if (dto.Dlb1.Count == 0)
                {
                    return BadRequest("Invalid dlb lines [0]");
                }
                if (dto.SaveHeadGuid == default)
                {
                    return BadRequest("Invalid DLB guid");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid user name");
                }

                var dlbRepliedDocs = new List<object>();
                var companies = dto.Dlb1.Select(s => s.SubSi).Distinct().ToList(); // list of company

                using var conn = new SqlConnection(_commDbConnStr);

                for (int c = 0; c < companies.Count; c++)
                {
                    var subsi = companies[c];
                    if (string.IsNullOrWhiteSpace(subsi)) continue;

                    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, subsi);
                    if (db == null) continue;

                    var qr_dlb = @$"Select * from {db.WEBDB}..FTAPP_DLB with (NOLOCK)
                                  Where HeadGuid = @headerGuid ";

                    var dlb = conn.Query<FTAPP_DLB>(qr_dlb, new
                    {
                        headerGuid = dto.SaveHeadGuid,

                    }).FirstOrDefault();
                    
                    if (dlb == null)
                    {
                        continue;
                    }

                    if (dlb.DLBStatus != "D")
                    {
                        continue;                        
                    }

                    var companyDlbs = dto.Dlb1.Where(d => d.SubSi == subsi).ToList();
                    if (companyDlbs.Count == 0) // there is no line belong to this company
                    {
                        continue; // to nex company
                    }

                    dlb.Remarks = dto.Remarks;
                    dlb.NRIC = dto.Nric;

                    if (string.IsNullOrWhiteSpace(dlb.WhsUserCode))
                    {
                        dlb.WhsUserCode = dto.UserCode;
                    }
                    
                    var helper = new DLBHelper(db, dto.SaveHeadGuid, dlb.TruckNo);
                                                                                                         
                    var dlbDocEntry = helper.CreateDLB(dlb, companyDlbs, dto.UserCode, dto.UserName, dto.IsInterbranch);
                    if (dlbDocEntry == -1)
                    {
                        return BadRequest(helper.Error);
                    }

                    var dlbRepliedDoc = new
                    {
                        DLBEntry = dlbDocEntry,
                        DocStatus = "Success, Intransit",
                        SubSi = db.COMPANYNAME
                    };

                    dlbRepliedDocs.Add(dlbRepliedDoc);
                } // for loop 

                return Ok(dlbRepliedDocs); // list of created dlb docs

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                // reset the user to un transaction
                if (!string.IsNullOrWhiteSpace(dto.UserToken) && Program.UserTransToken_CreateDLB.Count > 0)
                {
                    Program.UserTransToken_CreateDLB.Remove(dto.UserToken);
                }
            }
        }

        //IActionResult SaveDLB_ByDriver_Split(Dto_Delivery dto)
        //{
        //    try
        //    {
        //        // check the user in dlb creation 
        //        if (string.IsNullOrWhiteSpace(dto.UserToken))
        //        {
        //            goto ByPassTransCheck;                    
        //        }

        //        // check the memory for the key exist
        //        if (Program.UserTransToken_CreateDLB == null) Program.UserTransToken_CreateDLB = new Dictionary<string, bool>();

        //        // check user token in list
        //        var isListed = Program.UserTransToken_CreateDLB.ContainsKey(dto.UserToken);

        //        if (isListed) // yes in 
        //        {
        //            bool inTran = Program.UserTransToken_CreateDLB[dto.UserToken];
        //            if (inTran)
        //            {
        //                return BadRequest("Create DLB in process, please recheck the created DLB Thanks.");
        //            }
        //            else
        //            {
        //                Program.UserTransToken_CreateDLB[dto.UserToken] = true;
        //            }
        //        }
        //        else // no then add in and set true 
        //        {
        //            Program.UserTransToken_CreateDLB.Add(dto.UserToken, true); // add and set to intrans
        //        }

        //    ByPassTransCheck:

        //        if (dto.Dlb1 == null)
        //        {
        //            return BadRequest("Invalid dlb lines");
        //        }
        //        if (dto.Dlb1.Count == 0)
        //        {
        //            return BadRequest("Invalid dlb lines [0]");
        //        }
        //        if (dto.SaveHeadGuid == default)
        //        {
        //            return BadRequest("Invalid DLB guid");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.UserCode))
        //        {
        //            return BadRequest("Invalid user code");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.UserName))
        //        {
        //            return BadRequest("Invalid user name");
        //        }

        //        var dlbRepliedDocs = new List<object>();
        //        var companies = dto.Dlb1.Select(s => s.SubSi).Distinct().ToList(); // list of company

        //        using var conn = new SqlConnection(_commDbConnStr);

        //        for (int c = 0; c < companies.Count; c++)
        //        {
        //            var subsi = companies[c];
        //            if (string.IsNullOrWhiteSpace(subsi)) continue;

        //            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, subsi);
        //            if (db == null) continue;

        //            var qr_dlb = @$"Select * from {db.WEBDB}..FTAPP_DLB with (NOLOCK)
        //                          Where HeadGuid = @headerGuid ";

        //            var dlb = conn.Query<FTAPP_DLB>(qr_dlb, new
        //            {
        //                headerGuid = dto.SaveHeadGuid,

        //            }).FirstOrDefault();

        //            if (dlb == null)
        //            {
        //                continue;
        //            }

        //            if (dlb.DLBStatus != "D")
        //            {
        //                continue;                        
        //            }

        //            var companyDlbs = dto.Dlb1.Where(d => d.SubSi == subsi).ToList();
        //            if (companyDlbs.Count == 0) // there is no line belong to this company
        //            {
        //                continue; // to nex company
        //            }

        //            dlb.Remarks = dto.Remarks;
        //            dlb.NRIC = dto.Nric;

        //            if (string.IsNullOrWhiteSpace(dlb.WhsUserCode))
        //            {
        //                dlb.WhsUserCode = dto.UserCode;
        //            }

        //            // 20251214
        //            // split the interbranch and gps to 1 dlb 
        //            // handler the delivery to store as another DLB 

        //            var interBranches_docs = companyDlbs
        //                .Where(i => i.App_Determined_IsInterbranch == true).ToList();

        //            var dlbs_created = new List<long>();
        //            long interBranches_dlbEntry = -1;
        //            if (interBranches_docs.Count > 0)
        //            {
        //                // handler the create of the DLB for interbranch 
        //                interBranches_dlbEntry = HandlerInterBranch_DLBCreation(db, dto, dlb,  interBranches_docs);
        //                dlbs_created.Add(interBranches_dlbEntry);
        //            }

        //            // process as per normal
        //            var normal_docs = companyDlbs
        //                .Where(i => i.App_Determined_IsInterbranch == false).ToList();

        //            var helper = new DLBHelper(db, dto.SaveHeadGuid, dlb.TruckNo);                    
        //            var dlbDocEntry = helper.CreateDLB(dlb, normal_docs, dto.UserCode, dto.UserName, false);
        //            if (dlbDocEntry == -1)
        //            {
        //                return BadRequest(helper.Error);
        //            }

        //            dlbs_created.Add(dlbDocEntry);
                    
        //            var dlbRepliedDoc = new
        //            {
        //                DLBEntry = string.Join(",", dlbs_created.Distinct()) , // in comma separated 
        //                DocStatus = "Success, Intransit",
        //                SubSi = db.COMPANYNAME
        //            };

        //            dlbRepliedDocs.Add(dlbRepliedDoc);
        //        } // for loop 

        //        return Ok(dlbRepliedDocs); // list of created dlb docs

        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //    finally
        //    {
        //        // reset the user to un transaction
        //        if (!string.IsNullOrWhiteSpace(dto.UserToken) && Program.UserTransToken_CreateDLB.Count > 0)
        //        {
        //            Program.UserTransToken_CreateDLB.Remove(dto.UserToken);
        //        }
        //    }
        //}

        //// 20251214
        //long HandlerInterBranch_DLBCreation (DbInfo db, Dto_Delivery dto, FTAPP_DLB dlb , List<FTAPP_DLB1> normalDocs)
        //{
        //    try
        //    {
        //        var helper = new DLBHelper(db, dto.SaveHeadGuid, dlb.TruckNo);
        //        var dlbDocEntry = helper.CreateDLB(dlb, normalDocs, dto.UserCode, dto.UserName, true);
        //        if (dlbDocEntry == -1)
        //        {
        //            return -1;
        //        }

        //        return dlbDocEntry;
        //    }
        //    catch (Exception except)
        //    {
        //        LastError = $"{except.Message}\n{except.StackTrace}";
        //        _logger.LogError(LastError);
        //        return -1;
        //    }
        //}

        string GetDocType(string docType)
        {
            switch (docType)
            {
                case "I": return "Invoice";
                case "C": return "COG";
                case "T": return "Transfer";
                default: return "";
            }
        }

        IActionResult VerifyAndUpdateDriverDoc(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.DocNum))
            {
                return BadRequest("Invalid Doc number");
            }
            if (string.IsNullOrWhiteSpace(dto.DocType))
            {
                return BadRequest("Invalid Doc Type");
            }
            if (dto.HeadGuid == default)
            {
                return BadRequest("invalid guid head");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            var query_box = @$" Select * 
                                    from  {db.WEBDB}..FTAPP_DLB1
                                    Where DocNum = @DocNum
                                    and DocType = @DocType
                                    and HeadGuid = @HeadGuid";

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var foundDoc = conn.Query<FTAPP_DLB1>(query_box, new
                {
                    dto.DocNum,
                    dto.HeadGuid,
                    dto.DocType
                }, trans).FirstOrDefault();

                if (foundDoc == null)
                {
                    trans.Rollback();
                    return BadRequest($"The query doc number no found, pls try again.");
                }

                //foundDoc.TransInDt = DateTime.Now;

                // else perform update of the intransist date time 
                var update_sql = @$"Update {db.WEBDB}..FTAPP_DLB1
                        set TransInDt = GETDATE()
                        Where id = @id";

                var updateRes = conn.Execute(update_sql, new { id = foundDoc.id }, trans);
                if (updateRes == 1)
                {
                    trans.Commit();
                    return Ok();
                }

                trans.Rollback();
                return BadRequest($"Update doc: {foundDoc.DocNum}, {foundDoc.DocType} fail, please try again.");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult VerifyAndUpdateDriverBox(Dto_Delivery dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid invoice number");
            }
            if (string.IsNullOrWhiteSpace(dto.BoxId))
            {
                return BadRequest("Invalid box id");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            var query_box = @$" Select * from  {db.WEBDB}..FTAPP_DLB2
                                    Where InvDocNum = @InvDocNum
                                          and BoxId = @BoxId";

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var foundBx = conn.Query<FTAPP_DLB2>(query_box, new
                {
                    InvDocNum = dto.InvNum,
                    BoxId = dto.BoxId
                }, trans).FirstOrDefault();

                if (foundBx == null)
                {
                    return BadRequest($"Box {dto.BoxId}, no found for transfer, please try again.");
                }

                if (foundBx.InTransDt != default)
                {
                    return BadRequest($"Box {dto.BoxId} in transited {foundBx.InTransDt:dd-MMM-yy hh:mm tt}, " +
                                        $"please try again.");
                }
                // else perform update of the intransist date time 
                var update_sql = @$"Update {db.WEBDB}..FTAPP_DLB2                                    
                        set InTransDt = GETDATE()
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
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DriverCheckDoc(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid invoice number");
                }
                if (string.IsNullOrWhiteSpace(dto.DriverName))
                {
                    return BadRequest("Invalid driver name");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // check the driver plate number and invoice is ready for out 
                var sql_check = @$"select t1.* 
                                    from {db.WEBDB}..FTAPP_DLB t0 
                                    inner join {db.WEBDB}..FTAPP_DLB1 t1 on t0.HeadGuid = t1.HeadGuid
                                    Where t0.TruckNo = @TruckNo
                                    and t1.DocNum = @DocNum
                                    and t0.DriverName = @DriverName";

                using var conn = new SqlConnection(_commDbConnStr);
                var foundDoc = conn.Query<FTAPP_DLB1>(sql_check, new
                {
                    TruckNo = dto.PlateNo,
                    DocNum = dto.DocNum,
                    DriverName = dto.DriverName
                }).FirstOrDefault();

                if (foundDoc == null) return NotFound();

                switch (foundDoc.DocType)
                {
                    case "I":
                        {
                            // for driver app sreen display
                            // return ok when all status 
                            // get the invoice from sap                     
                            var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                            OINV inv = conn.Query<OINV>(query_inv, new { docnum = dto.DocNum }).FirstOrDefault();
                            if (inv == null)
                            {
                                return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for sap invoice.");
                            }

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
                                return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for boxes.");
                            }

                            inv.Subsi = db.COMPANYNAME;
                            inv.SubsiId = db.COMPANYID;
                            foundDoc.Invoice = inv;

                            foundDoc.DocDate = inv.DocDate;
                            foundDoc.DocTotal = inv.DocTotal;
                            foundDoc.CartonNo = inv.Boxes.Count;
                            break;
                        }
                    case "C":
                        {
                            var query_cog = @$"Select *
                                        , (Select sum(LineTotal) from {db.WEBDB}..COG1 where DocEntry = @docNum) [DocTotal] 
                                        from {db.WEBDB}..COG with (nolock)
                                        Where DocEntry = @docNum";

                            var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                            if (cog == null) return NotFound();

                            var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                            cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = dto.DocNum }).ToList();
                            cog.SubSi = db.COMPANYNAME;

                            foundDoc.Cog = cog;
                            foundDoc.DocDate = (DateTime)cog.DOCDATE;
                            foundDoc.DocTotal = cog.DocTotal;
                            foundDoc.CartonNo = 0;
                            break;
                        }
                    case "T":
                        {
                            var query_transfer = @$"Select * from {db.SAPDB}..OWTR with (nolock)
                                   Where DocNum = @docNum";

                            var transfer = conn.Query<OWTR_Ext>(query_transfer, new { docNum = dto.DocNum }).FirstOrDefault();
                            if (transfer == null) return NotFound();

                            var query_cogLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                   Where DocEntry = @DocEntry";

                            transfer.Lines = conn.Query<WTR1_Ext>(query_cogLine, new { transfer.DocEntry }).ToList();
                            transfer.SubSi = db.COMPANYNAME;

                            foundDoc.Transfer = transfer;
                            foundDoc.DocDate = transfer.DocDate;
                            foundDoc.DocTotal = transfer.DocTotal;
                            foundDoc.CartonNo = 0;
                            break;
                        }
                }

                return Ok(foundDoc);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DriverLogin(Dto_Delivery dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.PlateNo))
                {
                    return BadRequest("Invalid plate no");
                }

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid SUBSI");
                }

                var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
                var logins = new List<FTAPP_TruckCapacity>();
                using var conn = new SqlConnection(_commDbConnStr);

                // get the app setting 
                // for by pass the droid device 
                var sp_GetSetting = @"select setupvalue 
                                    from ktcw_common..FTAPP_Config Where SetupName = 'DeliveryAppCheckDeviceID'";

                var isCheckDroidDeviceId = false;
                var isCheckDroidDeviceId_setup = conn.Query<FTApp_Config>(sp_GetSetting).FirstOrDefault();
                if (isCheckDroidDeviceId_setup != null)
                {
                    isCheckDroidDeviceId = $"{isCheckDroidDeviceId_setup?.SetupValue}".ToLower() == "y"? true : false;
                }

                sp_GetSetting =  @"select setupvalue 
                                    from ktcw_common..FTAPP_Config Where SetupName = 'DeliveryAppCheckDeviceID_Activate'";

                var isCheckDroidDeviceId_Activate = false;
                var isCheckDroidDeviceId_Activate_setup = conn.Query<FTApp_Config>(sp_GetSetting).FirstOrDefault();
                if (isCheckDroidDeviceId_Activate_setup != null)
                {
                    isCheckDroidDeviceId_Activate = $"{isCheckDroidDeviceId_Activate_setup?.SetupValue}".ToLower() == "y" ? true : false;
                }

                for (int i = 0; i < dbs.Count; i++)
                {
                    var db = dbs[i];
                    if (db == null) continue;
                    //var sp_query = @"exec sp_VerifyDriverLogin @webDb, @plate";

                    var sp_query = @"exec sp_VerifyDriverLoginV1 @webDb, @plate, @pw";
                    var login = conn.Query<FTAPP_TruckCapacity>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        plate = dto.PlateNo,
                        pw = $"{dto.Password}"
                    }).ToList();

                    if (login.Count == 0) continue;
                    logins.AddRange(login);
                }

                if (logins.Count == 0) return NotFound();
                var foundCoy1 = logins.Where(l => $"{l.SubSi}".Trim().ToLower()
                                                   .Equals($"{dto.Subsi}".Trim().ToLower()))
                                                   .FirstOrDefault();

                // 20251221
                // by pass the guid and device id check 
                if (isCheckDroidDeviceId_Activate == false)
                {
                    if (foundCoy1 != null)
                    {
                        for (int p = 0; p < logins.Count; p++)
                        {
                            logins[p].Pass = string.Empty;
                            logins[p].Pass2 = string.Empty;

                            logins[p].Driver1_Device_Id = string.Empty;
                            logins[p].Driver1_Guid = string.Empty;

                            logins[p].Driver2_Device_Id = string.Empty;
                            logins[p].Driver2_Guid = string.Empty;
                        }

                        return Ok(logins);                        
                    }
                    else
                    {
                        return BadRequest("There is not matched login");
                    }
                }

                // else process the check for device id and guid 
                // double check with selected driver login SUBSI 
                if (foundCoy1 == null)
                {
                    foundCoy1 = logins.Where(l => $"{l.SubSi}".Trim().ToLower()
                                                    .Equals($"{dto.Subsi}".Trim().ToLower()))
                                                    .FirstOrDefault();
                }

                if (foundCoy1 == null) return NotFound();
                if (foundCoy1.Skip_Guid == true)
                {
                    for (int p = 0; p < logins.Count; p++)
                    {
                        logins[p].Pass = string.Empty;
                        logins[p].Pass2 = string.Empty;

                        logins[p].Driver1_Device_Id = string.Empty;
                        logins[p].Driver1_Guid = string.Empty;

                        logins[p].Driver2_Device_Id = string.Empty;
                        logins[p].Driver2_Guid = string.Empty;
                    }
                    return Ok(logins);
                }

                // Driver 1
                if (string.Equals(foundCoy1.Pass, dto.Password, StringComparison.Ordinal))
                {                   
                    if (isCheckDroidDeviceId)
                    {
                        if ($"{foundCoy1.Driver1_Device_Id}" != $"{dto.Device_id}")
                        {
                            return BadRequest($"The driver 1 device id was not matched\nDevice: {dto.Device_id}\nServer: {foundCoy1.Driver1_Device_Id}");                            
                        }
                    }

                    // GUID check (only if one is already registered for driver1)
                    if (!string.IsNullOrWhiteSpace($"{foundCoy1.Driver1_Guid}"))
                    {
                        string device_driver1_guid = $"{dto.Driver1_guid}";
                        if (string.IsNullOrWhiteSpace(device_driver1_guid))
                        {
                            goto driver1Continue; // to save at device 
                        }
                        if (foundCoy1.Driver1_Guid != device_driver1_guid)
                        {
                            return BadRequest("The driver 1 device guid was not matched");
                        }
                    }

                    driver1Continue:
                    
                    // reset the driver 2 info
                    var index = logins.IndexOf(foundCoy1);
                    if (index == -1) return Ok(logins);

                    for (int l = 0; l < logins.Count; l++)
                    {
                        if (index == l)
                        {   
                            logins[l].Pass = "*";

                            logins[l].Pass2 = string.Empty;
                            logins[l].Driver2_Device_Id = string.Empty;
                            logins[l].Driver2_Guid = string.Empty;
                            continue;
                        }

                        logins[l].Pass2 = string.Empty;
                        logins[l].Pass = string.Empty;
                    }
                    return Ok(logins);
                }

                // Driver 2
                if (string.Equals(foundCoy1.Pass2, dto.Password, StringComparison.Ordinal))
                {                   
                    if (isCheckDroidDeviceId)
                    {
                        if ($"{foundCoy1.Driver2_Device_Id}" != $"{dto.Device_id}")
                        {
                            return BadRequest($"The driver 2 device id was not matched\nDevice: {dto.Device_id}\nServer: {foundCoy1.Driver2_Device_Id}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(foundCoy1.Driver2_Guid))
                    {
                        string device_driver2_guid = $"{dto.Driver2_guid}";
                        if (string.IsNullOrWhiteSpace(device_driver2_guid))
                        {
                            goto driver2Continue; // to save at device 
                        }
                        if (foundCoy1.Driver2_Guid != device_driver2_guid)
                        {
                            return BadRequest("The driver 2 device guid was not matched");
                        }                        
                    }

                    driver2Continue:
                    // reset the driver 1 info
                    var index = logins.IndexOf(foundCoy1);
                    if (index == -1) return Ok(logins);                  

                    for (int l = 0; l < logins.Count; l ++)
                    {
                        if (index == l)
                        {
                            logins[l].Pass2 = "*";

                            logins[l].Pass = string.Empty;
                            logins[l].Driver1_Device_Id = string.Empty;
                            logins[l].Driver1_Guid = string.Empty;
                            continue;
                        }

                        logins[l].Pass2 = string.Empty;
                        logins[l].Pass = string.Empty;
                    }

                    return Ok(logins);
                }


                return BadRequest("There is not matched login");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadCounted_IBTBoxes(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid SUBSI");
                }
                if (string.IsNullOrWhiteSpace(dto.IbtNum))
                {
                    return BadRequest("Invalid IBT number");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid head guid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // 20230415
                // add in column LabelConsistTotalBoxes
                var query = $@"select  distinct t0.BoxId
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
                               inner join {db.WEBDB}..FTAPP_DLB2 t1 with (nolock) on t1.BoxId = t0.BoxId
                               Where InvDocNum = @InvDocNum 
                               and HeadGuid = @HeadGuid";

                using var conn = new SqlConnection(_commDbConnStr);
                var boxes = conn.Query<FTAPP_Box>(query, new { InvDocNum = dto.IbtNum, HeadGuid = dto.HeadGuid }).ToList();

                if (boxes.Count == 0) return NotFound();
                return Ok(boxes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadCountedBoxes(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvNum))
                {
                    return BadRequest("Invalid invoice number");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid head guid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                // 20230415
                // add in column LabelConsistTotalBoxes
                var query = $@"select  distinct t0.BoxId
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
                               inner join {db.WEBDB}..FTAPP_DLB2 t1 with (nolock) on t1.BoxId = t0.BoxId
                               Where InvDocNum = @InvDocNum 
                               and HeadGuid = @HeadGuid";

                using var conn = new SqlConnection(_commDbConnStr);
                var boxes = conn.Query<FTAPP_Box>(query, new { InvDocNum = dto.InvNum, HeadGuid = dto.HeadGuid }).ToList();

                if (boxes.Count == 0) return NotFound();
                return Ok(boxes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearDraft(Dto_Delivery dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace($"{dto.DraftHeadGuid}"))
            {
                return BadRequest("Invalid draft guid");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // delete dlb 
                // delete the head 
                var sp_delete = @$"delete from {db.WEBDB}..FTAPP_DLB Where HeadGuid = @headerGuid";
                var res = conn.Execute(sp_delete, new { headerGuid = dto.DraftHeadGuid }, trans);

                // query  dlb1 
                var query_inv = @$"Select * 
                                From {db.WEBDB}..FTAPP_DLB1 t0 with (nolock) 
                                Where HeadGuid = @headerGuid";

                var invoices = conn.Query<FTAPP_DLB1>(query_inv, new
                {
                    headerGuid = $"{dto.DraftHeadGuid}"
                }, trans).ToList();

                // delete the invoice
                sp_delete = @$"delete from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @headerGuid";
                res = conn.Execute(sp_delete, new { headerGuid = dto.DraftHeadGuid }, trans);

                // delete the onhold and dlb boxes
                // based on dlb1 invoice invoice number, delete dlb2                
                sp_delete = @$"delete from {db.WEBDB}..FTAPP_DLB2 Where InvDocNum = @DocNum";
                conn.Execute(sp_delete, invoices, trans);

                // delete onhold
                sp_delete = @$"delete from {db.WEBDB}..FTAPP_HoldDlvryDocs Where DocNum = @DocNum";
                conn.Execute(sp_delete, invoices, trans);

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

        IActionResult CheckDraftLine(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid usercode");
            }
            if (string.IsNullOrWhiteSpace(dto.DriverName))
            {
                return BadRequest("Invalid driver name");
            }
            if (string.IsNullOrWhiteSpace(dto.PlateNo))
            {
                return BadRequest("Invalid plate no");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            try
            {
                var sp_select = @$"Select * from {db.WEBDB}..FTAPP_DLB with (nolock) 
                                where  WhsUserCode = @userCode
                                and TruckNo = @truckNo
                                and DriverName = @driverName 
                                and DLBStatus = @DLBStatus";

                var foundDraft = conn.Query<FTAPP_DLB>(sp_select, new
                {
                    userCode = dto.UserCode,
                    truckNo = dto.PlateNo,
                    driverName = dto.DriverName,
                    DLBStatus = "D"
                }).FirstOrDefault();

                if (foundDraft == null)
                {
                    return BadRequest("No draft found");
                }

                var query_draftline = @$"Select * from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @headGuid";
                var drafts = conn.Query<FTAPP_DLB1>(query_draftline, new { headGuid = foundDraft.HeadGuid }).ToList();

                if (drafts.Count == 0) return NotFound();

                for (int d = 0; d < drafts.Count; d++)
                {
                    var dlbDoc = drafts[d];
                    if (dlbDoc == null) continue;

                    switch (dlbDoc.DocType)
                    {
                        case "I":
                            {
                                var sp_query = @"exec sp_GetInvoiceForDlbDraft_SingleInv @webDb, @guid, @docEntry";
                                var invoice = conn.Query<OINV>(sp_query, new
                                {
                                    webDb = db.WEBDB,
                                    guid = $"{foundDraft.HeadGuid}",
                                    docEntry = dlbDoc.DocEntry
                                }).FirstOrDefault();

                                if (invoice == null)
                                {
                                    continue;
                                }

                                // get the box list from web portal
                                //var query_box = $@"Select BoxId 
                                //                   from {db.WEBDB}..FTAPP_Box with (nolock)
                                //                   Where baseentry = @baseentry";

                                // get the box list from web portal
                                var query_box = $@"select DISTINCT  t0.BoxId
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

                                invoice.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = invoice.U_SOID }).ToList();
                                invoice.BoxesCount = invoice.Boxes.Count;
                                drafts[d].BoxesCount = invoice.Boxes.Count;

                                var query = $@"select distinct t0.BoxId
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
                                               inner join {db.WEBDB}..FTAPP_DLB2 t1 with (nolock) on t1.BoxId = t0.BoxId
                                               Where InvDocNum = @InvDocNum ";

                                // check counted box 
                                var boxes = conn.Query<FTAPP_Box>(query, new { InvDocNum = invoice.DocNum }).ToList();
                                if (boxes.Count == 0)
                                {
                                    invoice.BoxStatusDesc = "await scan box out";
                                }
                                else if (invoice.Boxes.Count == boxes.Count)
                                {
                                    invoice.BoxStatusDesc = "All box out";
                                }
                                drafts[d].Invoice = invoice;
                                break;
                            }
                        case "T":
                            {
                                var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                                var transfer = conn.Query<OWTR_Ext>(query_transfer,
                                    new
                                    {
                                        webDb = db.WEBDB,
                                        transferDocNum = drafts[d].DocNum
                                    }).FirstOrDefault();


                                if (transfer == null)
                                {
                                    continue;
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

                                from {db.WEBDB}..FTAPP_IBTBox t0 with (nolock)
                                left join  {db.WEBDB}..FTAPP_IBTBox1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                left join {db.WEBDB}..IBT t3 on t3.DocEntry = t0.BaseEntry
                                Where t3.TRANSITNO = @IbtDocNum 
                                and t1.BoxGuid is not null";

                                // based on scan in transfer doc 

                                transfer.Boxes = conn.Query<FTAPP_Box>(query_box, new { IbtDocNum = transfer.DocNum }).ToList();
                                if (transfer.Boxes.Count == 0)
                                {
                                    continue;
                                }

                                drafts[d].BoxesCount = transfer.Boxes.Count;

                                var query = $@"select distinct t0.BoxId
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
                                               inner join {db.WEBDB}..FTAPP_DLB2 t1 with (nolock) on t1.BoxId = t0.BoxId
                                               Where InvDocNum = @InvDocNum ";

                                // check counted box 
                                var boxes = conn.Query<FTAPP_Box>(query, new { InvDocNum = transfer.DocNum }).ToList();
                                if (boxes.Count == 0)
                                {
                                    transfer.BoxStatusDesc = "await scan box out";
                                }
                                else if (transfer.Boxes.Count == boxes.Count)
                                {
                                    transfer.BoxStatusDesc = "All box out";
                                }
                                drafts[d].Transfer = transfer;

                                break;
                            }
                        case "C":
                            {
                                var query_cog = @$"Select * from {db.WEBDB}..COG with (nolock)
                                   Where DocEntry = @docNum";

                                var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                                if (cog == null) return NotFound();

                                var query_cogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docNum";

                                cog.LINES = conn.Query<COG_Line>(query_cogLine, new { docNum = dto.DocNum }).ToList();
                                cog.SubSi = db.COMPANYNAME;

                                drafts[d].Cog = cog;
                                break;
                            }
                    }
                }

                var replied = new
                {
                    Lines = drafts,
                    HeadGuid = foundDraft.HeadGuid
                };

                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDBLToOut(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid dlb guid");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var update_dlb = @$"Update {db.WEBDB}..FTAPP_DLB set DLBStatus = @Dlbstatus
                                    where HeadGuid = @headGuid";

                var conn = new SqlConnection(_commDbConnStr);
                var res = conn.Execute(update_dlb, new { Dlbstatus = "O", headGuid = dto.HeadGuid });
                if (res >= 1)
                {
                    return Ok();
                }

                return BadRequest("Error update the DLB draft status to out");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDLB1(Dto_Delivery dto)
        {
            if (dto.Dlb == null)
            {
                return BadRequest("Invalid invoice head");
            }

            if (dto.Dlb1 == null)
            {
                return BadRequest("Invalid invoice number(s)");
            }

            if (dto.Dlb1.Count == 0)
            {
                return BadRequest("Invalid invoice number(s) (0)");
            }

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var checkDupFTAPP_DLB = @$"Select HeadGuid, DriverName 
                                           from {db.WEBDB}..FTAPP_DLB  
                                           where HeadGuid = @headGuid 
                                           and DriverName = @driverName ";

                var foundDlb = conn.Query<FTAPP_DLB>(checkDupFTAPP_DLB, new
                {
                    headGuid = dto.Dlb.HeadGuid,
                    driverName = dto.Dlb.DriverName
                }, trans).FirstOrDefault();

                if (foundDlb == null) // if no found then insert
                {
                    //// delete the head 
                    //var sp_delete = @$"delete from {db.WEBDB}..FTAPP_DLB 
                    //                  Where HeadGuid = @headerGuid";

                    //var res = conn.Execute(sp_delete, new { headerGuid = dto.Dlb.HeadGuid }, trans);

                    // insert the head
                    var sp_insert = @$" INSERT INTO {db.WEBDB}..FTAPP_DLB (
                                                 WhsUserCode
                                               , WhsUserName
                                               , OutTransDt
                                               , TruckNo
                                               , TruckCardCode
                                               , TruckCardName
                                               , HeadGuid 
                                               , Remarks
                                               , DriverName
                                               , DLBStatus
                                               , SubSi
                                    ) values (
                                           @WhsUserCode
                                          ,@WhsUserName
                                          ,GETDATE()
                                          ,@TruckNo
                                          ,@TruckCardCode
                                          ,@TruckCardName
                                          ,@HeadGuid 
                                          ,@Remarks
                                          ,@DriverName
                                          ,@DLBStatus 
                                          ,@SubSi
                                    )";

                    var res = conn.Execute(sp_insert, dto.Dlb, trans);
                    if (res <= 0)
                    {
                        return BadRequest("Error insert DLB head, please try again later.");
                    }
                }


                // get the new line to insert 
                var newInsertLines = dto.Dlb1.Where(x => $"{x.SaveAs}".Equals("savenew")).ToList();
                if (newInsertLines.Count > 0)
                {
                    // insert then lines
                    var sp_insert1 = @$"INSERT INTO {db.WEBDB}..FTAPP_DLB1 (                                              
                                                 DocNum                                                                                              
                                               , StoreCode
                                               , StoreName
                                               , DocEntry 
                                               , DocStatus
                                               , StatusDesc
                                               , HeadGuid
                                               , BoxStatusDesc
                                               , DocType
                                               , DocDate
                                               , DocTotal
                                               , CartonNo
                                               , RefNo
                                               , ConsigmentNo
                                               , SubSi
                                               , ToWhsCode
                                               , ToWhsName 
                                               , IBTEntry 
                                               , App_Determined_IsInterbranch, GeoCode, GeoType
                                ) values (                                           
                                            @DocNum                                           
                                           ,@StoreCode
                                           ,@StoreName
                                           ,@DocEntry 
                                           ,@DocStatus
                                           ,@StatusDesc
                                           ,@HeadGuid
                                           ,@BoxStatusDesc
                                           ,@DocType
                                           ,@DocDate
                                           ,@DocTotal
                                           ,@CartonNo
                                           ,@RefNo
                                           ,@ConsigmentNo
                                           ,@SubSi
                                           ,@ToWhsCode
                                           ,@ToWhsName
                                           ,@IBTEntry
                                           ,@App_Determined_IsInterbranch, @GeoCode, @GeoType
                                )";
                    var res1 = conn.Execute(sp_insert1, newInsertLines, trans);
                }

                // get the line for update
                var newUpdateLines = dto.Dlb1.Where(x => $"{x.SaveAs}".Equals("saveupdate")).ToList();
                if (newUpdateLines.Count > 0)
                {
                    var sp_updateLines = $@"update {db.WEBDB}..FTAPP_DLB1 set 
                                                 StoreCode = @StoreCode
                                               , StoreName = @StoreName                                              
                                               , DocStatus = @DocStatus
                                               , StatusDesc = @StatusDesc
                                               , BoxStatusDesc = @BoxStatusDesc
                                               , DocType = @DocType
                                               , DocDate = @DocDate
                                               , DocTotal = @DocTotal
                                               , CartonNo = @CartonNo
                                               , RefNo = @RefNo
                                               , ConsigmentNo = @ConsigmentNo, 
                                               , SubSi = @SubSi
                                               , App_Determined_IsInterbranch = @App_Determined_IsInterbranch, 
                                               , GeoCode = @GeoCode
                                               ，GeoType = @GeoType
                                        where HeadGuid = @HeadGuid
                                        and DocEntry = @DocEntry
                                        and DocNum = @DocNum";
                    conn.Execute(sp_updateLines, newUpdateLines, trans);
                }

                // remove the line 
                var deleteLines = dto.Dlb1.Where(x => $"{x.SaveAs}".Equals("savedelete")).ToList();
                if (deleteLines.Count > 0)
                {
                    var sp_delete2 = $@"delete from {db.WEBDB}..FTAPP_DLB1
                                        where HeadGuid = @HeadGuid
                                        and DocEntry = @DocEntry
                                        and DocNum = @DocNum
                                        and DocType = @DocType ";

                    conn.Execute(sp_delete2, deleteLines, trans);
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

        // for remove the cog and trasfer doc 
        IActionResult RemoveDoc(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DocNum == null)
            {
                return BadRequest("Invalid doc number");
            }
            if (dto.DocType == null)
            {
                return BadRequest("Invalid doc number");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // doc
                var sp_delete = @$"Delete from {db.WEBDB}..FTAPP_DLB1 
                                    Where DocNum = @DocNum 
                                    and DocType = @DocType";

                conn.Execute(sp_delete, new { dto.DocNum, dto.DocType }, trans);

                // remove onhold invoice 
                sp_delete = @$"Delete from {db.WEBDB}..FTAPP_HoldDlvryDocs
                                    Where DocNum = @DocNum
                                    and DocType = @DocType";

                conn.Execute(sp_delete, new { dto.DocNum, dto.DocType }, trans);

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

        IActionResult RemoveDeliveryInv(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.InvNum))
            {
                return BadRequest("Invalid invoice number");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // boxes 
                var sp_delete = @$"Delete from {db.WEBDB}..FTAPP_DLB2 
                                    Where InvDocNum = @invDocNum";

                conn.Execute(sp_delete, new { invDocNum = dto.InvNum }, trans);

                // invoice 
                sp_delete = @$"Delete from {db.WEBDB}..FTAPP_DLB1 
                                    Where DocNum = @invDocNum 
                                    and DocType = @DocType";

                conn.Execute(sp_delete, new { invDocNum = dto.InvNum, DocType = "I" }, trans);

                // remove onhold invoice 
                sp_delete = @$"Delete from {db.WEBDB}..FTAPP_HoldDlvryDocs
                                    Where DocNum = @invDocNum
                                    and DocType = @DocType";

                conn.Execute(sp_delete, new { invDocNum = dto.InvNum, DocType = "I" }, trans);

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

        IActionResult RemoveDeliveryDoc(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.DocNum))
            {
                return BadRequest("Invalid doc number");
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

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var query_dlb1 = @$"select * from {db.WEBDB}..FTAPP_DLB1 with (nolock)
                                    Where DocNum = @DocNum 
                                          and DocType = @docType ";

                var dlb1 = conn.Query<FTAPP_DLB1>(query_dlb1, new
                {
                    DocNum = dto.DocNum,
                    docType = dto.DocType
                }, trans).FirstOrDefault();

                if (dlb1 == null)
                {
                    trans.Rollback();
                    return BadRequest($"Invalid query of FTAPP_DLB1 doc from database, please try again. \n" +
                        $"{db.COMPANYNAME}, docNum # {dto.DocNum}, Type: {dto.DocType}");
                }

                // boxes  -----------------------
                var sp_delete = @$"Delete from {db.WEBDB}..FTAPP_DLB2 
                                    Where convert( nvarchar(50) ,HeadGuid) = @hguid 
                                          and InvDocNum = @InvDocNum ";

                var resDelBoxes = conn.Execute(sp_delete, new
                {
                    hguid = $"{dlb1.HeadGuid}",
                    InvDocNum = dto.DocNum
                }, trans);

                if (resDelBoxes < 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error delete FTAPP_DLB2 \n" +
                        $"{db.COMPANYNAME}, docNum # {dto.DocNum}, Type: {dto.DocType}");
                }

                // added doc 
                var sp_delete1 = @$"Delete from {db.WEBDB}..FTAPP_DLB1 
                                    Where convert( nvarchar(50) ,HeadGuid) = @hguid
                                          and DocNum = @DocNum 
                                          and DocType = @docType ";

                var resDelDocs = conn.Execute(sp_delete1, new
                {
                    hguid = $"{dlb1.HeadGuid}",
                    DocNum = dto.DocNum,
                    docType = dto.DocType
                }, trans);
                if (resDelDocs < 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error delete FTAPP_DLB1 \n" +
                       $"{db.COMPANYNAME}, docNum # {dto.DocNum}, Type: {dto.DocType}");

                }

                // remove onhold invoice 
                var sp_delete2 = @$"Delete from {db.WEBDB}..FTAPP_HoldDlvryDocs
                                    Where DocNum = @DocNum
                                    and DocType = @DocType";

                var resDeleteDlbDoc = conn.Execute(sp_delete2, new { dto.DocNum, dto.DocType }, trans);
                if (resDeleteDlbDoc <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error delete FTAPP_HoldDlvryDocs \n" +
                       $"{db.COMPANYNAME}, docNum # {dto.DocNum}, Type: {dto.DocType}");
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

        IActionResult RemoveDeliveryInv_ByDriver(Dto_Delivery dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.InvNum == null)
            {
                return BadRequest("Invalid invoice number");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // boxes 
                var sp_delete = @$"update {db.WEBDB}..FTAPP_DLB2 
                                    set InTransDt = null
                                    Where InvDocNum = @invDocNum";

                conn.Execute(sp_delete, new { invDocNum = dto.InvNum }, trans);

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

        IActionResult SaveOutBox(Dto_Delivery dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI");
            }

            if (dto.Dbl2 == null)
            {
                return BadRequest("Invalid out box");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("invalid dbi");
            }

            // check exist
            using var connSelect = new SqlConnection(_commDbConnStr);
            var sp_CheckDupl = @$"select InvDocNum ,  BoxId 
                                      from {db.WEBDB}..FTAPP_DLB2  with (nolock)
                                      Where InvDocNum = @InvDocNum 
                                      and HeadGuid = @HeadGuid 
                                      and BoxId = @BoxId ";

            var found = connSelect.Query<FTAPP_DLB1>(sp_CheckDupl, new
            {
                InvDocNum = dto.Dbl2.InvDocNum,
                HeadGuid = dto.Dbl2.HeadGuid,
                BoxId = dto.Dbl2.BoxId
            }).FirstOrDefault();

            if (found != null) // already save 
            {
                return Ok();
                //var sp_Delete = @$"Delete {db.WEBDB}..FTAPP_DLB2 
                //                  Where InvDocNum = @InvDocNum 
                //                        and HeadGuid = @HeadGuid 
                //                        and BoxId = @BoxId ";

                //var deleRes = conn.ExecuteScalar<int>(sp_Delete, new
                //{
                //    InvDocNum = dto.Dbl2.InvDocNum,
                //    HeadGuid = dto.Dbl2.HeadGuid,
                //    BoxId = dto.Dbl2.BoxId
                //}, trans);

                //if (deleRes < 0)
                //{
                //    trans.Rollback();
                //    return BadRequest($"Error delete existing box. {dto.Dbl2.BoxId} ");
                //}
            }

            // start a transaction and insert 
            // 20240616

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // perform the insert
                var sp_insert = @$"INSERT INTO {db.WEBDB}..FTAPP_DLB2 (                                       
                                 InvDocNum
                               , BoxId
                               , OutTransDt   
                               , InTransDt                            
                               , SoDocEntry 
                               , DlbEntry
                               , HeadGuid
                                ) values ( 
                                @InvDocNum
                               ,@BoxId
                               ,GETDATE()
                               ,GETDATE()
                               ,@SoDocEntry 
                               ,@DlbEntry 
                               ,@HeadGuid      
                                )";

                var res = conn.Execute(sp_insert, dto.Dbl2, trans);
                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest("Error insert out box, please try again.");
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

        IActionResult VerifyTransfer(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid doc number");
                }
                if (string.IsNullOrWhiteSpace(dto.SubsiId))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid usercode");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid username");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type ");
                }

                // check dlb1 with status != R 

                //var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                var db = string.IsNullOrWhiteSpace(dto.Subsi) ?
                    new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubsiId) :
                    new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                if (dto.IsAgedInvoice)
                {
                    goto ProcessNormallCheck;
                }

                // 20240202
                // query check to got any aged invoice 
                var conn1 = new SqlConnection(_commDbConnStr);
                var sp_checkInvWhs = @$"Select FROMWHS from {db.WEBDB}..IBT Where TRANSITNO = @docNum";
                var whsCode = conn1.Query<string>(sp_checkInvWhs, new { docNum = dto.DocNum }).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(whsCode))
                {
                    goto ProcessNormallCheck;
                }

                // 20240331
                if (db.ISJAMWHS_CHECK == null)
                {
                    db.ISJAMWHS_CHECK = "N";
                }
                if ($"{db.ISJAMWHS_CHECK}".ToLower().Equals("n"))
                {
                    goto performAgedIBT;
                }

                // check the whs is jam or not 
                // 20240329 
                var sp_QueryJamConfition = @"exec sp_GetIntruckDt_IBT @webDb,  @whsCode ";
                var Whses = conn1.Query<JamWhs>(sp_QueryJamConfition, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode

                }).ToList();
                if (Whses.Count > 0) // got invoice delivery over the grade period 
                {

                    if ($"{Whses.First().IsJam}".ToLower() != "Y")
                    {
                        // update the sap whs as Y 
                        var sp_UppdateSapOWHS = @$"Update {db.SAPDB}..OWHS
                                                   set U_JAMWHS = 'Y'
                                                    where whscode = @whsCode";
                        conn1.Execute(sp_UppdateSapOWHS, new { whsCode });
                    }

                    return BadRequest($"{db.COMPANYNAME}, Warehouse {whsCode} in JAM /BLOCKED, " +
                        $"No IBT loading allow until aged IBT delivered.");
                }

            performAgedIBT:

                // for determine the return of the aged docs                
                var sp_checkInvWhs1 = $@"exec sp_QueryWhsActualLoc @webDb , @invno ";
                var whsCode1 = conn1.Query<string>(sp_checkInvWhs1, new { webDb = db.WEBDB, invno = dto.InvNum }).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(whsCode1))
                {
                    goto ProcessNormallCheck;
                }

                // 20240323 
                // get the default whs aged day 
                var sp_defaultAgedday = $@"select setupValue 
                                        from KTCW_COMMON..FTApp_Config 
                                        Where SetupName = 'DeliveryAppPickInvoiceAgedInv'";

                var defaultAgedDay = conn1.ExecuteScalar<int>(sp_defaultAgedday);

                // 20240323 
                // query the whs custom aged day
                var sp_WhsAgedDay = @$"select ISNULL(U_WhsAgedDocDay, {defaultAgedDay}) 
                                    from  {db.SAPDB}..OWHS 
                                    where WhsCode = @whsCode ";

                var agedDay = conn1.ExecuteScalar<int>(sp_WhsAgedDay, new { whsCode = whsCode });

                // check aged invoices
                //var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1 @webDb , @whsCode  ";
                var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1_partA  @webDb , @whsCode, @aagedDay ";
                var agedInvs = conn1.Query<AgedDoc>(sp_CheckAgedInv, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }).ToList();

                var newAgedDocs = new List<AgedDoc>();
                if (agedInvs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs);
                }

                // 20240323
                // check aged invoices
                sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1_partB @webDb , @whsCode, @aagedDay  ";
                agedInvs = conn1.Query<AgedDoc>(sp_CheckAgedInv, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }).ToList();

                if (agedInvs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs);
                }

                // 20230316
                // check for aged instransit warehouse 
                var sp_CheckAgedInv_InsWhs = @"exec sp_GetOldestAgedInv_TransWhs_v1 @webDb , @whsCode  ";
                var agedInvs_InWhs = conn1.Query<AgedDoc>(sp_CheckAgedInv_InsWhs, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode
                }).ToList();

                if (agedInvs_InWhs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs_InWhs);
                }

                // remove duplicated invioce 
                // group the same to 1
                newAgedDocs = newAgedDocs.GroupBy(g => g.DocNum)
                    .Select(i => i.FirstOrDefault()).ToList();

                // check aged ibt
                var sp_CheckAgedIBT = @"exec sp_GetOldestAgedIBT_v1 @webDb , @whsCode  ";
                var agedIbts = conn1.Query<AgedDoc>(sp_CheckAgedIBT, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode
                }).ToList();

                if (agedIbts.Count > 0)
                {
                    newAgedDocs.AddRange(agedIbts);
                }

                if (newAgedDocs.Count == 0)
                {
                    goto ProcessNormallCheck;
                }

                // 20240524 
                // add select the top 10 record to driver 
                var showNumAgedRec_sp = "select setupValue from ktcw_common..FTApp_Config Where SetupName = 'NumOfAgedDocShow'";
                int showNumAgedRec = conn1.ExecuteScalar<int>(showNumAgedRec_sp);

                if (showNumAgedRec == 0)
                {
                    showNumAgedRec = 10;
                }

                // show the top 10 record 
                newAgedDocs = new List<AgedDoc>(newAgedDocs.OrderBy(d => d.DocDate).Take(showNumAgedRec));

                // mix the invoie anf ibt base on aged
                //newAgedDocs = newAgedDocs.OrderBy(a => a.DayAged).ToList();
                if (newAgedDocs.Count > 0)
                {
                    var newDto1 = new Dto_AgedDoc
                    {
                        AgedDocs = newAgedDocs,
                        Transfer = null
                    };
                    return Ok(newDto1); // return to the app 
                }

            ProcessNormallCheck:

                var query = @$"select * 
                               from {db.WEBDB}..DLB1 with (nolock)
                               where DocNum = @docnum 
                               and doctype = @docType 
                               order by CONVERT(date, DMODIFIED) ";

                using var conn = new SqlConnection(_commDbConnStr);
                var transfers = conn.Query<DLB1>(query, new
                {
                    docnum = dto.DocNum,
                    docType = dto.DocType
                }).ToList();

                // when no found any transfers in previuos dlb consider to ok to add.
                if (transfers.Count == 0)
                {
                    goto FurtherProcess;
                }

                // when dlb is O = out / intransit
                if (transfers.Count >= 3)
                {
                    return BadRequest($"transfer #{dto.DocNum} was blocked for delivery," +
                        $" please try again later. [>=3 tried]");
                }

                var lastTransfer = transfers.LastOrDefault();
                if (lastTransfer == null)
                {
                    lastTransfer = transfers[0];
                }

                if (lastTransfer.STATUS == "O")
                {
                    return BadRequest($"transfers #{dto.DocNum} was out / Intransit of delivery," +
                       $" please try again later. [Intransit]");
                }

                if (lastTransfer.STATUS == "R")
                {
                    return BadRequest($"transfers #{dto.DocNum} was signed returned" +
                       $" please try again later. [Returned]");
                }

            FurtherProcess:
                // onhold the cog 
                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @docnum 
                                and doctype = @doctype";

                var transferOnHold = conn.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    docnum = dto.DocNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (transferOnHold != null)
                {
                    return BadRequest($"transfers #{dto.DocNum} was hold in loading transaction by {transferOnHold.UserName}" +
                      $" please try again later. [Onhold]");
                }

                // hold it
                var isOnhOld = PutDocOnHold(db, dto); // transfer
                if (!isOnhOld)
                {
                    return BadRequest($"transfers #{dto.DocNum} put onhold fail. " +
                      $"Please try again later. [Onhold-Fail]");
                }

                var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                var transfer = conn.Query<OWTR_Ext>(query_transfer,
                    new
                    {
                        webDb = db.WEBDB,
                        transferDocNum = dto.DocNum
                    }).FirstOrDefault();

                if (transfer == null) return NotFound();

                var query_TransferLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                   Where DocEntry = @DocEntry";

                transfer.Lines = conn.Query<WTR1_Ext>(query_TransferLine, new { transfer.DocEntry }).ToList();

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
                    return BadRequest($"{db.COMPANYNAME}, Transfer #{dto.InvNum} " +
                                      $"from {dto.Subsi}, Error query for boxes.");
                }

                var newDto = new Dto_AgedDoc
                {
                    AgedDocs = null,
                    Transfer = transfer
                };
                return Ok(newDto); // return to the app 

                //return Ok(transfer);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult VerifyCog(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid doc number");
                }
                //if (string.IsNullOrWhiteSpace(dto.Subsi))
                //{
                //    return BadRequest("Invalid subsi");
                //}
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid usercode");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid username");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type ");
                }

                DbInfo db = null;
                if (!string.IsNullOrWhiteSpace(dto.Subsi)) // for support old delivery app 
                {
                    db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                }
                else if (!string.IsNullOrWhiteSpace(dto.SubsiId))
                {
                    db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubsiId); // for support new release app 
                }

                // check dlb1 with status != R 
                //var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalie dbi");
                }

                var query = @$"select * 
                               from {db.WEBDB}..DLB1 with (nolock)
                               where DocNum = @docnum 
                               and doctype = @docType 
                               order by CONVERT(date, DMODIFIED) ";

                using var conn = new SqlConnection(_commDbConnStr);
                var cogs = conn.Query<DLB1>(query, new
                {
                    docnum = dto.InvNum,
                    docType = dto.DocType
                }).ToList();

                // when no found any invoice in previuos dlb consider to ok to add.
                if (cogs.Count == 0)
                {
                    goto FurtherProcess;
                }

                // when dlb is O = out / intransit
                if (cogs.Count >= 3)
                {
                    return BadRequest($"Cog #{dto.DocNum} was blocked for delivery," +
                        $" please try again later. [>=3 tried]");
                }

                var lastRetMemo = cogs.LastOrDefault();
                if (lastRetMemo == null)
                {
                    lastRetMemo = cogs[0];
                }

                if (lastRetMemo.STATUS == "O")
                {
                    return BadRequest($"Cog #{dto.DocNum} was out / Intransit of delivery," +
                       $" please try again later. [Intransit]");
                }

                if (lastRetMemo.STATUS == "R")
                {
                    return BadRequest($"Cog #{dto.DocNum} was signed returned" +
                       $" please try again later. [Returned]");
                }

            FurtherProcess:
                // onhold the cog 
                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @docnum 
                                and doctype = @doctype";

                var cogOnHold = conn.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    docnum = dto.DocNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (cogOnHold != null)
                {
                    return BadRequest($"Cog #{dto.DocNum} was hold in loading transaction by {cogOnHold.UserName}" +
                      $" please try again later. [Onhold]");
                }

                var query_cog = @$"Select * 
                                   , (Select sum(LineTotal) from {db.WEBDB}..COG1 with (nolock)
                                        where docentry = @docNum) [DocTotal]
                                   from {db.WEBDB}..COG with (nolock)
                                   Where DocEntry = @docNum";

                var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                if (cog == null) return NotFound();


                // 20220914
                // prevent the non submit cog doc being pick 
                if ($"{cog.DOCSTATUS}".ToLower() != "s")
                {
                    return BadRequest("COG doc doc status not S, not picking allowed.");
                }

                var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = dto.DocNum }).ToList();
                cog.SubSi = db.COMPANYNAME;


                // hold it for and process next 
                // hold the doc at last 
                var isOnhOld = PutDocOnHold(db, dto); // cog 
                if (!isOnhOld)
                {
                    return BadRequest($"Cog #{dto.DocNum} put onhold fail. " +
                      $"Please try again later. [Onhold-Fail]");
                }

                return Ok(cog);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult VerifyInvoice(Dto_Delivery dto) // use by the driver to scan add invoice
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.InvNum))
                {
                    return BadRequest("Invalid invoice number");
                }
                //if (string.IsNullOrWhiteSpace(dto.Subsi))
                //{
                //    return BadRequest("Invalid subsi");
                //}

                //if (string.IsNullOrWhiteSpace(dto.SubsiId))
                //{
                //    return BadRequest("Invalid subsi id");
                //}
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid usercode");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid username");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type ");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid app guid");
                }

                // check dlb1 with status != R 

                DbInfo db = null;
                if (!string.IsNullOrWhiteSpace(dto.Subsi)) // for support old delivery app 
                {
                    db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                }
                else if (!string.IsNullOrWhiteSpace(dto.SubsiId))
                {
                    db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubsiId); // for support new release app 
                }

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var query = @$"select * 
                               from {db.WEBDB}..DLB1 with (nolock)
                               where DocNum = @docnum 
                               and doctype = @docType 
                               order by CONVERT(date, DMODIFIED) ";

                using var conn = new SqlConnection(_commDbConnStr);
                var invoices = conn.Query<DLB1>(query, new
                {
                    docnum = dto.InvNum,
                    docType = dto.DocType
                }).ToList();

                // when no found any invoice in previuos dlb consider to ok to add.
                if (invoices.Count == 0)
                {
                    goto FurtherProcess;
                }

                // when dlb is O = out / intransit
                if (invoices.Count >= 3)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was blocked for delivery," +
                        $" please try again later. [>=3 tried]");
                }

                var lastInv = invoices.LastOrDefault();
                if (lastInv == null)
                {
                    lastInv = invoices[0];
                }

                if (lastInv.STATUS == "O")
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was out / Intransit of delivery," +
                       $" please try again later. [Intransit]");
                }

                if (lastInv.STATUS == "R")
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was signed returned" +
                       $" please try again later. [Returned]");
                }

            FurtherProcess:
                // 20220725 
                // check invoice / SO - PO Expdate date 
                var sp_query = @"exec sp_QuerySOPoExpDates @webDb,  @InvDocNum ";
                var docSo = conn.Query<SO>(sp_query, new { webDb = db.WEBDB, InvDocNum = dto.InvNum }).FirstOrDefault();
                if (docSo == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was having bad query with SOrder details" +
                     $" please try again later. [Bad Order Details]");
                }

                if (docSo.PoExpDate == default)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was having bad query with SOrder details for PO Exp dates" +
                     $" please try again later. [Bad Order Details - PO Exp date in null]");
                }

                var dtCompare = DateTime.Compare(docSo.PoExpDate, DateTime.Now.Date);

                //Less than zero t1 is earlier than t2.
                //Zero t1 is the same as t2.
                if (dtCompare <= 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum}, PO expired : {docSo.PoExpDate:dd-MMM-yy}, " +
                        $"Server date {DateTime.Now.Date: dd-MMM-yy}");
                }

                //Greater than zero   t1 is later than t2.
                // 20220725 
                // -----------------------------------------------------------

                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @invoiceno and Doctype = @doctype";

                var InvOnHold = conn.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    invoiceno = dto.InvNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (InvOnHold != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was hold in loading transaction by {InvOnHold.UserName}" +
                      $" please try again later. [Onhold]");
                }

                // return ok when all status 
                // get the invoice from sap                     
                var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                OINV inv = conn.Query<OINV>(query_inv, new { docnum = dto.InvNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} from {dto.Subsi}, Error query for sap invoice.");
                }

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

                                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                Where t0.BaseEntry = @baseentry 
                                and t1.BoxGuid is not null";

                inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                if (inv.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} from {dto.Subsi}, Error query for boxes.");
                }

                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;

                // put onhold at last 
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    dto.DocNum = dto.InvNum;
                }

                // hold it for and process next 
                var isOnhOld = PutDocOnHold(db, dto); // invoice
                if (!isOnhOld)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} put onhold fail. " +
                      $"Please try again later. [Onhold-Fail]");
                }

                return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// This verify add dlb invoice 
        /// only allow fresh invoice to be add in
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult VerifyInvoice_FreshAdd(Dto_Delivery dto) // use by the driver to scan add invoice
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.InvNum))
                {
                    return BadRequest("Invalid invoice number");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("Invalid username");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type ");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid app guid");
                }

                // check dlb1 with status != R 

                DbInfo db = null;
                if (!string.IsNullOrWhiteSpace(dto.Subsi)) // for support old delivery app 
                {
                    db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                }
                else if (!string.IsNullOrWhiteSpace(dto.SubsiId))
                {
                    db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubsiId); // for support new release app 
                }

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                if (dto.IsAgedInvoice)
                {
                    goto ProcessNormallCheck;
                }

                // 20240202
                // query check to got any aged invoice 
                //var sp_checkInvWhs = @$"Select WhsCode from {db.WEBDB}..SO Where INVNO = @invNo";

                var sp_checkInvWhs = $@"exec sp_QueryWhsActualLoc @webDb , @invno ";
                var whsCode = conn.Query<string>(sp_checkInvWhs, new { webDb = db.WEBDB, invno = dto.InvNum }).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(whsCode))
                {
                    goto ProcessNormallCheck;
                }

                // 20240331
                if (db.ISJAMWHS_CHECK == null)
                {
                    db.ISJAMWHS_CHECK = "N";
                }
                if ($"{db.ISJAMWHS_CHECK}".ToLower().Equals("n"))
                {
                    goto performAgedInvoice;
                }

                // check the whs is jam or not - invoice only
                // 20240329 
                var sp_QueryJamConfition = @"exec sp_GetIntruckDt @webDb,  @whsCode ";
                var Whses = conn.Query<JamWhs>(sp_QueryJamConfition, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode

                }).ToList();
                if (Whses.Count > 0) // got invoice delivery over the grade period 
                {

                    if ($"{Whses.First().IsJam}".ToLower() != "Y")
                    {
                        // 20240430
                        using var trans = conn.BeginTransaction();
                        try
                        {
                            // add transaction make it update success
                            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                            // update the sap whs as Y 
                            var sp_UppdateSapOWHS = @$"Update {db.SAPDB}..OWHS
                                                   set U_JAMWHS = 'Y'
                                                    where whscode = @whsCode";
                            conn.Execute(sp_UppdateSapOWHS, new { whsCode }, trans);
                            trans.Commit();
                        }
                        catch (Exception eUpdateWhsJam)
                        {
                            trans.Rollback();
                            _logger.LogError($"{eUpdateWhsJam.Message}\n{eUpdateWhsJam.StackTrace}");
                        }
                    }

                    return BadRequest($"{db.COMPANYNAME}, Warehouse {whsCode} in JAM /BLOCKED, " +
                        $"No loading invoice allow until aged invoice delivered.");
                }

            performAgedInvoice:
                // 20240323 
                // get the default whs aged day 

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

                var sp_defaultAgedday = $@"select setupValue 
                                        from KTCW_COMMON..FTApp_Config 
                                        Where SetupName = 'DeliveryAppPickInvoiceAgedInv'";

                var defaultAgedDay = conn.ExecuteScalar<int>(sp_defaultAgedday);

                // 20240323 
                // query the whs custom aged day
                var sp_WhsAgedDay = @$"select ISNULL(U_WhsAgedDocDay, {defaultAgedDay}) 
                                    from  {db.SAPDB}..OWHS 
                                    where WhsCode = @whsCode ";

                var agedDay = conn.ExecuteScalar<int>(sp_WhsAgedDay, new { whsCode = whsCode });

                // for determine the return of the aged docs
                var newAgedDocs = new List<AgedDoc>();

                // check aged invoices
                //var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1 @webDb , @whsCode  ";
                var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1_partA  @webDb , @whsCode, @aagedDay ";
                var agedInvs = conn.Query<AgedDoc>(sp_CheckAgedInv, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }, commandTimeout: 0).ToList();

                if (agedInvs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs);
                }

                // 20240323
                // check aged invoices
                sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1_partB @webDb , @whsCode, @aagedDay  ";
                agedInvs = conn.Query<AgedDoc>(sp_CheckAgedInv, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }, commandTimeout: 0).ToList();

                if (agedInvs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs);
                }

                // 20230316
                // check for aged instransit warehouse 
                var sp_CheckAgedInv_InsWhs = @"exec sp_GetOldestAgedInv_TransWhs_v1 @webDb , @whsCode  ";
                var agedInvs_InWhs = conn.Query<AgedDoc>(sp_CheckAgedInv_InsWhs, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode
                }, commandTimeout: 0).ToList();

                if (agedInvs_InWhs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs_InWhs);
                }

                // remove duplicated invoice 
                // group the same to 1
                newAgedDocs = newAgedDocs.GroupBy(g => g.DocNum)
                    .Select(i => i.FirstOrDefault()).ToList();

                // check aged ibt
                var sp_CheckAgedIBT = @"exec sp_GetOldestAgedIBT_v1 @webDb , @whsCode  ";
                var agedIbts = conn.Query<AgedDoc>(sp_CheckAgedIBT, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode
                }, commandTimeout: 0).ToList();

                if (agedIbts.Count > 0)
                {
                    newAgedDocs.AddRange(agedIbts);
                }

                // get the older dates 
                // 20240520
                if (newAgedDocs.Count == 0)
                {
                    goto ProcessNormallCheck;
                }

                // 20240524 
                // add select the top 10 record to driver 
                var showNumAgedRec_sp = "select setupValue from ktcw_common..FTApp_Config Where SetupName = 'NumOfAgedDocShow'";
                int showNumAgedRec = conn.ExecuteScalar<int>(showNumAgedRec_sp);

                if (showNumAgedRec == 0)
                {
                    showNumAgedRec = 10;
                }

                // show the top 10 record 
                newAgedDocs = new List<AgedDoc>(newAgedDocs.OrderBy(d => d.DocDate).Take(showNumAgedRec));

                //ProcessNormal:

                // mix the invoice anf ibt base on aged
                //newAgedDocs = newAgedDocs.OrderByDescending(a => a.DayAged).ThenBy(d => d.DocNum).ToList();

                if (newAgedDocs.Count > 0)
                {
                    var newDto1 = new Dto_AgedDoc
                    {
                        AgedDocs = newAgedDocs,
                        Invoice = null
                    };

                    return Ok(newDto1); // return to the app 
                }

            // process single invoice 
            ProcessNormallCheck:

                if (dto.IsAgedInvoice) // 20240205
                {
                    goto FurtherProcess;
                }

                var query = @$"select DOCENTRY
                                          , LINENUM
                                          , DOCTYPE
                                          , DOCNUM
                                          , DOCDATE
                                          , CARDCODE
                                          , CARDNAME
                                          , DOCTOTAL
                                          , TERRITORY
                                          , GEOCODE
                                          , TOTALPAGES
                                          , CARTONNO
                                          , REFNO
                                          , STATUS
                                          , RETDATE
                                          , PAGES
                                          , UMODIFIED
                                          , DMODIFIED
                                          , RECDATE
                                          , CONSIGNMENTNO
                                          , RECTIME
                                          , DRIVERDATE
                                          , DRIVERTIME

                               from {db.WEBDB}..DLB1 with (NOLOCK)
                               where DocNum = @docnum 
                               and doctype = @docType 
                               order by CONVERT(date, DMODIFIED) ";


                var invoices = conn.Query<DLB1>(query, new
                {
                    docnum = dto.InvNum,
                    docType = dto.DocType
                }, commandTimeout: 0).ToList();


                // when no found any invoice in previous dlb consider to ok to add.
                if (invoices.Count == 0)
                {
                    goto FurtherProcess;
                }
                else
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.InvNum} is not fresh entry for DLB. " +
                        $"please use rescan DLB module to process this invoice, Thanks.");
                }

            FurtherProcess:

                // check invoice in draft or not
                // 20241024

                var sp_checkDraftExit = $"exec sp_CheckDLBInDraft @webDb , @docType, @docStatus , @docNum ";
                var foundDlbDraft_Inv = conn.Query<FTAPP_DLB1>(sp_checkDraftExit, new
                {
                    webDb = db.WEBDB,
                    docType = "I",
                    docNum = dto.DocNum,
                    docStatus = "D",
                }).FirstOrDefault();

                if (foundDlbDraft_Inv != null)
                {
                    // 20250309
                    // auto delete the dlb 1 by id 
                    // and continue / allow this loading 

                    var delete_dlb1 = @$"delete from {db.WEBDB}..FTAPP_DLB1 Where Id = @DraftID";
                    var isdelected = conn.Execute(delete_dlb1, new { DraftID = foundDlbDraft_Inv.id });

                    //return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.InvNum} found draft for DLB. " +
                    //   $"Draft By {foundDlbDraft_Inv.DriverName} {foundDlbDraft_Inv.TruckNo} " +
                    //   $"Draft Id {foundDlbDraft_Inv.DraftID} When {foundDlbDraft_Inv.OutTransDt:dd-MMM-yy hh:mm tt}, Thanks.");
                }

                // 20220725 
                // check invoice / SO - PO Expired date 

                var sp_query = @"exec sp_QuerySOPoExpDates @webDb,  @InvDocNum ";
                var docSo = conn.Query<SO>(sp_query, new { webDb = db.WEBDB, InvDocNum = dto.InvNum }).FirstOrDefault();
                if (docSo == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was PO expired " +
                     $" please try again later. [PO Expired]");
                }

                if (docSo.PoExpDate == default)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} was having bad query with SOrder details for PO Exp dates" +
                     $" please try again later. [Bad Order Details - PO Exp date in null]");
                }

                var dtCompare = DateTime.Compare(docSo.PoExpDate, DateTime.Now.Date);

                //Less than zero t1 is earlier than t2.
                //Zero t1 is the same as t2.
                if (dtCompare <= 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum}, PO expired : {docSo.PoExpDate:dd-MMM-yy}, " +
                        $"Server date {DateTime.Now.Date: dd-MMM-yy}");
                }

                // return ok when all status 
                // get the invoice from sap                     
                //var query_inv = @$"select * from {db.SAPDB}..OINV with (NOLOCK) where DocEntry = @docEntry";

                var query_inv = @$"select t2.* 
                                    , t2.U_GLN [INVLevGPS]
                                    , t0.U_DELGLN [DriverStoreWhsGPS]
                                    , t0.GlblLocNum [SellerStoreGPS]
                                    , t0.U_DROPPOINT [DROP_POINT_WHSCODE]
                                    , t1.GlblLocNum [DROP_POINT_GEOCODE] 
                                    , t1.GlblLocNum [DROP_POINT_WHS_GPS]
                                    , t3.WHSCODE [SO_WHSCODE]
                                    , t4.GlblLocNum [SO_WHS_GPS]
                                     from      {db.SAPDB}..OCRD t0 with (NOLOCK)
                                    left join {db.SAPDB}..OWHS t1 with (NOLOCK) on t1.WhsCode = t0.U_DROPPOINT
                                    left join {db.SAPDB}..OINV t2 with (NOLOCK) on t2.CardCode = t0.CardCode
                                    left join {db.WEBDB}..SO   t3 with (NOLOCK) on t3.DocEntry = t2.U_SOID
                                    left join {db.SAPDB}..OWHS t4 with (NOLOCK) on t4.WhsCode = t3.WHSCODE
                                    where t2.DocEntry = @docEntry ";
                                     

                OINV inv = conn.Query<OINV>(query_inv, new { docEntry = docSo.INVENTRY }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{docSo.INVNO} from {docSo.INVENTRY}, Error query for sap invoice.");
                }

                // get the box list from web portal
                // 20230415
                // add in LabelConsistTotalBoxes to query the ftapp_box
                var query_box = $@"select distinct
                                               t0.BoxId
                                              ,t0.PickerCode
                                              ,t0.PickerName
                                              ,t0.PickDt
                                              ,t0.PackId
                                              ,t0.PackDt
                                              ,t0.PackerCode
                                              ,t0.PackerName
                                              ,t0.BaseEntry
                                              ,t0.BoxGuid
                                              ,t0.TimeStampSeq
                                              ,t0.AppVersion
                                              ,t0.BoxSize
                                              ,t0.OrderProcessWeek
                                              ,t0.BusinessCenterCode
                                              ,t0.CurrentCartonNo
                                              ,t0.OrderNo
                                              ,t0.LabelConsistTotalBoxes

                                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                                Where t0.BaseEntry = @baseentry 
                                and t1.BoxGuid is not null";

                inv.Boxes = conn.Query<FTAPP_Box>(query_box, new
                {
                    baseentry = inv.U_SOID
                }, commandTimeout: 0).ToList();

                if (inv.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} " +
                                    $" from {dto.Subsi}, Error query for boxes.");
                }

                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;

                // put onhold at last 
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    dto.DocNum = dto.InvNum;
                }

                // hold it for and process next 
                var isOnhOld = PutDocOnHold(db, dto); // invoice
                if (!isOnhOld)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.InvNum} put onhold fail. " +
                                $"Please try again later. [Onhold-Fail]");
                }

                var newDto = new Dto_AgedDoc
                {
                    AgedDocs = null,
                    Invoice = inv
                };

                return Ok(newDto);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        bool PutDocOnHold(DbInfo db, Dto_Delivery dto)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var newOnHold = new FTAPP_HoldDlvryDocs
                {
                    DocNum = dto.DocNum,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    Reason = "Loading",
                    DocType = dto.DocType,
                    HeadGuid = dto.HeadGuid
                };

                var sp_insert = $@" INSERT INTO {db.WEBDB}..FTAPP_HoldDlvryDocs (
                                        DocNum 
                                       ,HoldDt
                                       ,UserCode
                                       ,UserName
                                       ,Reason 
                                       ,DocType
                                       ,HeadGuid
                                ) values ( 
                                        @DocNum 
                                       ,GETDATE()
                                       ,@UserCode
                                       ,@UserName
                                       ,@Reason 
                                       ,@DocType  
                                       ,@HeadGuid
                                ) ";

                var res = conn.Execute(sp_insert, newOnHold, trans);
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

        IActionResult GetTrucks(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalie dbi");
                }

                //var sp_query = @"exec sp_GetTrucks @subsi";
                var sp_query = @"exec sp_GetTrucksV1 @webDb";

                using var conn = new SqlConnection(_commDbConnStr);
                var trucks = conn.Query<FTAPP_TruckCapacity>(sp_query, new
                {
                    webDb = db.WEBDB
                }).ToList();

                if (trucks.Count == 0) return NotFound();
                return Ok(trucks);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetListOfDlb(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date time");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date time");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company dbi");
                }
                var sp_query = @"exec sp_GetDLBs @webDb, @startDt, @endDt, @userCode";
                using var conn = new SqlConnection(_commDbConnStr);

                var dlbs = conn.Query<DLB>(sp_query, new
                {
                    webDb = db.WEBDB,
                    startDt = dto.StartDt.Date,
                    endDt = dto.EndDt.Date,
                    userCode = dto.UserCode
                }).ToList();

                if (dlbs.Count == 0) return NotFound();
                return Ok(dlbs);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetSingle_FTAPP_Dlb(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid head guid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company dbi");
                }
                var sp_query = @"exec sp_Get_DLBsFTAPP_Single @webDb, @guid";
                using var conn = new SqlConnection(_commDbConnStr);

                var dlb = conn.Query<FTAPP_DLB>(sp_query, new
                {
                    webDb = db.WEBDB,
                    guid = dto.HeadGuid
                }).FirstOrDefault();

                if (dlb == null) return NotFound();
                return Ok(dlb);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetListOf_FTAPP_Dlb(Dto_Delivery dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date time");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date time");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company dbi");
                }
                var sp_query = @"exec sp_Get_DLBsFTAPP @webDb,  @userCode, @startDt, @endDt ";
                using var conn = new SqlConnection(_commDbConnStr);

                var dlbs = conn.Query<FTAPP_DLB>(sp_query, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    startDt = dto.StartDt.Date,
                    endDt = dto.EndDt.Date
                }).ToList();

                if (dlbs.Count == 0) return NotFound();
                return Ok(dlbs);
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
