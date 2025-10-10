using Dapper;
using KTC_SalesAppWAPI.DTOs.StockCount;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.StockCount;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StockCountController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<StockCountController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public StockCountController(IConfiguration configuration, ILogger<StockCountController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult Post(SC_DTo dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "StockCount":
                    {
                        return StockCount(dto);
                    }
                case "StockCountLines":
                    {
                        return StockCountLines(dto);
                    }
                case "Save":
                    {
                        return Save(dto);
                    }
                case "QueryItem":
                    {
                        return QueryItem(dto);
                    }
                case "ClearStockCounts":
                    {
                        return ClearStockCounts(dto); // clear before save
                    }
            }
            return BadRequest("request no found");
        }

        IActionResult ClearStockCounts(SC_DTo dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.HeadGuid))
                {
                    return BadRequest("Invalid guid");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyName))
                {
                    return BadRequest("Invalid company name");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.QueryCompanyName);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var deleteHead = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_InStoreStockCount] 
                                    Where convert(nvarchar(50), HeadGuid) = @HeadGuid";

                using (var transaction = new TransactionScope())
                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    conn.Execute(deleteHead, new { HeadGuid = dto.HeadGuid });

                    var deleteLines = @$"Delete from [{db.WEBDB}].[dbo].[FTAPP_InStoreStockCountLine] 
                                    Where convert(nvarchar(50), HeadGuid) = @HeadGuid";
                    conn.Execute(deleteLines, new { HeadGuid = dto.HeadGuid });
                    transaction.Complete();
                }
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult StockCountLines(SC_DTo dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyName))
                {
                    return BadRequest("query company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.HeadGuid))
                {
                    return BadRequest("query guid is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.QueryCompanyName);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var sp_sql = "exec sp_SelectStouckCountLines @webDb, @guid";

                using var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_InStoreStockCountLine>(sp_sql, new { webDb = db.WEBDB, guid = dto.HeadGuid }).ToList();
                if (results == null) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult StockCount(SC_DTo dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyName))
                {
                    return BadRequest("query company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.StoreCode))
                {
                    return BadRequest("query store code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CounterCode))
                {
                    return BadRequest("query store code is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.QueryCompanyName);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var sp_sel = @" exec sp_SelectStockCount @webDb, @storeCode, @counterCode, @start, @end";

                using var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_InStoreStockCount>(sp_sel,
                    new
                    {
                        webDb = db.WEBDB,
                        storeCode = dto.StoreCode,
                        counterCode = dto.CounterCode,
                        start = dto.StartDt.Date,
                        end = dto.EndDt.Date
                    }).ToList();

                if (results == null) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult QueryItem(SC_DTo dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyName))
                {
                    return BadRequest("query company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryProdCode))
                {
                    return BadRequest("query product code name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.QueryCompanyName);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var sp_sql = @"exec sp_SelectOITM @sapDb , @itemCode";
                using var conn = new SqlConnection(_commDbConnStr);
                var found = conn.Query<OITM_Ext>(sp_sql, new { sapDb = db.SAPDB, itemCode = dto.QueryProdCode }).FirstOrDefault();
                if (found == null) return NotFound();
                return Ok(found);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult Save(SC_DTo dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QueryCompanyName))
                {
                    return BadRequest("query company name is empty");
                }

                if (dto.Head == null)
                {
                    return BadRequest("sc head is empty");
                }

                if (dto.Lines == null)
                {
                    return BadRequest("sc lines is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.QueryCompanyName);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                
                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var sqlInsertHead = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_InStoreStockCount] (
                                        CountName
                                       ,StoreCode
                                       ,StoreName
                                       ,CounterCode
                                       ,CounterName
                                       ,CounteDt
                                       ,HeadGuid
                                       ,CreatedDt
                                    ) VALUES (
                                        @CountName
                                       ,@StoreCode
                                       ,@StoreName
                                       ,@CounterCode
                                       ,@CounterName
                                       ,@CounteDt
                                       ,@HeadGuid
                                       ,@CreatedDt)";

                    conn.Open();
                    var result = conn.Execute(sqlInsertHead, dto.Head);

                    if (result >= 1)
                    {
                        dto.Lines.ForEach(line =>
                        {
                            var insertLinesHead = $@"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_InStoreStockCountLine] (
                                            ProdCode
                                           ,ProdName
                                           ,ExpiryInterval                                      
                                           ,DamageQty
                                           ,SellableQty
                                           ,TotalQty
                                           ,LineGuid
                                           ,HeadGuid
                                           ,CountSeq
                                           ,CountedDt
                                           ,CounterName ";
                            
                            var insertValues =@" 
                                        @ProdCode
                                        ,@ProdName
                                        ,@ExpiryInterval                                   
                                        ,@DamageQty
                                        ,@SellableQty
                                        ,@TotalQty
                                        ,@LineGuid
                                        ,@HeadGuid
                                        ,@CountSeq
                                        , GETDATE()
                                        ,@CounterName ";

                            if (line.ManufactureDt != default)
                            {
                                insertLinesHead += ", ManufactureDt ";
                                insertValues += ", @ManufactureDt ";
                            }

                            if (line.ExpiryDt != default)
                            {
                                insertLinesHead += ", ExpiryDt ";
                                insertValues += ", @ExpiryDt ";
                            }

                            var sqlInsert = $"{insertLinesHead}) values ({insertValues})";
                            conn.Execute(sqlInsert, line);
                        });
                    }
                }

                // add in the post success log 
                if (dto.Line != null)
                {
                    dto.Line.PostResult = dto.Head.CountName;;
                    dto.Line.Details = "Success";
                    new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                }

                return Ok();
            }
            catch (Exception e)
            {
                if (dto.Line != null)
                {
                    dto.Line.PostResult = dto.Head.CountName; ;
                    dto.Line.Details = "Fail";
                    new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                }

                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


    }
}
