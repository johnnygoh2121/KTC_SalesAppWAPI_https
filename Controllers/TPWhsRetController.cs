using Dapper;
using KTC_SalesAppWAPI.DTOs.TPWhsRet;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.TPWhsRet;
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
    public class TPWhsRetController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<TPWhsRetController> _logger;

        //string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public TPWhsRetController(IConfiguration configuration, ILogger<TPWhsRetController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
          //  WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_TPWhsRet dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetReasonCodes":
                    {
                        return GetReasonCodes(dto);
                    }
                case "GetCogDetails":
                    {
                        return GetCogDetails(dto);
                    }
                case "GetCogList":
                    {
                        return GetCogList(dto);
                    }
                case "SaveCogTPDraft":
                    {
                        return SaveCogTPDraft(dto);
                    }
                case "SaveCogLines":
                    {
                        return SaveCogLines(dto);
                    }
                case "GetTPCogDraftLine":
                    {
                        return GetTPCogDraftLine(dto);
                    }
                case "DeleteCogLine_App":
                    {
                        return DeleteCogLine_App(dto);
                    }
                case "GetPostedCogLines":
                    {
                        return GetPostedCogLines(dto);
                    }
                case "GetCog_Lines":
                    {
                        return GetCog_Lines(dto);
                    }
                default:
                    {
                        return BadRequest($"Request not found {request}");
                    }
            }
        }

        IActionResult GetCog_Lines(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var query_lines = $@"select * from {db.WEBDB}..COG1
                                    where DocEntry = @DocEntry ";

                var conn = new SqlConnection(_commDbConnStr);
                var lines = conn.Query<COG_Line>(query_lines, new
                {
                    DocEntry = dto.CogEntry
                }).ToList();

                lines = LoadLineBarcodes(lines, conn, db);
                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetPostedCogLines(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var query_lines = $@"select * from {db.WEBDB}..FTAPP_COG1
                                    where CogDocEntry = @CogDocEntry ";

                var lines = new SqlConnection(_commDbConnStr).Query<FTAPP_COG1>(query_lines, new
                {
                    CogDocEntry = dto.CogEntry
                }).ToList();

                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult DeleteCogLine_App(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var conn = new SqlConnection(_commDbConnStr);
                DeleteCogLine(db, conn, dto.CogEntry, "FTAPP_COG1_Draft");

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult GetTPCogDraftLine(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var query_draf_line = $@"Select * 
                                       from {db.WEBDB}..FTAPP_COG1_DRAFT
                                        Where CogDocEntry =@CogDocEntry ";

                var conn = new SqlConnection(_commDbConnStr);
                var draftlines = conn.Query<FTAPP_COG1>(query_draf_line, new { CogDocEntry = dto.CogEntry }).ToList();

                return Ok(draftlines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveCogTPDraft(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                if (dto.CogLines == null)
                {
                    return BadRequest("invalid draft line");
                }

                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var conn = new SqlConnection(_commDbConnStr);
                DeleteCogLine(db, conn, dto.CogEntry, "FTAPP_Cog1_Draft");
                Save_FTAPP_COG1(db, conn, dto.CogLines, "FTAPP_Cog1_Draft");
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void Save_FTAPP_COG1(DbInfo db, SqlConnection conn, List<FTAPP_COG1> lines, string tableName)
        {
            try
            {
                for (int id = 0; id < lines.Count; id++)
                {
                    var line = lines[id];
                    if (line == null) continue;

                    // insert the draft
                    var insert_head = $@"INSERT INTO {db.WEBDB}..{tableName} (                                         
                                            CogDocEntry
                                          , CogBaseLine
                                          , LineNum
                                          , ItemName
                                          , ItemCode
                                          , ReasonCode                                     
                                          , Quantity
                                          , QuantityCs
                                          , QuantityPc
                                          , Remarks                                         
                                          , LotNo
                                          , Batch
                                          , LineGuid
                                          , ScanInCode
                                          , UomQty
                                          , BarcodeStr
                                          , ReceivedQty
                                          , VarianceQty
                                          , CogIssueQty
                                          , RecWhsCode
                                          , RecWhsName
                                          , RecReasonCode ";

                    var insert_value = @"@CogDocEntry
                                       ,@CogBaseLine
                                       ,@LineNum
                                       ,@ItemName
                                       ,@ItemCode
                                       ,@ReasonCode                                    
                                       ,@Quantity
                                       ,@QuantityCs
                                       ,@QuantityPc
                                       ,@Remarks                                     
                                       ,@LotNo
                                       ,@Batch
                                       ,@LineGuid
                                       ,@ScanInCode
                                       ,@UomQty
                                       ,@BarcodeStr
                                       ,@ReceivedQty
                                       ,@VarianceQty
                                       ,@CogIssueQty
                                       ,@RecWhsCode
                                       ,@RecWhsName
                                       ,@RecReasonCode ";

                    if (line.MfrDate != default)
                    {
                        insert_head += " ,MfrDate";
                        insert_value += " ,@MfrDate";
                    }

                    if (line.ExpDate != default)
                    {
                        insert_head += " ,ExpDate";
                        insert_value += " ,@ExpDate";
                    }

                    var combineInsertSql = insert_head + " ) VALUES  ( " + insert_value + ")";
                    var res1 = conn.Execute(combineInsertSql, line);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult SaveCogLines(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                if (dto.CogLines == null)
                {
                    return BadRequest("invalid draft line");
                }

                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid draft doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var conn = new SqlConnection(_commDbConnStr);
                DeleteCogLine(db, conn, dto.CogEntry, "FTAPP_Cog1");
                Save_FTAPP_COG1(db, conn, dto.CogLines, "FTAPP_Cog1");
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void DeleteCogLine(DbInfo db, SqlConnection conn, int CogEntry, string tableName)
        {
            try
            {
                var delete_draft_query = $"Delete from {db.WEBDB}..{tableName} Where CogDocEntry = @CogDocEntry";
                var deleteRes = conn.Execute(delete_draft_query, new
                {
                    CogDocEntry = CogEntry
                });
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult GetCogList(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var sp_query = $@"exec sp_SelectTPCogList @webDB, @startDt, @endDt ";
                var conn = new SqlConnection(_commDbConnStr);

                var results = conn.Query<COG_Doc>(sp_query, new
                {
                    webDB = db.WEBDB,
                    startDt = $"{dto.StartDt:yyyy-MM-dd}",
                    endDt = $"{dto.EndDt:yyyy-MM-dd}"

                }).ToList();

                if (results?.Count == 0) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetReasonCodes(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var sql = $@"Select * from {db.WEBDB}..FTAPP_TPReasonCode";
                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_TPReasonCode>(sql).ToList();
                if (results?.Count == 0) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCogDetails(Dto_TPWhsRet dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.CogEntry <= 0)
                {
                    return BadRequest("invalid Cog #");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null) return BadRequest("Invalid subsi ");

                var sp_query = $@"exec sp_SelectTPCog @webDB, @cogDocEntry";
                var conn = new SqlConnection(_commDbConnStr);

                var result = conn.Query<COG_Doc>(sp_query, new
                {
                    webDB = db.WEBDB,
                    cogDocEntry = dto.CogEntry
                }).FirstOrDefault();

                if (result == null) return BadRequest($"Invalid cog #{dto.CogEntry} or COG posted as CN. " +
                                                    $"@ company: {db.COMPANYNAME}");

                // else query it line;
                sp_query = $@"exec sp_SelectTPCogLines @webDb, @cogDocEntry";
                result.LINES = conn.Query<COG_Line>(sp_query, new
                {
                    webDB = db.WEBDB,
                    cogDocEntry = dto.CogEntry
                }).ToList();

                if (result.LINES?.Count == 0)
                    return BadRequest($"Invalid cog #{dto.CogEntry} with empty line(s), @ company: {db.COMPANYNAME}");

                // massage the barcode lines 
                // load the barcode table if any 
                result.LINES = LoadLineBarcodes(result.LINES, conn, db);
                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        List<COG_Line> LoadLineBarcodes(List<COG_Line> lines, SqlConnection conn, DbInfo db)
        {
            try
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                    lines[i].BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                        new
                        {
                            erpDb = db.SAPDB,
                            itemCode = lines[i].ITEMCODE
                        }).ToList();

                    // add more barcode 
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

                    var SuppCatNumCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = lines[i].SuppCatNum,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    lines[i].BarCodes.Add(SuppCatNumCode);
                }

                return lines;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return lines;
            }
        }

    }
}
