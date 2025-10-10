using Dapper;
using KTC_SalesAppWAPI.DTOs.TruckInspection;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.TrcukInspection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.TruckInspection
{
    [Route("[controller]")]
    [ApiController]
    public class TruckInspectionController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<TruckInspectionController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public TruckInspectionController(IConfiguration configuration, ILogger<TruckInspectionController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult Post(Dto_TruckInspection dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetTruckInspectionLines":
                    {
                        return GetTruckInspectionLines(dto);
                    }
                case "GetTruckInspectionTemplate":
                    {
                        return GetTruckInspectionTemplate();
                    }
                case "GetTruckInspections":
                    {
                        return GetTruckInspections(dto);
                    }
                case "SaveInspections":
                    {
                        return SaveInspections(dto);
                    }
                case "UpdateTruckInspectionSignedDoc":
                    {
                        return UpdateTruckInspectionSignedDoc(dto);
                    }


            }
            return BadRequest("request no found");
        }
        IActionResult UpdateTruckInspectionSignedDoc(Dto_TruckInspection dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }

            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var updateSql = @$"Update {db.WEBDB}..FTAPP_TruckInspection 
                                    set Files = @Files
                                    where docentry = @docEntry";

                var res = conn.Execute(updateSql, new { Files = dto.SignedFiles, docEntry = dto.DocEntry }, trans);

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetTruckInspectionLines(Dto_TruckInspection dto)
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

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_GetTruckInspectionLines @webDb, @docEntry ";

                var inspections = conn.Query<FTAPP_TruckInspection1>(query, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry
                }).ToList();

                if (inspections.Count == 0) return NotFound();
                return Ok(inspections);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetTruckInspections(Dto_TruckInspection dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.DriverCode))
                {
                    return BadRequest("invalid driver code");
                }
                if (string.IsNullOrWhiteSpace(dto.TruckNo))
                {
                    return BadRequest("Invalid truck no");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_GetTruckInspections @webDb, @driverCode, @truckNo, @startDt, @endDt";

                var list = conn.Query<FTAPP_TruckInspection>(query, new
                {
                    webDb = db.WEBDB,
                    driverCode = dto.DriverCode,
                    truckNo = dto.TruckNo,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

                if (list.Count == 0) return NotFound();

                return Ok(list);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetTruckInspectionTemplate()
        {
            try
            {
                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_GetTruckInspectionTemplate ";

                var inspections = conn.Query<FTAPP_TruckInspection1>(query).ToList();
                if (inspections.Count == 0) return NotFound();

                return Ok(inspections);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult SaveInspections(Dto_TruckInspection dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi ");
                }

                if (dto.Head == null)
                {
                    return BadRequest("Invalid inspection doc head");
                }
                if (dto.Details == null)
                {
                    return BadRequest("Invalid inspection details");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi for query dbi, please try again later.");
                }

                #region new inspection
                // check the docentry
                if (dto.Head.DocEntry == 0) // save new 
                {
                    // get the new max docentry from 
                    var maxDocEntry_query = @$"Select isnull(max(docentry), 0) + 1  
                                               from {db.WEBDB}..FTAPP_TruckInspection ";

                    using var conn = new SqlConnection(_commDbConnStr);
                    var maxDocEntry = conn.ExecuteScalar<int>(maxDocEntry_query);
                    if (maxDocEntry < 0)
                    {
                        return BadRequest("Unable to query the max docentry for new insertion, please try again later. Thanks.");
                    }

                    dto.Head.DocEntry = maxDocEntry;
                    dto.Details.ForEach(x =>
                    {
                        x.DocEntry = maxDocEntry;
                    });


                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();

                    try
                    {
                        var deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TruckInspection Where docentry = @docEntry";
                        conn.Execute(deleteSql, new { docEntry = maxDocEntry }, trans);

                        deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TruckInspection1 Where docentry = @docEntry";
                        conn.Execute(deleteSql, new { docEntry = maxDocEntry }, trans);

                        // perform the insert of the head 
                        var insertHeadSql = $@"INSERT INTO  {db.WEBDB}..FTAPP_TruckInspection (
                                                 DocEntry
                                               , DriverCode
                                               , DriverName
                                               , TruckNo
                                               , Date 
                                               , DocStatus
                                               , Remarks
                                            ) values  (
                                                @DocEntry
                                               ,@DriverCode
                                               ,@DriverName
                                               ,@TruckNo
                                               ,@Date 
                                               ,@DocStatus
                                               ,@Remarks
                                            ) ";

                        var res1 = conn.Execute(insertHeadSql, dto.Head, trans);
                        // 20230610
                        // ensure line is insert
                        if (res1 == 0)
                        {
                            trans.Rollback();
                            LastError = $"[Code: TYBGH20230610YTK] -NEW truck inspection head not save in," +
                                $" trans roll back {dto.Head.DriverCode}, {dto.Head.DriverName}, {dto.Head.Date}, DE#{dto.Head.DocEntry}";
                            _logger.LogError(LastError);
                            return BadRequest(LastError);
                        }

                        // insert the lines 
                        var insertLineSql = $@"INSERT INTO {db.WEBDB}..FTAPP_TruckInspection1 (
                                                    DocEntry
                                                   ,Section
                                                   ,Inspection
                                                   ,Description 
                                                   ,InspectionResult 
                                            ) values ( 
                                                @DocEntry
                                               ,@Section
                                               ,@Inspection
                                               ,@Description 
                                               ,@InspectionResult )";

                        // 20230610
                        // ensure line is insert
                        res1 = conn.Execute(insertLineSql, dto.Details, trans);
                        if (res1 == 0)
                        {
                            trans.Rollback();
                            LastError = $"[Code: TYBGH20230610YDF] -NEW truck inspection Line not save in," +
                                $" trans roll back {dto.Head.DriverCode}, {dto.Head.DriverName}, {dto.Head.Date}, DE#{dto.Head.DocEntry}";
                            _logger.LogError(LastError);
                            return BadRequest(LastError);
                        }

                        trans.Commit();
                        return Ok(dto.Head);
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        LastError = $"{ e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest(LastError);
                    }
                }
                #endregion new insert inspection


                #region update

                // for update 
                using var conn1 = new SqlConnection(_commDbConnStr);
                if (conn1.State == System.Data.ConnectionState.Closed) conn1.Open();
                using var trans1 = conn1.BeginTransaction();

                try
                {
                    var deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TruckInspection Where docentry = @docEntry";
                    conn1.Execute(deleteSql, new { docEntry = dto.Head.DocEntry }, trans1);

                    deleteSql = @$"Delete from {db.WEBDB}..FTAPP_TruckInspection1 Where docentry = @docEntry";
                    conn1.Execute(deleteSql, new { docEntry = dto.Head.DocEntry }, trans1);

                    // perform the insert of the head 
                    var insertHeadSql = $@"INSERT INTO  {db.WEBDB}..FTAPP_TruckInspection (
                                                 DocEntry
                                               , DriverCode
                                               , DriverName
                                               , TruckNo
                                               , Date 
                                               , DocStatus
                                               , Remarks
                                            ) values  (
                                                @DocEntry
                                               ,@DriverCode
                                               ,@DriverName
                                               ,@TruckNo
                                               ,@Date
                                               ,@DocStatus
                                               ,@Remarks
                                            ) ";

                    var res = conn1.Execute(insertHeadSql, dto.Head, trans1);
                    if (res == 0) // if not update 
                    {
                        trans1.Rollback();
                        LastError = $"[Code: TYBGH8D89721DS] truck inspection head not save in," +
                            $" trans roll back {dto.Head.DriverCode}, {dto.Head.DriverName}, {dto.Head.Date}, DE#{dto.Head.DocEntry}";
                        _logger.LogError(LastError);
                        return BadRequest(LastError);
                    }

                    // insert the lines 
                    var insertLineSql = $@"INSERT INTO {db.WEBDB}..FTAPP_TruckInspection1 (
                                                    DocEntry
                                                   ,Section
                                                   ,Inspection
                                                   ,Description 
                                                   ,InspectionResult 
                                            ) values ( 
                                                @DocEntry
                                               ,@Section
                                               ,@Inspection
                                               ,@Description 
                                               ,@InspectionResult )";

                    res = conn1.Execute(insertLineSql, dto.Details, trans1);

                    if (res == 0) // if not update 
                    {
                        trans1.Rollback();
                        LastError = $"[Code: TYBGHJN798965] truck inspection line not save in," +
                            $" trans roll back {dto.Head.DriverCode}, {dto.Head.DriverName}, {dto.Head.Date}, " +
                            $"DE#{dto.Head.DocEntry}";
                        _logger.LogError(LastError);
                        return BadRequest(LastError);
                    }

                    trans1.Commit();
                    return Ok(dto.Head);
                }
                catch (Exception e)
                {
                    trans1.Rollback();
                    LastError = $"[TruckINpectionController][SaveInspections] {e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest(LastError);
                }
                #endregion update 
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }



    }
}
