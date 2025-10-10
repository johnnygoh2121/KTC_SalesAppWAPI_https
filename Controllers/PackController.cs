using Dapper;
using KTC_SalesAppWAPI.DTOs.Pack;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Login;
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
    public class PackController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<PackController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;
        //bool IsDeveloperTest_Tupperware { get; set; } = true;

        public PackController(IConfiguration configuration, ILogger<PackController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(Dto_Pack dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "QueryBox":
                    {
                        return LoadPackedBoxes(dto);
                    }
                case "SavePack":
                    {
                        return SavePack(dto);
                    }
                case "SavePackSingle":
                    {
                        return SavePackSingle(dto);
                    }
                case "TpSaveSingle":
                    {
                        return TpSavePackSingle(dto);
                    }
                case "ServerTime":
                    {
                        return Ok(new { SvrDt = DateTime.Now.ToString("yyyy-MMM-dd HH:mm tt") });
                    }
                case "GetCustomer2":
                    {
                        return GetCustomer2(dto);
                    }
                case "LoadPackedBoxes":
                    {
                        return LoadPackedBoxes2(dto);
                    }
                case "QueryPickedBoxes":
                    {
                        return QueryPickedBoxes(dto);
                    }
                case "RemovePackedInfo":
                    {
                        return RemovePackedInfo(dto);
                    }
                case "GetSOPickedQty":
                    {
                        return GetSOPickedQty(dto);
                    }
                case "GetIBTPickedQty":
                    {
                        return GetIBTPickedQty(dto);
                    }
                case "SavePickPackAvgSec":
                    {
                        return SavePickPackAvgSec(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }

            }
        }

        IActionResult SavePickPackAvgSec(Dto_Pack dto)
        {

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Invalid company name");
            }

            if (dto.PickPackSecData == null)
            {
                return BadRequest("Invalid data receipt");
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
                // remove the last record
                var delete_last = $@"Delete from {db.WEBDB}..FTAPP_PickPackAvgTime 
                                     Where DataType = @DataType 
                                     and DocEntry = @DocEntry";
                var result = conn.Execute(delete_last, new
                {
                    DataType = dto.PickPackSecData.DataType,
                    DocEntry = dto.PickPackSecData.DocEntry
                }, trans);

                // insert the data 
                var insert_sql = $@"INSERT INTO {db.WEBDB}..FTAPP_PickPackAvgTime ( 
                                     UserCode
                                   , UserName
                                   , TransDt
                                   , TotalSecond
                                   , TotalSku
                                   , AvgValue
                                   , DocEntry
                                   , DataType
                                   , StartDt
                                   , EndDt 
                                    ) VALUES ( 
                                    @UserCode
                                   , @UserName
                                   , GETDATE()
                                   , @TotalSecond
                                   , @TotalSku
                                   , @AvgValue
                                   , @DocEntry
                                   , @DataType
                                   , @StartDt
                                   , @EndDt )";

                conn.Execute(insert_sql, dto.PickPackSecData, trans);
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

        IActionResult GetSOPickedQty(Dto_Pack dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid company name");
                }

                if (string.IsNullOrWhiteSpace(dto.SOID))
                {
                    return BadRequest("Invalid sales order / base entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid company name and info");
                }

                var query = $@"select sum(pickedqty) [TotalPickedQty] 
                               from {db.WEBDB}..SO1 with (nolock)
                               where DOCENTRY = @SOID";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.ExecuteScalar<int>(query, new { SOID = dto.SOID });
                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetIBTPickedQty(Dto_Pack dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid company name");
                }

                if (string.IsNullOrWhiteSpace(dto.IBTID))
                {
                    return BadRequest("Invalid sales order / base entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid company name and info");
                }

                var query = $@"select sum(PICKEDQTY) [TotalPickedQty] 
                               from {db.WEBDB}..IBT1 with (nolock)
                               where DOCENTRY = @IBTID";

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.ExecuteScalar<int>(query, new { IBTID = dto.IBTID });
                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult RemovePackedInfo(Dto_Pack dto)
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
                var update_sql = $@"update {db.WEBDB}..FTAPP_Box 
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


        IActionResult QueryPickedBoxes(Dto_Pack dto)
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
                             from {db.WEBDB}..FTAPP_Box 
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

        IActionResult LoadPackedBoxes(Dto_Pack dto)
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

                            FROM {db.WEBDB}..FTAPP_Box WITH (NOLOCK)
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
                SO repliedSo = dto.DeviceSo;
                if (repliedSo == null)
                {
                    string qr_so = @"exec sp_SelectPackQuerySo_v2 @webDb, @erpDb, @boxId, @subSi, @subSiID";
                    repliedSo = conn.Query<SO>(qr_so, new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        boxId = dto.QueryBoxid,
                        subSi = db.COMPANYNAME,
                        subSiID = db.COMPANYID
                    }, commandTimeout: 0).FirstOrDefault();

                    // if the SO still null
                    // 20240217
                    if (repliedSo == null)
                    {
                        return BadRequest("Please contact admin, re-check sql procedure sp_SelectPackQuerySo_v2");
                    }
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

                        FROM  {db.WEBDB}..FTAPP_Box WITH (NOLOCK)
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
                         from {db.WEBDB}..FTAPP_Box with (nolock)
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

                // 20220418
                // load in and process from code
                var sp_loadAlBxContent = @"exec sp_SelectCartonContent_RefOrderAsStr_All @webDb, @docEntry";
                List<Tp_BoxContent> AllBoxContent = conn.Query<Tp_BoxContent>(sp_loadAlBxContent, new
                {
                    webDb = db.WEBDB,
                    docEntry = box.BaseEntry
                }).ToList();

                CartonCount cartonInfo = null;
                List<Tp_BoxContent> boxContents = null;
                if (repliedSo != null)
                {
                    cartonInfo = GetCarton(db, repliedSo.DOCENTRY);
                    for (int b = 0; b < packedBoxes.Count; b++)
                    {
                        packedBoxes[b].TpBoxContents =
                                    AllBoxContent.Where(bc => bc.BoxId == packedBoxes[b].BoxId).ToList();
                    }

                    boxContents = GetBoxContent(conn, db, repliedSo.DOCENTRY, box.BoxId);
                }

                // 20220120
                // reset the ship to null
                // when the shipto and shiptoadd is same 
                if (repliedSo != null && $"{repliedSo.SHIPTOADD}" == $"{repliedSo.SHIPTO}")
                {
                    repliedSo.SHIPTOADD = null; // let app to reload the address from SAP.
                }

                //// get the packid 
                var queryPackId = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end) +1)  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) +1 [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_box Where BaseEntry = @docEntry";

                var packinfo = conn.Query<FTAPP_PackInfo>(queryPackId, new { docEntry = box.BaseEntry }).FirstOrDefault();
                if (packinfo == null)
                {
                    return BadRequest("Please try again, server in busy response. Thanks");
                }

                var isPrintSticker = false;

                // if box no packed then 
                if (string.IsNullOrWhiteSpace(box.OrderNo))
                {
                    box.OrderNo = string.IsNullOrWhiteSpace(repliedSo.REFNO) ? null : repliedSo.REFNO;
                }

                if (box.PackDt == default)
                {
                    // update the box being pack 
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        string sqlUpdate = "";
                        // handler TP order
                        if ($"{repliedSo.REFTYPE}" == "TPWARE" && $"{repliedSo.TpGrpShortName}" == "BC")
                        {
                            if (box.CurrentCartonNo == default) // new box
                            {
                                // new carton box
                                var cartonRunNo = GetBusinessCenterCartonNo(db, repliedSo.TpBizCenterCode, repliedSo.TpWeekOfOrder);
                                if (cartonRunNo == null)
                                {
                                    return BadRequest("Server busy, please try again. erro getting new Center carton number");
                                }

                                box.BusinessCenterCode = cartonRunNo.BusinessCenterCode;
                                box.OrderProcessWeek = cartonRunNo.OrderProcessWeek;
                                box.CurrentCartonNo = cartonRunNo.CurrentCartonNo;
                            }
                        }
                        else
                        { // if no tp box then set to null
                            box.BusinessCenterCode = null;
                            box.OrderProcessWeek = null;
                            box.CurrentCartonNo = default;
                        }

                        sqlUpdate = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
                                   SET PackId = @PackId 
                                       ,PackerCode = @PackerCode
                                       ,PackerName = @PackerName 
                                       ,PackDt = GETDATE()
                                       , OrderProcessWeek  = @OrderProcessWeek
                                       , BusinessCenterCode = @BusinessCenterCode
                                       , CurrentCartonNo = @CurrentCartonNo               
                                       , OrderNo = @OrderNo
                                   WHERE  id = @id ";


                        conn.Execute(sqlUpdate, new
                        {
                            PackId = packinfo.PackID,
                            PackerCode = dto.PackerCode,
                            PackerName = dto.PackerName,
                            OrderProcessWeek = box.OrderProcessWeek,
                            BusinessCenterCode = box.BusinessCenterCode,
                            CurrentCartonNo = box.CurrentCartonNo,
                            OrderNo = box.OrderNo,
                            id = box.id
                        }, trans);

                        //// get the packid 
                        var queryPackId1 = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end))  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_box Where BaseEntry = @docEntry";

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

                        //box.BoxSeq = packinfo1.CurrentCount;
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
                    RepliedSO = repliedSo,
                    RepliedPickedBoxIds = boxids,
                    RepliedCartonInfo = cartonInfo,
                    RepliedBoxContents = boxContents,
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

        IActionResult LoadPackedBoxes2(Dto_Pack dto)
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

                            FROM {db.WEBDB}..FTAPP_Box WITH (NOLOCK)
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
                    return BadRequest($"{dto.QueryBoxid} box no found, pls contact support for help. [2023Jan06]");
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

                        FROM  {db.WEBDB}..FTAPP_Box WITH (NOLOCK)
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
                SO repliedSo = dto.DeviceSo;
                if (repliedSo == null)
                {
                    string qr_so = @"exec sp_SelectPackQuerySo_v2 @webDb, @erpDb, @boxId, @subSi, @subSiID";
                    repliedSo = conn.Query<SO>(qr_so, new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        boxId = dto.QueryBoxid,
                        subSi = db.COMPANYNAME,
                        subSiID = db.COMPANYID
                    }, commandTimeout: 0).FirstOrDefault();
                }

                sql = $@"select BoxId 
                         from {db.WEBDB}..FTAPP_Box with (nolock)
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

                // 20220418
                // load in and process from code
                var sp_loadAlBxContent = @"exec sp_SelectCartonContent_RefOrderAsStr_All @webDb, @docEntry";
                List<Tp_BoxContent> AllBoxContent = conn.Query<Tp_BoxContent>(sp_loadAlBxContent, new
                {
                    webDb = db.WEBDB,
                    docEntry = box.BaseEntry
                }).ToList();

                CartonCount cartonInfo = null;
                List<Tp_BoxContent> boxContents = null;
                if (repliedSo != null)
                {
                    cartonInfo = GetCarton(db, repliedSo.DOCENTRY);
                    for (int b = 0; b < packedBoxes.Count; b++)
                    {
                        packedBoxes[b].TpBoxContents =
                                    AllBoxContent.Where(bc => bc.BoxId == packedBoxes[b].BoxId).ToList();
                    }

                    boxContents = GetBoxContent(conn, db, repliedSo.DOCENTRY, box.BoxId);
                }

                // 20220120
                // reset the ship to null
                // when the shipto and shiptoadd is same 
                if (repliedSo != null && $"{repliedSo.SHIPTOADD}" == $"{repliedSo.SHIPTO}")
                {
                    repliedSo.SHIPTOADD = null; // let app to reload the address from SAP.
                }

                // get the packid 
                var queryPackId = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end) +1)  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) +1 [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_box Where BaseEntry = @docEntry";

                var packinfo = conn.Query<FTAPP_PackInfo>(queryPackId, new { docEntry = box.BaseEntry }).FirstOrDefault();
                if (packinfo == null)
                {
                    return BadRequest("Please try again, server in busy response. Thanks");
                }

                var isPrintSticker = false;
                if (string.IsNullOrWhiteSpace(box.OrderNo))
                {
                    box.OrderNo = string.IsNullOrWhiteSpace(repliedSo.REFNO) ? null : repliedSo.REFNO;
                }


                // if box no packed then 
                if (box.PackDt == default) // incase scan in new box 
                {
                    // update the box being pack 
                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        string sqlUpdate = "";
                        // handler TP order
                        if ($"{repliedSo.REFTYPE}" == "TPWARE" && $"{repliedSo.TpGrpShortName}" == "BC")
                        {
                            if (box.CurrentCartonNo == default) // new box
                            {
                                // new carton box
                                var cartonRunNo = GetBusinessCenterCartonNo(db, repliedSo.TpBizCenterCode, repliedSo.TpWeekOfOrder);
                                if (cartonRunNo == null)
                                {
                                    return BadRequest("Server busy, please try again. erro getting new Center carton number");
                                }

                                box.BusinessCenterCode = cartonRunNo.BusinessCenterCode;
                                box.OrderProcessWeek = cartonRunNo.OrderProcessWeek;
                                box.CurrentCartonNo = cartonRunNo.CurrentCartonNo;
                            }
                        }
                        else
                        { // if no tp box then set to null
                            box.BusinessCenterCode = null;
                            box.OrderProcessWeek = null;
                            box.CurrentCartonNo = default;
                        }

                        sqlUpdate = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
                                   SET PackId = @PackId 
                                       ,PackerCode = @PackerCode
                                       ,PackerName = @PackerName 
                                       ,PackDt = GETDATE()
                                       , OrderProcessWeek  = @OrderProcessWeek
                                       , BusinessCenterCode = @BusinessCenterCode
                                       , CurrentCartonNo = @CurrentCartonNo               
                                       , OrderNo = @OrderNo
                                   WHERE  id = @id ";

                        conn.Execute(sqlUpdate, new
                        {
                            PackId = packinfo.PackID,
                            PackerCode = dto.PackerCode,
                            PackerName = dto.PackerName,
                            OrderProcessWeek = box.OrderProcessWeek,
                            BusinessCenterCode = box.BusinessCenterCode,
                            CurrentCartonNo = box.CurrentCartonNo,
                            OrderNo = box.OrderNo,
                            id = box.id
                        }, trans);

                        //var sqlUpdate = $@"UPDATE {db.WEBDB}..FTAPP_Box
                        //       SET PackId = @PackId 
                        //           ,PackerCode = @PackerCode
                        //           ,PackerName = @PackerName 
                        //           ,PackDt = GETDATE()
                        //       WHERE Id = @id ";

                        //conn.Execute(sqlUpdate, new
                        //{
                        //    PackId = packinfo.PackID,
                        //    PackerCode = dto.PackerCode,
                        //    PackerName = dto.PackerName,
                        //    id = box.id
                        //}, trans);

                        //// get the packid 
                        var queryPackId1 = @$"select 
                                    convert(nvarchar, count (case when packid is not null then 1 end))  + ' / ' +
                                    convert(nvarchar, count(1)) [PackId]
                                    , count (case when packid is not null then 1 end) [CurrentCount]
                                    , count (1) [TotalBoxCount]
                                    from {db.WEBDB}..FTAPP_box Where BaseEntry = @docEntry";

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
                    RepliedSO = repliedSo,
                    RepliedPickedBoxIds = boxids,
                    RepliedCartonInfo = cartonInfo,
                    RepliedBoxContents = boxContents,
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

        int GetPackId(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId)) return 1;
            var splitted = packId.Split('/');
            if (splitted.Length <= 2)
            {
                return int.Parse($"{splitted[0]}".Trim());
            };
            return 1;
        }

        /// <summary>
        /// 20211013T2304
        /// </summary>
        /// <returns></returns>


        IActionResult GetCustomer2(Dto_Pack dto) // get address based on address Type
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.AddrType))
                {
                    return BadRequest("address type is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("db information retrieve error");
                }

                var sql_getcard = @"exec sp_SelectOCRD @erpDB, @cardCode";
                var conn = new SqlConnection(_commDbConnStr);
                var card = conn.Query<OCRD_Ext>(sql_getcard, new
                {
                    erpDB = db.SAPDB,
                    cardCode = dto.CardCode
                }).FirstOrDefault();

                if (card == null) return NotFound();

                var sql_address = @"exec sp_SelectCRD1 @erpDB, @cardCode, @adresType";
                var ship_address = conn.Query<CRD1>(sql_address,
                        new
                        {
                            erpDB = db.SAPDB,
                            cardCode = dto.CardCode,
                            adresType = dto.AddrType // S for ship, b for billing address
                        }).FirstOrDefault();

                if (ship_address == null)
                {
                    return Ok(card);
                }

                // get the ship address;
                card.ShipAdd = ship_address.GetAddress();
                return Ok(card);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SavePackSingle(Dto_Pack dto)
        {

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.PackedBox == null)
            {
                return BadRequest("Packed box name is empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("company info query error");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sql = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
                               SET PackId = @PackId 
                                   ,PackerCode = @PackerCode
                                   ,PackerName = @PackerName 
                                   ,PackDt = GETDATE()
                               WHERE     BaseEntry = @BaseEntry 
                                   AND BoxGuid = @BoxGuid ";


                conn.Execute(sql, dto.PackedBox, trans);
                trans.Commit();
                return Ok(); // return the box with number
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult TpSavePackSingle(Dto_Pack dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.PackedBox == null)
            {
                return BadRequest("Packed box name is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.QueryBoxid))
            {
                return BadRequest("query box id is empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("company info query error");
            }

            // check he packed is the TPWARE Sales order
            var TpwSo = GetSavePackBoxTPW(db, dto.QueryBoxid);
            if (TpwSo != null)
            {
                if (dto.PackedBox.CurrentCartonNo != default)
                {
                    goto PerformUpdate;
                }

                var cartonRunNo = GetBusinessCenterCartonNo(db, TpwSo.TpBizCenterCode, TpwSo.TpWeekOfOrder);
                if (cartonRunNo == null)
                {
                    return BadRequest("Svr busy, please try again later");
                }

                dto.PackedBox.BusinessCenterCode = cartonRunNo.BusinessCenterCode;
                dto.PackedBox.OrderProcessWeek = cartonRunNo.OrderProcessWeek;
                dto.PackedBox.CurrentCartonNo = cartonRunNo.CurrentCartonNo;
                goto PerformUpdate;
            }

            dto.PackedBox.BusinessCenterCode = null;
            dto.PackedBox.OrderProcessWeek = null;
            dto.PackedBox.CurrentCartonNo = default;

        PerformUpdate:

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {

                var sql = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
                               SET PackId = @PackId 
                                   ,PackerCode = @PackerCode
                                   ,PackerName = @PackerName 
                                   ,PackDt = GETDATE()
                                   , OrderProcessWeek  = @OrderProcessWeek
                                   , BusinessCenterCode = @BusinessCenterCode
                                   , CurrentCartonNo = @CurrentCartonNo               
                                   , OrderNo = @OrderNo
                               WHERE     BaseEntry = @BaseEntry 
                                   AND BoxGuid = @BoxGuid ";

                conn.Execute(sql, dto.PackedBox, trans);
                trans.Commit();
                return Ok(dto.PackedBox); // return the box with number
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //IActionResult TpSavePackSingle2(Dto_Pack dto)
        //{
        //    if (string.IsNullOrWhiteSpace(dto.CompanyName))
        //    {
        //        return BadRequest("Company name is empty");
        //    }
        //    if (dto.PackedBox == null)
        //    {
        //        return BadRequest("Packed box name is empty");
        //    }
        //    if (string.IsNullOrWhiteSpace(dto.QueryBoxid))
        //    {
        //        return BadRequest("query box id is empty");
        //    }

        //    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
        //    if (db == null)
        //    {
        //        return BadRequest("company info query error");
        //    }

        //    // check he packed is the TPWARE Sales order
        //    var TpwSo = GetSavePackBoxTPW(db, dto.QueryBoxid);
        //    if (TpwSo != null)
        //    {
        //        if (dto.PackedBox.CurrentCartonNo != default)
        //        {
        //            goto PerformUpdate;
        //        }

        //        var cartonRunNo = GetBusinessCenterCartonNo(db, TpwSo.TpBizCenterCode, TpwSo.TpWeekOfOrder);
        //        if (cartonRunNo == null)
        //        {
        //            return BadRequest("Svr busy, please try again later");
        //        }

        //        dto.PackedBox.BusinessCenterCode = cartonRunNo.BusinessCenterCode;
        //        dto.PackedBox.OrderProcessWeek = cartonRunNo.OrderProcessWeek;
        //        dto.PackedBox.CurrentCartonNo = cartonRunNo.CurrentCartonNo;
        //        goto PerformUpdate;
        //    }

        //    dto.PackedBox.BusinessCenterCode = null;
        //    dto.PackedBox.OrderProcessWeek = null;
        //    dto.PackedBox.CurrentCartonNo = default;

        //PerformUpdate:

        //    using var conn = new SqlConnection(_commDbConnStr);
        //    conn.Open();
        //    using var trans = conn.BeginTransaction();
        //    try
        //    {

        //        var sql = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
        //                       SET PackId = @PackId 
        //                           ,PackerCode = @PackerCode
        //                           ,PackerName = @PackerName 
        //                           ,PackDt = GETDATE()
        //                           , OrderProcessWeek  = @OrderProcessWeek
        //                           , BusinessCenterCode = @BusinessCenterCode
        //                           , CurrentCartonNo = @CurrentCartonNo               
        //                           , OrderNo = @OrderNo
        //                       WHERE     BaseEntry = @BaseEntry 
        //                           AND BoxGuid = @BoxGuid ";

        //        conn.Execute(sql, dto.PackedBox, trans);
        //        trans.Commit();
        //        return Ok(dto.PackedBox); // return the box with number
        //    }
        //    catch (Exception e)
        //    {
        //        trans.Rollback();
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult SavePack(Dto_Pack dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Company name is empty");
            }
            if (dto.PackedBoxes == null)
            {
                return BadRequest("Company name is empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
            if (db == null)
            {
                return BadRequest("company info query error");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                for (int b = 0; b < dto.PackedBoxes.Count; b++)
                {
                    var sql = $@"UPDATE [{db.WEBDB}].[dbo].[FTAPP_Box] 
                               SET PackId = @PackId 
                                   ,PackerCode = @PackerCode
                                   ,PackerName = @PackerName 
                                   ,PackDt = GETDATE()
                                   , OrderProcessWeek  = @OrderProcessWeek
                                   , BusinessCenterCode = @BusinessCenterCode
                                   , CurrentCartonNo = @CurrentCartonNo               

                               WHERE BaseEntry = @BaseEntry 
                                     AND BoxGuid = @BoxGuid ";

                    conn.Execute(sql, dto.PackedBoxes[b], trans);
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

        SO GetSavePackBoxTPW(DbInfo db, string queryBoxId)
        {
            try
            {
                var query = @"exec sp_SelectPackQuerySo_v2 @webDb, @erpDb, @boxId, @subSi, @subSiID";
                using var conn = new SqlConnection(_commDbConnStr);
                var so = conn.Query<SO>(query, new
                {
                    webDb = db.WEBDB,
                    erpDb = db.SAPDB,
                    boxId = queryBoxId,
                    subSi = db.COMPANYNAME,
                    subSiID = db.COMPANYID
                }).FirstOrDefault();

                if (so != null &&
                        $"{so.REFTYPE}" == "TPWARE" &&
                        $"{so.TpGrpShortName}" == "BC")
                {
                    return so;
                }
                return null;
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
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