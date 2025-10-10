using Dapper;
using KTC_SalesAppWAPI.DTOs.Pick;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PickDraftController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<PickDraftController> _logger;

        //string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public PickDraftController(IConfiguration configuration, ILogger<PickDraftController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            //WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult Post(Dto_Pick dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {

                case "HandlerSaveLineAsDaft":
                    {
                        return HandlerSaveLineAsDaft(dto);
                    }
                case "CancelOrder":
                    {
                        return CancelOrder(dto);
                    }
                case "SetSOTop":
                    {
                        return SetSOTop(dto);
                    }

                default:
                    {
                        return BadRequest("Request no found");
                    }


            }
        }

        IActionResult SetSOTop(Dto_Pick dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.ResetSoDocEntries == null)
            {
                return BadRequest("Invalid doc entry");
            }
            if (dto.ResetSoDocEntries.Length == 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            // get the min date of all 
            var sp_GetMinDt = @$"select MIN(DMODIFIED) 
                                     from {db.WEBDB}..SO t0 with (nolock)
                                     where DOCSTATUS = 'Q' ";

            using var conn = new SqlConnection(_commDbConnStr);
            var minDate = conn.ExecuteScalar<DateTime>(sp_GetMinDt);
            if (minDate == default)
            {
                minDate = DateTime.Now;
            }

            // deduct one more day
            minDate = minDate.AddDays(-1); // minus more dat
            var sp_UpdateModiDt = @$"update {db.WEBDB}..SO 
                                         set DMODIFIED = @processDt 
                                         where docentry = @docEntry ";

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                for (int d = 0; d <  dto.ResetSoDocEntries.Length; d ++)
                {
                    var updated = conn.Execute(sp_UpdateModiDt, new
                    {
                        processDt = minDate,
                        docEntry = dto.ResetSoDocEntries[d]
                    }, trans);

                    if (updated <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"SO #{dto.ResetSoDocEntries[d]} update to top error, please try again. Thanks.");
                    }
                }               

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                trans.Rollback();
                return BadRequest($"Request not handler {LastError}");
            }
        }

        IActionResult CancelOrder(Dto_Pick dto)
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
                if (string.IsNullOrWhiteSpace(dto.AppVersion))
                {
                    return BadRequest("Invalid app version");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Invalid card code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var onhold_query = @$"Select * from {db.WEBDB}..FTAPP_OnHoldSoInPicking
                                    where HoldDocEntry = @DocEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var isOnhold = conn.Query<FTAPP_OnHoldSoInPicking>(onhold_query, new
                {
                    dto.DocEntry
                }).FirstOrDefault();

                if (isOnhold == null)
                {
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        var updateSo_cancel_query = @$"Update {db.WEBDB}..SO 
                                                    SET DOCSTATUS = @DocStatus
                                                    Where docentry = @DocEntry";

                        var res = conn.Execute(updateSo_cancel_query, new
                        {
                            DocStatus = "L",
                            DocEntry = dto.DocEntry
                        }, trans);

                        if (res == 1)
                        {
                            trans.Commit();

                            var excepLog1 = new FTAPP_AppPostLog
                            {
                                AppModule = "Seller cancel SO",
                                UserCode = $"{dto.UserCode}",
                                CardCode = $"{dto.CardCode}",
                                SubSi = dto.Subsi,
                                Details = $"#{dto.DocEntry}",
                                PostResult = "SO Cancel",
                                AppVersion = $"{dto.AppVersion}"
                            };

                            AppPostLogging(excepLog1);
                            return Ok();
                        }
                        trans.Rollback();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"Request not handler {LastError}");
                    }
                    return Ok();
                }

                return BadRequest($"SO #{dto.DocEntry} in picking progress by " +
                    $"{isOnhold.HoldByUserCode} {isOnhold.HoldByUserName}, no cancel allowed.");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"Request not handler {LastError}");
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
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    //delete all record in transaction and insert 
                    var delete_sp = @$"Delete from {db.WEBDB}..FTAPP_SO1_DRAFT   where DOCENTRY = @DOCENTRY; 
                                       Delete from {db.WEBDB}..FTAPP_Box_DRAFT   where BaseEntry = @DOCENTRY; 
                                       Delete from {db.WEBDB}..FTAPP_Box1_DRAFT  where BaseEntry = @DOCENTRY;
                                       Delete from {db.WEBDB}..FTAPP_Batch_Draft where DOCENTRY = @DOCENTRY; ";


                    var deleteResult = conn.Execute(delete_sp, new { DOCENTRY = dto.DocEntry }, transaction);
                    if (deleteResult < 0)
                    {
                        transaction.Rollback();
                        return BadRequest($"Error delete {db.COMPANYNAME}, SO {dto.DocEntry}");
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

                    #region insert the boxes

                    if (dto.Boxes != null && dto.Boxes.Count > 0)
                    { // insert box 
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
                                   , BoxSize  , LabelConsistTotalBoxes, PickMode
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
                                   , @BoxSize , @LabelConsistTotalBoxes, @PickMode
                                    )";
                        insert_res = conn.Execute(insert_draft, dto.Boxes, transaction);
                        if (insert_res <= 0)
                        {
                            transaction.Rollback();
                            return BadRequest($@"Error insert FTAPP_Box_DRAFT for  {db.COMPANYNAME} SO# {dto.DocEntry} ");
                        }
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

                    var batches = new List<FTAPP_Batch>();
                    dto.PickedDoc.Lines.ForEach(k =>
                       {
                           if (k.FTAPP_Batches != null && k.FTAPP_Batches.Count > 0)
                           {
                               batches.AddRange(k.FTAPP_Batches);
                           }
                       });

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
                                          ,GETDATE()
                                          ,@PickingMode) ";

                        var insertBatchRes = conn.Execute(insert_sql, batches, transaction);
                        if (insertBatchRes <= 0)
                        {
                            transaction.Rollback();
                            return BadRequest($@"Error insert FTAPP_Batch_Draft for {db.COMPANYNAME} SO# {dto.DocEntry} ");
                        }
                    }

                    transaction.Commit();
                    var replied = new SoDocResult
                    {
                        actionSuccess = true,
                        errorMessage = "",
                        actionResult = $"{dto.DocEntry}",
                        documentStatus = "draft",
                        updateDocType = "draft",
                        docType = "draft"
                    };
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
        }
    }
}
