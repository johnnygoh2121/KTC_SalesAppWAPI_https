using Dapper;
using KTC_SalesAppWAPI.DTOs.Dashboard;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Dashboard;
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
    public class AppDashBoardController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<AppDashBoardController> _logger;

        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        public AppDashBoardController(IConfiguration configuration, ILogger<AppDashBoardController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
        }

        [HttpPost]
        public IActionResult Post(AppDashboard_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "DashBoard":
                    {
                        return DashBoard(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult DashBoard(AppDashboard_Dto dto)
        {
            var infoBoards = new List<InfoBoard>();
            var resultlist = GetMTD_Sales(dto);
            if (resultlist != null) infoBoards.AddRange(resultlist);

            var result = GetMTDCollection(dto);
            if (result != null) infoBoards.Add(result);

            result = GetActiveOutlet(dto);
            if (result != null) infoBoards.Add(result);

            result = GetActiveCall(dto);
            if (result != null) infoBoards.Add(result);

            result = GetCallCompliance(dto);
            if (result != null) infoBoards.Add(result);

            return Ok(infoBoards);
        }

        /// <summary>
        /// Call compliance 
        /// Compare the geofence stay on count with plannned schedule call
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        InfoBoard GetCallCompliance(AppDashboard_Dto dto)
        {
            try
            {
                var today = DateTime.Now;

                DateTime startDt = new DateTime(today.Year, today.Month, 1);
                var dayInMth = DateTime.DaysInMonth(today.Year, today.Month);

                DateTime endDt = new DateTime(today.Year, today.Month, dayInMth);

                if (dto.Companies == null) return null;

                var callCounted = 0.00;
                var callCompliance = 0.00;
                using var dbNameHelper = new DbNameHelper();
                using var conn = new SqlConnection(_commDbConnStr);

                // get stay on std minutes from setup 
                var query_stayOnMins = @"SELECT [SetupValue] 
                                        FROM [KTCW_COMMON].[dbo].[FTApp_Config] WITH (NOLOCK)
                                        WHERE [SetupName] = 'Geofence_StayOnMinute' ";

                var stayonDuMins = conn.ExecuteScalar<int>(query_stayOnMins);
                if (stayonDuMins <= 0)
                {
                    stayonDuMins = 5;
                }

                for (int c = 0; c < dto.Companies.Count; c++)
                {
                    var company = dto.Companies[c];
                    var dbInfor = dbNameHelper.GetDbInfo(_commDbConnStr, company);
                    if (dbInfor == null) continue;

                    var sql = @$"SELECT COUNT(*) [CALLED] 
                                FROM [{dbInfor.WEBDB}].[dbo].[FTAppGeoTrack] t0 WITH (NOLOCK) INNER JOIN  
                                     [{dbInfor.WEBDB}].[dbo].[FTAppGeoTrackLine] t1 WITH (NOLOCK) on t0.HeaderGuid = t1.HeadGuid
                                WHERE t1.TripID = 1
                              --  AND LastStayOnDt IS NOT NULL
                                AND CONVERT(date, LastEnterDt) >= @startDt
                                AND CONVERT(date, LastEnterDt) <= @endDt
                                AND DateDiff(mi, LastEnterDt, LastStayOnDt) >= @stayonDuMins
                                and UserCode = @UserIdCode";

                    var called = conn.ExecuteScalar<int>(sql, new { startDt, endDt, stayonDuMins, dto.UserIdCode });

                    if (called > 0)
                    {
                        callCounted += called;
                    }

                    // get the current month planned schedule
                    sql = @$"SELECT Count(1) [plannedCall]
                          FROM [{dbInfor.WEBDB}].[DBO].[FTApp_BaseRouteSchedule] WITH (NOLOCK)
                          WHERE UserCode = @UserIdCode 
                          AND ScheduleDate >= @startDt  
                          AND ScheduleDate <= @endDt";

                    var plannedCall = conn
                        .ExecuteScalar<int>(sql, new { UserIdCode = dto.UserIdCode, startDt, endDt });

                    if (plannedCall > 0)
                    {
                        callCompliance += plannedCall;
                    }
                }

                if (callCompliance == 0) callCompliance = 1;
                if (callCounted == 0) return null;

                return new InfoBoard
                {
                    Title = "Call Compliance (Combined)",
                    MValue = callCounted,
                    MTarget = callCompliance,
                    //Percentage = callCompliance == 1 ? 0 : (callCounted / callCompliance) * 100,
                    Desc = $"{callCounted:N0}  vs Target {callCompliance:N0}"
                };
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        /// <summary>
        /// need to create a table FTAPP_ActiveCall - to kept all the created invoice with link to a call   
        /// based on Sales order date as call date
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        InfoBoard GetActiveCall(AppDashboard_Dto dto)
        {
            try
            {
                return null;

                // sales person 
                // company
                // place order call vs planned call 

                //DateTime startDt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                //DateTime endDt = DateTime.Now;

                //if (dto.Companies == null)
                //{
                //    return null;
                //}

                //using var dbNameHelper = new DbNameHelper();
                //using var conn = new SqlConnection(_commDbConnStr);

                //var activeCall = 0.00;
                //var plannedCall = 0.00;
                //dto.Companies.ForEach(x =>
                //{
                //    var dbInfo = dbNameHelper.GetDbInfo(_commDbConnStr, x);
                //    if (dbInfo == null) return;

                //    // select count the record
                //    // FTApp_ActiveCall = should be update when whs picked process
                //    var sql = @$"SELECT Count(*) 
                //              FROM [{dbInfo.WEBDB}].[DBO].[FTApp_ActiveCall]
                //              WHERE UserCode = @UserIdCode 
                //              AND ScheduleDate >= @startDt 
                //              AND ScheduleDate <= @endDt";

                //    var countResult = conn.ExecuteScalar<int>(
                //        sql,
                //        new { dto.UserIdCode, startDt, endDt });

                //    if (countResult > 0)
                //    {
                //        activeCall += countResult;
                //    }

                //    // get planned routing plan 
                //    // FTApp_BaseRouteSchedule generated based on AI 

                //    sql = @$"SELECT Count(StoreCode) 
                //          FROM [{dbInfo.WEBDB}].[DBO].[FTApp_BaseRouteSchedule]  
                //          WHERE UserCode = @UserIdCode 
                //          AND ScheduleDate >= @startDt  
                //          AND ScheduleDate <= @endDt";

                //    countResult = conn.ExecuteScalar<int>(
                //                                       sql,
                //                                       new { dto.UserIdCode, startDt, endDt });

                //    if (countResult > 0)
                //    {
                //        plannedCall += countResult;
                //    }
                //});

                //if (plannedCall == 0) return null;
                //if (activeCall == 0) return null;

                //return new InfoBoard
                //{
                //    Title = "Active Call (Combined)",
                //    MValue = activeCall,
                //    MTarget = plannedCall,
                //    Percentage = plannedCall == 0 ? 0 : (activeCall / plannedCall) * 100,
                //    Desc = $"{activeCall:N0}  vs Target {plannedCall:N0}"
                //};
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        /// <summary>
        /// Return board info for active outlet
        /// when from month to day, how many outlet had place order 
        /// count 1 outlet 1 active
        /// suppport mutiple company
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        InfoBoard GetActiveOutlet(AppDashboard_Dto dto)
        {
            try
            {
                //return null;
                // sales person 
                // company
                // result - sumary of MTD 
                var today = DateTime.Now;

                DateTime startDt = new DateTime(today.Year, today.Month, 1);
                var dayInMth = DateTime.DaysInMonth(today.Year, today.Month);
                DateTime endDt = new DateTime(today.Year, today.Month, dayInMth);

                if (dto.Companies == null)
                {
                    return null;
                }

                using var dbNameHelper = new DbNameHelper();
                using var conn = new SqlConnection(_commDbConnStr);

                var procedure = "exec sp_QueryActiveOutlet @webDb, @erpDb, @startDate, @endDate, @userCode, @cardType, @docType";
                var activeCount = 0.00;
                var outletCount = 0.00;

                dto.Companies.ForEach(x =>
                {
                    var dbInfo = dbNameHelper.GetDbInfo(_commDbConnStr, x);
                    if (dbInfo == null) return; // continue to next member of the list

                    var activeCardCodesCount = conn.Query<int>(procedure,
                        new
                        {
                            webDb = dbInfo.WEBDB,
                            erpDb = dbInfo.SAPDB,
                            startDate = $"{startDt:yyyyMMdd}",
                            endDate = $"{endDt:yyyyMMdd}",
                            userCode = dto.UserIdCode,
                            cardType = "C",
                            docType = "I"
                        }, commandTimeout:0).ToList();


                    if (activeCardCodesCount != null)
                    {
                        activeCount += activeCardCodesCount.Count;
                    }

                    var sql = $"SELECT Count (*) FROM [{dbInfo.WEBDB}].dbo.USERCARD WITH (NOLOCK) " +
                              $" Where CardType ='C' AND UserCode = @UserIdCode";

                    outletCount += conn.ExecuteScalar<int>(sql, new { dto.UserIdCode });
                });

                return new InfoBoard
                {
                    Title = "Active Outlet (Combined)",
                    MValue = activeCount,
                    MTarget = outletCount,
                    Percentage = outletCount == 0 ? 0 : (activeCount / outletCount) * 100,
                    Desc = $"{activeCount:N0}  vs Target {outletCount:N0}"
                };
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        /// <summary>
        /// Get collection MTD collection and target of collection
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        InfoBoard GetMTDCollection(AppDashboard_Dto dto)
        {
            try
            {
                return null;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }


        /// <summary>
        /// get MTD sales 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        List<InfoBoard> GetMTD_Sales(AppDashboard_Dto dto)
        {
            try
            {
                var returnList = new List<InfoBoard>();
                // sales person 
                // company
                // result - sumary of MTD 

                var today = DateTime.Now;
                DateTime startDt = new DateTime(today.Year, today.Month, 1);
                var dayInMth = DateTime.DaysInMonth(today.Year, today.Month);
                DateTime endDt = new DateTime(today.Year, today.Month, dayInMth);

                if (dto.Companies == null) return null;

                // run sp from kTCW_common 
                // exec sp_QueryMTDSales 'KTC100101_SAP', '100102020', '20210301', '20210330'
                // var procedure = "[Sales by Year]";     
                using var dbNameHelper = new DbNameHelper();
                using var conn = new SqlConnection(_commDbConnStr);

                // get list of currency 
                //var getCurrecySql = "SELECT DISTINCT Currency FROM FTAPP_MSalesTarget WHERE USERCODE = @UserIdCode";
                //var currencies = conn.Query<string>(getCurrecySql, new { dto.UserIdCode }).ToList();
                //if (currencies == null) return null;
                //if (currencies.Count == 0) return null;
                //currencies.ForEach(currency =>
                //{

                var procedure = "exec sp_QueryMTDSales @companyRef_, @salesPerson_, @startDate_, @endDate_";
                var targetSales = 0.00;
                var mtdSales = 0.00;

                // get month date sales, based on different company (sap db)
                // with store procedure sit in db_common

                for (int c =0; c < dto.Companies.Count; c++)
                {
                    var company = dto.Companies[c];
                    var dbInfor = dbNameHelper.GetDbInfo(_commDbConnStr, company);
                    if (dbInfor == null) continue; // continue the next member

                    // get the slp code from the table                    
                    var querySAPSlpCode = @$"SELECT SLPCODE FROM [{dbInfor.WEBDB}].[dbo].[USERS] WITH (NOLOCK) 
                                             WHERE USERCODE = @UserIdCode ";

                    // get sales person 
                    var slpCode = conn.ExecuteScalar(querySAPSlpCode, new { dto.UserIdCode });

                    var sums = conn.ExecuteScalar(procedure, new
                    {
                        companyRef_ = dbInfor.SAPDB,
                        salesPerson_ = slpCode,
                        startDate_ = $"{startDt:yyyyMMdd}",
                        endDate_ = $"{endDt:yyyyMMdd}"
                    });

                    var isNumeric = double.TryParse($"{sums}", out double salesResult);
                    if (isNumeric)
                    {
                        mtdSales += salesResult;
                    }

                    // get the sale month target (webdb)   
                    // table sit in web db
                    var queryGetSalesTarget = $@"SELECT SUM([{DateTime.Now:MMM}]) [Target]
                                            FROM 
                                                [dbo].[FTAPP_MSalesTarget] WITH (NOLOCK)
                                            WHERE UserCode =  @UserIdCode                                                                                                 
                                                AND CompanyName = @COMPANYNAME
                                                AND Currency = @QueryCurrency
                                                AND [Year] = {endDt:yyyy} ";

                    //var slpTarget = conn.Query<SalesTarget>(queryGetSalesTarget, new { dto.UserIdCode, currency }).ToList();
                    var slpTarget = conn.ExecuteScalar(queryGetSalesTarget, new { dto.UserIdCode, dto.QueryCurrency, dbInfor.COMPANYNAME });

                    isNumeric = double.TryParse($"{slpTarget}", out double targetResult);
                    if (isNumeric)
                    {
                        targetSales += targetResult;
                    }
                }

                if (mtdSales >= 0)
                {
                    if (targetSales == 0) targetSales = 1; // avoid division by zero                        
                    var newInfo = new InfoBoard
                    {
                        Title = $"MTD Sales (Combined)",
                        MValue = mtdSales,
                        MTarget = targetSales,
                       // Percentage = targetSales == 0 ? 0 : (mtdSales / targetSales) * 100,
                        //Desc = $"{mtdSales:N}  vs Target {targetSales:N} ({currency.ToUpper()})"
                        Desc = $"{mtdSales:N}  vs Target {targetSales:N}"
                    };
                    returnList.Add(newInfo);
                }

                return returnList;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }
    }
}
