using Dapper;
using KTC_SalesAppWAPI.DTOs.WStockTake;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.WStockCount;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WStockCountController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<WStockCountController> _logger;
        string CommDbConnStr = string.Empty;
        string LastError = string.Empty;

        public WStockCountController(IConfiguration configuration, ILogger<WStockCountController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            CommDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult PostAsync(WhsSC_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadWhs":
                    {
                        return LoadWhs(dto);
                    }
                case "VerifyStockCountCode":
                    {
                        return VerifyStockCountCode(dto);
                    }
                case "InsertOrUpdate_Count":
                    {
                        return InsertOrUpdate_Count(dto);
                    }
                case "LoadPrevCount":
                    {
                        return LoadPrevCount(dto);
                    }
                case "RemoveCountedLine":
                    {
                        return RemoveCountedLine(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult RemoveCountedLine(WhsSC_Dto dto)
        {

            if (dto.CountLine == null)
            {
                return BadRequest("invalid count line");
            }
            if (string.IsNullOrWhiteSpace(dto.CountLine.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CountLine.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid subsi info");
            }

            // check record duplicated
            var sql = $@"DELETE 
                             FROM [{db.WEBDB}].[dbo].[FTAPP_SC1] 
                            WHERE scLineGuid = @scLineGuid ";

            using var conn = new SqlConnection(CommDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var result = conn.Execute(sql, new { scLineGuid = dto.CountLine.ScLineGuid }, trans);

                if (result == 1)
                {
                    trans.Commit();
                    return Ok();
                }

                trans.Rollback();
                return BadRequest("Dleete count line fail");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


        IActionResult LoadPrevCount(WhsSC_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("Invalid warehouse code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CountMode))
                {
                    return BadRequest("Invalid count mode");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi info");
                }

                var sql = $@"SELECT 
                             t1.ManBtchNum [ManBtchNum]
                            ,t0.Id
                            ,t0.ScLineGuid
                            ,t0.ScGuid
                            ,t0.ItemCode
                            ,t0.ItemName
                            ,t0.CountedQty
                            ,t0.ReCountQty
                            ,t0.UomQty
                            ,t0.SeqNo
                            ,t0.SpaceId
                            ,t0.CounterCode
                            ,t0.CounterName
                            ,t0.ReCounterCode
                            ,t0.ReCounterName
                            ,t0.CountedDt
                            ,t0.ReCountedDt
                            ,t0.WhsCode
                            ,t0.WhsName
                            ,t0.SubSi
                            ,t0.ScanCode
                            ,t0.MfgDate
                            ,t0.ExpDate
                            ,t0.Remarks
                            ,t0.LineStatus
                            ,t0.UomType
                            ,t0.SUPPCATNUM
                            ,t0.CODEBARS
                            ,t0.U_CSUS_UOM
                            ,t0.QUANTITY
                            ,t0.REQUANTITY
                            ,ISNULL(t0.BatchNo, '') [BatchNo]
                            ,t0.ReMfgDate
                            ,t0.ReExpDate
                            ,t0.ReRemarks
                            ,t0.BatchId

                            FROM [{db.WEBDB}].[dbo].[FTAPP_SC1] t0 WITH (nolock) 
                                 left join 
                                [{db.SAPDB}].[dbo].[OITM] t1 WITH (nolock) on t1.ItemCode = t0.ItemCode
                             WHERE WhsCode = @WhsCode                              
                             AND LineStatus = 'O' ";

                const string _NewCount = "New count";

                if (dto.CountMode.Equals(_NewCount))
                {
                    sql += " AND CounterCode = @CounterCode ";
                }
                else
                {
                    sql += " AND ReCounterCode = @CounterCode ";
                }

                sql += " ORDER BY SeqNo DESC"; // order by 

                dynamic param = new ExpandoObject();
                param.WhsCode = dto.WhsCode;
                param.CounterCode = dto.UserCode;

                using var conn = new SqlConnection(CommDbConnStr);
                var result = conn.Query<SC1>(sql, (object)param).ToList();
                if (result == null) return NotFound();
                if (result.Count == 0) return NotFound();

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult InsertOrUpdate_Count(WhsSC_Dto dto)
        {

            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }

            if (dto.CountLine == null)
            {
                return BadRequest("Invalid count line");
            }

            var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid subsi info");
            }

            // check record duplicated
            var sql = $@"SELECT * 
                             FROM [{db.WEBDB}].[dbo].[FTAPP_SC1] WITH (nolock)
                            WHERE scLineGuid = @scLineGuid ";

            using var conn = new SqlConnection(CommDbConnStr);            
            var duplicated = conn.Query<SC1>(sql, new { scLineGuid = dto.CountLine.ScLineGuid }).FirstOrDefault();

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (duplicated != null)
                {
                    // peform the update
                    var updateSql = @$"
                        UPDATE [{db.WEBDB}].[dbo].[FTAPP_SC1]
                           SET  CountedQty = @CountedQty
                              , ReCountQty = @ReCountQty
                              , ReCounterCode = @ReCounterCode
                              , ReCounterName = @ReCounterName                                                                                          
                              , Remarks = @Remarks                              
                              , QUANTITY = @QUANTITY
                              , REQUANTITY = @REQUANTITY
                              , ReRemarks = @ReRemarks 
                              , BatchNo = @BatchNo "; // 20220215T1507 add in the batch update 

                    if (dto.CountLine.MfgDate != default)
                    {
                        updateSql += ", MfgDate = @MfgDate ";
                    }
                    if (dto.CountLine.ExpDate != default)
                    {
                        updateSql += ", ExpDate = @ExpDate ";
                    }
                    if (dto.CountLine.ReMfgDate != default)
                    {
                        updateSql += ", ReMfgDate = @ReMfgDate ";
                    }
                    if (dto.CountLine.ReExpDate != default)
                    {
                        updateSql += ", ReExpDate = @ReExpDate ";
                    }
                    if (dto.CountLine.ReCountedDt != default)
                    {
                        updateSql += ", ReCountedDt = GETDATE()";
                    }

                    updateSql += $" WHERE Id = {duplicated.Id}";

                    conn.Execute(updateSql, dto.CountLine, trans);
                    trans.Commit();
                    return Ok();
                }

                // perform an insert
                var sqlinsert = $@"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_SC1] (
                                          ScLineGuid
                                         ,ScGuid
                                         ,ItemCode
                                         ,ItemName
                                         ,CountedQty
                                         ,ReCountQty
                                         ,UomQty
                                         ,SeqNo
                                         ,SpaceId
                                         ,CounterCode
                                         ,CounterName
                                         ,ReCounterCode
                                         ,ReCounterName                                         
                                         ,WhsCode
                                         ,WhsName
                                         ,SubSi
                                         ,ScanCode
                                         ,Remarks
                                         ,LineStatus
                                         ,UomType
                                         ,SUPPCATNUM
                                         ,CODEBARS
                                         ,U_CSUS_UOM
                                         ,QUANTITY
                                         ,REQUANTITY
                                         ,BatchNo
                                         ,ReRemarks ";

                var values_line = @" @ScLineGuid
                                    ,@ScGuid
                                    ,@ItemCode
                                    ,@ItemName
                                    ,@CountedQty
                                    ,@ReCountQty
                                    ,@UomQty
                                    ,@SeqNo
                                    ,@SpaceId
                                    ,@CounterCode
                                    ,@CounterName
                                    ,@ReCounterCode
                                    ,@ReCounterName                                         
                                    ,@WhsCode
                                    ,@WhsName
                                    ,@SubSi
                                    ,@ScanCode
                                    ,@Remarks
                                    ,@LineStatus
                                    ,@UomType
                                    ,@SUPPCATNUM
                                    ,@CODEBARS
                                    ,@U_CSUS_UOM
                                    ,@QUANTITY
                                    ,@REQUANTITY
                                    ,@BatchNo
                                    ,@ReRemarks ";

                if (dto.CountLine.CountedDt != default)
                {
                    sqlinsert += ", CountedDt ";
                    values_line += ", GETDATE()";
                }

                if (dto.CountLine.ReCountedDt != default)
                {
                    sqlinsert += ", ReCountedDt ";
                    values_line += ", GETDATE()";
                }

                if (dto.CountLine.MfgDate != default)
                {
                    sqlinsert += ", MfgDate ";
                    values_line += ", @MfgDate";
                }

                if (dto.CountLine.ExpDate != default)
                {
                    sqlinsert += ", ExpDate ";
                    values_line += ", @ExpDate ";
                }

                if (dto.CountLine.ReMfgDate != default)
                {
                    sqlinsert += ", ReMfgDate ";
                    values_line += ", @ReMfgDate";
                }

                if (dto.CountLine.ReExpDate != default)
                {
                    sqlinsert += ", ReExpDate ";
                    values_line += ", @ReExpDate ";
                }

                var slq_combine = $"{sqlinsert} ) VALUES ( {values_line})";
                conn.Execute(slq_combine, dto.CountLine, trans);
                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult VerifyStockCountCode(WhsSC_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.ScanCode))
                {
                    return BadRequest("Invalid code");
                }

                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("Invalid whs code");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi, error getting db info");
                }

                var results = new List<OITM_Ext>();
                var checkInCode = dto.OriginalInCode.Replace("\n", "");

                // pre check the bcd 
                var sql = $@"Select * from {db.SAPDB}..OITM t0 with (nolock)
                             inner join {db.SAPDB}..OBCD t1 with (nolock) on t1.ItemCode = t0.ItemCode
                             Where t1.BcdCode = @BcdCode 
                             AND frozenFor = 'N' 
                             AND validFor = 'Y' ";

                var conn = new SqlConnection(CommDbConnStr);
                var foundItems = conn
                    .Query<OITM_Ext>(sql, new { BcdCode = checkInCode }).ToList(); //.FirstOrDefault();

                if (foundItems != null && foundItems.Count > 0)
                {
                    results.AddRange(foundItems);
                    goto ProcessBcdCode;
                }

                // 20210918 remove the leading zero and ending zero
                checkInCode = dto.ScanCode;
                checkInCode = checkInCode.TrimStart(new char[] { '0' });
                checkInCode = checkInCode.TrimEnd(new char[] { '0' });
                

                var foundItem = conn
                    .Query<OITM_Ext>(sql, new { BcdCode = dto.ScanCode }).FirstOrDefault();

                if (foundItem != null)
                {
                    results.Add(foundItem);
                    goto ProcessBcdCode;
                }

                // process as per normal 
                sql = @"exec sp_SelectStockCountCode  @erpDb, @code, @subsi, @whsCode";

                results = conn.Query<OITM_Ext>(sql, new
                {
                    erpDb = db.SAPDB,
                    code = checkInCode,
                    subsi = dto.SubSi,
                    whsCode = dto.WhsCode
                }).ToList();

                // check tupperware code logic
                if (results?.Count == 0 && !string.IsNullOrWhiteSpace(dto.OriginalInCode))
                {
                    results = conn.Query<OITM_Ext>(sql, new
                    {
                        erpDb = db.SAPDB,
                        code = dto.OriginalInCode,
                        subsi = dto.SubSi,
                        whsCode = dto.WhsCode
                    }).ToList();

                    // add into check tupperwarecode 
                    // 2021 10 13
                    if (results?.Count == 0)
                    {
                        var tpHelper = new TupperwareCartonScanLogic();
                        var possiblesCode = tpHelper.GetPossibleCodes(dto.OriginalInCode);

                        sql = @"exec sp_SelectStockCountCode_Tupperware  @erpDb, @code, @subsi, @whsCode";
                        results = conn.Query<OITM_Ext>(sql, new
                        {
                            erpDb = db.SAPDB,
                            code = possiblesCode,
                            subsi = dto.SubSi,
                            whsCode = dto.WhsCode
                        }).ToList();
                    }

                    if (results == null) return NotFound();
                    if (results.Count == 0) return NotFound();
                }

            // get all the bcd code in list for later use
            ProcessBcdCode:
                var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                for (int i = 0; i < results.Count; i++)
                {
                    results[i].BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                            new { erpDb = db.SAPDB, itemCode = results[i].ItemCode }).ToList();
                }

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult LoadWhs(WhsSC_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi, error getting db info");
                }

                var sql = @"exec sp_SelectWhs @subsi, @erpDb";
                var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<OWHS_Ext>(sql, new { subsi = dto.SubSi, erpDb = db.SAPDB }).ToList();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }
    }
}


//IActionResult GetCountedLine(WhsSC_Dto dto)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(dto.SubSi))
//        {
//            return BadRequest("Invalid subsi");
//        }
//        if (string.IsNullOrWhiteSpace(dto.WhsCode))
//        {
//            return BadRequest("Invalid warehouse code");
//        }
//        if (string.IsNullOrWhiteSpace(dto.ItemCode))
//        {
//            return BadRequest("Invalid item code");
//        }
//        if (string.IsNullOrWhiteSpace(dto.SpaceID))
//        {
//            return BadRequest("Invalid space id");
//        }
//        if (string.IsNullOrWhiteSpace(dto.ScanCode))
//        {
//            return BadRequest("Invalid scan code");
//        }
//        if (string.IsNullOrWhiteSpace(dto.UomType))
//        {
//            return BadRequest("Invalid uom type");
//        }

//        var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.SubSi);
//        if (db == null)
//        {
//            return BadRequest("Invalid subsi info");
//        }

//        var sql = $@"SELECT * FROM [{db.WEBDB}].[dbo].[FTAPP_SC1] WITH (nolock) 
//                     WHERE     ItemCode = @ItemCode                                   
//                            AND WhsCode = @WhsCode                                  
//                            AND SpaceID = @SpaceID 
//                            AND UomType = @UomType 
//                            AND (ScanCode = @ScanCode or ScanCode = @ItemCode)";

//        dynamic param = new ExpandoObject();
//        param.ItemCode = dto.ItemCode;
//        param.WhsCode = dto.WhsCode;
//        param.SpaceID = dto.SpaceID;
//        param.UomType = dto.UomType;
//        param.ScanCode = dto.ScanCode;

//        //if (dto.MfrDate != default)
//        //{
//        //    sql += " AND MfgDate = @MfrDate ";
//        //    param.MfrDate = dto.MfrDate;
//        //}
//        //else
//        //{
//        //    sql += " AND MfgDate IS NULL";     
//        //}

//        //if (dto.ExpDate != default)
//        //{
//        //    sql += " AND ExpDate = @ExpDate ";
//        //    param.ExpDate = dto.ExpDate;
//        //}
//        //else
//        //{
//        //    sql += " AND ExpDate IS NULL";
//        //}

//        var conn = new SqlConnection(CommDbConnStr);
//        var results = conn.Query<SC1>(sql, (object)param).ToList();

//        if (results == null) return NotFound();
//        if (results.Count == 0) return NotFound();
//        return Ok(results);

//        //if (results.Count == 1) return Ok(results);                
//        //// try to locate the item and return single
//        //var filterSearch = results.Where(c => c.ScanCode.Equals(dto.ScanCode)).FirstOrDefault();
//        //if (filterSearch == null ) return Ok(results);
//        //var singleList = new List<SC1>
//        //{
//        //    filterSearch
//        //};
//        //return Ok(singleList);
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest(LastError);
//    }
//}