using Dapper;
using KTC_SalesAppWAPI.DTOs;
using KTC_SalesAppWAPI.DTOs.Geofence;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Geofence;
using KTC_SalesAppWAPI.Models.GeoFence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controller

{
    [Route("[controller]")]
    [ApiController]
    public class GeofenceController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<GeofenceController> _logger;
        string LastError { get; set; } = string.Empty;

        string _commDbConnStr { get; set; } = string.Empty;

        public GeofenceController(IConfiguration configuration, ILogger<GeofenceController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(Geofence_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                //case "LoggingGeofence":
                //    {
                //        return LoggingGeofence(dto);
                //    }
                case "DateTime":
                    {
                        var datetime = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss}";
                        return Ok(datetime);
                    }
                case "ServerTime":
                    {
                        return Ok(new { SvrDt = DateTime.Now.ToString("yyyy-MMM-dd HH:mm tt") });
                    }
                case "SaveStatus":
                    {
                        return SaveStatus(dto);
                    }
                case "SaveLog":
                    {
                        return SaveLog(dto);
                    }
                case "GetMissTypes":
                    {
                        return GetMissTypes(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetMissTypes(Geofence_Dto dto)
        {
            try
            {
                var sql_select = @"SELECT id, MissType, RemarkOpt FROM [FTAPP_TypeOfMiss] ";
                var conn = new SqlConnection(_commDbConnStr); // connect to common database

                var results = conn.Query<FTAPP_TypeOfMiss>(sql_select).ToList();

                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveLog(Geofence_Dto dto)
        {
            try
            {
                // add in the post success log 
                if (dto.Line == null)
                {
                    return BadRequest("Log line empty");
                }

                new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveStatus(Geofence_Dto dto)
        {
            try
            {
                #region validation before save
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The geo company name is empty");
                }

                if (string.IsNullOrEmpty(dto.GeoSetTransitionField))
                {
                    return BadRequest("The geo field value is empty");
                }

                if (dto.GeoTripId <= 0)
                {
                    return BadRequest("The geo trip id is invalid");
                }

                if (string.IsNullOrWhiteSpace(dto.GeoSetTransitionDisField))
                {
                    return BadRequest("The geo dist f is invalid");
                }

                if (dto.GeoDistance <= 0)
                {
                    return BadRequest("The geo dist f is invalid");
                }

                if (string.IsNullOrWhiteSpace(dto.GeoUserCode))
                {
                    return BadRequest("The geo user code is invalid");
                }

                if (string.IsNullOrWhiteSpace(dto.GeoType))
                {
                    return BadRequest("The geo type is invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.GeoStoreCode))
                {
                    return BadRequest("Invalid store code");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("The geo dn info is empty");
                }

                #endregion validation before save

                // query the user code GUID 
                var sqlGetGuid = @$"Select convert(nvarchar(50), HeaderGuid) [HeaderGuid] 
                                    FROM {dbInfo.WEBDB}..FTAppGeoTrack WITH (NOLOCK) 
                                    Where UserCode = @GeoUserCode  
                                    AND StoreCode = @GeoStoreCode
                                    AND Convert(date, ScheduleDate) = @Date";

                var Date = $"{DateTime.Now:yyyyMMdd}";
                using var conn = new SqlConnection(_commDbConnStr);
                var guid = conn.ExecuteScalar<string>(sqlGetGuid,
                        new
                        {
                            dto.GeoUserCode,
                            dto.GeoStoreCode,
                            Date
                        });

                if (string.IsNullOrWhiteSpace(guid))
                {
                    return NotFound("Either Store code or user code invalid");
                }

                //check entry in FTAppGeoTrackLine
                //@WebDb as nvarchar(120), 
                //@HeaderGuid as nvarchar(max),
                //@TripId as int
                var sql_sp = "exec sp_SelectGeoTrackLineWithTripId @WebDb, @HeaderGuid, @TripId";
                var param = new
                {
                    WebDb = dbInfo.WEBDB,
                    HeaderGuid = guid,
                    Tripid = dto.GeoTripId
                };

                var companies = GetUserCompanies(dto.GeoUserCode);
                if (companies == null) return NotFound();
                if (companies.Count == 0) return NotFound();

                var line = conn.Query<FTAppGeoTrackLine>(sql_sp, param).FirstOrDefault();                
                // end and closs the conn at this step
                
                if (line == null && $"{dto.GeoType}".Equals("Entry"))
                {
                    // check is more thean 30 then insert log
                    // 2021 07 04
                    // convert into int for compare
                    // get system setup radius
                    var sql_query = @"select SetupValue 
                                    from KTCW_COMMON..FTApp_Config WITH (NOLOCK) 
                                    where SetupName = 'Geofence_Radius_Meter' ";

                    using var connForEntry = new SqlConnection(_commDbConnStr);
                    var int_radius = connForEntry.ExecuteScalar<int>(sql_query);
                    if (int_radius <= 0)
                    {
                        int_radius = 30;
                    }

                    var int_distanct = Convert.ToInt32(dto.GeoDistance);
                    if (int_radius < int_distanct)
                    {
                        var newlog = new FTAPP_AppPostLog
                        {
                            AppModule = "Server, Geo entered",
                            UserCode = dto.GeoUserCode,
                            CardCode = dto.GeoStoreCode,
                            SubSi = dto.CompanyName,
                            Details = $"Over Geofence Radius Meter, Act: {dto.GeoDistance} | {int_distanct}" +
                                                                $", Sys: {int_radius}, over {dto.GeoDistance - int_radius} meter",
                            PostResult = "NA",
                            AppVersion = dto.AppVersion
                        };

                        new AppPostLogHelper().Create(_commDbConnStr, newlog);
                    }

                    // insert new line
                    var insertSql = @$"INSERT INTO {dbInfo.WEBDB}..FTAppGeoTrackLine (
                                            HeadGuid
                                           ,LineGuid
                                           ,TripID
                                           ,LastEnterDt    
                                           ,LastEnterDt_DisToStore
                                           ,ActualLat
                                           ,ActualLongi
                                           ,AppVersion
                                        ) VALUES (
                                            @HeadGuid
                                           ,@LineGuid
                                           ,@TripID
                                           ,GETDATE()
                                           ,@LastEnterDt_DisToStore
                                           ,@ActualLat
                                           ,@ActualLongi
                                           ,@AppVersion
                                        )";

                    var newLine = new FTAppGeoTrackLine
                    {
                        HeadGuid = Guid.Parse(guid),
                        LineGuid = Guid.NewGuid(),
                        TripID = dto.GeoTripId,
                        LastEnterDt_DisToStore = dto.GeoDistance,
                        ActualLat = dto.Lat,
                        ActualLongi = dto.Longi,
                        AppVersion = $"{dto.AppVersion}"
                    };

                    if (connForEntry.State == System.Data.ConnectionState.Closed) connForEntry.Open();
                    using var transEntry = connForEntry.BeginTransaction();
                    try
                    {
                        int insertResult = connForEntry.Execute(insertSql, newLine, transEntry);
                        if (insertResult <= 0)
                        {
                            LastError = $"Error Insert GEO Entry, {dbInfo.COMPANYNAME}," +
                                        $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [Entry0]";
                            _logger.LogError(LastError);
                            transEntry.Rollback();
                            return BadRequest(LastError);
                        }

                        // comit and return
                        transEntry.Commit();
                        return LoadSchedule_MultipleTrip(new UserProfile_Dto
                        {
                            Companies = companies,
                            QueryUserCode = dto.GeoUserCode
                        });
                    }
                    catch (Exception e)
                    {
                        transEntry.Rollback();
                        LastError = $"Error Insert GEO Entry, { dbInfo.COMPANYNAME}," +
                                             $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [Entry1]" +
                                             $" \n\n{e.Message} \n{e.StackTrace}";

                        _logger.LogError(LastError);
                        return BadRequest(LastError);
                    }
                }

                // handle stay
                if ($"{dto.GeoType}".Equals("Stay"))
                {
                    using var connForStay = new SqlConnection(_commDbConnStr);
                    // get compliance minute 
                    var query_getComplied_minute = @"SELECT [SetupValue] 
                                                    FROM KTCW_COMMON..FTApp_Config WITH (NOLOCK)
                                                    WHERE SetupName = 'Geofence_StayOnMinute' ";

                    
                    var std_duration = connForStay.ExecuteScalar<int>(query_getComplied_minute);
                    if (std_duration <= 0) std_duration = 5;
                    var minutesAddUp = (dto.StayDuration < std_duration) ? std_duration : dto.StayDuration;
                    var date = line.LastEnterDt.AddMinutes(minutesAddUp);
                    
                    if (connForStay.State == System.Data.ConnectionState.Closed) connForStay.Open();                   
                    using (var transUpd = connForStay.BeginTransaction())
                    {
                        try
                        {
                            var updateSql = @$"UPDATE {dbInfo.WEBDB}..FTAppGeoTrackLine
                                       SET {dto.GeoSetTransitionField} = @date,
                                           {dto.GeoSetTransitionDisField} = {dto.GeoDistance}
                                       WHERE HeadGuid = @HeadGuid
                                       AND TripID = @GeoTripId ";

                            int updateResult = connForStay.Execute(updateSql, new
                            {
                                HeadGuid = guid,
                                dto.GeoTripId,
                                date
                            }, transUpd);

                            if (updateResult < 0)
                            {
                                LastError = $"Error update GEO Stay, { dbInfo.COMPANYNAME}," +
                                               $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [Stay0]";
                                _logger.LogError(LastError);
                                transUpd.Rollback();
                                return BadRequest(LastError);
                            }

                            transUpd.Commit();
                            return LoadSchedule_MultipleTrip(new UserProfile_Dto
                            {
                                Companies = companies,
                                QueryUserCode = dto.GeoUserCode
                            });
                        }
                        catch (Exception e)
                        {
                            transUpd.Rollback();
                            LastError = $"Error update GEO Stay, {dbInfo.COMPANYNAME}," +
                                                $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [Stay1] " +
                                                $" \n\n{e.Message} {e.StackTrace}";
                            _logger.LogError(LastError);
                            return BadRequest(LastError);
                        }
                    }   // using transaction
                }

                // handle exit
                if ($"{dto.GeoType}".Equals("Exit")) // Exit // Exit
                {
                    var sql_checkDup = @$"SELECT * FROM {dbInfo.WEBDB}..FTAppGeoTrackLine
                                        WHERE HeadGuid = @HeadGuid
                                        AND TripID = @GeoTripId ";

                    // make new database connect
                    using var connForExit = new SqlConnection(_commDbConnStr);
                    var foundLine = connForExit.Query<FTAppGeoTrackLine>(sql_checkDup,
                            new
                            {
                                HeadGuid = guid,
                                dto.GeoTripId
                            }).FirstOrDefault();

                    if (foundLine == null)
                    {
                        if (connForExit.State == System.Data.ConnectionState.Closed) connForExit.Open();
                        using var transExit = connForExit.BeginTransaction();

                        try
                        {
                            // incase no entry, but declare exit outside of the store
                            // create the exit record
                            var newExitLine = new FTAppGeoTrackLine
                            {
                                HeadGuid = Guid.Parse(guid),
                                LineGuid = Guid.NewGuid(),
                                TripID = dto.GeoTripId,
                                //LastExitDt = DateTime.Now, // get server date
                                LastExitDt_DisToStore = dto.GeoDistance,
                                ActualLat = dto.Lat,
                                ActualLongi = dto.Longi,
                                AppVersion = $"{dto.AppVersion}",
                                TypeOfMiss = $"{dto.TypeOfMiss}",
                                Remarks = $"{dto.Remarks}"
                            };

                            // insert new line 
                            var sql_insert = $@"INSERT INTO {dbInfo.WEBDB}..FTAppGeoTrackLine  (
                                                     HeadGuid
                                                   , LineGuid
                                                   , TripID                                              
                                                   , LastExitDt
                                                   , LastExitDt_DisToStore                                              
                                                   , ActualLat
                                                   , ActualLongi
                                                   , AppVersion
                                                   , TypeOfMiss
                                                   , Remarks 
                                               ) values (
                                                     @HeadGuid
                                                    ,@LineGuid
                                                    ,@TripID                
                                                    ,GETDATE()
                                                    ,@LastExitDt_DisToStore 
                                                    ,@ActualLat
                                                    ,@ActualLongi
                                                    ,@AppVersion
                                                    ,@TypeOfMiss
                                                    ,@Remarks 
                                                )";

                            var insertResult = connForExit.Execute(sql_insert, newExitLine, transExit);
                            if (insertResult <= 0)
                            {
                                LastError = $"Error insert GEO for exit, { dbInfo.COMPANYNAME}," +
                                          $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [0]";
                                _logger.LogError(LastError);
                                transExit.Rollback();
                                return BadRequest(LastError);
                            }

                            transExit.Commit();
                            RepairEntry(dbInfo, guid);
                            return LoadSchedule_MultipleTrip(new UserProfile_Dto
                            {
                                Companies = companies,
                                QueryUserCode = dto.GeoUserCode
                            });
                        }
                        catch (Exception e)
                        {
                            LastError = $"Error insert GEO for exit, { dbInfo.COMPANYNAME}," +
                                             $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode}  [1]";
                            _logger.LogError(LastError);
                            transExit.Rollback();
                            return BadRequest(LastError);
                        } // end try

                    } // end if 

                    // if found the line then perform update exit datetime
                    // handler entry the store, and declare exit in front of the store.
                    var updateSql = @$"UPDATE {dbInfo.WEBDB}..FTAppGeoTrackLine 
                                   SET {dto.GeoSetTransitionField} = GETDATE(),
                                       {dto.GeoSetTransitionDisField} = @GeoDistance, 
                                   TypeOfMiss = @TypeOfMiss,
                                   Remarks = @Remarks
                                   WHERE HeadGuid = @HeadGuid                                       
                                   AND TripID = @GeoTripId ";

                    using var connForExit1 = new SqlConnection(_commDbConnStr);
                    connForExit1.Open();
                    using var transExit1 = connForExit1.BeginTransaction();

                    try
                    {
                        int updateResult = connForExit1.Execute(updateSql,
                            new
                            {
                                HeadGuid = guid,
                                GeoTripId = dto.GeoTripId,
                                GeoDistance = dto.GeoDistance,
                                AppVersion = $"{dto.AppVersion}",
                                TypeOfMiss = $"{dto.TypeOfMiss}",
                                Remarks = $"{dto.Remarks}"
                            }, transExit1);

                        if (updateResult <= 0)
                        {
                            transExit1.Rollback();
                            LastError = $"Error update GEO for exit, { dbInfo.COMPANYNAME}," +
                                        $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [00]";
                            _logger.LogError(LastError);
                            return BadRequest(LastError);
                        }

                        transExit1.Commit();
                        RepairEntry(dbInfo, guid);
                        return LoadSchedule_MultipleTrip(new UserProfile_Dto
                        {
                            Companies = companies,
                            QueryUserCode = dto.GeoUserCode
                        });
                    }
                    catch (Exception e)
                    {
                        transExit1.Rollback();
                        LastError = $"Error update GEO for exit, { dbInfo.COMPANYNAME}," +
                                    $"UID: {dto.GeoUserCode}, STORECODE: {dto.GeoStoreCode} [01] " +
                                    $"\n\n{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);                        
                        return BadRequest(LastError);
                    }
                }

                LastError = $"No recognised GeoType, " +
                            $"please try again. STORECODE:{dto.GeoStoreCode} UID: {dto.GeoUserCode}";

                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void RepairEntry(DbInfo db, string headGuid)
        {
            try
            {
                var sqlGeoLine = $"Select * from {db.WEBDB}..FTAPPGeoTRackLine with (nolock) " +
                                    $"Where HeadGuid = @HeadGuid " +
                                    $"order by id";

                using var conn = new SqlConnection(_commDbConnStr);
                var trackLines = conn.Query<FTAppGeoTrackLine>(sqlGeoLine, new
                {
                    HeadGuid = headGuid
                }).ToList();

                if (trackLines.Count == 0) return;

                var query_getComplied_minute = @"SELECT [SetupValue] 
                                                 FROM [KTCW_COMMON].[DBO].[FTApp_Config] WITH (NOLOCK)
                                                 WHERE SetupName = 'Geofence_StayOnMinute' ";

                var std_duration = conn.ExecuteScalar<int>(query_getComplied_minute);
                // condition 1
                /*
                        update ktcw_kk..FTAppGeoTrackLine 
                        set LastEnterDt = DATEADD(MINUTE, -5, LastExitDt)
                        Where LastEnterDt is null and tripid =1 and len(TypeOfMiss) =0                 
                */
                // got trip 1 but last enter time is null

                var trip1 = trackLines
                        .Where(x => x.LastEnterDt == default &&
                                    x.TripID == 1 &&
                                    x.LastExitDt != default &&
                                    string.IsNullOrWhiteSpace(x.TypeOfMiss)).FirstOrDefault();

                if (trip1 != null)
                {
                    var udpateSql = $@"update {db.WEBDB}..FTAppGeoTrackLine 
                        set LastEnterDt = DATEADD(MINUTE, -{std_duration}, LastExitDt)
                        Where id = @id";

                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        conn.Execute(udpateSql, new { id = trip1.Id }, trans);
                        trans.Commit();
                        return;
                    }
                    catch (Exception e)
                    {
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        trans.Rollback();
                        return;
                    }
                }

                // there is no trip 1
                // create the trip 1
                trip1 = trackLines
                       .Where(x => x.TripID == 1 &&
                                   string.IsNullOrWhiteSpace(x.TypeOfMiss)).FirstOrDefault();

                // no trip 1 recorded 
                if (trip1 == null)
                {
                    // take the available trip
                    // and modified
                    trip1 = trackLines.FirstOrDefault();
                    if (trip1 != null && trip1.LastExitDt != default)
                    {
                        var udpateSql = $@"update {db.WEBDB}..FTAppGeoTrackLine 
                                        set TripId = 1, LastEnterDt = DATEADD(MINUTE, -{std_duration}, LastExitDt)
                                        Where id = @id";

                        if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                        using var trans1 = conn.BeginTransaction();
                        try
                        {
                            conn.Execute(udpateSql, new { id = trip1.Id }, trans1);
                            trans1.Commit();
                            return;
                        }
                        catch (Exception e)
                        {
                            LastError = $"{e.Message}\n{e.StackTrace}";
                            _logger.LogError(LastError);
                            trans1.Rollback();
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }


        IActionResult LoadSchedule_MultipleTrip(UserProfile_Dto dto)
        {
            try
            {
                // based on the user coder and use slpname to load today store

                if (dto.Companies == null)
                {
                    return BadRequest("Invalid query company name");
                }

                if (dto.Companies.Count == 0)
                {
                    return BadRequest("Invalid query company name");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryUserCode))
                {
                    return BadRequest("Invalid query user code");
                }

                var results = new List<FTAppGeoTrack>();
                dto.Companies.ForEach(c =>
                {
                    var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, c);
                    if (dbInfo == null) return;

                    var query = @$"SELECT * FROM [{dbInfo.WEBDB}].[dbo].[FTAppGeoTrack] WITH (NOLOCK) 
                                    WHERE UserCode = @QueryUserCode 
                                    AND Convert(date, ScheduleDate) = @QueryScheduleDate";

                    using (var conn = new SqlConnection(_commDbConnStr))
                    {
                        var result = conn.Query<FTAppGeoTrack>(query, new
                        {
                            dto.QueryUserCode,
                            QueryScheduleDate = DateTime.Now.Date
                        }).ToList();

                        if (result == null) return;
                        if (result.Count == 0) return;

                        // magge the line (if any)

                        result.ForEach(s =>
                        {
                            var sql = @$"SELECT * 
                                        FROM [{dbInfo.WEBDB}].[DBO].[FTAppGeoTrackLine] WITH (NOLOCK) 
                                        WHERE HeadGuid = @HeaderGuid";
                            s.Line = conn.Query<FTAppGeoTrackLine>(sql, new { s.HeaderGuid }).ToList();
                        });
                        results.AddRange(result);
                    };
                });
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        List<string> GetUserCompanies(string userCode)
        {
            try
            {
                var sql = @"select UserCompany from [FTAPP_SSO] WITH (NOLOCK) 
                                where UserCode = @userCode";
                using var conn = new SqlConnection(_commDbConnStr);
                return conn.Query<string>(sql, new { userCode }).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }



        bool IsDuplicatedScheduleLog(FTApp_BaseRouteSchedule log, DbInfo dbInfo, SqlConnection conn, GeofenceResult_Ext result)
        {
            string checkSql = string.Empty;
            string objecJson = JsonConvert.SerializeObject(result);
            string objecJson1 = JsonConvert.SerializeObject(log);

            try
            {
                var date = new DateTime(result.Year, result.Month, result.Day).ToString("yyyyMMdd");

                checkSql = @$"SELECT * FROM [{dbInfo.WEBDB}].[dbo].[FTApp_BaseRouteSchedule_Log] WITH (NOLOCK) 
                            WHERE UserCode = @UserCode 
                            AND StoreCode = @CardCode                          
                            AND CONVERT(Date, ScheduleDate) = @date";

                using (conn)
                {
                    var found = conn.Query<FTApp_BaseRouteSchedule_Log>(checkSql,
                        new
                        {
                            result.UserCode,
                            result.CardCode,
                            date
                        }).FirstOrDefault();

                    return (found == null) ? false : true;
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}\n\n " +
                    $"[[IsDuplicatedScheduleLog]] json geo result -> {objecJson}\n\n" +
                    $"json schedule log {objecJson1} \n\nsql check exit->{checkSql}\n\n";
                _logger.LogError(LastError);
                return true;
            }
        }
    }
}

//IActionResult LoggingGeofence(Geofence_Dto dto)
//{
//    if (dto.Result == null)
//    {
//        return BadRequest("Geofence result is null");
//    }

//    if (string.IsNullOrWhiteSpace(dto.CompanyName))
//    {
//        return BadRequest("Geofence logging, company name is null");
//    }

//    // get the dbinfo 
//    var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
//    if (dbInfo == null)
//    {
//        return BadRequest("Geofence logging, no log in, company name no found.");
//    }

//    var sqlInsertLog = @$"INSERT INTO [{dbInfo.WEBDB}].[dbo].[FTAPP_Geofence_Log] (
//                               UserCode
//                              ,UserName
//                              ,CardCode
//                              ,Year
//                              ,Month
//                              ,Day
//                              ,Occured
//                              ,LastEnterTime
//                              ,LastExitTime
//                              ,Transition
//                              ,RegionId
//                              ,Duration
//                              ,SinceLastEntry
//                              ,Latitude
//                              ,Longitude
//                              ,Accuracy
//                              ,TransitionName
//                              ,DistanceToStore
//                              ,TripId
//                                ) VALUES (
//                               @UserCode
//                              ,@UserName
//                              ,@CardCode
//                              ,@Year
//                              ,@Month
//                              ,@Day
//                              ,@Occured
//                              ,@LastEnterTime
//                              ,@LastExitTime
//                              ,@Transition
//                              ,@RegionId
//                              ,@Duration
//                              ,@SinceLastEntry
//                              ,@Latitude
//                              ,@Longitude
//                              ,@Accuracy
//                              ,@TransitionName
//                              ,@DistanceToStore
//                              ,@TripId
//                               )";

//    using var conn = new SqlConnection(_commDbConnStr);
//    conn.Open();
//    using var trans = conn.BeginTransaction();
//    try
//    {
//        var result = conn.Execute(sqlInsertLog, dto.Result);
//        if (result > 0)
//        {
//            HandlerTransition(dto.Result, dbInfo); // put into summary table for later refence
//            trans.Commit();
//            return Ok();
//        }
//        return BadRequest("Update may not success");
//    }
//    catch (Exception e)
//    {
//        trans.Rollback();
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}

// Perform summary of data, save into or call compliance
//void HandlerTransition(GeofenceResult_Ext result, DbInfo dbInfo)
//{
//    var checkTable = string.Empty;
//    var isExit = false;
//    try
//    {
//        switch (result.Transition)
//        {
//            case (int)EnumGeofenceTransName.Entered:
//                {
//                    checkTable = "FTApp_Geofence_Entered";
//                    break;
//                }
//            case (int)EnumGeofenceTransName.Stayed:
//                {
//                    checkTable = "FTApp_Geofence_Stayed";
//                    InsertBaseRouteScheduleLog(result, dbInfo); // update the FTApp_BaseRouteSchedule_Log 
//                    break;
//                }
//            case (int)EnumGeofenceTransName.Exited:
//                {
//                    isExit = true;
//                    checkTable = "FTApp_Geofence_Exited";
//                    break;
//                }
//            default:
//                {
//                    return;
//                }
//        }

//        if (string.IsNullOrWhiteSpace(checkTable))
//        {
//            return;
//        }

//        // check the record exist
//        var sqlCheckExistance =
//            @$"SELECT * FROM [{dbInfo.WEBDB}].[dbo].[{checkTable}] WITH (NOLOCK) 
//                        Where [Year] = @Year
//                        AND [Month] = @Month
//                        AND [Day] = @Day
//                        AND CardCode = @CardCode 
//                        AND UserCode = @UserCode ";

//        using var conn = new SqlConnection(_commDbConnStr);
//        var date = $"{DateTime.Now:yyyyMMdd}";
//        var found = conn.Query<GeofenceSummary>(sqlCheckExistance, new
//        {
//            Year = result.Year,
//            Month = result.Month,
//            Day = result.Day,
//            result.CardCode,
//            result.UserCode
//        }).FirstOrDefault();

//        if (found == null) // to ensure always one 
//        {
//            int insertResult = InsertSummary(dbInfo, checkTable, result, conn);
//            return;
//        }
//        else if (isExit)
//        {
//            // update the table
//            int updateResult = UpdateSummary(dbInfo, checkTable, result.Occured, found, conn);
//        }

//        // else 
//        // do nothing since already log
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//    }
//}

//int UpdateSummary(DbInfo dbInfo, string checkTable, DateTime Occured, GeofenceSummary summ, SqlConnection conn)
//{
//    try
//    {

//        var updateSql =
//                    @$"UPDATE [{dbInfo.WEBDB}].[dbo].[{checkTable}] 
//                    SET Occured = @Occured
//                    WHERE Id = @Id";

//        var updateResult = conn.Execute(updateSql,
//            new
//            {
//                Occured = Occured,
//                summ.Id
//            });

//        return updateResult;
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return -1;
//    }
//}

//resued code for all
//int InsertSummary(DbInfo dbInfo, string checkTable, GeofenceResult_Ext result, SqlConnection conn)
//{
//    // perform insert the summary 
//    try
//    {
//        var insertSql =
//                    @$"INSERT INTO [{dbInfo.WEBDB}].[dbo].[{checkTable}] (
//                               TransitionName
//                              ,CardCode
//                              ,UserCode
//                              ,Occured
//                              ,Year
//                              ,Month
//                              ,Day
//                              ,DistanceToStore
//                              ) VALUES (
//                               @TransitionName
//                              ,@CardCode
//                              ,@UserCode
//                              ,@Occured
//                              ,@Year
//                              ,@Month
//                              ,@Day
//                              ,@DistanceToStore
//                                )";

//        var insertResult = conn.Execute(insertSql,
//            new
//            {
//                result.TransitionName,
//                result.CardCode,
//                result.UserCode,
//                result.Occured,
//                result.Year,
//                result.Month,
//                result.Day,
//                result.DistanceToStore
//            });

//        return insertResult;
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return -1;
//    }
//}

// insert BaseRouteSchedule log 
// to match the store called complianace 
//void InsertBaseRouteScheduleLog(GeofenceResult_Ext result, DbInfo dbInfo)
//{
//    string sql = string.Empty;
//    string insertSql = string.Empty;
//    string objecJson = JsonConvert.SerializeObject(result);
//    try
//    {
//        // look for the scehdule call
//        // by day month year
//        var date = new DateTime(result.Year, result.Month, result.Day).ToString("yyyyMMdd");

//        sql = @$"SELECT * FROM [{dbInfo.WEBDB}].[dbo].[FTApp_BaseRouteSchedule] WITH (NOLOCK) 
//                    WHERE UserCode = @SlpName 
//                    AND StoreCode = @CardCode                             
//                    AND CONVERT(Date, ScheduleDate) = @date";

//        var conn = new SqlConnection(_commDbConnStr);
//        var found = conn.Query<FTApp_BaseRouteSchedule>(sql,
//            new
//            {
//                result.SlpName,
//                result.CardCode,
//                date
//            }).FirstOrDefault();

//        if (found == null) return; // if not in schedule plan then ignore

//        // yes there is a scehdule 
//        // then check duplicated log 
//        if (IsDuplicatedScheduleLog(found, dbInfo, conn, result))
//        {
//            return; // since already save, then ignore
//        }

//        // insert new log
//        var newLog = new FTApp_BaseRouteSchedule_Log
//        {
//            ScheduleNo = found.ScheduleNo,
//            ScheduleName = found.ScheduleName,
//            ScheduleDate = found.ScheduleDate,
//            ScheduleType = found.ScheduleType,
//            RouteCode = found.RouteCode,
//            StoreCode = found.StoreCode,
//            StoreName = found.StoreName,
//            UserCode = found.UserCode,
//            IsCompliance = 1,
//            ComplianceDate = DateTime.Now
//        };

//        insertSql = @$"INSERT INTO [{dbInfo.WEBDB}].[dbo].[FTApp_BaseRouteSchedule_Log] ( 
//                             ScheduleNo
//                            ,ScheduleName
//                            ,ScheduleDate
//                            ,ScheduleType
//                            ,RouteCode
//                            ,StoreCode
//                            ,StoreName
//                            ,UserCode
//                            ,IsCompliance
//                            ,ComplianceDate
//                            ) VALUES (
//                             @ScheduleNo
//                            ,@ScheduleName
//                            ,@ScheduleDate
//                            ,@ScheduleType
//                            ,@RouteCode
//                            ,@StoreCode
//                            ,@StoreName
//                            ,@UserCode
//                            ,@IsCompliance
//                            ,@ComplianceDate
//                            )";

//        conn = new SqlConnection(_commDbConnStr);
//        conn.Execute(insertSql, newLog);
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}\n\n " +
//            $"[[InsertBaseRouteScheduleLog]] json -> {objecJson}\n\nsql check exit->{sql}\n\n sql insert->{insertSql}";
//        _logger.LogError(LastError);
//    }
//}
//void UpdateBlankExit(string webDb, string headGuid, double distance)
//{
//    try
//    {
//        var sql = @$"SELECT Id
//                  ,HeadGuid
//                  ,LineGuid
//                  ,TripID
//                  ,LastEnterDt
//                  ,LastStayOnDt
//                  ,LastExitDt
//                  ,LastEnterDt_DisToStore
//                  ,LastStayedDt_DisToStore
//                  ,LastExitDt_DisToStore
//                  ,Duration_EntryDtToExitDt
//                  ,ActualLat
//                  ,ActualLongi
//              FROM [{webDb}].[dbo].[FTAppGeoTrackLine] WITH (NOLOCK)
//              WHERE LastEnterDt IS NOT NULL 
//              AND LastExitDt IS NULL
//              AND HeadGuid = @headGuid order by id desc";

//        using var conn = new SqlConnection(_commDbConnStr);
//        var line = conn.Query<FTAppGeoTrackLine>(sql, new { headGuid }).FirstOrDefault();

//        if (line == null) return;

//        // always update the next empty exit date 
//        var sql_update = @$"UPDATE [{webDb}].[dbo].[FTAppGeoTrackLine] 
//                                SET LastExitDt = GETDATE(),
//                                    LastExitDt_DisToStore = @distance
//                                WHERE HeadGuid = @headGuid
//                                       AND Id= @Id";

//        conn.ExecuteScalar<int>(sql_update, new
//        {
//            distance = distance,
//            headGuid = headGuid,
//            Id = line.Id,
//        });
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return;
//    }
//}

// reserved code

//IActionResult SetExit(Geofence_Dto dto)
//{
//    try
//    {
//        // public DateTime UpdateExitDt {get; set;}
//        //public string UpdateStoreCode { get; set; }
//        //public string UpdateUserCode { get; set; }

//        if (string.IsNullOrWhiteSpace(dto.CompanyName))
//        {
//            return BadRequest("Invalid company name");
//        }

//        if (dto.UpdateExitDt == default)
//        {
//            return BadRequest("Invalid date");
//        }

//        if (string.IsNullOrWhiteSpace(dto.UpdateStoreCode))
//        {
//            return BadRequest("Invalid store code");
//        }

//        if (string.IsNullOrWhiteSpace(dto.UpdateUserCode))
//        {
//            return BadRequest("Invalid user code");
//        }

//        var dbInfor = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
//        if (dbInfor == null)
//        {
//            return BadRequest("Invalid Invalid company name");
//        }

//        using var conn = new SqlConnection(_commDbConnStr);
//        int insertResult = InsertSummary(dbInfor, "[FTApp_Geofence_Exited]", dto.Result, conn);

//        return Ok();
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}
