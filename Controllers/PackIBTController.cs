using Dapper;
using KTC_SalesAppWAPI.DTOs.PackIBT;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Pack;
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
    public class PackIBTController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<PackIBTController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public PackIBTController(IConfiguration configuration, ILogger<PackIBTController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(Dto_Pack_IBT dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "QueryIBTBox":
                    {
                        return LoadPackedBoxes(dto);
                    }
                case "LoadPackedIBTBoxes":
                    {
                        return LoadPackedIBTBoxes(dto);
                    }
                case "QueryPickedIBTBoxes":
                    {
                        return QueryPickedIBTBoxes(dto);
                    }
                case "RemovePackedIBTInfo":
                    {
                        return RemovePackedIBTInfo(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }

            }
        }

        IActionResult RemovePackedIBTInfo(Dto_Pack_IBT dto)
        {

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("invalid company name");
            }
            if (string.IsNullOrWhiteSpace(dto.SOID))
            {
                return BadRequest("invalid doc / base entry id");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("invalid company name and info");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var update_sql = $@"update {db.WEBDB}..FTAPP_IBTBox 
                                    set   PackId =  null
                                        , PackDt = null
                                        , PackerCode = null
                                        , PackerName = null
                                        , OrderProcessWeek = null
                                        , BusinessCenterCode = null
                                        , CurrentCartonNo = null
                                        , OrderNo = null
                                    Where BaseEntry = @SOID";

                conn.Execute(update_sql, new { SOID = dto.SOID }, trans);
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

        IActionResult QueryPickedIBTBoxes(Dto_Pack_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.SOID))
                {
                    return BadRequest("invalid SOID");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid company name and info");
                }

                var sql = $@"select BoxId 
                             from {db.WEBDB}..FTAPP_IBTBox 
                             where BASEENTRY = @BaseEntry 
                             order by NULLIF(CHARINDEX('-', BoxId, NULLIF(CHARINDEX('-', BoxId), 0) + 1), 0) + 1, LEN(BoxId)";

                var boxids = new SqlConnection(_commDbConnStr)
                                .Query<string>(sql, new { BaseEntry = dto.SOID }).Distinct().ToList();

                return Ok(boxids);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadPackedIBTBoxes(Dto_Pack_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryBoxid))
                {
                    return BadRequest("invalid scan in box id");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid company name and info");
                }

                // get the scn in box
                // 20230415
                // add in the column LabelConsistTotalBoxes for ftapp_box table
                var sql = $@"SELECT distinct id
                                , BoxId
                                , PickerCode
                                , PickerName
                                , PickDt
                                , PackId
                                , PackDt
                                , PackerCode
                                , PackerName
                                , BaseEntry
                                , BoxGuid
                                , TimeStampSeq
                                , AppVersion
                                , BoxSize
                                , OrderProcessWeek
                                , BusinessCenterCode
                                , CurrentCartonNo
                                , OrderNo , LabelConsistTotalBoxes

                            FROM {db.WEBDB}..FTAPP_IBTBox WITH (NOLOCK)
                            WHERE BoxId = @QueryBoxid ";

                using var conn = new SqlConnection(_commDbConnStr);
                var box = conn
                    .Query<FTAPP_Box>(sql, new
                    {
                        dto.QueryBoxid
                    }, commandTimeout: 0)
                    .FirstOrDefault();

                #region handler box not found, copy from draft (if any)
                if (box == null)
                {
                    return BadRequest($"{dto.QueryBoxid} box no found, pls contact support for help. [2023May16]");
                }
                #endregion

                // query all boxed with already packed
                sql = $@"SELECT  Distinct BoxId
                                    , PickerCode
                                    , PickerName
                                    , PickDt
                                    , PackId
                                    , PackDt
                                    , PackerCode
                                    , PackerName
                                    , BaseEntry
                                    , BoxGuid
                                    , TimeStampSeq
                                    , AppVersion
                                    , BoxSize
                                    , OrderProcessWeek
                                    , BusinessCenterCode
                                    , CurrentCartonNo
                                    , OrderNo ,  LabelConsistTotalBoxes
                                    , SUBSTRING(packid, 0, CHARINDEX('/', packid, 1) -1) [PackSeqId] 

                        FROM  {db.WEBDB}..FTAPP_IBTBox WITH (NOLOCK)
                        WHERE BASEENTRY = @BaseEntry 
                        AND  PackerCode IS NOT NULL 
                        ORDER BY SUBSTRING(packid, 0, CHARINDEX('/', packid, 1) -1) desc ";

                // all packed boxes
                var packedBoxes = conn
                    .Query<FTAPP_Box>(sql, new
                    {
                        box.BaseEntry
                    }, commandTimeout: 0)
                    .Distinct()
                    .ToList();

                if (packedBoxes.Count == 0)
                {
                    return BadRequest("No packed box found");
                }

                // 20211013T1302
                // for tupperwre arrangement
                SO repliedIbt = dto.DeviceSo;
                if (repliedIbt == null)
                {
                    //string qr_so = @"exec sp_SelectPackQuerySo_v2 @webDb, @erpDb, @boxId, @subSi, @subSiID";
                    string qr_ibt = @"exec sp_SelectPackQueryIBT @webDb, @boxId ";
                    repliedIbt = conn.Query<SO>(qr_ibt, new
                    {
                        webDb = db.WEBDB,                       
                        boxId = dto.QueryBoxid                        
                    }, commandTimeout: 0).FirstOrDefault();
                }

                sql = $@"select BoxId 
                         from {db.WEBDB}..FTAPP_IBTBox with (nolock)
                         where BASEENTRY = @BaseEntry 
                         order by NULLIF(CHARINDEX('-', BoxId, NULLIF(CHARINDEX('-', BoxId), 0) + 1), 0) + 1, LEN(boxid)";

                // all box id
                var boxids = conn
                    .Query<string>(sql,
                    new
                    {
                        BaseEntry = box.BaseEntry
                    }, commandTimeout: 0)
                    .Distinct()
                    .ToList();

                // get the packid 
                var queryPackId = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end) +1)  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) +1 [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_IBTbox Where BaseEntry = @docEntry";

                var packinfo = conn.Query<FTAPP_PackInfo>(queryPackId, new { docEntry = box.BaseEntry }).FirstOrDefault();
                if (packinfo == null)
                {
                    return BadRequest("Please try again, server in busy response. Thanks");
                }

                var isPrintSticker = false;

                // if box no packed then 
                if (box.PackDt == default) // incase scan in new box 
                {
                    // update the box being pack 
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        string sqlUpdate = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_IBTBox] 
                                   SET PackId = @PackId 
                                       ,PackerCode = @PackerCode
                                       ,PackerName = @PackerName 
                                       ,PackDt = GETDATE()                                      
                                   WHERE  id = @id ";

                        conn.Execute(sqlUpdate, new
                        {
                            PackId = packinfo.PackID,
                            PackerCode = dto.PackerCode,
                            PackerName = dto.PackerName,                          
                            id = box.id
                        }, trans);

                        
                        //// get the packid 
                        var queryPackId1 = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end))  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_IBTBox Where BaseEntry = @docEntry";

                        var packinfo1 = conn.Query<FTAPP_PackInfo>(queryPackId1,
                            new
                            {
                                docEntry = box.BaseEntry
                            }, trans).FirstOrDefault();

                        if (packinfo1 == null)
                        {
                            return BadRequest("Please try again, server in busy response. Thanks");
                        }

                        // update the latest value
                        packinfo.PackID = packinfo1.PackID;
                        packinfo.CurrentCount = packinfo1.CurrentCount;

                        trans.Commit();
                        box.PackDt = DateTime.Now;

                        box.PackSeqId = packinfo1.CurrentCount;
                        packedBoxes.Add(box);
                        isPrintSticker = true;
                    }
                    catch (Exception sqlecep)
                    {
                        trans.Rollback();
                        LastError = $"{ sqlecep.Message}\n{sqlecep.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }
                }
                else
                {
                    packinfo.PackID = box.PackId;
                }

                // lastly reply
                var repliedDto = new
                {
                    RepliedBox = box,
                    RepliedBoxes = packedBoxes,
                    RepliedSO = repliedIbt,
                    RepliedPickedBoxIds = boxids,                    
                    IsPrintSticker = isPrintSticker
                };

                return Ok(repliedDto);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadPackedBoxes(Dto_Pack_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryBoxid))
                {
                    return BadRequest("invalid scan in box id");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid company name and info");
                }

                // get the scn in box
                var sql = $@"SELECT distinct id
                                , BoxId
                                , PickerCode
                                , PickerName
                                , PickDt
                                , PackId
                                , PackDt
                                , PackerCode
                                , PackerName
                                , BaseEntry
                                , BoxGuid
                                , TimeStampSeq
                                , AppVersion
                                , BoxSize
                                , OrderProcessWeek
                                , BusinessCenterCode
                                , CurrentCartonNo
                                , OrderNo
                                , LabelConsistTotalBoxes

                            FROM {db.WEBDB}..FTAPP_IBTBox WITH (NOLOCK)
                            WHERE BoxId = @QueryBoxid ";

                //  20120415
                // add in column , LabelConsistTotalBoxes for ftapp_box

                using var conn = new SqlConnection(_commDbConnStr);
                var box = conn
                    .Query<FTAPP_Box>(sql, new
                    {
                        dto.QueryBoxid
                    }, commandTimeout: 0)
                    .FirstOrDefault();

                #region handler box not found, copy from draft (if any)
                if (box == null)
                {
                    return BadRequest($"{dto.QueryBoxid} box no found, pls contact support for help. [2023Jan06]");
                }
                #endregion

                // 20211013T1302
                // for tupperwre arrangement
                SO repliedIbt = dto.DeviceSo;
                if (repliedIbt == null)
                {   
                    string qr_ibtAsSO = @"exec sp_SelectPackQueryIBT @webDb , @boxId ";
                    repliedIbt = conn.Query<SO>(qr_ibtAsSO, new
                    {
                        webDb = db.WEBDB,
                        boxId = dto.QueryBoxid
                    }, commandTimeout: 0).FirstOrDefault();
                }

                // query all boxed with already packed
                //  20120415
                // add in column , LabelConsistTotalBoxes for ftapp_box
                sql = $@"SELECT  Distinct BoxId
                                    , PickerCode
                                    , PickerName
                                    , PickDt
                                    , PackId
                                    , PackDt
                                    , PackerCode
                                    , PackerName
                                    , BaseEntry
                                    , BoxGuid
                                    , TimeStampSeq
                                    , AppVersion
                                    , BoxSize
                                    , OrderProcessWeek
                                    , BusinessCenterCode
                                    , CurrentCartonNo
                                    , OrderNo
                                    , LabelConsistTotalBoxes
                                    , SUBSTRING(packid, 0, CHARINDEX('/', packid, 1) -1) [PackSeqId] 

                        FROM  {db.WEBDB}..FTAPP_IBTBox WITH (NOLOCK)
                        WHERE BASEENTRY = @BaseEntry 
                        AND  Packid IS NOT NULL 
                        ORDER BY SUBSTRING(packid, 0, CHARINDEX('/', packid, 1) -1) desc ";

                // all packed boxes
                var packedBoxes = conn
                    .Query<FTAPP_Box>(sql, new
                    {
                        box.BaseEntry
                    }, commandTimeout: 0)
                    .Distinct()
                    .ToList();

                sql = $@"select BoxId 
                         from {db.WEBDB}..FTAPP_IBTBox with (nolock)
                         where BASEENTRY = @BaseEntry 
                         order by NULLIF(CHARINDEX('-', BoxId, NULLIF(CHARINDEX('-', BoxId), 0) + 1), 0) + 1, LEN(boxid)";

                // all box id
                var boxids = conn
                    .Query<string>(sql,
                    new
                    {
                        BaseEntry = box.BaseEntry
                    }, commandTimeout: 0)
                    .Distinct()
                    .ToList();

                
                //// get the packid 
                var queryPackId = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end) +1)  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) +1 [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_IBTBox Where BaseEntry = @docEntry";

                var packinfo = conn.Query<FTAPP_PackInfo>(queryPackId, new { docEntry = box.BaseEntry }).FirstOrDefault();
                if (packinfo == null)
                {
                    return BadRequest("Please try again, server in busy response. Thanks");
                }

                var isPrintSticker = false;

                if (box.PackDt == default)
                {
                    // update the box being pack 
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        string sqlUpdate =  $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_IBTBox] 
                                   SET PackId = @PackId 
                                       ,PackerCode = @PackerCode
                                       ,PackerName = @PackerName 
                                       ,PackDt = GETDATE()
                                   WHERE  id = @id ";

                        conn.Execute(sqlUpdate, new
                        {
                            PackId = packinfo.PackID,
                            PackerCode = dto.PackerCode,
                            PackerName = dto.PackerName,
                            id = box.id
                        }, trans);

                        //// get the packid 
                        var queryPackId1 = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end))  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_IBTBox Where BaseEntry = @docEntry";

                        var packinfo1 = conn.Query<FTAPP_PackInfo>(queryPackId1,
                            new
                            {
                                docEntry = box.BaseEntry
                            }, trans).FirstOrDefault();

                        if (packinfo1 == null)
                        {
                            return BadRequest("Please try again, server in busy response. Thanks");
                        }

                        // update the latest value
                        packinfo.PackID = packinfo1.PackID;
                        packinfo.CurrentCount = packinfo1.CurrentCount;

                        trans.Commit();
                        box.PackDt = DateTime.Now;                        
                        isPrintSticker = true;
                    }
                    catch (Exception sqlecep)
                    {
                        trans.Rollback();
                        LastError = $"{ sqlecep.Message}\n{sqlecep.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }
                }
                else
                {
                    packinfo.PackID = box.PackId;
                    packedBoxes.Add(box);
                }

                // lastly reply

                var repliedDto = new
                {
                    RepliedBox = box,
                    RepliedBoxes = packedBoxes,
                    RepliedSO = repliedIbt,
                    RepliedPickedBoxIds = boxids,
                    //RepliedCartonInfo = null, //cartonInfo,
                    //RepliedBoxContents = null, // boxContents,
                    PackId = packinfo.PackID, // new pack id from db
                    CurrentSeqId = packinfo.CurrentCount,
                    IsPrintSticker = isPrintSticker
                };

                return Ok(repliedDto);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        FTAPP_TPCartonRunNo GetBusinessCenterCartonNo(DbInfo db, string bizCenter, string orderOfWeek)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var query = @$"SELECT *
                                FROM {db.WEBDB}..FTAPP_TPCartonRunNo 
                                WHERE OrderProcessWeek   = @OrderProcessWeek
                                AND   BusinessCenterCode = @BusinessCenterCode ";

                var cartonNo = conn.Query<FTAPP_TPCartonRunNo>(query, new
                {
                    OrderProcessWeek = orderOfWeek,
                    BusinessCenterCode = bizCenter
                }, trans).FirstOrDefault();

                // insert new when is null
                if (cartonNo == null)
                {
                    var newCartonNo = new FTAPP_TPCartonRunNo
                    {
                        OrderProcessWeek = orderOfWeek,
                        BusinessCenterCode = bizCenter,
                        CurrentCartonNo = 1
                    };

                    var insertCartonSql = @$"insert into {db.WEBDB}..FTAPP_TPCartonRunNo (
                                               OrderProcessWeek 
                                             , BusinessCenterCode
                                             , CurrentCartonNo
                                             ) values (
                                               @OrderProcessWeek 
                                             , @BusinessCenterCode
                                             , @CurrentCartonNo )";

                    conn.Execute(insertCartonSql, newCartonNo, trans);
                    trans.Commit();
                    return newCartonNo;
                }

                // perform update increase carton no by 1 

                cartonNo.CurrentCartonNo++;
                var updateCartonNo = $@"Update {db.WEBDB}..FTAPP_TPCartonRunNo
                                        SET CurrentCartonNo = @CurrentCartonNo 
                                        Where OrderProcessWeek= @OrderProcessWeek 
                                            and BusinessCenterCode = @BusinessCenterCode 
                                            and id = @Id";

                conn.Execute(updateCartonNo, cartonNo, trans);
                trans.Commit();
                return cartonNo;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        List<Tp_BoxContent> GetBoxContent(SqlConnection conn, DbInfo db, long DocEntry, string boxId)
        {
            try
            {
                // change the reforder number as string
                //var sp_sql = "exec sp_SelectCartonContent @webDb, @docEntry , @boxId";
                // 20211223
                var sp_sql = "exec sp_SelectCartonContent_RefOrderAsStr @webDb, @docEntry, @boxId";
                return conn.Query<Tp_BoxContent>(sp_sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = DocEntry,
                    boxId = boxId
                }).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        CartonCount GetCarton(DbInfo db, long DocEntry)
        {
            try
            {
                var sp_sql = "exec sp_SelectCartonCountSumm @webDb, @docEntry ";
                return new SqlConnection(_commDbConnStr).Query<CartonCount>(sp_sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = DocEntry
                }).FirstOrDefault();
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }



    }
}
