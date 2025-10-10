using Dapper;
using KTC_SalesAppWAPI.DTOs.Batches;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Batches;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BatchController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";        
        readonly IConfiguration _configuration;
        readonly ILogger<BatchController> _logger;

        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        /* 
           OIBT - Itemcode batch no, whs
           IBT1 Batch transaction
           OBTN - Batch Numbers Master Data
           OBTD - Journal Vouchers List
           OBTQ - Batch No. Quantities
         */

        public BatchController(IConfiguration configuration, ILogger<BatchController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(Dto_Batch dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                //case "CheckBatchQty":
                //    {
                //        //return QueryBox(dto);
                //        return CheckBatchQty(dto);
                //    }
                case "GetBatches":
                    {
                        return GetBatches(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult GetBatches (Dto_Batch dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("invalid item code");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("invalid whs code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid item db info");
                }

                var conn = new SqlConnection(_commDbConnStr);
                var sp_query = "exec sp_AvailBatches @webDb, @whsCode, @itemCode ";
                var results = conn.Query<FTAPP_Batch>(sp_query, new
                {
                    webDb = db.WEBDB,
                    whsCode = dto.WhsCode,
                    itemCode = dto.ItemCode                    
                }).ToList();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //IActionResult CheckBatchQty(Dto_Batch dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("Invalid subsi");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.ItemCode))
        //        {
        //            return BadRequest("invalid item code");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.WhsCode))
        //        {
        //            return BadRequest("invalid whs code");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.QueryBatchNo))
        //        {
        //            return BadRequest("invalid item query batch no");
        //        }
        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("invalid item db info");
        //        }
        //        var conn = new SqlConnection(_commDbConnStr);
        //        var sp_query = "exec sp_CheckBatchQty @webDb, @itemCode, @batchNo, @whsCode ";
        //        var result = conn.Query<Batch>(sp_query, new
        //        {
        //            webDb = db.WEBDB,
        //            itemCode = dto.ItemCode,
        //            batchNo = dto.QueryBatchNo,
        //            whsCode = dto.WhsCode
        //        });

        //        return Ok(result);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}


    }
}
