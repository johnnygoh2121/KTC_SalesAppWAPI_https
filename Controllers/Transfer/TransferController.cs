using Dapper;
using KTC_SalesAppWAPI.DTOs.Transfer;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.Delivery;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Transfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Transfer
{
    [Route("[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<TransferController> _logger;
        string _commDbConnStr = "";
        string LastError = "";

        public TransferController(IConfiguration configuration, ILogger<TransferController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        public IActionResult PostAsync(Dto_Transfer dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "VerifyTransferInvoice":
                    {
                        return VerifyTransferInvoice(dto); //return VerifyDlbReScanDoc(dto); // for bug checking 
                    }
                case "VerifyTransferInvoice_BetwDriver":
                    {
                        return VerifyTransferInvoice_BetwDriver(dto); // 20241017
                    }
                case "SaveTransferBoxes":
                    {
                        return SaveTransferBoxes(dto);
                    }
                case "CreateTransferDraft":
                    {
                        return CreateTransferDraft(dto);
                    }
                case "RemoveInvoiceDoc":
                    {
                        return RemoveInvoiceDoc(dto);
                    }
                case "RemoveSavedDraft":
                    {
                        return RemoveSavedDraft(dto);
                    }
                case "GetTransferDrafts":
                    {
                        return GetTransferDrafts(dto);
                    }
                case "LoadDraftDetails":
                    {
                        return LoadDraftDetails(dto);
                    }
                case "LoadTransferDetails":
                    {
                        return LoadTransferDetails(dto);
                    }
                case "SaveDraffToTransfered":
                    {
                        return SaveDraffToTransfered(dto);
                    }
                case "GetTransferedList":
                    {
                        return GetTransferedList(dto);
                    }
                case "SaveDraffToTransfered_DlvryApp":
                    {
                        return SaveDraffToTransfered_DlvryApp(dto);
                    }
                case "LoadAddedTransferBoxes":
                    {
                        return LoadAddedTransferBoxes(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult LoadAddedTransferBoxes(Dto_Transfer dto)
        {
            try
            {
                if (dto.SaveGuid == default)
                {
                    return BadRequest("invalid transfer guid");
                }
                if (dto.InvNum == 0)
                {
                    return BadRequest("invalid invoice number");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);

                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_query = @$"Select distinct       t1.BoxId
                                                      , t1.PickerCode
                                                      , t1.PickerName
                                                      , t1.PickDt
                                                      , t1.PackId
                                                      , t1.PackDt
                                                      , t1.PackerCode
                                                      , t1.PackerName
                                                      , t1.BaseEntry
                                                      , t1.BoxGuid
                                                      , t1.TimeStampSeq
                                                      , t1.AppVersion
                                                      , t1.BoxSize
                                                      , t1.OrderProcessWeek
                                                      , t1.BusinessCenterCode
                                                      , t1.CurrentCartonNo
                                                      , t1.OrderNo 
                                                      , t1.LabelConsistTotalBoxes

                                    from {db.WEBDB}..FTAPP_Transfer2 t0 with (nolock)
                                    inner join {db.WEBDB}..FTAPP_Box t1 with (nolock) on t1.BoxId = t0.BoxId
                                    where t0.GroupGuid = @saveGuid
                                    and t0.InvNo = @invNo";

                using var conn = new SqlConnection(_commDbConnStr);
                var boxes = conn.Query<FTAPP_Box>(sp_query, new
                {
                    saveGuid = dto.SaveGuid,
                    invNo = dto.InvNum
                }).ToList();

                if (boxes.Count == 0)
                {
                    return NotFound();
                }

                return Ok(boxes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTransferedList(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ReceiverCode))
                {
                    return BadRequest("Invalid receivercode / plate no");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start datetime");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end datetime");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_query = $@"select * 
                                    from {db.WEBDB}..FTAPP_TRANSFER with (nolock)
                                    Where DocStatus = @DocStatus 
                                    and ReceiverCode = @ReceiverCode 
                                    and convert(date, TransDt) >= @StartDt
                                    and convert(date, TransDt) <= @EndDt";

                using var conn = new SqlConnection(_commDbConnStr);
                var transfereds = conn.Query<FTAPP_Transfer>(sp_query,
                    new
                    {
                        DocStatus = "T",
                        ReceiverCode = dto.ReceiverCode,
                        StartDt = $"{dto.StartDt:yyyy-MM-dd}",
                        EndDt = $"{dto.EndDt:yyyy-MM-dd}",
                    }).ToList();

                if (transfereds.Count == 0) return NotFound();
                return Ok(transfereds);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDraffToTransfered(Dto_Transfer dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI");
            }
            if (dto.SaveGuid == default)
            {
                return BadRequest("Invalid guid");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            
            
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var sp_GetTransfer = $@"select * from {db.WEBDB}..FTAPP_Transfer Where GroupGuid = @GroupGuid ";
                var transfer = conn.Query<FTAPP_Transfer>(sp_GetTransfer, new
                {
                    GroupGuid = dto.SaveGuid
                }, trans).FirstOrDefault();


                if (transfer != null)
                {
                    var sp_updateTransfer = @$"update {db.WEBDB}..FTAPP_Transfer
                                        set DocStatus = @DocStatus 
                                        Where GroupGuid = @GroupGuid";

                    var res = conn.Execute(sp_updateTransfer, new
                    {
                        DocStatus = "T", // <-- done transfer
                        GroupGuid = dto.SaveGuid
                    }, trans);

                    trans.Commit();
                    return Ok();
                }

                trans.Rollback();
                return BadRequest("Error update the transfer, please try again. Thank you");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveDraffToTransfered_DlvryApp(Dto_Transfer dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI");
            }
            if (dto.SaveGuid == default)
            {
                return BadRequest("Invalid guid");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            try
            {

               


                // prepare the dlb1 list 
                var newDlbs = new List<FTAPP_DLB1>();
                var newDlbs_Boxes = new List<FTAPP_DLB2>();

                // 20220912
                // base on the invoice close the DLB 
                // recreate the new dlb from the list 
                var queryInvs = @$"Select distinct * 
                                   from {db.WEBDB}..FTAPP_Transfer1 
                                   Where GroupGuid = @GroupGuid";

                var invs = conn.Query<FTAPP_Transfer1>(queryInvs, new { GroupGuid = dto.SaveGuid }).ToList();
                if (invs.Count == 0) return NotFound();

                // get the start and end date transfer 
                var sp_query_transfereDt = @$"select min(transdt) [StartLoad]
                                                    , max(transdt) [EndLoad]
                                    from {db.WEBDB}..FTAPP_Transfer2
                                    where GroupGuid =  @GroupGuid";

                var transferStartEndDt = conn
                    .Query<TransferStartEndDt>(sp_query_transfereDt,
                    new
                    {
                        GroupGuid = dto.SaveGuid
                    }).FirstOrDefault();

                long lastDLbEntry = -1;
                // the list later reuse to recreate the new DLB record
                // loop each invoice 
                for (int i = 0; i < invs.Count; i++)
                {
                    var invoice = invs[i];
                    var queryDlb = @$"select * 
                                    from {db.WEBDB}..DLB1 
                                    where Status = @Status 
                                    and DocNum = @DocNum  
                                    and DocType = @DocType 
                                    Order by CONVERT(date, DMODIFIED) desc ";

                    var dlbInvs = conn.Query<DLB1>(queryDlb,
                        new
                        {
                            Status = "O",
                            DocNum = invoice.InvNo,
                            DocType = "I"
                        }).ToList();

                    // if no open invoice 
                    if (dlbInvs.Count == 0) continue;

                    var lastInv = dlbInvs.FirstOrDefault();
                    if (lastInv == null)
                    {
                        lastInv = dlbInvs[0]; // the last invoice
                    }

                    lastDLbEntry = lastInv.DOCENTRY;
                    var lastDlb_sp = $"select * from {db.WEBDB}..FTAPP_DLB where DLBEntry = @lastDLbEntry";
                    var lastFTAPP_DLb = conn.Query<FTAPP_DLB>(lastDlb_sp, new { lastDLbEntry }).FirstOrDefault();
                    if (lastFTAPP_DLb == null)
                    {
                        // if null how to process
                        return BadRequest($"Error query the FTAPP_DLB Last DLB Entry {lastDLbEntry}, {dto.Subsi}");
                    }

                    // get the dlb1 invoice 
                    var last_Dlb1_sp = @$"select * from {db.WEBDB}..FTAPP_DLB1 
                                         where HeadGuid = @HeadGuid
                                         and DocNum = @DocNum 
                                         and DocType = 'I'";

                    var dlb1 = conn.Query<FTAPP_DLB1>(last_Dlb1_sp, new
                    {
                        HeadGuid = lastFTAPP_DLb.HeadGuid,
                        DocNum = lastInv.DOCNUM
                    }).FirstOrDefault();
                    if (dlb1 == null)
                    {
                        return BadRequest($"Error query the FTAPP_DLB1 {lastInv.DOCNUM}, {dto.Subsi}");
                    }

                    var sapInv_sp = $@"select * from {db.SAPDB}..OINV t0 with (NOLOCK) where docnum  = @docnum ";
                    var sapInvoice = conn.Query<OINV>(sapInv_sp, new { docnum = lastInv.DOCNUM }).FirstOrDefault();

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

                    var boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = sapInvoice.U_SOID }).ToList();

                    // try to get the transfer min and max date time 
                    bool isSameDateTime = transferStartEndDt.StartLoad == transferStartEndDt.EndLoad; //

                    var newdlb1 = new FTAPP_DLB1
                    {
                        DocNum = sapInvoice.DocNum,
                        StoreCode = sapInvoice.CardCode,
                        StoreName = sapInvoice.CardName,
                        DocEntry = sapInvoice.DocEntry,
                        DocStatus = "O",
                        StatusDesc = "Transfer",
                        HeadGuid = dto.SaveGuid,
                        DocType = "I",
                        BoxStatusDesc = "load in truck",
                        BoxesCount = boxes.Sum(v => v.LabelConsistTotalBoxes),
                        DocDate = dlb1.DocDate,
                        DocTotal = lastInv.DOCTOTAL,
                        CartonNo = boxes.Sum(v => v.LabelConsistTotalBoxes),
                        RefNo = lastInv.REFNO,
                        ConsigmentNo = lastInv.CONSIGNMENTNO,                        
                        LastDlbEntry = (int) lastDLbEntry, 
                        App_Determined_IsInterbranch = (bool) dlb1?.App_Determined_IsInterbranch, 
                        GeoCode = dlb1?.GeoCode, 
                        GeoType = dlb1?.GeoType,
                        SubSi = dto.Subsi, 
                        TransInDt = isSameDateTime == true? transferStartEndDt.StartLoad.AddMinutes(6) : transferStartEndDt.EndLoad
                        //DateTime.Now // (DateTime) dlb1?.TransInDt
                    };

                    newDlbs.Add(newdlb1);

                    // try to get
                    var last_Dlb2_sp = $@"Select TOP 1 * from {db.WEBDB}.. FTAPP_DLB2 
                                          where HeadGuid = @headGuid 
                                            and InvDocNum = @InvDocNum ";

                    var dlb2 = conn.Query<FTAPP_DLB2>(last_Dlb2_sp, new
                    {
                        HeadGuid = lastFTAPP_DLb.HeadGuid,
                        InvDocNum = lastInv.DOCNUM,
                        
                    }).FirstOrDefault();

                    var OutTransDt = DateTime.Now;
                    var InTransDt = DateTime.Now;
                    if (transferStartEndDt != null)
                    {
                        if (isSameDateTime)
                        {
                            OutTransDt = transferStartEndDt.StartLoad;
                            InTransDt = transferStartEndDt.EndLoad.AddMinutes(6);
                        }
                        else
                        {
                            OutTransDt = transferStartEndDt.StartLoad;
                            InTransDt = transferStartEndDt.EndLoad;
                        }
                    }

                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var box = boxes[b];
                        if (box == null) continue;
                        var newDlb2 = new FTAPP_DLB2
                        {
                            InvDocNum = sapInvoice.DocNum,
                            BoxId = box.BoxId,
                            OutTransDt = OutTransDt,
                            InTransDt = InTransDt,
                            SoDocEntry = box.BaseEntry,
                            DlbEntry = -1,
                            HeadGuid = dto.SaveGuid
                        };

                        newDlbs_Boxes.Add(newDlb2);
                    }

                } // end for loop

                // get the transfer record 
                var transfer_qr = $@"select * from {db.WEBDB}..FTAPP_Transfer Where GroupGuid = @GroupGuid";
                var transfer = conn.Query<FTAPP_Transfer>(transfer_qr,
                    new
                    {
                        GroupGuid = dto.SaveGuid
                    }).FirstOrDefault();

                if (transfer == null)
                {
                    return BadRequest("Invalid guid for reading transfer data");
                }

                var isInterBranch = newDlbs.Any(h => h.App_Determined_IsInterbranch == true);

                // create the dlb head 
                var dlbHead = new FTAPP_DLB
                {
                    WhsUserCode = transfer.ReceiverCode,
                    WhsUserName = transfer.ReceiverName,
                    OutTransDt = DateTime.Now,
                    TruckNo = transfer.ReceiverCode,
                    TruckCardCode = transfer.LocationCode,  // refer to truck company code
                    TruckCardName = transfer.LocationName, // refer to truck company name
                    HeadGuid = dto.SaveGuid,
                    Remarks = "TransferToDriver",
                    DriverName = transfer.DriverName,                    
                    DLBStatus = "O",
                    Subsi = dto.Subsi, 
                    IsInterbranch = isInterBranch
                };

                // need to insert into the FTAPP_DLB structure 
                // with not box 

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // create the FTAPP DLB + DLB1
                        var insertedFTAPP_Dlb = InsertDlbWhenTransfer(db, dlbHead, newDlbs, newDlbs_Boxes, conn, trans);
                        if (!insertedFTAPP_Dlb)
                        {
                            trans.Rollback();
                            return BadRequest($"{db.COMPANYNAME}, Error create FTAPP DLB record before DLB, " +
                                                $"Trans roll backed, pls try again [0]");
                        }

                        // create the portal DLB + transfer 
                        var dlbHelper = new DLBHelper(db, dto.SaveGuid, dlbHead.TruckNo);
                        var dlbEntry = dlbHelper
                            .CreateDLB_WNoTransfer(dlbHead, newDlbs, transfer.ReceiverCode, 
                                                   transfer.ReceiverName, trans, conn, _commDbConnStr);

                        if (dlbEntry <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"{db.COMPANYNAME}, Error create FTAPP DLB record before DLB, " +
                                                $"Trans roll backed, pls try again [1]");
                        }

                        // lastly update the transfer status to T 
                        var sp_updateTransfer = @$"update {db.WEBDB}..FTAPP_Transfer
                                            set DocStatus = @DocStatus , DLBEntry = @DLBEntry
                                            Where GroupGuid = @GroupGuid";

                        var updateResult = conn.Execute(sp_updateTransfer, new
                        {
                            DocStatus = "T",
                            GroupGuid = dto.SaveGuid,
                            DLBEntry = dlbEntry
                        }, trans);

                        if (updateResult < 0)
                        {
                            trans.Rollback();
                            return BadRequest($"{db.COMPANYNAME}, DLB: {dlbEntry} update FTAPP_Transfer record before DLB, " +
                                                $"Trans roll backed, pls try again [1]");
                        }


                        // preform the update to DLB portal to update the invoice status 
                        long lastDlbEntry = -1;
                        for (int d = 0; d < newDlbs.Count; d++)
                        {
                            var newDlb = newDlbs[d];
                            if (newDlb == null) continue;

                            //// update the invoice with DLB cancel 
                            var update_dlb = $@"Update {db.WEBDB}..DLB1 
                                        set Status = @Status 
                                        Where DocEntry = @DocEntry 
                                        and DocNum = @DocNum 
                                        and DocType = @DocType ";

                            conn.Execute(update_dlb, new
                            {
                                Status = "C",
                                DocEntry = newDlb.LastDlbEntry,
                                DocNum = newDlb.DocNum,
                                DocType = "I"
                            }, trans);

                            lastDlbEntry = newDlb.LastDlbEntry;
                        }

                        trans.Commit();
                        var replied = new
                        {
                            NewDlbEntry = dlbEntry,
                            LastDlbEntry = lastDlbEntry,
                            ResultMessage = "Success transferred and new DLB created",
                        };

                        return Ok(replied);
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }
                } // close the transaction
            }
            catch (Exception e)
            {                
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        bool InsertDlbWhenTransfer(DbInfo db, FTAPP_DLB head, List<FTAPP_DLB1> lines, List<FTAPP_DLB2> boxes,
                                    SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                // insert the head FTAPP_DLB
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
                                               , DLBStatus, SubSi, IsInterbranch
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
                                          ,@DLBStatus , @SubSi, @IsInterbranch
                                    )";

                var res = conn.Execute(sp_insert, head, trans);

                // insert the lines, FTAPP_DLB1
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
                                               , LastDlbEntry
                                               , App_Determined_IsInterbranch
                                               , GeoCode
                                               , GeoType
                                               , TransInDt
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
                                           ,@LastDlbEntry
                                           ,@App_Determined_IsInterbranch
                                           ,@GeoCode
                                           ,@GeoType
                                           ,@TransInDt
                                )";

                var res1 = conn.Execute(sp_insert1, lines, trans);

                var insertBox_sp = @$"INSERT INTO {db.WEBDB}..FTAPP_DLB2 ( 
                                            InvDocNum
                                           ,BoxId
                                           ,OutTransDt
                                           ,InTransDt                                            
                                           ,SoDocEntry
                                           ,DlbEntry
                                           ,HeadGuid 
                                        ) values (
                                            @InvDocNum
                                           ,@BoxId
                                           ,@OutTransDt
                                           ,@InTransDt
                                           ,@SoDocEntry
                                           ,@DlbEntry
                                           ,@HeadGuid  )";

                res1 = conn.Execute(insertBox_sp, boxes, trans);

                // need to create the FTAPP_HoldDlryInvoice
                // insert the on hold invoice 

                for (int i = 0; i < lines.Count; i++)
                {
                    var invDoc = lines[i];
                    if (invDoc == null) continue;

                    var newOnHold = new FTAPP_HoldDlvryDocs
                    {
                        DocNum = $"{invDoc.DocNum}",
                        UserCode = head.TruckCardCode,
                        UserName = head.TruckCardName,
                        Reason = "Loading",
                        DocType = "I",
                        HeadGuid = invDoc.HeadGuid
                    };

                    var sp_insert_hold = $@" INSERT INTO {db.WEBDB}..FTAPP_HoldDlvryDocs (
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

                    var res2 = conn.Execute(sp_insert_hold, newOnHold, trans);
                }
                return true;
            }
            catch (Exception e) // insert exception
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        IActionResult LoadDraftDetails(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.SaveGuid == default)
                {
                    return BadRequest("Invalid guid");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var query_draftInvoice = @$"select t0.* 
                                            from 
                                            {db.SAPDB}..OINV t0 with (nolock) inner join 
                                            {db.WEBDB}..FTAPP_Transfer1 t1 with (nolock) on t1.InvNo = t0.DocNum
                                            where t1.GroupGuid = @GroupGuid";

                using var conn = new SqlConnection(_commDbConnStr);
                var invoices = conn.Query<OINV>(query_draftInvoice, new { GroupGuid = dto.SaveGuid }).ToList();

                if (invoices.Count == 0) return NotFound();

                // else load in the box
                var invoices_retList = new List<OINV>();
                for (int invId = 0; invId < invoices.Count; invId++)
                {
                    var inv = invoices[invId];
                    if (inv == null) continue;

                    // get the box list from web portal
                    //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                    //                Where baseentry = @baseentry";

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
                        continue;
                    }

                    inv.Subsi = db.COMPANYNAME;
                    inv.SubsiId = db.COMPANYID;
                    inv.ScanInCode = $"{dto.ScanInCode}".ToUpper();

                    // load in the continue scan box details 
                    // 20221014
                    // query the added box 
                    var query_addedBox = @$"Select count(1) 
                                        from {db.WEBDB}..FTAPP_Transfer2 with (nolock)
                                        where GroupGuid = @groupGuid
                                        and InvNo = @invNo";

                    var scanAddedBoxesCnt = conn.ExecuteScalar<int>(query_addedBox,
                        new
                        {
                            groupGuid = dto.SaveGuid,
                            invNo = inv.DocNum
                        });

                    inv.AddBoxInfo = new ScanAddBoxesInfo
                    {
                        ScanAddStatus = inv.Boxes.Count == scanAddedBoxesCnt ? "complete" : "partial",
                        TotalInvBoxes = inv.Boxes.Count,
                        ScanAddedBoxes = scanAddedBoxesCnt
                    };

                    inv.IsCompleted_AddBoxes = (inv.Boxes.Count == scanAddedBoxesCnt);

                    invoices_retList.Add(inv); // add into the new list
                }

                // sort based on the uncomplete box scan add.
                var sortedList = invoices_retList.OrderBy(x => x.IsCompleted_AddBoxes).ToList();
                return Ok(sortedList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// Copied and modified from LoadDraftDetails
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult LoadTransferDetails(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.SaveGuid == default)
                {
                    return BadRequest("Invalid guid");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var query_draftInvoice = @$"select t0.* 
                                            from 
                                            {db.SAPDB}..OINV t0 with (nolock) inner join 
                                            {db.WEBDB}..FTAPP_Transfer1 t1 with (nolock) on t1.InvNo = t0.DocNum
                                            where t1.GroupGuid = @GroupGuid";

                using var conn = new SqlConnection(_commDbConnStr);
                var invoices = conn.Query<OINV>(query_draftInvoice, new { GroupGuid = dto.SaveGuid }).ToList();

                if (invoices.Count == 0) return NotFound();

                // else load in the box
                List<OINV> invoices_retList = new List<OINV>();
                for (int invId = 0; invId < invoices.Count; invId++)
                {
                    var inv = invoices[invId];
                    if (inv == null) continue;

                    // get the box list from web portal
                    //var query_box = $@"select BoxId from {db.WEBDB}..FTAPP_Box with (nolock)
                    //                Where baseentry = @baseentry";

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
                        continue;
                    }

                    inv.Subsi = db.COMPANYNAME;
                    inv.SubsiId = db.COMPANYID;
                    inv.ScanInCode = $"{dto.ScanInCode}".ToUpper();

                    invoices_retList.Add(inv); // add into the new list
                }

                return Ok(invoices_retList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTransferDrafts(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.LocationName))
                {
                    return BadRequest("Invalid lcoation name");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_queryDrafts = @$"select '{db.COMPANYNAME}' [Subsi]
                                        , '{db.COMPANYID}' [SubsiId]
                                        , * 
                                        from {db.WEBDB}..FTAPP_TRANSFER with (nolock)
                                        where DocStatus  = @DocStatus 
                                        and LocationName = @LocationName 
                                        order by id desc ";

                using var conn = new SqlConnection(_commDbConnStr);
                var drafts = conn.Query<FTAPP_Transfer>(sp_queryDrafts, new
                {
                    DocStatus = "D",
                    LocationName = dto.LocationName
                }).ToList();

                if (drafts.Count == 0) return NotFound();

                return Ok(drafts);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult RemoveSavedDraft(Dto_Transfer dto)
        {
            if (dto.SaveGuid == default)
            {
                return BadRequest("Invalid save guid");
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

            var sp_deleteTransfer1 = $@"Delete from {db.WEBDB}..FTAPP_TRANSFER1
                                            Where GroupGuid = @GroupGuid ";

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // delete the invoice
                conn.Execute(sp_deleteTransfer1, new
                {
                    GroupGuid = dto.SaveGuid
                }, trans);

                // delete the box
                var sp_deleteTransfer2 = $@"Delete from {db.WEBDB}..FTAPP_Transfer2
                                            Where GroupGuid = @GroupGuid ";

                conn.Execute(sp_deleteTransfer2, new
                {
                    GroupGuid = dto.SaveGuid
                }, trans);


                // delete the head 
                var sp_deleteTransfer = $@"Delete from {db.WEBDB}..FTAPP_Transfer
                                            Where GroupGuid = @GroupGuid ";

                conn.Execute(sp_deleteTransfer, new
                {
                    GroupGuid = dto.SaveGuid
                }, trans);

                // commit
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

        IActionResult RemoveInvoiceDoc(Dto_Transfer dto)
        {

            if (dto.SaveGuid == default)
            {
                return BadRequest("Invalid save guid");
            }
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.InvNum <= 0)
            {
                return BadRequest("Invalid invoice number");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            var sp_deleteTransfer1 = $@"Delete from {db.WEBDB}..FTAPP_TRANSFER1
                                            Where GroupGuid = @GroupGuid 
                                            and InvNo = @InvNo ";
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // delete the invoice
                conn.Execute(sp_deleteTransfer1, new
                {
                    GroupGuid = dto.SaveGuid,
                    InvNo = dto.InvNum
                }, trans);

                // delete the box
                var sp_deleteTransfer2 = $@"Delete from {db.WEBDB}..FTAPP_Transfer2
                                            Where GroupGuid = @GroupGuid 
                                            and InvNo = @InvNo ";

                conn.Execute(sp_deleteTransfer2, new
                {
                    GroupGuid = dto.SaveGuid,
                    InvNo = dto.InvNum
                }, trans);

                // commit
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

        IActionResult CreateTransferDraft(Dto_Transfer dto)
        {
            if (dto.SaveGuid == default)
            {
                return BadRequest("Invalid save guid");
            }
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI");
            }
            if (dto.TransferHead == null)
            {
                return BadRequest("Invalid transfer head");
            }
            if (dto.TransferInvoices == null)
            {
                return BadRequest("Invalid transfer invoices");
            }
            if (dto.TransferInvoices.Count == 0)
            {
                return BadRequest("Invalid transfer invoices [Z]");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            var sp_lastRec = @$"select * 
                                from {db.WEBDB}..FTAPP_Transfer with (NOLOCK) 
                                where GroupGuid = @GroupGuid";

            using var conn = new SqlConnection(_commDbConnStr);
            var found = conn.Query<FTAPP_Transfer>(sp_lastRec, new { GroupGuid = dto.SaveGuid }).FirstOrDefault();

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // delete and insert new
                    //var sp_delete = $@"delete from {db.WEBDB}..FTAPP_Transfer Where GroupGuid = @GroupGuid";
                    //conn.Execute(sp_delete, new { GroupGuid = dto.SaveGuid }, trans);

                    var sp_delete_invs = $@"delete from {db.WEBDB}..FTAPP_Transfer1 Where GroupGuid = @GroupGuid";
                    var delResult = conn.Execute(sp_delete_invs, new { GroupGuid = dto.SaveGuid }, trans);
                    if (delResult < 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME} remove FTAPP_Transfer1 record.");
                    }

                    if (found == null)
                    {
                        // insert new head
                        var insert_transfer = @$"insert into {db.WEBDB}..FTAPP_Transfer (
                                              ReceiverCode
                                            , ReceiverName
                                            , LocationCode
                                            , LocationName
                                            , TransDt
                                            , GroupGuid
                                            , DocStatus  
                                            , DriverName  
                                            , Module    
                                            , DLBEntry
                                            ) values (
                                               @ReceiverCode
                                              ,@ReceiverName
                                              ,@LocationCode
                                              ,@LocationName
                                              ,GETDATE()
                                              ,@GroupGuid
                                              ,@DocStatus
                                              ,@DriverName
                                              ,@Module 
                                              ,@DLBEntry  )";

                        var insertResult = conn.Execute(insert_transfer, dto.TransferHead, trans);
                        if (insertResult <= 0)
                        {
                            trans.Rollback();
                            return BadRequest($"{db.COMPANYNAME} insert FTAPP_Transfer record.");
                        }
                    }
                    else
                    {
                        dto.TransferHead.Id = found.Id;

                        // perform an update
                        var update_transfer = @$"update {db.WEBDB}..FTAPP_Transfer 
                                             set ReceiverCode = @ReceiverCode, 
                                                 ReceiverName = @ReceiverName, 
                                                 LocationCode = @LocationCode, 
                                                 DriverName = @DriverName 
                                            where Id = @id 
                                            and GroupGuid = @GroupGuid ";

                        var updateResult = conn.Execute(update_transfer, dto.TransferHead, trans);
                        if (updateResult < 0)
                        {
                            trans.Rollback();
                            return BadRequest($"{db.COMPANYNAME} update FTAPP_Transfer records.");
                        }
                    }

                    // insert the lines 
                    var insert_lines = $@"insert into {db.WEBDB}..FTAPP_Transfer1 ( 
                                          InvNo
                                        , TransDt
                                        , GroupGuid 
                                    ) values (  
                                          @InvNo
                                         , GETDATE()
                                         , @GroupGuid 
                                    ) ";

                    var insertLine_Result = conn.Execute(insert_lines, dto.TransferInvoices, trans);
                    if (insertLine_Result <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME} insert FTAPP_Transfer1 records.");
                    }

                    trans.Commit(); // commit everything 

                    using (var newConn = new SqlConnection(_commDbConnStr))
                    {
                        var sp_select = @$"Select * from {db.WEBDB}..FTAPP_Transfer Where GroupGuid = @GroupGuid";
                        var transfer = newConn.Query<FTAPP_Transfer>(sp_select, new
                        {
                            GroupGuid = dto.SaveGuid
                        }).FirstOrDefault();

                        if (transfer != null)
                        {
                            return Ok(transfer);
                        }

                        // query the the given id 
                        return BadRequest("Error saving the transfer record");
                    }
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

        IActionResult SaveTransferBoxes(Dto_Transfer dto)
        {
            if (dto.Box == null)
            {
                return BadRequest("Invalid box value, please try again");
            }

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SUBSI info, please try again.");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid DBI, please try again.");
            }

            // look for duplicated
            var check_dupl = @$"select * 
                                from {db.WEBDB}..FTAPP_Transfer2 with (NOLOCK)
                                where GroupGuid = @GroupGuid 
                                and BoxId = @BoxId 
                                and InvNo = @InvNo ";

            using var conn = new SqlConnection(_commDbConnStr);
            var dupl = conn.Query<FTAPP_Transfer2>(check_dupl, new
            {
                GroupGuid = dto.Box.GroupGuid,
                BoxId = dto.Box.BoxId,
                InvNo = dto.Box.InvNo
            }).FirstOrDefault();

            if (dupl != null) return Ok(); // already added

            // else insert 
            var insert_sql = @$"insert into {db.WEBDB}..FTAPP_Transfer2 (
                                         BoxId
                                        ,InvNo
                                        ,TransDt
                                        ,GroupGuid 
                                    ) values (
                                         @BoxId
                                        ,@InvNo
                                        ,GETDATE()
                                        ,@GroupGuid  ) ";

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                conn.Execute(insert_sql, dto.Box, trans);
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

        IActionResult VerifyTransferInvoice(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ScanInCode))
                {
                    return BadRequest("Invalid scan in code");
                }

                if (!dto.ScanInCode.Contains("-"))
                {
                    return BadRequest("Invalid scan in code [ND]");
                }

                var splitedArr = dto.ScanInCode.Split('-');
                if (splitedArr.Length < 3) // invNo-page-companyID
                {
                    return BadRequest("Invalid scan in code format [NL]");
                }

                string docNum = $"{splitedArr[0]}".Trim();
                string subsiId = $"{splitedArr[2]}".Trim();

                if (string.IsNullOrWhiteSpace(docNum))
                {
                    return BadRequest("Invalid doc number");
                }
                if (string.IsNullOrWhiteSpace(subsiId))
                {
                    return BadRequest("Invalid SUBSI id");
                }

                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, subsiId);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                // 20240318
                // comment the checking part to allow all invoice can be entry / transfer to whs
                //check the invoice in O status 
                //var sql_checkDocStatus = @$"Select * from {db.WEBDB}..DLB1 with (nolock)
                //                            where DocNum = @docNum 
                //                            and Status = @status 
                //                            and DocType = @docType";

                //using var conn = new SqlConnection(_commDbConnStr);
                //var dlb = conn.Query<DLB1>(sql_checkDocStatus, new
                //{
                //    docNum = docNum,
                //    status = "O",
                //    docType = "I"
                //}).FirstOrDefault();

                //if (dlb == null)
                //{
                //    return BadRequest($"{db.COMPANYNAME}, Doc #{docNum} found no in OUT status, transfer no allowed.");
                //}

                // prepare to get the doc 
                // reply the app 

                // return ok when all status 
                // get the invoice from sap
                using var conn = new SqlConnection(_commDbConnStr);
                var query_inv = @$"select * from {db.SAPDB}..OINV with (NOLOCK) where DocNum = @docnum";
                OINV inv = conn.Query<OINV>(query_inv, new { docnum = docNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{docNum}, Error query for sap invoice.");
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

                                from {db.WEBDB}..FTAPP_Box t0 with (NOLOCK)
                                left join  {db.WEBDB}..FTAPP_Box1 t1  with (NOLOCK) on t0.BoxGuid = t1.BoxGuid
                                Where t0.BaseEntry = @baseentry 
                                and t1.BoxGuid is not null";

                inv.Boxes = conn.Query<FTAPP_Box>(query_box, new { baseentry = inv.U_SOID }).ToList();
                if (inv.Boxes.Count == 0)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{docNum}, Error query for boxes.");
                }

                // get the latest dlb entry 
                // 20260101
                var sp_getLastDlbEntry = $@"SELECT TOP (1) t0.DLBEntry
                                            FROM {db.WEBDB}..FTAPP_DLB AS t0 with (NOLOCK)
                                            INNER JOIN {db.WEBDB}..FTAPP_DLB1 AS t1 with (NOLOCK)
                                              ON t0.HeadGuid = t1.HeadGuid
                                            INNER JOIN {db.WEBDB}..DLB1 AS t2 with (NOLOCK)
                                              ON t2.DOCNUM = t1.DocNum
                                             AND t2.DOCTYPE = t1.DocType
                                             AND t2.App_Determined_IsInterbranch = '1'
                                            WHERE t1.DocNum = @DocNum
                                              AND t1.DocType = 'I'
                                            ORDER BY t0.ID DESC ";

                inv.DlbEntry = conn.ExecuteScalar<int>(sp_getLastDlbEntry, new { DocNum = inv.DocNum });

                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;
                inv.ScanInCode = $"{dto.ScanInCode}".ToUpper();
                return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // transfer between driver  
        IActionResult VerifyTransferInvoice_BetwDriver(Dto_Transfer dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ScanInCode))
                {
                    return BadRequest("Invalid scan in code");
                }

                if (!dto.ScanInCode.Contains("-"))
                {
                    return BadRequest("Invalid scan in code [ND]");
                }

                var splitedArr = dto.ScanInCode.Split('-');
                if (splitedArr.Length < 3) // invNo-page-companyID
                {
                    return BadRequest("Invalid scan in code format [NL]");
                }

                string docNum = $"{splitedArr[0]}".Trim();
                string subsiId = $"{splitedArr[2]}".Trim();

                if (string.IsNullOrWhiteSpace(docNum))
                {
                    return BadRequest("Invalid doc number");
                }
                if (string.IsNullOrWhiteSpace(subsiId))
                {
                    return BadRequest("Invalid subsi id");
                }

                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, subsiId);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                // get the setup value for max dlb for an invoice 
                var sp_maxInvDlb = @"select setupvalue 
                                     from KTCW_COMMON..FTAPP_Config 
                                     where setupname = 'MaxDLBCretateForAInvn'";

                using var conn = new SqlConnection(_commDbConnStr);
                var maxInvDlb = conn.ExecuteScalar<string>(sp_maxInvDlb);
                var isInt = int.TryParse(maxInvDlb, out int maxInvDlbRes);

                if (!isInt)
                {
                    maxInvDlbRes = 4;
                }

                // 20241017
                // add in control to check the latest dlb times
                var sp_query = $@"select t0.* 
                  from {db.WEBDB}..DLB1 t0 with (nolock)
                  Where t0.docnum = @docnum 
                  and t0.doctype = @doctype
                  order by t0.DocEntry desc";

                
                var dlb1s = conn.Query<DLB1>(sp_query, new { docnum = docNum, doctype = "I" }).ToList();

                // invoice no found
                if (dlb1s.Count >= maxInvDlbRes)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {docNum} exceed the DLB tried records {dlb1s.Count} >= {maxInvDlbRes}");
                }


                var isAnyOutStatus = dlb1s.Any(d => d.STATUS == "O");
                if (isAnyOutStatus == false) // not dlb in O
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice # {docNum} was closed in all DLB. no transfer allowed.");
                }

                // invoice status in O 
                //var oStatus = dlb1s.Where(d => d.STATUS == "O").FirstOrDefault();
                //if (oStatus == null)
                //{
                //    return BadRequest($"{db.COMPANYNAME}, Invoice # {docNum} found CLOSED status, transfer no allowed.");
                //}

                // 20240318
                // comment the checking part to allow all invoice can be entry / transfer to whs
                //check the invoice in O status 
                //var sql_checkDocStatus = @$"Select * from {db.WEBDB}..DLB1 with (nolock)
                //                            where DocNum = @docNum 
                //                            and Status = @status 
                //                            and DocType = @docType";

                //using var conn = new SqlConnection(_commDbConnStr);
                //var dlb = conn.Query<DLB1>(sql_checkDocStatus, new
                //{
                //    docNum = docNum,
                //    status = "O",
                //    docType = "I"
                //}).FirstOrDefault();

                //if (dlb == null)
                //{
                //    return BadRequest($"{db.COMPANYNAME}, Doc #{docNum} found no in OUT status, transfer no allowed.");
                //}

                // prepare to get the doc 
                // reply the app 

                // return ok when all status 
                // get the invoice from sap

                var query_inv = @$"select * from {db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                OINV inv = conn.Query<OINV>(query_inv, new { docnum = docNum }).FirstOrDefault();
                if (inv == null)
                {
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{docNum}, Error query for sap invoice.");
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
                    return BadRequest($"{db.COMPANYNAME}, Invoice #{docNum}, Error query for boxes.");
                }

                inv.Subsi = db.COMPANYNAME;
                inv.SubsiId = db.COMPANYID;
                inv.ScanInCode = $"{dto.ScanInCode}".ToUpper();
                return Ok(inv);
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

