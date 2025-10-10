using Dapper;
using KTC_SalesAppWAPI.DTOs.InStoreAct;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.InStoreAct;
using KTC_SalesAppWAPI.Models.Login;
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
    public class InStoreActivityController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<InStoreActivityController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public InStoreActivityController(IConfiguration configuration, ILogger<InStoreActivityController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
        }

        [HttpPost]
        public IActionResult Post(InStoreAct_DTO dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetInstoreAct":
                    {
                        return GetInstoreAct(dto);
                    }
                case "GetInstoreAct_Lines":
                    {
                        return GetInstoreAct_Lines(dto);
                    }
                case "GetUserAgency":
                    {
                        return GetUserAgency(dto);
                    }
                case "SaveInstoreAct":
                    {
                        return SaveInstoreAct(dto);
                    }
                case "GetAgencyBrands":
                    {
                        return GetAgencyBrands(dto);
                    }
                case "GetActNames":
                    {
                        return GetActNames(dto);
                    }
                case "GetPhotoNames_All":
                    {
                        return GetPhotoNames_All(dto);
                    }
                case "GetPhotoNameDesc":
                    {
                        return GetPhotoNameDesc(dto);
                    }
                case "GetInstoreActReason":
                    {
                        return GetInstoreActReasons(dto);
                    }
            }
            return BadRequest("request no found");
        }

        IActionResult GetInstoreActReasons(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company subsi");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbInfo == null)
                {
                    return BadRequest("Invalid databaase info.");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_SelectInStoreActReasons @webDb";

                var reasons = conn.Query<FTAPP_InStoreActReasons>(query, new { webDb = dbInfo.WEBDB }).ToList();
                if (reasons.Count == 0) return NotFound();

                return Ok(reasons);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetPhotoNameDesc(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company subsi");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbInfo == null)
                {
                    return BadRequest("Invalid databaase info.");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_SelectInStoreActPhotsDesc @webDb";

                var descs = conn.Query<FTAPP_InStoreActPhotoDesc>(query, new { webDb = dbInfo.WEBDB }).ToList();
                if (descs.Count == 0) return NotFound();

                return Ok(descs);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetPhotoNames_All(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company subsi");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbInfo == null)
                {
                    return BadRequest("Invalid databaase info.");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_SelectPhotosNames_All @webDb";

                var names = conn.Query<FTAPP_InStoreActPhotoNames>(query, new { webDb = dbInfo.WEBDB }).ToList();
                if (names.Count == 0) return NotFound();

                return Ok(names);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetActNames(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company subsi");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbInfo == null)
                {
                    return BadRequest("Invalid databaase info.");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_SelectActNames @webDb";

                var names = conn.Query<FTAPP_InStoreActConfig>(query, new { webDb = dbInfo.WEBDB }).ToList();
                if (names.Count == 0) return NotFound();

                return Ok(names);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetAgencyBrands(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.AgencyCode))
                {
                    return BadRequest("Invalid agency name");
                }
                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (dbInfo == null)
                {
                    return BadRequest("Invalid databaase info.");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var query = @"exec sp_SelectAgencyBrands @sapDb, @agencyCode";

                var brands = conn.Query<string>(query, new { sapDb = dbInfo.SAPDB, agencyCode = dto.AgencyCode }).ToList();
                if (brands.Count == 0) return NotFound();

                return Ok(brands);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetUserAgency(InStoreAct_DTO dto)
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
                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("Invalid card type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var sp_query = @"exec sp_SelectInStoreActAgency @webDb, @erpDb, @cardType, @userCode, @subsi";
                var results = new SqlConnection(_commDbConnStr).Query<OCRD_Ext>(sp_query,
                    new
                    {
                        webDb = db.WEBDB,
                        erpDb = db.SAPDB,
                        cardType = dto.CardType,
                        userCode = dto.UserCode,
                        subsi = dto.Subsi
                    }).ToList();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult SaveInstoreAct(InStoreAct_DTO dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.Head == null)
            {
                return BadRequest("the doc head is empty");
            }

            if (dto.Lines == null)
            {
                return BadRequest("the doc lines is empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Query company info fail, please try again later.");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                if (dto.Head.id > 0)
                {
                    // delete tye head 
                    // delete the lines
                    var deleteSql = $"Delete from {db.WEBDB}..FTAPP_InStoreAct Where id = @id";
                    conn.Execute(deleteSql, new { id = dto.Head.id }, trans);

                    deleteSql = $"Delete from {db.WEBDB}..FTAPP_InStoreAct1 Where HeadGuid = @HeadGuid";
                    conn.Execute(deleteSql, new { HeadGuid = dto.Head.HeadGuid }, trans);
                }

                // insert the header 
                var sql_insertHead = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_InStoreAct] ( 
                                             ActName
                                           , StoreCode
                                           , StoreName
                                           , UserCode
                                           , UserName
                                           , ActDt
                                           , HeadGuid
                                           , CreatedDt
                                           , AgencyCode
                                           , AgencyName
                                           , SubSi , DocStatus
                                            ) VALUES ( 
                                             @ActName
                                           ,@StoreCode
                                           ,@StoreName
                                           ,@UserCode
                                           ,@UserName
                                           ,@ActDt
                                           ,@HeadGuid
                                           ,GETDATE()
                                           ,@AgencyCode
                                           ,@AgencyName
                                           ,@SubSi, @DocStatus ) ";


                var res = conn.Execute(sql_insertHead, dto.Head, trans);

                var sql_insertLine = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_InStoreAct1]
                                           ( HeadGuid
                                           , LineGuid
                                           , ActName
                                           , ActDesc
                                           , ActFile
                                           , Remarks 
                                           , FileDt
                                           , PhotoName, PhotoDesc, ItemGroup, Reason
                                        ) VALUES (
                                            @HeadGuid
                                           ,@LineGuid
                                           ,@ActName
                                           ,@ActDesc
                                           ,@ActFile
                                           ,@Remarks 
                                           ,@FileDt 
                                           ,@PhotoName, @PhotoDesc, @ItemGroup, @Reason)";

                res = conn.Execute(sql_insertLine, dto.Lines, trans);
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


        IActionResult GetInstoreAct_Lines(InStoreAct_DTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.HeadGuid))
                {
                    return BadRequest("query guid is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Query company info fail, please try again later.");
                }

                var sp_sql = "exec sp_SelectInStoreActLines @webDb, @guid";

                using var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_InStoreAct1>(sp_sql, new
                {
                    webDb = db.WEBDB,
                    guid = dto.HeadGuid
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

        IActionResult GetInstoreAct(InStoreAct_DTO dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Invalid card code");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.StartDt.Equals(default))
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt.Equals(default))
                {
                    return BadRequest("Invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info query");
                }

                var sp_GetInstoreAct = @"exec sp_SelectInstoreAct @webDb, @userCode, @cardCode, @subsi, @startDt, @endDt";
                var conn = new SqlConnection(_commDbConnStr);
                var results = conn.Query<FTAPP_InStoreAct>(sp_GetInstoreAct, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    cardCode = dto.CardCode,
                    subsi = dto.Subsi,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

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
