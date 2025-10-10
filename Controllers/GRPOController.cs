using Dapper;
using KTC_SalesAppWAPI.DTOs.Grpo;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.GRPO;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.Pick;
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
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GRPOController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<GRPOController> _logger;

        string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public GRPOController(IConfiguration configuration, ILogger<GRPOController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Grpo_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetUserAgencies":
                    {
                        return GetUserAgencies(dto);
                    }
                case "GetPOs":
                    {
                        return GetPOs(dto);
                    }
                case "PostGRN":
                    {
                        return PostGRN(dto);
                    }
                case "GetGrns":
                    {
                        return GetGrns(dto);
                    }
                case "GetPOsItems1":
                    {
                        return GetPOsItems1(dto);
                    }
                case "LoadGrnLinesWithDoNumber":
                    {
                        return LoadGrnLinesWithDoNumber(dto);
                    }
                case "SaveFTAPP_GRNLines":
                    {
                        return SaveFTAPP_GRNLines(dto);
                    }
                case "LoadAllGrnLinesWithPoNumber":
                    {
                        return LoadAllGrnLinesWithPoNumber(dto);
                    }
                case "CheckPONumOnHold":
                    {
                        return CheckPONumOnHold(dto);
                    }
                case "GetGrnForApproval":
                    {
                        return GetGrnForApproval(dto);
                    }
                case "SaveGrnDraft":
                    {
                        return SaveGrnDraft(dto);
                    }
                case "SaveGrnLinesDetails":
                    {
                        return SaveGrnLinesDetails(dto);
                    }
                case "LoadGrnDraft":
                    {
                        return LoadGrnDraft(dto);
                    }
                case "UnholdPO":
                    {
                        return UnholdPO(dto);
                    }
                default:
                    {
                        return BadRequest($"Request not found {request}");
                    }
            }
        }

        IActionResult UnholdPO(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                {
                    return BadRequest("Invalid purchase order / delivery order number");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var conn = new SqlConnection(_commDbConnStr);
                var dele_query = $@"Delete from {db.WEBDB}..FTAPP_GRN where DELIVERY_ORDER = @DeliveryOrderNo";
                conn.Execute(dele_query, new { DeliveryOrderNo = dto.DeliveryOrderNo });

                //dele_query = $@"Delete from {db.WEBDB}..FTAPP_GRN1_DRAFT where DELIVERY_ORDER = @DeliveryOrderNo";
                //conn.Execute(dele_query, new { DeliveryOrderNo = dto.DeliveryOrderNo });

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadGrnDraft(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                {
                    return BadRequest("Invalid purchase order / delivery order number");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var query = $@"select * from {db.WEBDB}..FTAPP_GRN_DRAFT with (nolock) Where Delivery_Order = @deliveryOrder";
                var conn = new SqlConnection(_commDbConnStr);
                var grndraft = conn.Query<FTAPP_GRN>(query, new { deliveryOrder = dto.DeliveryOrderNo }).FirstOrDefault();

                if (grndraft != null)
                {
                    query = $@"select * from {db.WEBDB}..FTAPP_GRN1_DRAFT with (nolock) Where Delivery_Order = @deliveryOrder";
                    grndraft.Lines = conn.Query<FTAPP_GRN1>(query, new { deliveryOrder = dto.DeliveryOrderNo }).ToList();
                    return Ok(grndraft);
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

        // to save the app final posted grn line 
        IActionResult SaveGrnLinesDetails(Grpo_Dto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
            {
                return BadRequest("Invalid purchase order / delivery order number");
            }
            if (dto.GrnDocEntry < 0)
            {
                return BadRequest("invalid GRN Doc entry");
            }
            if (dto.GrnDoc == null)
            {
                return BadRequest("Invalid Grn doc");
            }
            if (dto.GrnDoc.Lines == null)
            {
                return BadRequest("Invalid Grn doc lines");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid db info");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the R draft 
                var delete_sql = $@"Delete from {db.WEBDB}..FTAPP_GRN1_FINAL 
                                    Where DELIVERY_ORDER = @DeliveryOrder 
                                    and DocEntry = @GrnDocEntry";


                var deleteGrn1 = conn.Execute(delete_sql, new
                {
                    DeliveryOrder = dto.DeliveryOrderNo,
                    GrnDocEntry = dto.GrnDocEntry
                }, trans);

                if (deleteGrn1 < 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, delete FTAPP_GRN1_FINAL error, PO: {dto.DeliveryOrderNo}, " +
                        $"Grn Entry : {dto.GrnDocEntry}");
                }

                // insert the grn line for app
                for (int i = 0; i < dto.GrnDoc.Lines.Count; i++)
                {
                    var line = dto.GrnDoc.Lines[i];
                    if (line == null) continue;

                    var insertHeaderSql = $@"insert into { db.WEBDB}..FTAPP_GRN1_FINAL(
                                                DOCENTRY
                                                , LINENUM
                                                , ITEMCODE
                                                , ITEMNAME
                                                , CODEBARS
                                                , UOMQTY
                                                , STOCKQTY
                                                , QUANTITYCS
                                                , QUANTITYPC
                                                , QUANTITY
                                                , PRICE
                                                , LINETOTAL
                                                , BASEDOCNUM
                                                , SUPPCATNUM
                                                , DISCOUNT
                                                , FOC
                                                , BASELINE
                                                , BASEENTRY
                                                , PONO
                                                , REASON
                                                , FROZENFOR
                                                , OLDCODE
                                                , LOTNO
                                                , REMARKS
                                                , HEADGUID
                                                , USERCODE
                                                , USERNAME
                                                , DELIVERY_ORDER
                                                ,Subsi         
                                                ,SubsiId       
                                                ,UOM           
                                                ,LineGuid      
                                                ,ReceiptQtyPc  
                                                ,ReceiptQtyCs  
                                                ,ReceiptQty    
                                                ,BarCodesStr
                                                ,ManBtchNum 
                                                ,ScanUser ";

                    var valueSql = @") values (
                                     @DOCENTRY
                                    , @LINENUM
                                    , @ITEMCODE
                                    , @ITEMNAME
                                    , @CODEBARS
                                    , @UOMQTY
                                    , @STOCKQTY
                                    , @QUANTITYCS
                                    , @QUANTITYPC
                                    , @QUANTITY
                                    , @PRICE
                                    , @LINETOTAL
                                    , @BASEDOCNUM
                                    , @SUPPCATNUM
                                    , @DISCOUNT
                                    , @FOC
                                    , @BASELINE
                                    , @BASEENTRY
                                    , @PONO
                                    , @REASON
                                    , @FROZENFOR
                                    , @OLDCODE
                                    , @LOTNO                                   
                                    , @REMARKS
                                    , @HEADGUID
                                    , @USERCODE
                                    , @USERNAME
                                    , @DELIVERY_ORDER 
                                    , @Subsi         
                                    , @SubsiId       
                                    , @UOM           
                                    , @LineGuid      
                                    , @ReceiptQtyPc  
                                    , @ReceiptQtyCs  
                                    , @ReceiptQty    
                                    , @BarCodesStr 
                                    , @ManBtchNum 
                                    , @ScanUser";

                    if (line.EXPDATE != default)
                    {
                        insertHeaderSql += ", EXPDATE";
                        valueSql += " , @EXPDATE";
                    }
                    if (line.MFRDATE != default)
                    {
                        insertHeaderSql += ", MFRDATE";
                        valueSql += " , @MFRDATE ";
                    }

                    // 20230111
                    // add in the indate for the table 
                    if (line.InDt != default)
                    {
                        insertHeaderSql += ", InDt";
                        valueSql += " , @InDt";
                    }

                    var combineSql = insertHeaderSql + valueSql + ")";
                    var insertRes_Lines = conn.Execute(combineSql, line, trans);

                    if (insertRes_Lines < 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, insert FTAPP_GRN1_FINAL error, PO: {dto.DeliveryOrderNo}, " +
                            $"Grn Entry : {dto.GrnDocEntry}");
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
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveGrnDraft(Grpo_Dto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
            {
                return BadRequest("Invalid purchase order / delivery order number");
            }
            if (dto.GrnDoc == null)
            {
                return BadRequest("Invalid Grn doc");
            }
            if (dto.GrnDoc.Lines == null)
            {
                return BadRequest("Invalid Grn doc lines");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid db info");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the R draft 
                var delete_sql = $@" Delete from {db.WEBDB}..FTAPP_GRN_DRAFT Where DELIVERY_ORDER = @DeliveryOrder; ";
                delete_sql += $@" Delete from {db.WEBDB}..FTAPP_GRN1_DRAFT Where DELIVERY_ORDER = @DeliveryOrder; ";

                var deleteDraftres = conn.Execute(delete_sql, new { DeliveryOrder = dto.DeliveryOrderNo }, trans);
                if (deleteDraftres < 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, PO # {dto.DeliveryOrderNo}, Delete FTAPP_GRN1_DRAFT and FTAPP_GRN1_DRAFT fail.");
                }

                //delete_sql = $@"Delete from {db.WEBDB}..FTAPP_GRN1_DRAFT Where DELIVERY_ORDER = @DeliveryOrder";
                //conn.Execute(delete_sql, new { DeliveryOrder = .DeliveryOrderNo }, trans);

                // insert into the draf                t
                var insert_sql = $@" INSERT INTO { db.WEBDB}..FTAPP_GRN_DRAFT (                                              
                                             DOCENTRY       
                                           , DOCNUM         
                                           , DOCDATE        
                                           , CARDCODE       
                                           , CARDNAME       
                                           , REFNO          
                                           , REMARKS                                                   
                                           , DELIVERY_ORDER 
                                           , ApproveUser
                                           , FILES 
                                          ) VALUES ( 
                                             @DOCENTRY     
                                           , @DOCNUM       
                                           , @DOCDATE      
                                           , @CARDCODE     
                                           , @CARDNAME     
                                           , @REFNO        
                                           , @REMARKS
                                           , @DELIVERY_ORDER 
                                           , @ApproveUser
                                           , @FILES 
                                          )";

                var insertHead = conn.Execute(insert_sql, dto.GrnDoc, trans);
                if (insertHead <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"{db.COMPANYNAME}, PO # {dto.DeliveryOrderNo}, Insert FTAPP_GRN_DRAFT fail.");
                }

                for (int i = 0; i < dto.GrnDoc.Lines.Count; i++)
                {
                    var line = dto.GrnDoc.Lines[i];
                    if (line == null) continue;

                    var insertHeaderSql = $@"insert into {db.WEBDB}..FTAPP_GRN1_DRAFT (
                                                DOCENTRY
                                                , LINENUM
                                                , ITEMCODE
                                                , ITEMNAME
                                                , CODEBARS
                                                , UOMQTY
                                                , STOCKQTY
                                                , QUANTITYCS
                                                , QUANTITYPC
                                                , QUANTITY
                                                , PRICE
                                                , LINETOTAL
                                                , BASEDOCNUM
                                                , SUPPCATNUM
                                                , DISCOUNT
                                                , FOC
                                                , BASELINE
                                                , BASEENTRY
                                                , PONO
                                                , REASON
                                                , FROZENFOR
                                                , OLDCODE
                                                , LOTNO
                                                , REMARKS
                                                , HEADGUID
                                                , USERCODE
                                                , USERNAME
                                                , DELIVERY_ORDER
                                                ,Subsi         
                                                ,SubsiId       
                                                ,UOM           
                                                ,LineGuid      
                                                ,ReceiptQtyPc  
                                                ,ReceiptQtyCs  
                                                ,ReceiptQty    
                                                ,BarCodesStr
                                                ,ManBtchNum 
                                                ,ScanUser ";

                    var valueSql = @") values (
                                     @DOCENTRY
                                    , @LINENUM
                                    , @ITEMCODE
                                    , @ITEMNAME
                                    , @CODEBARS
                                    , @UOMQTY
                                    , @STOCKQTY
                                    , @QUANTITYCS
                                    , @QUANTITYPC
                                    , @QUANTITY
                                    , @PRICE
                                    , @LINETOTAL
                                    , @BASEDOCNUM
                                    , @SUPPCATNUM
                                    , @DISCOUNT
                                    , @FOC
                                    , @BASELINE
                                    , @BASEENTRY
                                    , @PONO
                                    , @REASON
                                    , @FROZENFOR
                                    , @OLDCODE
                                    , @LOTNO                                   
                                    , @REMARKS
                                    , @HEADGUID
                                    , @USERCODE
                                    , @USERNAME
                                    , @DELIVERY_ORDER 
                                    , @Subsi         
                                    , @SubsiId       
                                    , @UOM           
                                    , @LineGuid      
                                    , @ReceiptQtyPc  
                                    , @ReceiptQtyCs  
                                    , @ReceiptQty    
                                    , @BarCodesStr 
                                    , @ManBtchNum 
                                    , @ScanUser";

                    if (line.EXPDATE != default)
                    {
                        insertHeaderSql += ", EXPDATE";
                        valueSql += " , @EXPDATE";
                    }
                    if (line.MFRDATE != default)
                    {
                        insertHeaderSql += ", MFRDATE";
                        valueSql += " , @MFRDATE ";
                    }

                    // 20230111
                    // add in the indate for the table 
                    if (line.InDt != default)
                    {
                        insertHeaderSql += ", InDt";
                        valueSql += " , @InDt";
                    }

                    var combineSql = insertHeaderSql + valueSql + ")";
                    var insertRes = conn.Execute(combineSql, line, trans);
                    if (insertRes <= 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, PO # {dto.DeliveryOrderNo}, Insert FTAPP_GRN1_DRAFT fail.");
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

        void InsertGrnLines(DbInfo db, SqlConnection conn, List<FTAPP_GRN1> lines, string tableName)
        {
            try
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line == null) continue;

                    var insertHeaderSql = $@"insert into { db.WEBDB}..{tableName}(
                                                DOCENTRY
                                                , LINENUM
                                                , ITEMCODE
                                                , ITEMNAME
                                                , CODEBARS
                                                , UOMQTY
                                                , STOCKQTY
                                                , QUANTITYCS
                                                , QUANTITYPC
                                                , QUANTITY
                                                , PRICE
                                                , LINETOTAL
                                                , BASEDOCNUM
                                                , SUPPCATNUM
                                                , DISCOUNT
                                                , FOC
                                                , BASELINE
                                                , BASEENTRY
                                                , PONO
                                                , REASON
                                                , FROZENFOR
                                                , OLDCODE
                                                , LOTNO
                                                , REMARKS
                                                , HEADGUID
                                                , USERCODE
                                                , USERNAME
                                                , DELIVERY_ORDER
                                                ,Subsi         
                                                ,SubsiId       
                                                ,UOM           
                                                ,LineGuid      
                                                ,ReceiptQtyPc  
                                                ,ReceiptQtyCs  
                                                ,ReceiptQty    
                                                ,BarCodesStr
                                                ,ManBtchNum 
                                                ,ScanUser ";

                    var valueSql = @") values (
                                     @DOCENTRY
                                    , @LINENUM
                                    , @ITEMCODE
                                    , @ITEMNAME
                                    , @CODEBARS
                                    , @UOMQTY
                                    , @STOCKQTY
                                    , @QUANTITYCS
                                    , @QUANTITYPC
                                    , @QUANTITY
                                    , @PRICE
                                    , @LINETOTAL
                                    , @BASEDOCNUM
                                    , @SUPPCATNUM
                                    , @DISCOUNT
                                    , @FOC
                                    , @BASELINE
                                    , @BASEENTRY
                                    , @PONO
                                    , @REASON
                                    , @FROZENFOR
                                    , @OLDCODE
                                    , @LOTNO                                   
                                    , @REMARKS
                                    , @HEADGUID
                                    , @USERCODE
                                    , @USERNAME
                                    , @DELIVERY_ORDER 
                                    , @Subsi         
                                    , @SubsiId       
                                    , @UOM           
                                    , @LineGuid      
                                    , @ReceiptQtyPc  
                                    , @ReceiptQtyCs  
                                    , @ReceiptQty    
                                    , @BarCodesStr 
                                    , @ManBtchNum 
                                    , @ScanUser";

                    if (line.EXPDATE != default)
                    {
                        insertHeaderSql += ", EXPDATE";
                        valueSql += " , @EXPDATE";
                    }
                    if (line.MFRDATE != default)
                    {
                        insertHeaderSql += ", MFRDATE";
                        valueSql += " , @MFRDATE ";
                    }

                    // 20230111
                    // add in the indate for the table 
                    if (line.InDt != default)
                    {
                        insertHeaderSql += ", InDt";
                        valueSql += " , @InDt";
                    }

                    var combineSql = insertHeaderSql + valueSql + ")";
                    var insertRes = conn.Execute(combineSql, line);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        void DeleteGRNDraft(DbInfo db, string deliveryOrder)
        {
            try
            {
                // delete the R draft 
                var delete_sql = $@"Delete from {db.WEBDB}..FTAPP_GRN_DRAFT Where DELIVERY_ORDER = @DeliveryOrder";
                var conn = new SqlConnection(_commDbConnStr);
                conn.Execute(delete_sql, new { DeliveryOrder = deliveryOrder });

                delete_sql = $@"Delete from {db.WEBDB}..FTAPP_GRN1_DRAFT Where DELIVERY_ORDER = @DeliveryOrder";
                conn.Execute(delete_sql, new { DeliveryOrder = deliveryOrder });
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        void DeleteGRNFinal(DbInfo db, string deliveryOrder, long GrnDocEntry)
        {
            try
            {
                // delete the R draft 
                var delete_sql = $@"Delete from {db.WEBDB}..FTAPP_GRN1_FINAL 
                                    Where DELIVERY_ORDER = @DeliveryOrder 
                                    and DocEntry = @GrnDocEntry";

                var conn = new SqlConnection(_commDbConnStr);
                conn.Execute(delete_sql, new { DeliveryOrder = deliveryOrder, GrnDocEntry = GrnDocEntry });
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult GetGrnForApproval(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.GrnDocEntry <= 0)
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                var conn = new SqlConnection(_commDbConnStr);

                // check does this user auth to approval this grn 
                // 20211021
                //@webDb as nvarchar(100),
                //@erpDb as nvarchar(100), 
                //@userCode as nvarchar(100), 
                //@docEntry as int

                var sq_checkAuth = @"exec sp_CheckUserCodeWithGrnAuth @webDb, @erpDb, @userCode, @docEntry";
                var isAuth = conn.Query<Grn>(sq_checkAuth, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    userCode = dto.UserCode,
                    docEntry = dto.GrnDocEntry
                }).FirstOrDefault();

                if (isAuth == null)
                {
                    return BadRequest($"User {dto.UserCode} " +
                        $"not authorised to approval of GRN# {dto.GrnDocEntry} or the doc was rejected.");
                }

                var sql = @$"Select '{db.COMPANYNAME}' [Subsi], '{db.COMPANYID}' [SubsiId],  t1.delivery_order [DeliveryOrderNo],
                            t0.* from {db.WEBDB}..GRN t0 with (nolock) 
                            left join {db.WEBDB}..FTAPP_GRN t1 with (nolock)  on t1.DocEntry = t0.DocEntry
                            Where t0.DocEntry = @DocEntry";


                var grnDoc = conn.Query<Grn>(sql, new { DocEntry = dto.GrnDocEntry }).FirstOrDefault();
                if (grnDoc == null) return NotFound();

                // query the line 
                sql = @$"Select * from {db.WEBDB}..GRN1 with (nolock) 
                            Where DocEntry = @DocEntry";

                grnDoc.Lines = conn.Query<Grn1>(sql, new { DocEntry = dto.GrnDocEntry }).ToList();

                // query the final grn1 line 
                sql = @$"Select * from {db.WEBDB}..FTAPP_GRN1_FINAL with (nolock) 
                            Where DocEntry = @DocEntry";

                grnDoc.Lines1 = conn.Query<FTAPP_GRN1>(sql, new { DocEntry = dto.GrnDocEntry }).ToList();
                return Ok(grnDoc);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CheckPONumOnHold(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    BadRequest("Invalid subsi");
                }

                if (string.IsNullOrWhiteSpace($"{dto.PoDocNum}"))
                {
                    BadRequest("Invalid purchase order num");
                }

                if (string.IsNullOrWhiteSpace($"{dto.UserCode}"))
                {
                    BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace($"{dto.UserName}"))
                {
                    BadRequest("Invalid user name");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var checkDupHoldSql = $@"Select * from {db.WEBDB}..FTAPP_GRN 
                                        Where DELIVERY_ORDER = @PoDocNum ";

                var res = new SqlConnection(_commDbConnStr)
                    .Query<FTAPP_GRN>(checkDupHoldSql, new { dto.PoDocNum }).FirstOrDefault();

                if (res == null)    // no onhold
                {
                    // put the po number onhold here
                    // create the GRN , if not exist
                    var newgrn = new FTAPP_GRN
                    {
                        GUID = Guid.NewGuid(),
                        DELIVERY_ORDER = $"{dto.PoDocNum}",
                        DOCSTATUS = "R",// receiving, C completed
                        UCREATED = dto.UserCode,
                        REMARKS = dto.UserName
                    };

                    var insertGrn = @$"insert into {db.WEBDB}..FTAPP_GRN 
                                    ( GUID, DELIVERY_ORDER , DOCSTATUS, UCREATED, REMARKS, DCREATED) 
                                    values (@GUID, @DELIVERY_ORDER, @DOCSTATUS, @UCREATED, @REMARKS, GETDATE() ) ";

                    var conn = new SqlConnection(_commDbConnStr);
                    conn.Execute(insertGrn, newgrn);
                    Task.Delay(300);

                    return Ok();
                }

                if (res.UCREATED.Equals(dto.UserCode))
                {
                    return Ok(); // if the requested is the same user id allow to continue.
                }

                // onhold // yes
                return BadRequest($"PO#{res.DELIVERY_ORDER} hold by {res.UCREATED}, {res.REMARKS} " +
                                    $"starting {res.DCREATED:dd-MM-yyyy hh:mm tt}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadAllGrnLinesWithPoNumber(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                {
                    return BadRequest("invalid delivery order");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi [DB]");
                }

                var sql = $@"select distinct t1.*
                            from {db.WEBDB}..FTAPP_GRN t0 with (nolock)
                            inner join {db.WEBDB}..FTAPP_GRN1 t1 with (nolock) on t1.HEADGUID = t0.GUID
                            Where t0.DELIVERY_ORDER = @deliveryOrder
                            order by PONO, BASELINE, USERCODE";

                var lines = new SqlConnection(_commDbConnStr)
                    .Query<FTAPP_GRN1>(sql, new { deliveryOrder = dto.DeliveryOrderNo }).ToList();

                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveFTAPP_GRNLines(Grpo_Dto dto)
        {
            try
            {// save individual user grn line 
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.GrnDoc == null)
                {
                    return BadRequest("Invalid grn doc");
                }
                if (dto.GrnDoc.Lines == null)
                {
                    return BadRequest("Invalid grn doc line");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi [DB]");
                }

                // clear the previous line 
                var deleteLines = @$"Delete from {db.WEBDB}..FTAPP_GRN1 
                                WHERE USERCODE = @USERCODE 
                                AND HEADGUID = @HEADGUID";

                var conn = new SqlConnection(_commDbConnStr);
                conn.Open();
                var delRes = conn.Execute(deleteLines, new
                {
                    USERCODE = dto.UserCode,
                    HEADGUID = dto.GrnDoc.GUID
                });

                for (int i = 0; i < dto.GrnDoc.Lines.Count; i++)
                {
                    var line = dto.GrnDoc.Lines[i];
                    if (line == null) continue;

                    var insertHeaderSql = $@"insert into { db.WEBDB}..FTAPP_GRN1(
                                                DOCENTRY
                                                , LINENUM
                                                , ITEMCODE
                                                , ITEMNAME
                                                , CODEBARS
                                                , UOMQTY
                                                , STOCKQTY
                                                , QUANTITYCS
                                                , QUANTITYPC
                                                , QUANTITY
                                                , PRICE
                                                , LINETOTAL
                                                , BASEDOCNUM
                                                , SUPPCATNUM
                                                , DISCOUNT
                                                , FOC
                                                , BASELINE
                                                , BASEENTRY
                                                , PONO
                                                , REASON
                                                , FROZENFOR
                                                , OLDCODE
                                                , LOTNO
                                                , REMARKS
                                                , HEADGUID
                                                , USERCODE
                                                , USERNAME
                                                , ScanUser ";

                    var valueSql = @") values(
                                     @DOCENTRY
                                    , @LINENUM
                                    , @ITEMCODE
                                    , @ITEMNAME
                                    , @CODEBARS
                                    , @UOMQTY
                                    , @STOCKQTY
                                    , @QUANTITYCS
                                    , @QUANTITYPC
                                    , @QUANTITY
                                    , @PRICE
                                    , @LINETOTAL
                                    , @BASEDOCNUM
                                    , @SUPPCATNUM
                                    , @DISCOUNT
                                    , @FOC
                                    , @BASELINE
                                    , @BASEENTRY
                                    , @PONO
                                    , @REASON
                                    , @FROZENFOR
                                    , @OLDCODE
                                    , @LOTNO                                   
                                    , @REMARKS
                                    , @HEADGUID
                                    , @USERCODE
                                    , @USERNAME 
                                    , @ScanUser ";

                    if (line.EXPDATE != default)
                    {
                        insertHeaderSql += ", EXPDATE";
                        valueSql += " , @EXPDATE";
                    }
                    if (line.MFRDATE != default)
                    {
                        insertHeaderSql += ", MFRDATE";
                        valueSql += " , @MFRDATE ";
                    }

                    // line entry dates
                    if (line.InDt != default) // 20220123
                    {
                        insertHeaderSql += ", InDt";
                        valueSql += " , @InDt ";
                    }

                    var combineSql = insertHeaderSql + valueSql + ")";
                    var insertRes = conn.Execute(combineSql, line);
                }
                return Ok();

                // why no update the grn / grn draft table 

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadGrnLinesWithDoNumber(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                {
                    return BadRequest("invalid delivery order");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest("invalid user name");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi [DB]");
                }

                var sqlgrn = @$"SELECT
                                      ID
                                    , DOCENTRY
                                    , DOCNUM
                                    , DOCDATE
                                    , DOCSTATUS
                                    , CARDCODE
                                    , CARDNAME
                                    , REFNO
                                    , WHSCODE
                                    , TOWHS
                                    , REMARKS
                                    , POSTENTRY
                                    , POSTDATE
                                    , APPRREM
                                    , APPRLEVEL
                                    , CURRLEVEL
                                    , UCREATED
                                    , UMODIFIED
                                    , DCREATED
                                    , DMODIFIED
                                    , BOP
                                    , GRNTYPE
                                    , AVREASONCODE
                                    , DRAFT_STATUS
                                    , GUID
                                    , DELIVERY_ORDER
                                    , POSTDT
                                    , APPROVEDDT
                                    , ApproveUser
                                    , ISNULL(FILES, (SELECT TOP 1 FILES FROM {db.WEBDB}..FTAPP_GRN_DRAFT t1 Where t1.DELIVERY_ORDER = @DeliveryOrderNo)) as [FILES]
                            FROM {db.WEBDB}..FTAPP_GRN with (nolock)
                            WHERE DELIVERY_ORDER = @DeliveryOrderNo ";

                var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<FTAPP_GRN>(sqlgrn, new
                {
                    DeliveryOrderNo = dto.DeliveryOrderNo,
                }).FirstOrDefault();

                if (result == null)
                {
                    // create the GRN , if not exist
                    var newgrn = new FTAPP_GRN
                    {
                        GUID = Guid.NewGuid(),
                        DELIVERY_ORDER = dto.DeliveryOrderNo,
                        DOCSTATUS = "R",// receiving, C completed
                        UCREATED = dto.UserCode,
                        REMARKS = dto.UserName
                    };

                    var insertGrn = @$"insert into {db.WEBDB}..FTAPP_GRN 
                                    ( GUID, DELIVERY_ORDER , DOCSTATUS, UCREATED, REMARKS, DCREATED) 
                                    values (@GUID, @DELIVERY_ORDER, @DOCSTATUS, @UCREATED, @REMARKS, GETDATE() ) ";

                    conn.Execute(insertGrn, newgrn);
                    return Ok(newgrn);
                }

                // load in the line by created user
                var sql_queryLines = @$"SELECT * 
                            FROM {db.WEBDB}..FTAPP_GRN1 with (nolock)
                            WHERE HEADGUID = @HEADGUID 
                            AND USERCODE = @USERCODE";

                var lines = conn.Query<FTAPP_GRN1>(sql_queryLines,
                    new
                    {
                        HEADGUID = result.GUID,
                        USERCODE = dto.UserCode
                    }).ToList();

                // load in the lines barcode 
                if (lines != null && lines.Count > 0)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var sql = $@"SELECT * FROM [{db.SAPDB}]..[OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                        var item = lines[i];
                        if (item == null) continue;

                        if (item.UOMQTY == 0) lines[i].UOMQTY = 1;
                        lines[i].UOM = item.UOMQTY == 1 ? "PC" : "CS";

                        // load item code related barcode
                        lines[i].BarCodes = conn.Query<OBCD_Ext>(sql, new { itemcode = item.ITEMCODE }).ToList();
                        if (lines[i].BarCodes == null) lines[i].BarCodes = new List<OBCD_Ext>();

                        //add itemcode as barcode
                        var itemCode = new OBCD_Ext
                        {
                            BcdEntry = -1,
                            BcdCode = lines[i].ITEMCODE,
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
                            BcdCode = lines[i].CODEBARS,
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
                            BcdCode = lines[i].SUPPCATNUM,
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
                    }
                }

                // assign to the doc head
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

        IActionResult GetPOsItems1(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Invalid agency code");
                }

                if (string.IsNullOrWhiteSpace(dto.PoDocEntries))
                {
                    return BadRequest("Invalid PO doc entries");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi [DB]");
                }

                // hide for multiple po
                //var poEntries = dto.PoDocEntries.Split(",");
                //if (poEntries == null)
                //{
                //    return BadRequest("Invalid selected POs [ON]");
                //}
                //if (poEntries.Length == 0)
                //{
                //    return BadRequest("Invalid selected POs [ZL]");
                //}

                // loop eah po entry to look for the lines
                var conn = new SqlConnection(_commDbConnStr);

                //List<FTAPP_GRN1> grnLines = null;
                //for (int p = 0; p < poEntries.Length; p++)
                //{
                var poEntry = dto.PoDocEntries; // $"{poEntries[p]}".Trim();
                                                //if (string.IsNullOrWhiteSpace(poEntry)) continue;

                var sp_checkPoLine = $@"exec sp_SelectPoLineAsGrn_MulPO @erpDb, @poDocEntry, @subsi, @subsiID ";

                var lines = conn.Query<FTAPP_GRN1>(sp_checkPoLine, new
                {
                    erpDb = db.SAPDB,
                    poDocEntry = poEntry,
                    subsi = db.COMPANYNAME,
                    subsiID = db.COMPANYID
                }).ToList();

                //if (lines.Count == 0) continue;

                // query and prepare the code
                for (int i = 0; i < lines.Count; i++)
                {
                    var sql = $@"SELECT * FROM [{db.SAPDB}]..[OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var item = lines[i];
                    if (item == null) continue;

                    item.LineGuid = Guid.NewGuid();

                    if (item.UOMQTY == 0) lines[i].UOMQTY = 1;
                    lines[i].UOM = item.UOMQTY == 1 ? "PC" : "CS";

                    // load item code related barcode
                    lines[i].BarCodes = conn.Query<OBCD_Ext>(sql, new { itemcode = item.ITEMCODE }).ToList();
                    if (lines[i].BarCodes == null) lines[i].BarCodes = new List<OBCD_Ext>();

                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].ITEMCODE,
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
                        BcdCode = lines[i].CODEBARS,
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
                        BcdCode = lines[i].SUPPCATNUM,
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

                    //if (grnLines == null) grnLines = new List<FTAPP_GRN1>();
                    //grnLines.Add(lines[i]);
                    //}
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

        // return list of GRN
        IActionResult GetGrns(Grpo_Dto dto)
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
                if (dto.GrnStartDt == default)
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.GrnEndDt == default)
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Invalid user code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db infor");
                }

                var sp_query = @"sp_SelectGrns @webDb, @startDt, @enddt, @userCode, @subsi , @subsiID, @agencyCode";
                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<Grn>(sp_query, new
                {
                    webDb = db.WEBDB,
                    startDt = dto.GrnStartDt,
                    enddt = dto.GrnEndDt,
                    userCode = dto.UserCode,
                    subsi = db.COMPANYNAME,
                    subsiID = db.COMPANYID,
                    agencyCode = dto.AgencyCode
                }).ToList();

                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                // load in the grn line
                for (int l = 0; l < results.Count; l++)
                {
                    var sqlQueryLines = @"exec sp_GetGrnLines  @webDb , @grnDocEntry,  @subsi, @subsiId";
                    results[l].Lines = conn.Query<Grn1>(sqlQueryLines, new
                    {
                        webDb = db.WEBDB,
                        grnDocEntry = results[l].Docentry,
                        subsi = db.COMPANYNAME,
                        subsiId = db.COMPANYID
                    }).ToList();
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

        IActionResult PostGRN(Grpo_Dto dto)
        {
          
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Invalid company id");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveDocType))
                {
                    return BadRequest("Invalid Save Doc Type");
                }
                if (string.IsNullOrWhiteSpace(dto.Token))
                {
                    return BadRequest("Invalid Save Doc token");
                }
                if (dto.Doc1 == null)
                {
                    return BadRequest("Invalid Save Doc [NL]");
                }

                //if (string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                //{
                //    return BadRequest("DO number invalid");
                //}

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db infor");
                }

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($@"{svrAdr}GoodsReceiptPO/{dto.CompanyId}/{dto.SaveDocType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.Token}");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", dto.Doc1, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    // else no success
                    var newLog = new FTAPP_AppPostLog
                    {
                        AppModule = "GRN",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = $"{dto.Subsi}",
                        Details = $"PO# {dto.PoDocNum},{dto.PoDocEntry}, {dto.SaveDocType}, Received: {response.Content}",
                        PostResult = "Fail",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    AppPostLogging(newLog);
                    var newReplied = new PortalReplied
                    {
                        actionSuccess = false,
                        actionResult = "",
                        errorMessage = response.Content,
                        documentStatus = ""
                    };

                    return BadRequest(newReplied); // no success 
                }

                var replied = JsonConvert.DeserializeObject<PortalReplied>(response.Content);

                // no success replied 
                if (replied == null)
                {
                    // no success replied is empty
                    var newLog1 = new FTAPP_AppPostLog
                    {
                        AppModule = "GRN",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = $"{dto.Subsi}",
                        Details = $"PO# {dto.PoDocNum},{dto.PoDocEntry}, {dto.SaveDocType}, Received: {response.Content}",
                        PostResult = "Fail",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    //if (isValidJson)
                    //{
                    //    var replied1 = JsonConvert.DeserializeObject<PortalReplied>(response.Content);
                    //    AppPostLogging(newLog1);
                    //    return BadRequest(replied1?.errorMessage + "\n\n" + response.Content);
                    //}

                    AppPostLogging(newLog1);
                    return BadRequest(response.Content);
                }

                // success
                var log = new FTAPP_AppPostLog
                {
                    AppModule = "GRN",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"PO# {dto.PoDocNum},{dto.PoDocEntry}, {dto.SaveDocType}, Received: {response.Content}",
                    PostResult = $"{replied.actionResult}",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                AppPostLogging(log);

                // update the FTAPP_GRN and FTAPP_GRN1 to avoid reselect
                // receiving, C completed

                // update onhold table for the docentry
                using var conn = new SqlConnection(_commDbConnStr);
                conn.Open();
                using var trans = conn.BeginTransaction();

            try 
            { 

                if (!string.IsNullOrWhiteSpace(dto.DeliveryOrderNo))
                {
                    var updateSql = @$"UPDATE {db.WEBDB}..FTAPP_GRN 
                                                Set DOCSTATUS = 'C', DOCENTRY = @DocEntry , Files = @Files
                                                Where DELIVERY_ORDER = @DeliveryOrder";


                    var update_res = conn.Execute(updateSql, new
                    {
                        DeliveryOrder = dto.DeliveryOrderNo,
                        DocEntry = replied.actionResult,
                        Files = dto.Files
                    }, trans);

                    if (update_res < 0)
                    {
                        trans.Rollback();
                        return BadRequest($"{db.COMPANYNAME}, Update FTAPP_GRN fail, PO# {dto.DeliveryOrderNo} ");
                    }
                }

                trans.Commit();
                // else replied 
                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var newLog1 = new FTAPP_AppPostLog
                {
                    AppModule = "GRN",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.CardCode}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"PO# {dto.PoDocNum},{dto.PoDocEntry}, {dto.SaveDocType}, Received: {e.Message}",
                    PostResult = "Fail",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };

                AppPostLogging(newLog1);
                trans.Rollback();
                return BadRequest($"request not handler.\n{LastError}");
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

        IActionResult GetUserAgencies(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company id");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi and db info");
                }

                var sp_query = @"exec sp_Select_Grpo_WhsUserAgency @webDb , @erpDb, @userCode, @subsi, @subsiid";
                var results = new SqlConnection(_commDbConnStr).Query<OCRD_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    userCode = dto.UserCode,
                    subsi = db.COMPANYNAME,
                    subsiid = db.COMPANYID
                }).ToList();

                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {

                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetPOs(Grpo_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid sub si");
                }
                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Invalid agency code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi and db info");
                }

                var sp_query = @"exec sp_SelectPOs @webDb,
                                                   @erpDb,
                                                   @userCode,
                                                   @subsi, 
                                                   @subsiId,
                                                   @agencyCode";

                var results = new SqlConnection(_commDbConnStr).Query<OPOR_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    userCode = dto.UserCode,
                    subsi = db.COMPANYNAME,
                    subsiId = db.COMPANYID,
                    agencyCode = dto.AgencyCode
                }).ToList();

                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                // remove open line = 0 POs
                var massgeResult = results.Where(x => x.TotalLine > 0).ToList();

                if (massgeResult == null) return NotFound();
                if (massgeResult.Count == 0) return NotFound();

                return Ok(massgeResult);
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
