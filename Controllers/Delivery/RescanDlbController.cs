using Dapper;
using KTC_SalesAppWAPI.DTOs.Delivery;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Delivery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Data.SqlClient;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.COG;
using System.Collections.Generic;
using KTC_SalesAppWAPI.Helpers.Delivery;
using KTC_SalesAppWAPI.Models.CommonDb;
using System.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KTC_SalesAppWAPI.Controllers.Delivery
{
    [Route("[controller]")]
    [ApiController]
    public class RescanDlbController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<RescanDlbController> _logger;
        string _commDbConnStr = "";
        string LastError = "";

        public RescanDlbController(IConfiguration configuration, ILogger<RescanDlbController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_Rescan dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "VerifyDlbReScanDoc_Transfer":
                    {
                        return VerifyDlbReScanDoc_Transfer(dto); // for bug checking 
                    }
                case "VerifyDlbReScanDoc_Inv":
                    {
                        return VerifyDlbReScanDoc_Inv(dto);
                    }
                case "VerifyDlbReScanDoc_Cog":
                    {
                        return VerifyDlbReScanDoc_Cog(dto);
                    }
                case "SaveDLB1_ReScan":
                    {
                        return SaveDLB1_ReScan(dto);
                    }
                case "CheckDriverDlb_Rescan":
                    {
                        return CheckDriverDlb_Rescan(dto);
                    }
                //case "RemoveDeliverySaveDraft_Rescan":
                //    {
                //        return RemoveDeliverySaveDraft_Rescan(dto);
                //    }
                case "RemoveDeliveryDoc_ReScan":
                    {
                        return RemoveDeliveryDoc_ReScan(dto);
                    }
                case "VerifyAndUpdateDriverDoc_ReScan":
                    {
                        return VerifyAndUpdateDriverDoc_ReScan(dto);
                    }
                case "SaveDLB_ByDriver_ReScan":
                    {
                        return SaveDLB_ByDriver_ReScan(dto);
                    }
                case "CheckingDriverDrafts_Rescan":
                    {
                        return CheckingDriverDrafts_Rescan(dto);
                    }
                case "CheckDriverRescanDlb_ByGuid":
                    {
                        return CheckDriverRescanDlb_ByGuid(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult CheckingDriverDrafts_Rescan(Dto_Rescan dto)
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

                    var sp_query = @"exec sp_QueryDriverDlbDrafts_Rescan @webDb , @dlbStatus,  @truckNo, @driverName";
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

        // loop thru the company and get the doc this driver scan in
        IActionResult CheckDriverRescanDlb_ByGuid(Dto_Rescan dto)
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
                                    // query the sap transfer doc 
                                    // load heading 
                                    var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                                    var transfer = conn.Query<OWTR_Ext>(query_transfer,
                                        new
                                        {
                                            webDb = db.WEBDB,
                                            transferDocNum = foundDoc.DocNum
                                        }).FirstOrDefault();

                                    if (transfer == null) return NotFound();

                                    // load lines
                                    var query_transferLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                       Where DocEntry = @DocEntry";

                                    transfer.Lines = conn.Query<WTR1_Ext>(query_transferLine, new { transfer.DocEntry }).ToList();

                                    // load box 
                                    // 20230518
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
                                        return BadRequest($"{db.COMPANYNAME}, Transfer #{transfer.DocNum} from {dto.Subsi}, Error query for boxes.");
                                    }

                                    // check the box loading status 
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

                                    transfer.SubSi = db.COMPANYNAME;
                                    transfer.SubsiId = db.COMPANYID;

                                    foundDoc.Transfer = transfer;
                                    foundDoc.DocDate = transfer.DocDate;
                                    foundDoc.DocTotal = transfer.DocTotal;
                                    foundDoc.CartonNo = transfer.Boxes.Count;

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

        IActionResult SaveDLB_ByDriver_ReScan(Dto_Rescan dto)
        {
            try
            {
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
                for (int c = 0; c < companies.Count; c++)
                {
                    var subsi = companies[c];
                    if (string.IsNullOrWhiteSpace(subsi)) continue;
                    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, subsi);
                    if (db == null) continue;

                    var qr_dlb = @$"Select * 
                                    from {db.WEBDB}..FTAPP_DLB with (nolock)
                                    Where HeadGuid = @headerGuid ";

                    using var conn = new SqlConnection(_commDbConnStr);
                    var dlb = conn.Query<FTAPP_DLB>(qr_dlb, new
                    {
                        headerGuid = dto.SaveHeadGuid
                    }).FirstOrDefault();

                    if (dlb == null)
                    {
                        return BadRequest("DLB no found, please try again later.");
                    }
                    if (dlb.DLBStatus != "D")
                    {
                        return BadRequest("DLB already posted as OUT.");
                    }

                    // no dlb lines for this company
                    var compLines = dto.Dlb1.Where(c => c.SubSi == subsi).ToList();
                    if (compLines.Count == 0)
                    {
                        continue;
                    }
                    dlb.Remarks = dto.Remarks;
                    dlb.NRIC = dto.Nric;

                    if (string.IsNullOrWhiteSpace(dlb.WhsUserCode))
                    {
                        dlb.WhsUserCode = dto.UserCode;
                    }

                    var helper = new DLBHelper(db, dto.SaveHeadGuid, dlb.TruckNo);
                     //var isReScan = true;
                    var dlbDocEntry = helper.CreateDLB(dlb, compLines, dto.UserCode, dto.UserName,
                        dto.IsInterbranch); //, isReScan);

                    if (dlbDocEntry == -1)
                    {
                        return BadRequest(helper.Error);
                    }

                    var dlbRepliedDoc = new
                    {
                        DLBEntry = dlbDocEntry,
                        DocStatus = "Success, In-transit",
                        SubSi = db.COMPANYNAME
                    };

                    dlbRepliedDocs.Add(dlbRepliedDoc);

                    //var test = dto.Dlb1;

                    // handler the dlb is rescan entry .. 
                    // update the DLB1 to status of R with and dl doc entry
                    // doc number and doc type 
                    var filterRescanDlbs = compLines.Where(x => x.IsReScan).ToList();
                    if (filterRescanDlbs.Count == 0) continue; // company loop

                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();

                    try
                    {
                        for (int rc = 0; rc < filterRescanDlbs.Count; rc++)
                        {
                            //check before the update
                            var sp_query = @$"select * from {db.WEBDB}..DLB1 with (NOLOCK) 
                                            where docentry = @docentry
                                            and doctype = @doctype
                                            and docnum = @docnum ";

                            var dlb1_res = conn.Query<DLB1>(sp_query, new
                            {
                                docentry = filterRescanDlbs[rc].LastDlbEntry,
                                doctype = filterRescanDlbs[rc].DocType,
                                docnum = filterRescanDlbs[rc].DocNum
                            }, trans).FirstOrDefault();

                            // update only when pre dlb1 is status = O
                            if (dlb1_res != null && dlb1_res.STATUS == "O")
                            {
                                var sp_update = @$"Update {db.WEBDB}..DLB1 
                                          set Status = @status
                                              ,UMODIFIED = @uModified
                                              ,DMODIFIED = GETDATE()
                                          where docentry = @LastDlbEntry
                                          and DocNum = @DocNum 
                                          and DocType = @DocType";

                                conn.Execute(sp_update, new
                                {
                                    status = "R",
                                    uModified = "SVR",
                                    LastDlbEntry = filterRescanDlbs[rc].LastDlbEntry,
                                    DocNum = filterRescanDlbs[rc].DocNum,
                                    DocType = filterRescanDlbs[rc].DocType
                                }, trans);
                            }
                        }

                        trans.Commit();
                    }
                    catch (Exception e) // for update rescan dlb
                    {
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        trans.Rollback();
                    }

                } // end loop of companies

                return Ok(dlbRepliedDocs); // list of created dlb docs
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

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

        IActionResult VerifyAndUpdateDriverDoc_ReScan(Dto_Rescan dto)
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
            if (dto.SaveHeadGuid == default)
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
                                    and HeadGuid = @SaveHeadGuid";

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var foundDoc = conn.Query<FTAPP_DLB1>(query_box, new
                {
                    dto.DocNum,
                    dto.SaveHeadGuid,
                    dto.DocType
                }, trans).FirstOrDefault();

                if (foundDoc == null)
                {
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

        IActionResult RemoveDeliveryDoc_ReScan(Dto_Rescan dto)
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
            if (dto.SaveHeadGuid == default)
            {
                return BadRequest("Invalid saved guid");
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
                var query_dlb1 = @$"select * from {db.WEBDB}..FTAPP_DLB1 
                                    Where DocNum = @DocNum 
                                        and DocType = @docType ";

                var dlb1 = conn.Query<FTAPP_DLB1>(query_dlb1, new { DocNum = dto.DocNum, docType = dto.DocType }, trans).FirstOrDefault();
                if (dlb1 == null)
                {
                    trans.Commit();
                    return BadRequest("Invalid query of dlb doc from database, please try again.");
                }

                // boxes 
                var sp_delete = @$"Delete from {db.WEBDB}..FTAPP_DLB2 
                                    Where convert( nvarchar(50) ,HeadGuid) = @hguid";

                conn.Execute(sp_delete, new { hguid = $"{dlb1.HeadGuid}" }, trans);


                // added doc 
                var sp_delete1 = @$"Delete from {db.WEBDB}..FTAPP_DLB1 
                                    Where convert( nvarchar(50) ,HeadGuid) = @hguid";

                conn.Execute(sp_delete1, new { hguid = $"{dlb1.HeadGuid}" }, trans);

                // remove onhold dlb doc  
                var sp_delete2 = @$"Delete from {db.WEBDB}..FTAPP_HoldDlvryDocs
                                    Where DocNum = @DocNum
                                    and DocType = @DocType";

                conn.Execute(sp_delete2, new { dto.DocNum, dto.DocType }, trans);

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

        //IActionResult RemoveDeliverySaveDraft_Rescan(Dto_Rescan dto)
        //{
        //    try
        //    {
        //        if (dto.SaveHeadGuid == default)
        //        {
        //            return BadRequest("Invalid guid");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.DriverName))
        //        {
        //            return BadRequest("Invalid driver name");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.PlateNo))
        //        {
        //            return BadRequest("Invalid plate no");
        //        }

        //        var dbs = new DbNameHelper().GetDbInfo_DeliveryApp(_commDbConnStr);
        //        for (int i = 0; i < dbs.Count; i++)
        //        {
        //            var db = dbs[i];
        //            if (db == null)
        //            {
        //                return BadRequest("invalid dbi");
        //            }

        //            // query the dlb 
        //            // if dlb is empty tehn by pass the the dlb
        //            var query_dlb = @$"select * from {db.WEBDB}..FTAPP_DLB Where HeadGuid = @HeadeGuid";
        //            using var conn = new SqlConnection(_commDbConnStr);
        //            var dlb = conn.Query<FTAPP_DLB>(query_dlb, new
        //            {
        //                HeadeGuid = dto.SaveHeadGuid
        //            }).FirstOrDefault();

        //            if (dlb == null) continue; // continue next db

        //            // query list of doc num under this head guis 
        //            var query_docs = @$"select * from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @HeadeGuid";
        //            var dlb1s = conn.Query<FTAPP_DLB1>(query_docs, new
        //            {
        //                HeadeGuid = dto.SaveHeadGuid
        //            }).ToList();

        //            // having doc in it 
        //            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
        //            using var trans = conn.BeginTransaction();
        //            try
        //            {
        //                // delete each doc 
        //                for (int d = 0; d < dlb1s.Count; d++)
        //                {
        //                    var doc = dlb1s[d];
        //                    if (doc == null) continue;

        //                    if (doc.DocType == "I") // delete the boxes for invoice
        //                    {
        //                        var delete_sql = @$"delete from {db.WEBDB}..FTAPP_DLB2 
        //                                            Where InvDocNum = @DocNum 
        //                                            and HeadGuid = @HeadeGuid ";

        //                        conn.Execute(delete_sql, new
        //                        {
        //                            DocNum = doc.DocNum,
        //                            HeadeGuid = dto.SaveHeadGuid
        //                        }, trans);
        //                    }

        //                    // delete the on hold docment 
        //                    var delete_onhold = @$"delete from {db.WEBDB}..FTAPP_HoldDlvryDocs 
        //                                           where DocNum = @DocNum 
        //                                           and DocType = @DocType 
        //                                           and UserCode = @PlateNo
        //                                           and UserName = @DriverName
        //                                           and HeadGuid = @HeadeGuid";

        //                    conn.Execute(delete_onhold, new
        //                    {
        //                        DocNum = doc.DocNum,
        //                        DocType = doc.DocType,
        //                        Plateno = dto.PlateNo,
        //                        DriverName = dto.DriverName,
        //                        HeadeGuid = dto.SaveHeadGuid
        //                    }, trans);
        //                }

        //                // delete the doc 
        //                var delete_query_doc = @$"delete from {db.WEBDB}..FTAPP_DLB1 Where HeadGuid = @HeadeGuid";
        //                conn.Execute(delete_query_doc, new { HeadeGuid = dto.SaveHeadGuid }, trans);

        //                // delete the head 
        //                var delete_query_doc_head = @$"delete from {db.WEBDB}..FTAPP_DLB Where HeadGuid = @HeadeGuid";
        //                conn.Execute(delete_query_doc_head, new { HeadeGuid = dto.SaveHeadGuid }, trans);

        //                trans.Commit();
        //                continue;
        //            }
        //            catch (Exception e)
        //            {
        //                trans.Rollback();
        //                LastError = $"{e.Message}\n{e.StackTrace}";
        //                _logger.LogError(LastError);
        //                return BadRequest($"request not handler.\n{LastError}");
        //            }
        //        }

        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult CheckDriverDlb_Rescan(Dto_Rescan dto)
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

        IActionResult SaveDLB1_ReScan(Dto_Rescan dto)
        {
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
                    // delete the head 
                    var sp_delete = @$"delete from {db.WEBDB}..FTAPP_DLB 
                                  Where HeadGuid = @HeadGuid";

                    var res = conn.Execute(sp_delete, new { dto.Dlb.HeadGuid }, trans);

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
                                               , IsReScan
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
                                          ,@IsReScan
                                    )";

                    // 20221014 
                    // Add in isRescan column
                    res = conn.Execute(sp_insert, dto.Dlb, trans);

                    // get the new line to insert 
                    var newInsertLines = dto.Dlb1.Where(x => $"{x.SaveAs}".Equals("savenew")).ToList();
                    if (newInsertLines.Count > 0)
                    {   // insert then lines
                        
                        var newListDoc = new List<FTAPP_DLB1>();
                        for (int g= 0; g < newInsertLines.Count; g++)
                        {
                            var doc = newInsertLines[g];
                            if (doc == null) continue;

                            var dupliCheck_sp = @$"Select * 
                                                   From {db.WEBDB}..FTAPP_DLB1 
                                                   Where HeadGuid = @HeadGuid 
                                                     and DocNum = @DocNum 
                                                     and DocType = @DocType ";

                            var found = conn.Query<FTAPP_DLB1>(dupliCheck_sp, new
                            {
                                dto.Dlb.HeadGuid, 
                                doc.DocNum,
                                doc.DocType,

                            }, trans).FirstOrDefault();

                            if (found == null)
                            {
                                newListDoc.Add(doc);
                            }
                        }

                        if (newListDoc != null && newListDoc.Count > 0 )
                        {
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
                                               , IsReScan
                                               , LastDlbEntry
                                               , ToWhsCode
                                               , ToWhsName 
                                               , IBTEntry
                                               , App_Determined_IsInterbranch, GeoType, GeoCode
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
                                           ,@IsReScan
                                           ,@LastDlbEntry
                                           ,@ToWhsCode
                                           ,@ToWhsName
                                           ,@IBTEntry
                                           ,@App_Determined_IsInterbranch, @GeoType, @GeoCode
                                )";

                            var res1 = conn.Execute(sp_insert1, newListDoc, trans);
                        }
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
                                               , IsReScan = @IsReScan
                                               , LastDlbEntry = @LastDlbEntry
                                               , App_Determined_IsInterbranch = @App_Determined_IsInterbranch
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
        }

        IActionResult VerifyDlbReScanDoc_Transfer(Dto_Rescan dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid doc number");
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

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                var conn1 = new SqlConnection(_commDbConnStr);

                if (dto.IsAgedInvoice)
                {
                    goto ProcessNormalCheck;
                }              

                // 20240202
                // query check to got any aged invoice 

                var sp_checkInvWhs = @$"Select FROMWHS from {db.WEBDB}..IBT Where TRANSITNO = @docNum";
                var whsCode = conn1.Query<string>(sp_checkInvWhs, new { docNum = dto.DocNum }).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(whsCode))
                {
                    goto ProcessNormalCheck;
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

                // 20240331
                if (db.ISJAMWHS_CHECK == null) db.ISJAMWHS_CHECK = "N";
                if ($"{db.ISJAMWHS_CHECK}".ToLower().Equals("n"))
                {
                    goto PerformAgedIbtCheck;
                }

                // check the whs is jam or not 
                // 20240329                                                   
                var sp_QueryJamConfition = @"exec sp_GetIntruckDt_IBT_wAged @webDb,  @whsCode ,@aaged ";
                var Whses = conn1.Query<JamWhs>(sp_QueryJamConfition, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aaged = agedDay
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
                        $"No loading allow until aged IBT delivered.");
                }

                PerformAgedIbtCheck:

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
                var sp_CheckAgedInv_InsWhs = @"exec sp_GetOldestAgedInv_TransWhs_v2 @webDb , @whsCode, @aagedDay  ";
                var agedInvs_InWhs = conn1.Query<AgedDoc>(sp_CheckAgedInv_InsWhs, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }).ToList();

                if (agedInvs_InWhs.Count > 0)
                {
                    newAgedDocs.AddRange(agedInvs_InWhs);
                }

                // remove duplicated 
                newAgedDocs = newAgedDocs.GroupBy(g => g.DocNum)
                    .Select(i => i.FirstOrDefault()).ToList();


                // check aged ibt
                var sp_CheckAgedIBT = @"exec sp_GetOldestAgedIBT_v2 @webDb , @whsCode, @aagedDay  ";
                var agedIbts = conn1.Query<AgedDoc>(sp_CheckAgedIBT, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }).ToList();

                if (agedIbts.Count > 0)
                {
                    newAgedDocs.AddRange(agedIbts);
                }

                if (newAgedDocs.Count == 0)
                {
                    goto ProcessNormalCheck;
                }

                // Find the oldest calendar date
                var oldestDate = newAgedDocs.Min(d => d.DocDate.Date);

                // Select all docs that fall on that date, optionally order them for display
                var oldestDocs = newAgedDocs
                    .Where(d => d.DocDate.Date == oldestDate)
                    .OrderBy(d => d.DocDate) // if you want time-of-day order within that date
                    .ToList();

                var dto1 = new Dto_AgedDoc
                {
                    AgedDocs = oldestDocs,
                    Invoice = null
                };

                return Ok(dto1);



            // 20240524 
            // add select the top 10 record to driver 
            //var showNumAgedRec_sp = "select setupValue from ktcw_common..FTApp_Config Where SetupName = 'NumOfAgedDocShow'";
            //int showNumAgedRec = conn1.ExecuteScalar<int>(showNumAgedRec_sp);

            //if (showNumAgedRec == 0)
            //{
            //    showNumAgedRec = 10;
            //}

            //// show the top 10 record 
            //newAgedDocs = new List<AgedDoc>(newAgedDocs.OrderBy(d => d.DocDate).Take(showNumAgedRec));


            //    // get the older dates 
            //    // 20240520
            //    var olderDate = newAgedDocs.OrderBy(d => d.DocDate).First().DocDate;
            //    if (olderDate == default)
            //    {
            //        goto ProcessNormal;
            //    }

            //    var olderDatesList = newAgedDocs.Where(d => d.DocDate.Date == olderDate.Date).Distinct().ToList();
            //    newAgedDocs = new List<AgedDoc>(olderDatesList);

            //ProcessNormal:

            // mix the invoie anf ibt base on aged
            //newAgedDocs = newAgedDocs.OrderBy(a => a.DayAged).ToList();
            //if (newAgedDocs.Count > 0)
            //{
            //    var newDto1 = new Dto_AgedDoc
            //    {
            //        AgedDocs = newAgedDocs,
            //        Transfer = null
            //    };
            //    return Ok(newDto1); // return to the app 
            //}

            ProcessNormalCheck:

                var sp_query = $@"select t0.* 
                                  from {db.WEBDB}..DLB1 t0 with (nolock)
                                  Where t0.docnum = @docnum 
                                  and t0.doctype = @doctype
                                  order by t0.DocEntry desc";
                
                var dlb1s = conn1.Query<DLB1>(sp_query, new
                {
                    docNum = dto.DocNum,
                    docType = dto.DocType = "T"
                }).ToList();

                // ibt no found
                if (dlb1s.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer # {dto.DocNum} no found in DLB records. " +
                                      $"Or please use new DLB to add");
                }
                
                // check allow dlb 
                var dlb1 = dlb1s.First();
                if (dlb1 == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer # {dto.DocNum} no found in DLB records. [FN]");
                }

                if ($"{dlb1.STATUS}".ToLower() == "o")
                {
                    return BadRequest($"Transfer {dto.DocNum} was in OUT status, no rescan allow");
                }

                // check onhold and hold it 
                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @invoiceno and Doctype = @doctype";

                var InvOnHold = conn1.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    invoiceno = dto.DocNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (InvOnHold != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer #{dto.DocNum} was hold in loading transaction by {InvOnHold.UserName}" +
                      $" please try again later. [Onhold]");
                }

                var isOnhOld = PutDocOnHold(db, dto); // transfer
                if (!isOnhOld)
                {
                    return BadRequest($"transfers #{dto.DocNum} put onhold fail. " +
                      $"Please try again later. [Onhold-Fail]");
                }


                // query the sap transfer doc 
                var query_transfer = @"exec sp_GetIBTTransferDoc @webDb, @transferDocNum";
                var transfer = conn1.Query<OWTR_Ext>(query_transfer,
                    new
                    {
                        webDb = db.WEBDB,
                        transferDocNum = dto.DocNum
                    }).FirstOrDefault();

                if (transfer == null) return NotFound();

                var query_transferLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                                       Where DocEntry = @DocEntry";

                transfer.Lines = conn1.Query<WTR1_Ext>(query_transferLine, new { transfer.DocEntry }).ToList();

                // 20230518
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

                transfer.Boxes = conn1.Query<FTAPP_Box>(query_box, new { IbtDocNum = transfer.DocNum }).ToList();
                if (transfer.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Transfer #{transfer.DocNum} from {dto.Subsi}, Error query for boxes.");
                }

                transfer.SubSi = db.COMPANYNAME;
                transfer.SubsiId = db.COMPANYID;                
                transfer.LastDLBEntry = (int)dlb1.DOCENTRY;
                var newDto = new Dto_AgedDoc
                {
                    AgedDocs = null,
                    Transfer = transfer
                };
                return Ok(newDto); // return to the app 
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        bool PutDocOnHold(DbInfo db, Dto_Rescan dto)
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

        // new dlb rescan - invoice
        IActionResult VerifyDlbReScanDoc_Inv(Dto_Rescan dto)
        {
            int agedDay = 3;

            try
            {
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid invoice number");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
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

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                if (dto.IsAgedInvoice)
                {
                    goto ProcessNormalCheck;
                }


                // handler the aged 
                // 20240202

                // query check to got any aged invoice 
                //var sp_checkInvWhs = @$"Select WhsCode from {db.WEBDB}..SO Where INVNO = @invNo";               
                //var whsCode = conn.Query<string>(sp_checkInvWhs, new { invNo = dto.DocNum }).FirstOrDefault();

                var sp_checkInvWhs = $@"exec sp_QueryWhsActualLoc @webDb , @invno ";
                var whsCode = conn.Query<string>(sp_checkInvWhs, new { webDb = db.WEBDB, invno = dto.DocNum }).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(whsCode))
                {
                    goto ProcessNormalCheck;
                }

                // 20240331
                if (db.ISJAMWHS_CHECK == null) db.ISJAMWHS_CHECK = "N";
                if ($"{db.ISJAMWHS_CHECK}".ToLower().Equals("n") )
                {
                    goto PerformAgedInvCheck;
                }

                // 20240323 
                // get the default whs aged day 
                var sp_defaultAgedday = $@"select setupValue 
                                        from KTCW_COMMON..FTApp_Config 
                                        Where SetupName = 'DeliveryAppPickInvoiceAgedInv'";

                var defaultAgedDay = conn.ExecuteScalar<int>(sp_defaultAgedday);

                // 20240323 
                // query the whs custom aged day
                var sp_WhsAgedDay = @$"select ISNULL(U_WhsAgedDocDay, {defaultAgedDay}) 
                                    from  {db.SAPDB}..OWHS 
                                    where WhsCode = @whsCode ";

                agedDay = conn.ExecuteScalar<int>(sp_WhsAgedDay, new { whsCode = whsCode });

                // check the whs is jam or not 
                // 20240329                                                   
                var sp_QueryJamConfition = @"exec sp_GetIntruckDt_wAgedDay @webDb,  @whsCode, @aaged ";
                var Whses = conn.Query<JamWhs>(sp_QueryJamConfition, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aaged = agedDay
                }).ToList();

                if (Whses.Count > 0) // got invoice delivery over the grade period 
                {

                    if ($"{Whses.First().IsJam}".ToLower() != "Y")
                    {
                        // update the sap whs as Y 
                        var sp_UppdateSapOWHS = @$"Update {db.SAPDB}..OWHS
                                                   set U_JAMWHS = 'Y'
                                                    where whscode = @whsCode";
                        conn.Execute(sp_UppdateSapOWHS, new { whsCode });
                    }

                    return BadRequest($"{db.COMPANYNAME}, Warehouse {whsCode} in JAM /BLOCKED, " +
                        $"No loading allow until aged invoice delivered.");
                }

                PerformAgedInvCheck:                

                // check aged invoices
                //var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1 @webDb , @whsCode  ";
                var sp_CheckAgedInv = @"exec sp_GetOldestAgedInv_v1_partA  @webDb , @whsCode, @aagedDay ";
                var agedInvs = conn.Query<AgedDoc>(sp_CheckAgedInv, new
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
                agedInvs = conn.Query<AgedDoc>(sp_CheckAgedInv, new
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
                // check for aged in transit warehouse 
                var sp_CheckAgedInv_InsWhs = @"exec sp_GetOldestAgedInv_TransWhs_v2 @webDb , @whsCode , @aagedDay ";
                var agedInvs_InWhs = conn.Query<AgedDoc>(sp_CheckAgedInv_InsWhs, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
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
                var sp_CheckAgedIBT = @"exec sp_GetOldestAgedIBT_v2 @webDb , @whsCode , @aagedDay ";
                var agedIbts = conn.Query<AgedDoc>(sp_CheckAgedIBT, new
                {
                    webDb = db.WEBDB,
                    whsCode = whsCode,
                    aagedDay = agedDay
                }).ToList();

                if (agedIbts.Count > 0)
                {
                    newAgedDocs.AddRange(agedIbts);
                }
                if (newAgedDocs.Count == 0)
                {
                    goto ProcessNormalCheck;
                }

                // Find the oldest calendar date
                //var oldestDate = newAgedDocs.Min(d => d.DocDate.Date);

                //// Select all docs that fall on that date, optionally order them for display
                //var oldestDocs = newAgedDocs
                //    .Where(d => d.DocDate.Date == oldestDate)                    
                //    .ToList();


                /// 20251229
                // Find the oldest calendar date
                //var oldestDate = newAgedDocs.Min(d => d.DocDate.Date);
                
                // 20260203
                var oldest = newAgedDocs.Max(d => d.DayAged);

                // Select all docs that fall on that date, optionally order them for display
                var oldestDocs = newAgedDocs
                    .Where(d => d.DayAged == oldest)
                    .ToList();
                
                var dto1 = new Dto_AgedDoc
                {
                    AgedDocs = oldestDocs,
                    Invoice = null
                };
                return Ok(dto1);

            // 20240524 
            // add select the top 10 record to driver 
            //var showNumAgedRec_sp = "select setupValue from ktcw_common..FTApp_Config Where SetupName = 'NumOfAgedDocShow'";
            //int showNumAgedRec = conn.ExecuteScalar<int>(showNumAgedRec_sp);
            //if (showNumAgedRec == 0)
            //{
            //    showNumAgedRec = 10;
            //}

            // show the top 10 record 
            //newAgedDocs = new List<AgedDoc>(newAgedDocs.OrderBy(d => d.DocDate).Take(showNumAgedRec));


            // 20240520
            // get the older dates 
            //var olderDate = newAgedDocs.OrderBy(d => d.DocDate).First().DocDate;
            //if (olderDate == default)
            //{
            //    goto ProcessNormal;
            //}

            //var olderDatesList = newAgedDocs.Where(d => d.DocDate.Date == olderDate.Date).Distinct().ToList();
            //newAgedDocs = new List<AgedDoc>(olderDatesList);

            //ProcessNormal:
            // mix the invoie anf ibt base on aged
            //newAgedDocs = newAgedDocs.OrderBy(a => a.DayAged).ToList();
            //if (newAgedDocs.Count > 0)
            //{
            //    var newDto = new Dto_AgedDoc
            //    {
            //        AgedDocs = newAgedDocs,
            //        Invoice = null
            //    };
            //    return Ok(newDto); // return to the app 
            //}

            ProcessNormalCheck:
                var sp_query = $@"select t0.* 
                                  from {db.WEBDB}..DLB1 t0 with (nolock)
                                  Where t0.docnum = @docnum 
                                  and t0.doctype = @doctype
                                  order by t0.DocEntry desc";
                
                var dlb1s = conn.Query<DLB1>(sp_query, new { docnum = dto.DocNum, doctype = dto.DocType }).ToList();


                if (dto.IsAgedInvoice) // 20240207
                {
                    goto ByPassCHeckingDlbStatus;
                }

                // invoice no found
                if (dlb1s.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} no found in DLB records.");
                }

                // invoice status in O 
                var oStatus = dlb1s.Where(d => d.STATUS == "O").FirstOrDefault();
                if (oStatus != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} found OUT status, rescan no allowed.");
                }

                ByPassCHeckingDlbStatus:

                // special check from sap invoice , lines item master and biz card
                var sp_checkAgencyAllowrescan = @$"select ISNULL(t3.QryGroup1,'N') [IsAllowDlbReScan] 
                                                from {db.SAPDB}..OINV t0 with (nolock)
                                                inner join {db.SAPDB}..INV1 t1 with (nolock) on t1.DocEntry = t0.DocEntry
                                                inner join {db.SAPDB}..OITM t2 with (nolock) on t2.ItemCode = t1.ItemCode 
                                                inner join {db.SAPDB}..OCRD t3 with (nolock) on t3.CardCode = t2.CardCode
                                                Where t0.DocNum = @InvoiceNum";

                string isAgencyAllowRescan = conn.ExecuteScalar<string>(sp_checkAgencyAllowrescan, new { InvoiceNum = dto.DocNum });

                // agency in bp sap set to N or null
                if ($"{isAgencyAllowRescan}".Equals("N"))
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} no allow in DLB Rescan. [BP Agency Prop]");
                }

                if (dlb1s.Count == 0) // 20240119
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} no found in DLB records. [FN0]");
                }

                // check allow dlb 
                var dlb1 = dlb1s.First();
                if (dlb1 == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} no found in DLB records. [FN]");
                }

                if ($"{dlb1.STATUS}".ToLower() == "o")
                {
                    return BadRequest($"Invoice {dto.DocNum} was in Out status, no rescan allow");
                }

                // 20241014
                // for checking any draft 
                // hold by 
                var sp_checkDraftExit = $"exec sp_CheckDLBInDraft @webDb , @docType, @docStatus , @docNum ";
                var foundDlbDraft_Inv = conn.Query<FTAPP_DLB1>(sp_checkDraftExit, new
                {
                    webDb = db.WEBDB,
                    docType = "I",
                    docStatus = "D",
                    docNum = dto.DocNum
                }).FirstOrDefault();

                if (foundDlbDraft_Inv != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} found draft for DLB. " +
                       $"Draft By {foundDlbDraft_Inv.DriverName} {foundDlbDraft_Inv.TruckNo} " +
                       $"Draft Id {foundDlbDraft_Inv.DraftID} When {foundDlbDraft_Inv.OutTransDt:dd-MMM-yy hh:mm tt}, Thanks.");
                }

                // check onhold 
                // check onhold and hold it 
                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @invoiceno and Doctype = @doctype";

                var InvOnHold = conn.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    invoiceno = dto.DocNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (InvOnHold == null) // if no hold then put it onhold 
                {
                    var isOnhOld = PutDocOnHold(db, dto); // invoice
                    if (!isOnhOld)
                    {
                        return BadRequest($"transfers #{dto.DocNum} put onhold fail. " +
                          $"Please try again later. [Onhold-Fail]");
                    }
                }

                // 20230617
                //if (InvOnHold != null) // already hold 
                //{
                //    goto ProcessAfterHold;

                //    //return BadRequest($"{db.COMPANYNAME}, Invoice #{dto.DocNum} was hold in loading transaction by {InvOnHold.UserName}" +
                //    //  $" please try again later. [Onhold]");
                //}
                //else 
                //{
                //    var isOnhOld = PutDocOnHold(db, dto); // invoice
                //    if (!isOnhOld)
                //    {
                //        return BadRequest($"transfers #{dto.DocNum} put onhold fail. " +
                //          $"Please try again later. [Onhold-Fail]");
                //    }
                //}
                
                //ProcessAfterHold:

                // process the invoice 
                // return ok when all status 
                // get the invoice from sap
                // 
                //var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";

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
                                where t2.DocNum = @docnum

                                        -- LEN(ISNULL(U_DROPPOINT, '')) > 0 
                                        -- and t2.DocNum = @docnum ";
              
                OINV inv = conn.Query<OINV>(query_inv, new { docnum = dto.DocNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for sap invoice.");
                }

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
                inv.LastDlbEntry = (int)dlb1.DOCENTRY;

                // using dto to send back information
                var newDto1 = new Dto_AgedDoc
                {
                    AgedDocs = null,
                    Invoice = inv
                };
                return Ok(newDto1); // return to the app 
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // new dlb rescan cog
        IActionResult VerifyDlbReScanDoc_Cog(Dto_Rescan dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DocNum))
                {
                    return BadRequest("Invalid cog number");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
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

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_query = $@"select t0.* 
                                  from {db.WEBDB}..DLB1 t0 with (nolock)
                                  Where t0.docnum = @docnum 
                                  and t0.doctype = @doctype
                                  order by t0.DocEntry desc";

                using var conn = new SqlConnection(_commDbConnStr);
                var dlb1s = conn.Query<DLB1>(sp_query, new { docnum = dto.DocNum, doctype = dto.DocType }).ToList();

                // cog no found
                if (dlb1s.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, COG # {dto.DocNum} no found in DLB records.");
                }

                // invoice status in O 
                var oStatus = dlb1s.Where(d => d.STATUS == "O").FirstOrDefault();
                if (oStatus != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, COG # {dto.DocNum} found OUT status, rescan no allowed.");
                }

                // checking to see does it apply to COG as well
                // special check from sap invoice , lines item master and biz card
                //var sp_checkAgencyAllowrescan = @$"select ISNULL(t3.QryGroup1,'N') [IsAllowDlbReScan] 
                //                                from {db.SAPDB}..OINV t0 with (nolock)
                //                                inner join {db.SAPDB}..INV1 t1 with (nolock) on t1.DocEntry = t0.DocEntry
                //                                inner join {db.SAPDB}..OITM t2 with (nolock) on t2.ItemCode = t1.ItemCode 
                //                                inner join {db.SAPDB}..OCRD t3 with (nolock) on t3.CardCode = t2.CardCode
                //                                Where t0.DocNum = @CogNum";

                //string isAgencyAllowRescan = conn.ExecuteScalar<string>(sp_checkAgencyAllowrescan, new { CogNum = dto.DocNum });

                //// agency in bp sap set to N or null
                //if ($"{isAgencyAllowRescan}".Equals("N"))
                //{
                //    return BadRequest($"{db.COMPANYNAME}, Invoice # {dto.DocNum} no allow in DLB Rescan. [BP Agency Prop]");
                //}

                // check allow dlb 
                var dlb1 = dlb1s.First();
                if (dlb1 == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, COG # {dto.DocNum} no found in DLB records. [FN]");
                }

                if ($"{dlb1.STATUS}".ToLower() == "o")
                {
                    return BadRequest($"COG {dto.DocNum} was in Out status, no rescan allow");
                }

                // check onhold and hold it 
                // check invoice in other bay loading 
                var sp_check = $@"Select * 
                                from {db.WEBDB}..FTAPP_HoldDlvryDocs with (nolock) 
                                where DocNum = @invoiceno and Doctype = @doctype";

                var InvOnHold = conn.Query<FTAPP_HoldDlvryDocs>(sp_check, new
                {
                    invoiceno = dto.DocNum,
                    doctype = dto.DocType
                }).FirstOrDefault();

                if (InvOnHold != null)
                {
                    return BadRequest($"{db.COMPANYNAME}, COG #{dto.DocNum} was hold in loading transaction by {InvOnHold.UserName}" +
                      $" please try again later. [Onhold]");
                }

                var isOnhOld = PutDocOnHold(db, dto); // transfer
                if (!isOnhOld)
                {
                    return BadRequest($"transfers #{dto.DocNum} put onhold fail. " +
                      $"Please try again later. [Onhold-Fail]");
                }

                var query_cog = @$"Select * 
                                   , (Select sum(LineTotal) from {db.WEBDB}..COG1 with (nolock)
                                        where docentry = @docNum) [DocTotal]
                                   from {db.WEBDB}..COG with (nolock)
                                   Where DocEntry = @docNum";

                var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                if (cog == null) return NotFound();

                var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                                   Where DocEntry = @docEntry";

                cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = dto.DocNum }).ToList();
                cog.SubSi = db.COMPANYNAME;

                cog.LastDLBEntry = (int)dlb1.DOCENTRY;

                return Ok(cog);

                //switch (dlb1.DOCTYPE)
                //{
                //    case "I":
                //        {
                //            if ($"{dlb1.STATUS}".ToLower() == "o")
                //            {
                //                return BadRequest($"Invoice {dto.DocNum} was in Out status, no rescan allow");
                //            }

                //            // process the invoice 
                //            // return ok when all status 
                //            // get the invoice from sap
                //            // 
                //            var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                //            OINV inv = conn.Query<OINV>(query_inv, new { docnum = dto.DocNum }).FirstOrDefault();
                //            if (inv == null)
                //            {
                //                return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for sap invoice.");
                //            }

                //            // get the box list from web portal
                //            //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                //            //        Where baseentry = @baseentry";

                //            // get the box list from web portal
                //            var query_box = $@"select DISTINCT t0.*
                //                from {db.WEBDB}..FTAPP_Box t0 with (nolock)
                //                left join  {db.WEBDB}..FTAPP_Box1 t1  with (nolock) on t0.BoxGuid = t1.BoxGuid
                //                Where t0.BaseEntry = @baseentry 
                //                and t1.BoxGuid is not null";


                //            inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                //            if (inv.Boxes.Count == 0)
                //            {
                //                return BadRequest($"Invoice #{dto.DocNum} from {dto.Subsi}, Error query for boxes.");
                //            }

                //            inv.Subsi = db.COMPANYNAME;
                //            inv.SubsiId = db.COMPANYID;
                //            inv.LastDlbEntry = (int)dlb1.DOCENTRY;

                //            return Ok(inv);
                //        }
                //        //case "C":
                //        //    {
                //        //        if ($"{dlb1.STATUS}".ToLower() == "o")
                //        //        {
                //        //            return BadRequest($"COG {dto.DocNum} was in Out status, no rescan allow");
                //        //        }

                //        //        var query_cog = @$"Select * 
                //        //               , (Select sum(LineTotal) from {db.WEBDB}..COG1 with (nolock)
                //        //                    where docentry = @docNum) [DocTotal]
                //        //               from {db.WEBDB}..COG with (nolock)
                //        //               Where DocEntry = @docNum";

                //        //        var cog = conn.Query<COG_Doc>(query_cog, new { docNum = dto.DocNum }).FirstOrDefault();
                //        //        if (cog == null) return NotFound();

                //        //        var query_CogLine = @$"Select * from {db.WEBDB}..COG1 with (nolock)
                //        //               Where DocEntry = @docEntry";

                //        //        cog.LINES = conn.Query<COG_Line>(query_CogLine, new { docEntry = dto.DocNum }).ToList();
                //        //        cog.SubSi = db.COMPANYNAME;

                //        //        cog.LastDLBEntry = (int)dlb1.DOCENTRY;

                //        //        return Ok(cog);
                //        //    }
                //        //case "T":
                //        //    {
                //        //        if ($"{dlb1.STATUS}".ToLower() == "o")
                //        //        {
                //        //            return BadRequest($"Transfer {dto.DocNum} was in Out status, no rescan allow");
                //        //        }

                //        //        var query_transfer = @$"Select * from {db.SAPDB}..OWTR with (nolock)
                //        //               Where DocNum = @docNum";

                //        //        var transfer = conn.Query<OWTR_Ext>(query_transfer, new { docNum = dto.DocNum }).FirstOrDefault();
                //        //        if (transfer == null) return NotFound();

                //        //        var query_cogLine = @$"Select * from {db.SAPDB}..WTR1 with (nolock)
                //        //               Where DocEntry = @DocEntry";

                //        //        transfer.Lines = conn.Query<WTR1_Ext>(query_cogLine, new { transfer.DocEntry }).ToList();
                //        //        transfer.SubSi = db.COMPANYNAME;

                //        //        transfer.LastDLBEntry = (int)dlb1.DOCENTRY;
                //        //        return Ok(transfer);
                //        //    }
                //}

                //return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}
