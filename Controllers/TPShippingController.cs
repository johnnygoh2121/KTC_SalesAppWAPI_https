using Dapper;
using KTC_SalesAppWAPI.DTOs.TPShipping;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.Pack;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.TPShipping;
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
    public class TPShippingController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<TPShippingController> _logger;

        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public TPShippingController(IConfiguration configuration, ILogger<TPShippingController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(Dto_TpShipping dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetTpPackedBoxInfo":
                    {
                        return GetTpPackedBoxInfo(dto);
                    }
                case "Save":
                    {
                        return Save(dto);
                    }
                case "Update":
                    {
                        return Update(dto);
                    }
                case "CheckPackedIdExist":
                    {
                        return CheckPackedIdExist(dto);
                    }
                case "GetSavedCarton":
                    {
                        return GetSavedCarton(dto);
                    }
                case "RemoveLines":
                    {
                        return RemoveLines(dto);
                    }
                case "GetStudioInfo":
                    {
                        return GetStudioInfo(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult GetStudioInfo(Dto_TpShipping dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.Studio))
                {
                    return BadRequest("Invalid studio");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info by subsi");
                }

                var query = $@"Select * from {db.SAPDB}..OCRD with (nolock) 
                               Where U_TPCODE = @U_TPCODE";

                query = $@"select  t2.descript [TerritoryName], t0.* 
                            from  {db.SAPDB}..[OCRD] T0 WITH (NOLOCK)
	                        INNER JOIN {db.SAPDB}..[OPRC] t1 with (nolock)  on t1.PrcCode = T0.U_COSTCTR 
	                        INNER JOIN {db.SAPDB}..[OTER] t2 with (nolock)  on t2.territryID = T0.Territory
	                        Where U_TPCODE = @U_TPCODE";

                var conn = new SqlConnection(_commDbConnStr);
                var studio = conn.Query<OCRD_Ext>(query, new { U_TPCODE = dto.Studio }).FirstOrDefault();

                if (studio == null) return null;

                studio.CompanyID = db.COMPANYID;
                studio.CompanyName = db.COMPANYNAME;
                var processedCard = ProcessCardGLN(studio, conn, db);

                return Ok(processedCard);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // process the crd gps and address 
        OCRD_Ext ProcessCardGLN(OCRD_Ext card, SqlConnection conn, DbInfo db)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(card.GlblLocNum))
                {
                    var glnArray = card.GlblLocNum.Split(',');
                    if (glnArray?.Length < 2) return card; // cont. next

                    card.Latitude = SafeGetDouble(glnArray[0]); // actual code
                    card.Longitude = SafeGetDouble(glnArray[1]);
                }

                // get the bill address 
                // get the s address type 
                // get shipment address
                var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                var bill_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = card.CardCode }).FirstOrDefault();
                if (bill_address != null)
                {
                    card.Address = bill_address.GetAddress();
                }

                // get the s address type 
                // get shipment address
                sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='S'";

                var ship_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = card.CardCode }).FirstOrDefault();
                if (ship_address != null)
                {
                    card.ShipAdd = ship_address.GetAddress();
                }
                return card;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return card;
            }
        }

        double SafeGetDouble(string _value)
        {
            try
            {
                var isNumeric = Double.TryParse(_value, out double result);
                if (isNumeric) return result;
                return -1;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        IActionResult RemoveLines(Dto_TpShipping dto)
        {

            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.Lines == null)
            {
                return BadRequest("Invalid removing lines [N]");
            }
            if (dto.Lines.Count == 0)
            {
                return BadRequest("Invalid removing lines [Z]");
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
                for (int id = 0; id < dto.Lines.Count; id++)
                {
                    var line = dto.Lines[id];
                    if (line == null) continue;

                    var sql = @$"Delete from {db.WEBDB}..FTAPP_TallySheet1
                        Where LineGuid  = @LineGuid";

                    var found = conn.Execute(sql, new
                    {
                        LineGuid = line.LineGuid
                    }, trans);
                }

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        // get full list of the cartin running number
        IActionResult GetSavedCarton(Dto_TpShipping dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ScanInCode))
                {
                    return BadRequest("Invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @$"select * from {db.WEBDB}..FTAPP_TallySheet1 with (nolock)
                        Where InvNo = @InvoiceNo 
                              AND ScanInCode = @ScanInCode";

                var conn = new SqlConnection(_commDbConnStr);
                var found = conn.Query<FTAPP_TallySheet1>(sql, new
                {
                    InvoiceNo = dto.InvoiceNo,
                    ScanInCode = dto.ScanInCode
                }).FirstOrDefault();

                if (found == null) return NotFound();

                // query the carton head 

                var head_sql = $@"Select * from {db.WEBDB}..FTAPP_TallySheet with (nolock) 
                                    Where ShippingCartonNo = @ShippingCartonNo ";

                var carton = conn.Query<FTAPP_TallySheet>(head_sql, new { ShippingCartonNo = found.ShippingCartonNo }).FirstOrDefault();
                if (carton != null)
                {
                    // get all it contain line 
                    var lines_sql = $@"Select * from {db.WEBDB}..FTAPP_TallySheet1 with (nolock) 
                                    Where ShippingCartonNo = @ShippingCartonNo ";

                    carton.Lines = conn.Query<FTAPP_TallySheet1>(lines_sql, new
                    {
                        ShippingCartonNo = found.ShippingCartonNo
                    }).ToList();

                    return Ok(carton); // saved record
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CheckPackedIdExist(Dto_TpShipping dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.PackedId))
                {
                    return BadRequest("Invalid packed id");
                }
                if (string.IsNullOrWhiteSpace(dto.Studio))
                {
                    return BadRequest("Invalid studio");
                }
                if (string.IsNullOrWhiteSpace(dto.OrderNo))
                {
                    return BadRequest("Invalid order no");
                }
                if (string.IsNullOrWhiteSpace(dto.OrderDate))
                {
                    return BadRequest("Invalid order date");
                }
                
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @$"select * from {db.WEBDB}..FTAPP_TallySheet1 with (nolock) 
                            Where Packedid = @PackedId
                                and Studio = @Studio
                                and OrderNo = @OrderNo
                                and OrderDate = @OrderDate";

                var dupl = new SqlConnection(_commDbConnStr).Query<FTAPP_TallySheet1>(sql, new
                {
                    PackedId = dto.PackedId,
                    Studio = dto.Studio,
                    OrderNo = dto.OrderNo,
                    OrderDate = dto.OrderDate,                    
                }).FirstOrDefault();

                if (dupl == null) return Ok("No Duplicate");
                return BadRequest($"Packid {dto.PackedId} already add in carton run #{dupl.ShippingCartonNo}");
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTpPackedBoxInfo(Dto_TpShipping dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
                {
                    return BadRequest("Invalid Invoice No");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                // 
                var sp_query = @"exec Sp_GetPackedBoxInfo @webDb, @erpDb, @invoiceNo";
                var conn = new SqlConnection(_commDbConnStr);

                var info = conn.Query<TP_PackedBoxInfo>(sp_query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    invoiceNo = dto.InvoiceNo
                }).FirstOrDefault();

                if (info == null) return NotFound();

                return Ok(info);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Update(Dto_TpShipping dto)
        {

            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid user code");
            }
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                return BadRequest("Invalid user name");
            }
            if (string.IsNullOrWhiteSpace(dto.Studio))
            {
                return BadRequest("Invalid studio");
            }
            if (string.IsNullOrWhiteSpace(dto.OrderDate))
            {
                return BadRequest("Invalid OrderDate");
            }
            if (string.IsNullOrWhiteSpace(dto.AppVersion))
            {
                return BadRequest("Invalid AppVersion");
            }
            if (dto.Lines == null)
            {
                return BadRequest("Invalid saving lines");
            }
            if (string.IsNullOrWhiteSpace(dto.ShippingCartonNo))
            {
                return BadRequest("Invalid Shipping Carton No");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid db name");
            }

            var conn = new SqlConnection(_commDbConnStr);
            var sql_head = @$"Select * from {db.WEBDB}..FTAPP_Tallysheet
                                Where ShippingCartonNo = @ShippingCartonNo";

            var carton = conn.Query<FTAPP_TallySheet>(sql_head,
                new
                {
                    ShippingCartonNo = dto.ShippingCartonNo
                }).FirstOrDefault();

            if (carton == null)
            {
                return BadRequest("Head carton no found, update fail. Pls contact support.");
            }

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var del_sql = $@"Delete from {db.WEBDB}..FTAPP_TallySheet1 
                                Where HeadGuid = @HeadGuid";

                var res = conn.Execute(del_sql, new { HeadGuid = carton.HeadGuid }, trans);

                var tallySheetLines = new List<FTAPP_TallySheet1>();
                for (int x = 0; x < dto.Lines.Count; x++)
                {
                    var line = dto.Lines[x];
                    if (line == null) continue;

                    var newline = CreateTallySheetLine(db, conn, line, carton, trans);
                    if (newline != null)
                    {
                        tallySheetLines.Add(newline);
                    }
                }

                // insert the line 
                var insertLines = @$" INSERT INTO {db.WEBDB}..FTAPP_TallySheet1 (                                                       
                                                         HeadGuid
                                                       , LineGuid
                                                       , CtnNo
                                                       , OrderNo
                                                       , SoDocEntry
                                                       , OrderDate
                                                       , Studio
                                                       , ShippingCartonNo 
                                                       , PackedId
                                                       , InvNo
                                                       , ScanInCode
                                                       , BoxId
                                                       , OrigOrderNo
                                                       , OrderType
                                                       , ItemCode
                                                    ) VALUES ( 
                                                           @HeadGuid
                                                         , @LineGuid
                                                         , @CtnNo
                                                         , @OrderNo
                                                         , @SoDocEntry
                                                         , @OrderDate
                                                         , @Studio
                                                         , @ShippingCartonNo
                                                         , @PackedId
                                                         , @InvNo
                                                         , @ScanInCode
                                                         , @BoxId
                                                         , @OrigOrderNo
                                                         , @OrderType
                                                         , @ItemCode
                                                    )";


                res = conn.Execute(insertLines, tallySheetLines, trans);
                trans.Commit();
                return Ok(carton);
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        FTAPP_TallySheet1 CreateTallySheetLine(DbInfo db, SqlConnection conn, TP_PackedBoxInfo line,
            FTAPP_TallySheet carton, SqlTransaction trans)
        {
            try
            {
                var newLine = new FTAPP_TallySheet1
                {
                    HeadGuid = carton.HeadGuid,
                    LineGuid = Guid.NewGuid(),
                    CtnNo = GetCtnNo(line.PackedId),
                    OrderNo = int.Parse(line.OrderNo),
                    SoDocEntry = line.SoDocEntry,
                    OrderDate = $"{line.OrderDate:ddMMyy}",
                    Studio = line.StudioCode,
                    ShippingCartonNo = carton.ShippingCartonNo,
                    PackedId = line.PackedId,
                    InvNo = line.InvNo,
                    ScanInCode = line.ScanInCode,
                    OrderType = line.ODRTYPE
                };

                // get the box id
                // ----------------------------------------------------------
                var sql = $@"Select distinct   t0.BoxId
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

                                from {db.WEBDB}..FTAPP_Box t0  with (nolock)
                                Where BaseEntry = @BaseEntry 
                                and PackId = @PackId";

                var packId = $"{line.PackedId}".Trim().Replace("/", " / ");
                var box = conn.Query<FTAPP_Box>(sql, new
                {
                    BaseEntry = line.SoDocEntry,
                    PackId = packId
                }, trans).FirstOrDefault();
                newLine.BoxId = $"{box?.BoxId}";

                // query the box content                    
                // ----------------------------------------------------------
                sql = $@"select 
                              ItemCode
                            , ItemName
                            , Qty
                            , Packaging
                            , BoxGuid
                            , ContentGuid
                            , BaseEntry
                            , BaseLine
                            from {db.WEBDB}..FTAPP_BOX1 with (nolock)
                          Where BoxGuid = @BoxGuid";

                var boxContent = conn.Query<FTAPP_Box1>(sql, new { BoxGuid = box.BoxGuid }, trans).FirstOrDefault();

                // get the org order no                    
                // ----------------------------------------------------------
                sql = $@"select * from {db.WEBDB}..SO1 with (nolock)
                            Where DOCENTRY = @DocEntry 
                            and LINENUM = @LineNum
                            and ItemCode = @ItemCode ";

                var so1 = conn.Query<SO1>(sql, new
                {
                    DocEntry = box.BaseEntry,
                    LineNum = boxContent?.BaseLine,
                    ItemCode = boxContent.ItemCode
                }, trans).FirstOrDefault();

                newLine.OrigOrderNo = $"{so1.REFORDER}";

                // settle the item code
                if (string.IsNullOrWhiteSpace($"{so1.SUPPCATNUM}"))
                {
                    sql = $@"Select * from {db.SAPDB}..OITM with (nolock) Where ItemCode = @ItemCode";
                    var item = conn.Query<OITM_Ext>(sql, new
                    {
                        ItemCode = so1.ITEMCODE
                    }, trans).FirstOrDefault();

                    newLine.ItemCode = item.SuppCatNum;
                }
                else
                {
                    newLine.ItemCode = so1.SUPPCATNUM;
                }

                return newLine;
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult Save(Dto_TpShipping dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid user code");
            }
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                return BadRequest("Invalid user name");
            }
            if (string.IsNullOrWhiteSpace(dto.Studio))
            {
                return BadRequest("Invalid studio");
            }
            if (string.IsNullOrWhiteSpace(dto.OrderDate))
            {
                return BadRequest("Invalid OrderDate");
            }
            if (string.IsNullOrWhiteSpace(dto.AppVersion))
            {
                return BadRequest("Invalid AppVersion");
            }
            if (dto.Lines == null)
            {
                return BadRequest("Invalid saving lines");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid db name");
            }

            // create the carton 
            var sql_checkNum = @$"Select * from {db.WEBDB}..FTAPP_TPStudioCartonRunNo
                                     Where OrderDate= @OrderDate 
                                            and Studio = @Studio ";

            using var conn = new SqlConnection(_commDbConnStr);
            var currentCartonNo = conn.Query<FTAPP_TPStudioCartonRunNo>(sql_checkNum, new
            {
                OrderDate = dto.OrderDate,
                Studio = dto.Studio
            }).FirstOrDefault();

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // insert new 
                if (currentCartonNo == null)
                {
                    currentCartonNo = new FTAPP_TPStudioCartonRunNo
                    {
                        OrderDate = dto.OrderDate,
                        Studio = dto.Studio,
                        CurrentCartonNo = 1
                    };

                    var insertCartonSql = @$"insert into {db.WEBDB}..FTAPP_TPStudioCartonRunNo (
                                            OrderDate 
                                        , Studio
                                        , CurrentCartonNo
                                        ) values (
                                          @OrderDate 
                                        , @Studio
                                        , @CurrentCartonNo )";

                    conn.Execute(insertCartonSql, currentCartonNo, trans);
                    // update the 
                    var insertRes = CreateRunNo_ConsultantLog(dto, currentCartonNo, db, conn, trans);
                    if (insertRes == -1)
                    {
                        trans.Rollback();
                        return BadRequest("Error insert Loging, request not handler, rollback [NP]");
                    }
                }
                else // increase 1 and update
                {
                    currentCartonNo.CurrentCartonNo++;
                    var updateCartonNo = $@"Update {db.WEBDB}..FTAPP_TPStudioCartonRunNo
                                                    SET CurrentCartonNo = @CurrentCartonNo 
                                                    Where OrderDate= @OrderDate 
                                                        and Studio = @Studio 
                                                        and id = @Id";

                    conn.Execute(updateCartonNo, currentCartonNo, trans);
                    var insertRes = CreateRunNo_ConsultantLog(dto, currentCartonNo, db, conn, trans);
                    if (insertRes == -1)
                    {
                        trans.Rollback();
                        return BadRequest("Error insert Loging, request not handler, rollback  [EP]");
                    }
                }

                // create the Carton list
                // insert it line 
                // check duplication

                var formatedStudio = dto.Studio.Substring(dto.Studio.Length - 4, 4);
                var newCarton = new FTAPP_TallySheet
                {
                    Studio = dto.Studio,
                    OrderDate = dto.OrderDate,
                    ShippingCartonNo = $"{dto.OrderDate}-{formatedStudio}-{currentCartonNo.CurrentCartonNo.ToString("D4")}",
                    RunNo = currentCartonNo.CurrentCartonNo,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    TransDt = DateTime.Now,
                    HeadGuid = Guid.NewGuid()
                };

                var checkDup_sql = $@"Select * from {db.WEBDB}..FTAPP_TallySheet 
                                      Where ShippingCartonNo = @ShippingCartonNo ";

                var dupl = conn.Query<FTAPP_TallySheet>(checkDup_sql, new
                {
                    ShippingCartonNo = newCarton.ShippingCartonNo
                }, trans).FirstOrDefault();

                if (dupl != null) // perform insert
                {
                    var deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TallySheet 
                                       Where ShippingCartonNo = @ShippingCartonNo ";

                    conn.Execute(deleteSql, new { ShippingCartonNo = newCarton.ShippingCartonNo }, trans);

                    // delete the line too 
                    deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TallySheet1
                                       Where ShippingCartonNo = @ShippingCartonNo ";

                    conn.Execute(deleteSql, new { ShippingCartonNo = newCarton.ShippingCartonNo }, trans);
                }

                var tallySheetLines = new List<FTAPP_TallySheet1>();
                for (int x = 0; x < dto.Lines.Count; x++)
                {
                    var line = dto.Lines[x];
                    if (line == null) continue;

                    var newline = CreateTallySheetLine(db, conn, line, newCarton, trans);
                    if (newline != null)
                    {
                        tallySheetLines.Add(newline);
                    }
                }

                // insert the carton record 
                var insertHead = @$"INSERT INTO {db.WEBDB}..FTAPP_TallySheet (
                                                     Studio
                                                   , OrderDate
                                                   , ShippingCartonNo
                                                   , RunNo
                                                   , UserCode
                                                   , UserName 
                                                   , TransDt
                                                   , HeadGuid 
                                                ) VALUES ( 
                                                   @Studio
                                                 , @OrderDate
                                                 , @ShippingCartonNo
                                                 , @RunNo
                                                 , @UserCode
                                                 , @UserName 
                                                 , GETDATE()
                                                 , @HeadGuid 
                                                )";

                var res = conn.Execute(insertHead, newCarton, trans);

                // insert the line 
                var insertLines = @$" INSERT INTO {db.WEBDB}..FTAPP_TallySheet1 (                                                       
                                                         HeadGuid
                                                       , LineGuid
                                                       , CtnNo
                                                       , OrderNo
                                                       , SoDocEntry
                                                       , OrderDate
                                                       , Studio
                                                       , ShippingCartonNo 
                                                       , PackedId
                                                       , InvNo
                                                       , ScanInCode
                                                       , BoxId
                                                       , OrigOrderNo
                                                       , OrderType
                                                       , ItemCode
                                                    ) VALUES ( 
                                                           @HeadGuid
                                                         , @LineGuid
                                                         , @CtnNo
                                                         , @OrderNo
                                                         , @SoDocEntry
                                                         , @OrderDate
                                                         , @Studio
                                                         , @ShippingCartonNo
                                                         , @PackedId
                                                         , @InvNo
                                                         , @ScanInCode
                                                         , @BoxId
                                                         , @OrigOrderNo
                                                         , @OrderType
                                                         , @ItemCode
                                                    )";

                res = conn.Execute(insertLines, tallySheetLines, trans);
                trans.Commit();
                return Ok(newCarton);
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        int GetCtnNo(string packedId)
        {
            int defaultReturnValue = 0;
            try
            {
                if (string.IsNullOrWhiteSpace(packedId)) return defaultReturnValue;
                var arr = packedId.Split("/");
                if (arr != null && arr.Length <= 2)
                {
                    var isNumeric = int.TryParse(arr[0], out int result);
                    if (isNumeric) return result;
                }
                return defaultReturnValue;
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return defaultReturnValue;
            }
        }

        int CreateRunNo_ConsultantLog(Dto_TpShipping dto, FTAPP_TPStudioCartonRunNo newCartonNo, DbInfo db,
                                        SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                var newlog = new FTAPP_TPStudioCartonRunNo_Log
                {
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    TPRunNo = newCartonNo.CurrentCartonNo,
                    TPOrderDate = newCartonNo.OrderDate,
                    TPStudio = newCartonNo.Studio,
                    SubSi = dto.SubSi,
                    AppVersion = dto.AppVersion,
                    TransDt = DateTime.Now
                };

                var insertlog = @$"Insert into {db.WEBDB}..FTAPP_TPStudioCartonRunNo_Log  ( 
                                         UserCode
                                       , UserName
                                       , TPRunNo
                                       , TPOrderDate
                                       , TPStudio                                       
                                       , SubSi
                                       , AppVersion
                                       , TransDt 
                                        ) VALUES (                                    
                                    @UserCode
                                   ,@UserName
                                   ,@TPRunNo
                                   ,@TPOrderDate
                                   ,@TPStudio                                   
                                   ,@SubSi
                                   ,@AppVersion
                                   ,GETDATE() )";

                return conn.Execute(insertlog, newlog, trans);
            }
            catch (Exception e)
            {                
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return -1;
            }
        }
    }
}