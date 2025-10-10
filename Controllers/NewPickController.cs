using Dapper;
using KTC_SalesAppWAPI.DTOs.NewPick;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.NewPick;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Pick_IBT;
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
    public class NewPickController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<NewPickController> _logger;

        string WebHostAddrEndPoint { get; set; } = string.Empty;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;
        static bool IsBusyGetQueueSO3 { get; set; } = false;

        public NewPickController(IConfiguration configuration, ILogger<NewPickController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }


        [HttpPost]
        public IActionResult PostAsync(Dto_NewPick dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadSalesOrderLines": // single company
                    {
                        return LoadSalesOrderLines(dto);
                    }
                case "LoadSalesOrders":
                    {
                        return LoadSalesOrders(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult LoadSalesOrders(Dto_NewPick dto)
        {
            try
            {
                #region for no allow same user run this code twice 
                // check the user in dlb creation 
                if (string.IsNullOrWhiteSpace(dto.UserToken))
                {
                    goto ByPassTransCheck;
                }

                // check the memory for the key exist
                if (Program.UserTransToken_SelectPick == null)
                {
                    Program.UserTransToken_SelectPick = new Dictionary<string, bool>();
                }
                // check user token in list
                var isListed = Program.UserTransToken_SelectPick.ContainsKey(dto.UserToken);

                if (isListed) // yes in 
                {
                    bool inTran = Program.UserTransToken_SelectPick[dto.UserToken];
                    if (inTran)
                    {
                        return BadRequest("Select picking in process, please try again. Thanks.");
                    }
                    else
                    {
                        Program.UserTransToken_SelectPick[dto.UserToken] = true;
                    }
                }
                else // no then add in and set true 
                {
                    Program.UserTransToken_SelectPick.Add(dto.UserToken, true); // add and set to intrans
                }

            #endregion 

            ByPassTransCheck:

                if (IsBusyGetQueueSO3)
                {
                    return BadRequest("Please try again, server busy at the moment.");
                }

                IsBusyGetQueueSO3 = true;

                if (string.IsNullOrEmpty(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSiID))
                {
                    return BadRequest("Invalid Subsi id");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                // loop thru all user companies 
                // get all user company
                var sql_userlogons = "";
                if (dto.IsDevelopment) // for test ktc database 
                {
                    sql_userlogons = @"select * from FTAPP_SSO with (nolock)
                                        where UserCode = @UserCode
                                        and left(UserCompanyID, 1) = 'Z'";
                }
                else
                {
                    // for live ktc database
                    sql_userlogons = @"select * from FTAPP_SSO with (nolock)
                                        where UserCode = @UserCode
                                        and left(UserCompanyID, 1) <> 'Z'";
                }
                
                var companies = conn.Query<FTAPP_SSO>(sql_userlogons, new { UserCode = dto.UserCode }).ToList();
                if (companies == null) return BadRequest($"{dto.UserCode} no setup / config properly," +
                                                         $" No new available pick order");

                // loop thru all company to get the top 1 order / ibt 
                var portal_So_Ibts = new List<PORTAL_SO>();
                var dbNameHelper = new DbNameHelper();

                for (int c = 0; c < companies.Count; c++)
                {
                    var db = dbNameHelper.GetDbInfo(_commDbConnStr, companies[c].UserCompanyID);
                    if (db == null) continue;

                    // check each company detect any hold in the company 
                    #region check any on hold sales order
                    var sp_checkHold_SalesOrder = @$"Select * 
                                                     from {db.WEBDB}..FTAPP_OnHoldSoInPicking 
                                                     where HoldByUserCode = @UserCode ";

                    var foundSO_Hold = conn
                        .Query<FTAPP_OnHoldSoInPicking>(sp_checkHold_SalesOrder, new { UserCode = dto.UserCode })
                        .FirstOrDefault();

                    if (foundSO_Hold != null)
                    {
                        var sp_QueryGetHoldSO = @$"exec sp_SelectQueueSO_Version_NewPick_SingleSO @webDb, @userCode,  @SODoEntry";
                        var holdSO = conn.Query<PORTAL_SO>(sp_QueryGetHoldSO, new
                        {
                            webDb = db.WEBDB,
                            userCode = dto.UserCode,
                            SODoEntry = foundSO_Hold.HoldDocEntry
                        }).FirstOrDefault();

                        return Ok(holdSO);
                    }
                    #endregion

                    // check each company detect any hold in the company - IBT
                    #region check any on hold sales order
                    var sp_checkHold_IBT = @$"Select * 
                                        from {db.WEBDB}..FTAPP_OnHold_IBT_InPicking 
                                        Where HoldByUserCode = @UserCode ";

                    var foundIBT_Hold = conn
                        .Query<FTAPP_OnHold_IBT_InPicking>(sp_checkHold_IBT, new { UserCode = dto.UserCode })
                        .FirstOrDefault();

                    if (foundIBT_Hold != null)
                    {
                        var sp_QueryGetHoldSO = @$"exec sp_SelectQueueIBT_NewPick_SingleIBT @webDb, @userCode,  @IbtDocEntry ";
                        var holdIBT = conn.Query<PORTAL_SO>(sp_QueryGetHoldSO, new
                        {
                            webDb = db.WEBDB,
                            userCode = dto.UserCode,
                            IbtDocEntry = foundIBT_Hold.HoldDocEntry
                        }).FirstOrDefault();

                        return Ok(holdIBT); //
                    }
                    #endregion

                    var sp_query = @"exec sp_SelectQueueSO_Version_NewPick @webDb, userCode ";
                    var portal_so = conn.Query<PORTAL_SO>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        userCode = dto.UserCode
                    }).ToList();

                    if (portal_so.Count > 0)
                    {
                        portal_So_Ibts.AddRange(portal_so);
                    }

                    var sp_query_ibts = @"exec sp_SelectQueueIBT_NewPick @webDb, userCode ";
                    var ibts = conn.Query<PORTAL_SO>(sp_query_ibts, new
                    {
                        webDb = db.WEBDB,
                        userCode = dto.UserCode
                    }).ToList();

                    if (ibts.Count > 0)
                    {
                        portal_So_Ibts.AddRange(portal_so);
                    }
                }

                // sorting the order in list
                // for picking the first order (SO or IBT)
                if (portal_So_Ibts.Count > 0)
                {
                    portal_So_Ibts = portal_So_Ibts.OrderBy(x => x.SortedDate).ToList();
                }

                var firstSoOrIbt = portal_So_Ibts.First(); // select the first one 
                var selectedDb = dbNameHelper.GetDbInfoById(_commDbConnStr, firstSoOrIbt.SubSiID);

                // check on hold table for this order
                var sp_SelectOnhOld = "";
                if (firstSoOrIbt.IsIBT) // ibt n hold
                {
                    sp_SelectOnhOld = @$"Select * 
                                         from {selectedDb.WEBDB}..FTAPP_OnHoldSoInPicking 
                                         Where HoldEntry = @DocEntry";
                    var isHold = conn
                                  .Query<FTAPP_OnHoldSoInPicking>(sp_SelectOnhOld, new { DocEntry = firstSoOrIbt.DOCENTRY })
                                  .FirstOrDefault();

                    if (isHold == null) // hold it 
                    {
                        var holdResult = OnHoldSalesOrderForPick(firstSoOrIbt, dto, selectedDb);
                        if (holdResult == false)
                        {
                            return BadRequest(LastError);
                        }
                    }

                    return Ok(firstSoOrIbt);
                }

                // handler ibt on hold 
                sp_SelectOnhOld = @$"Select * 
                                from {selectedDb.WEBDB}..FTAPP_OnHold_IBT_InPicking 
                                Where HoldEntry = @DocEntry";

                var isHoldIbt = conn
                              .Query<FTAPP_OnHold_IBT_InPicking>(sp_SelectOnhOld, new { DocEntry = firstSoOrIbt.DOCENTRY })
                              .FirstOrDefault();

                if (isHoldIbt == null)
                {
                    var putIBTOnHold = OnHoldIBTForPick(firstSoOrIbt, dto, selectedDb);
                    if (putIBTOnHold == false)
                    {
                        return BadRequest(LastError);
                    }
                }

                return Ok(firstSoOrIbt);
            }
            finally
            {
                IsBusyGetQueueSO3 = false;

                // for ensure same user no run this code twice 
                if (!string.IsNullOrWhiteSpace(dto.UserToken) && Program.UserTransToken_SelectPick.Count > 0)
                {
                    Program.UserTransToken_SelectPick.Remove(dto.UserToken);
                }
            }
        }

        bool OnHoldSalesOrderForPick(PORTAL_SO pSo, Dto_NewPick dto, DbInfo db)
        {
            var newHold = new FTAPP_OnHoldSoInPicking
            {
                HoldDocEntry = (int)pSo.DOCENTRY,
                HoldByUserCode = dto.UserCode,
                HoldByUserName = dto.UserName,
                HoldStartDt = DateTime.Now,
                HoldReason = "Picking"
            };

            // insert the recrod
            var insertSql = $@"Insert into {db.WEBDB}..FTAPP_OnHoldSoInPicking (
                                     HoldDocEntry 
                                   , HoldByUserCode
                                   , HoldByUserName
                                   , HoldStartDt
                                   , HoldReason 
                                ) values (
                                      @HoldDocEntry 
                                    , @HoldByUserCode
                                    , @HoldByUserName
                                    , GETDATE()
                                    , @HoldReason 
                                )";

            using var connInsert = new SqlConnection(_commDbConnStr);
            connInsert.Open();
            using var transInsert = connInsert.BeginTransaction();

            try
            {
                var insertResult = connInsert.Execute(insertSql, newHold, transInsert);
                if (insertResult > 0)
                {
                    transInsert.Commit();
                    return true;
                }

                transInsert.Rollback();
                LastError = $"Put HOLD for SO# {pSo.DOCENTRY} fali, please try again.";
                _logger.LogError(LastError);
                return false;
            }
            catch (Exception e)
            {
                transInsert.Rollback();
                LastError = $"{ e.Message}, trace: {e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        bool OnHoldIBTForPick(PORTAL_SO pSo, Dto_NewPick dto, DbInfo db)
        {
            var newHold = new FTAPP_OnHold_IBT_InPicking
            {
                HoldDocEntry = (int)pSo.DOCENTRY,
                HoldByUserCode = dto.UserCode,
                HoldByUserName = dto.UserName,
                HoldStartDt = DateTime.Now,
                HoldReason = "Picking"
            };

            // insert the recrod
            var insertSql = $@"Insert into {db.WEBDB}..FTAPP_OnHold_IBT_InPicking (
                                     HoldDocEntry 
                                   , HoldByUserCode
                                   , HoldByUserName
                                   , HoldStartDt
                                   , HoldReason 
                                ) values (
                                      @HoldDocEntry 
                                    , @HoldByUserCode
                                    , @HoldByUserName
                                    , GETDATE()
                                    , @HoldReason 
                                )";

            using var connInsert = new SqlConnection(_commDbConnStr);
            connInsert.Open();
            using var transInsert = connInsert.BeginTransaction();

            try
            {
                var insertResult = connInsert.Execute(insertSql, newHold, transInsert);
                if (insertResult > 0)
                {
                    transInsert.Commit();
                    return true;
                }

                transInsert.Rollback();
                LastError = $"Put HOLD for IBT# {pSo.DOCENTRY} fali, please try again.";
                _logger.LogError(LastError);
                return false;
            }
            catch (Exception e)
            {
                transInsert.Rollback();
                LastError = $"{ e.Message}, trace: {e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }


        IActionResult LoadSalesOrderLines(Dto_NewPick dto)
        {
            try
            {
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSiID))
                {
                    return BadRequest("Invalid subsi ID");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("Invalid warehouse code");
                }
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubSiID);
                if (db == null)
                {
                    return BadRequest("Invalid subsi id ");
                }

                var sp_queryLines = @"exec sp_SelectQueueSOLine_NewPick @webDB,  @docEntry, @whsCode ";
                using var conn = new SqlConnection(_commDbConnStr);

                var lines = conn.Query<PORTAL_SO1>(sp_queryLines, new
                {
                    docentry = dto.DocEntry,
                    whsCode = dto.WhsCode
                }).ToList();

                if (lines.Count == 0) return NotFound();
                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}, trace: {e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }



        //[HttpPost]
        //public IActionResult PostAsync(Dto_Pick dto)
        //{
        //    var request = $"{dto.Request}";
        //    switch (request)
        //    {
        //        case "GetQueueSO_NewPick": // single company
        //            {
        //                return GetQueueSO_NewPick(dto);
        //            }
        //        case "GetPickOrder_NewPick": // same user in all company order 
        //            {
        //                return GetPickOrder_NewPick(dto);
        //            }
        //        case "NewPick_FinalCheckOnHold":
        //            {
        //                return NewPick_FinalCheckOnHold(dto);
        //            }
        //        case "NewPick_SetSOOutStock": // app decteced SO line have all zero qrt
        //            {
        //                return NewPick_SetSOOutStock(dto);
        //            }
        //        case "SP_SelectQueueSOLine_NewPick":
        //            {
        //                return SP_SelectQueueSOLine_NewPick(dto);
        //            }
        //        default:
        //            {
        //                return BadRequest("Request no found");
        //            }
        //    }
        //}

        //IActionResult SP_SelectQueueSOLine_NewPick(Dto_Pick dto)
        //{
        //    try
        //    {
        //        // query all SO lines 
        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("Invalid subsi / company name");
        //        }
        //        if (dto.DocEntry <= 0)
        //        {
        //            return BadRequest("Invalid Sales order entry no.");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.WhsCode))
        //        {
        //            return BadRequest("Invalid whs code.");
        //        }

        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("Invalid subsi db");
        //        }

        //        var sp_QuerySoLines = "exec sp_SelectQueueSOLine_NewPick @webDB, @docEntry, @whsCode ";
        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var lines = conn.Query<WSO1>(sp_QuerySoLines, new
        //        {
        //            webDB = db.WEBDB,
        //            docEntry = dto.DocEntry,
        //            whsCode = dto.WhsCode
        //        }).ToList();

        //        if (lines.Count == 0) return NotFound();
        //        return Ok(lines);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}, trace: {e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest(LastError);
        //    }
        //}

        //IActionResult NewPick_FinalCheckOnHold(Dto_Pick dto)
        //{
        //    if (string.IsNullOrWhiteSpace(dto.Subsi))
        //    {
        //        return BadRequest("Invalid subsi");
        //    }
        //    if (dto.DocEntry <= 0)
        //    {
        //        return BadRequest("Invalid doc entry");
        //    }
        //    if (string.IsNullOrWhiteSpace(dto.UserCode))
        //    {
        //        return BadRequest("Invalid user code");
        //    }

        //    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //    if (db == null)
        //    {
        //        return BadRequest("Invalid subsi db");
        //    }

        //    using var conn = new SqlConnection();
        //    // 20240428
        //    // check does it created invoice 
        //    var sp_checkSoConvertInv = @$"Select docentry 
        //                                from {db.WEBDB}..SO 
        //                                Where docentry = @docEntry 
        //                                and DocStatus = 'I' ";

        //    var foundSo = conn.Query<WSO>(sp_checkSoConvertInv,
        //        new
        //        {
        //            docEntry = dto.DocEntry
        //        }).FirstOrDefault();

        //    if (foundSo != null) // found SO invoiced
        //    {
        //        LastError = $"{db.COMPANYNAME}, Sales Order# {dto.DocEntry} already invoiced {foundSo.INVNO}";
        //        _logger.LogError(LastError);
        //        return BadRequest(LastError);
        //    }

        //    // check does it onhold 

        //    // 20240428
        //    // suppose is hold when assign at server

        //    var sp_checkSoOnhold = @$"Select * from {db.WEBDB}..FTAPP_OnHoldSoInPicking 
        //                                Where  HoldDocEntry = @docEntry ";

        //    var foundOnHoldDoc = conn.Query<FTAPP_OnHoldSoInPicking>(sp_checkSoOnhold,
        //        new
        //        {
        //            docEntry = dto.DocEntry
        //        }).FirstOrDefault();

        //    if (foundOnHoldDoc != null)
        //    {
        //        return Ok();
        //    }

        //    // else auto insert onhold again
        //    // CreateHoldDoc:
        //    // put the doc onhold

        //    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
        //    using var trans = conn.BeginTransaction();
        //    try
        //    {
        //        var insert_sql = @$"INSERT INTO {db.WEBDB}..FTAPP_OnHoldSoInPicking  ( 
        //                                 HoldDocEntry
        //                               , HoldByUserCode
        //                               , HoldByUserName
        //                               , HoldStartDt
        //                               , HoldReason
        //                              ) VALUES (
        //                                 @HoldDocEntry
        //                               , @HoldByUserCode
        //                               , @HoldByUserName
        //                               , GETDATE()
        //                               , @HoldReason
        //                              )";

        //        var insert_result = conn.Execute(insert_sql,
        //            new
        //            {
        //                HoldDocEntry = dto.DocEntry,
        //                HoldByUserCode = dto.UserCode,
        //                HoldByUserName = dto.UserName,
        //                HoldReason = "Picking"
        //            }, trans);


        //        if (insert_result == 0)
        //        {
        //            trans.Rollback();
        //            LastError = $"Try on hold SO# {dto.DocEntry} fail, with no row insert, please contact support for help. Thanks.";
        //            _logger.LogError(LastError);
        //            return BadRequest(LastError);
        //        }

        //        trans.Commit(); // auto insert by code done success
        //        return Ok();
        //    }
        //    catch (Exception eInsertOnholdExcep)
        //    {
        //        trans.Rollback();
        //        LastError = $"Try insert onhold SO# {dto.DocEntry} to db fail with error: " +
        //                    $"{eInsertOnholdExcep.Message}, trace: {eInsertOnholdExcep.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest(LastError);
        //    }
        //}

        //// get SO by earlier date time 
        //// return single avail order
        //// auto start no support of testing database auto order
        //// 20210922
        //IActionResult GetPickOrder_NewPick(Dto_Pick dto)
        //{
        //    try
        //    {
        //        if (IsBusyGetQueueSO3)
        //        {
        //            return BadRequest("Please try again, server busy at the moment.");
        //        }

        //        IsBusyGetQueueSO3 = true;
        //        if (string.IsNullOrWhiteSpace(dto.UserCode))
        //        {
        //            return BadRequest("Invalid user code");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.UserName))
        //        {
        //            return BadRequest("Invalid user name");
        //        }

        //        // get all user company
        //        var sql_userlogons = @"select * from FTAPP_SSO with (nolock)
        //                                where UserCode = @UserCode
        //                                and left(UserCompanyID, 1) <> 'Z'";

        //        if (!string.IsNullOrWhiteSpace(dto.IsAutoStartSOTestDb))
        //        {
        //            // get only the test database
        //            sql_userlogons = @"select * from FTAPP_SSO with (nolock)
        //                                where UserCode = @UserCode
        //                                and left(UserCompanyID, 1) = 'Z'";
        //        }


        //        using (var conn = new SqlConnection(_commDbConnStr))
        //        {
        //            var companies = conn.Query<FTAPP_SSO>(sql_userlogons, new { UserCode = dto.UserCode }).ToList();
        //            if (companies == null)
        //            {
        //                LastError = $"{dto.UserCode} no setup / config properly, No new available pick order";
        //                _logger.LogError(LastError);
        //                return BadRequest(LastError);
        //            }

        //            // for listing compare in next 
        //            var AllCompSOs_Q = new List<WSO>();
        //            var AllCompSOs_H = new List<WSO>();

        //            // geth all SO from the company
        //            var dbhelper = new DbNameHelper();

        //            for (int c = 0; c < companies.Count; c++)
        //            {
        //                var company = companies[c];
        //                var db = dbhelper.GetDbInfo(_commDbConnStr, company.UserCompany);
        //                var results = new List<WSO>();

        //                // query all the Q / unholded SO
        //                if (!IsByPassSO)
        //                {
        //                    // read all SO line 
        //                    // no checking for the line qty and avail qty
        //                    // include FOC lines
        //                    // 20240428

        //                    var sql = @"exec sp_SelectQueueSO_Version3 @webDB, @erpDb, @userCode, @subSi, @subSiId";
        //                    results = conn.Query<WSO>(sql, new
        //                    {
        //                        webDb = company.UserCompanyRef,
        //                        erpDb = company.UserCompanyErpRef,
        //                        userCode = company.UserCode,
        //                        subsi = company.UserCompany,
        //                        SubSiID = company.UserCompanyID
        //                    }).ToList();

        //                    // show the line with zero to picker 
        //                    // remove the zero line 
        //                    // 2021 0806
        //                    var sapNoOOSs = results.Where(x => x.LinesCount == 0).ToList(); // set OOS from web api
        //                    if (sapNoOOSs.Count > 0)
        //                    {
        //                        BatchUpdateForOOS_NewPick(sapNoOOSs, $"{dto.AppVersion}");
        //                    }
        //                }

        //                // 20230512
        //                if (IsSupportIBT)
        //                {
        //                    var sqlIbt = @"exec sp_SelectQueueIBT @webDB, @userCode";
        //                    var ibtRes = conn.Query<WSO>(sqlIbt, new
        //                    {
        //                        webDb = db.WEBDB,
        //                        userCode = dto.UserCode
        //                    }).ToList();

        //                    if (ibtRes.Count > 0)
        //                    {
        //                        results.AddRange(ibtRes);
        //                    }
        //                }

        //                var returnList = results.Where(x => x.LinesCount > 0).ToList();
        //                if (returnList.Count == 0)
        //                {
        //                    goto CheckOnHoldList;
        //                }

        //                // else
        //                AllCompSOs_Q.AddRange(returnList); // combine all sales order into a list

        //            CheckOnHoldList:
        //                // query the onhold doc and add into the list 
        //                // based on user code query 
        //                var sql_queryOnhold = "sp_SelectHoldSO_ByUserCode  @webDB, @erpDb , @subsi, @userCode , @docStatus, @SubSiID";
        //                var onholdDocs = conn.Query<WSO>(sql_queryOnhold, new
        //                {
        //                    webDB = company.UserCompanyRef,
        //                    erpDb = company.UserCompanyErpRef,
        //                    subsi = company.UserCompany,
        //                    userCode = company.UserCode,
        //                    docStatus = "Q",
        //                    SubSiID = company.UserCompanyID
        //                }).ToList();

        //                if (onholdDocs.Count > 0)
        //                {
        //                    AllCompSOs_H.AddRange(onholdDocs);
        //                }

        //                // 20230617
        //                // add in the ibt query on hold 
        //                var sql_IBT_queryOnhold = "exec sp_SelectHoldIBT_ByUserCode @webDB, @userCode";
        //                var onholdDocs_IBT = conn.Query<WSO>(sql_IBT_queryOnhold, new
        //                {
        //                    webDB = db.WEBDB,
        //                    userCode = dto.UserCode,
        //                }).ToList();

        //                if (onholdDocs_IBT.Count > 0)
        //                {
        //                    AllCompSOs_H.AddRange(onholdDocs_IBT); // add in the ibt together
        //                }
        //            } // all companies loop

        //            // prepare check each onhold order
        //            if (AllCompSOs_H.Count > 0)
        //            {
        //                var sortedEalierDtSo = AllCompSOs_H.OrderBy(x => x.SortedDate).ToList();
        //                for (int holdSOIndex = 0; holdSOIndex < sortedEalierDtSo.Count; holdSOIndex++)
        //                {
        //                    var order = sortedEalierDtSo[holdSOIndex];
        //                    if (order == null) continue;

        //                    var db = dbhelper.GetDbInfo(_commDbConnStr, order.SubSi);
        //                    if (db == null) continue;

        //                    // ensure the onhold still there 
        //                    // check does this doc hold by some one not me
        //                    // remove the select no lock
        //                    // let db manage the table when some one may insert data
        //                    // select statement can wait and the insert process

        //                    string checkDocHoldBySomeOne = "";
        //                    if (order.IsIBT)
        //                    {
        //                        checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //                    }
        //                    else // for ibt onhold checking 
        //                    {
        //                        checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  [{db.WEBDB}]..[FTAPP_OnHoldSoInPicking]
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //                    }

        //                    var holdeds = conn.Query<FTAPP_OnHoldSoInPicking>(checkDocHoldBySomeOne,
        //                                    new
        //                                    {
        //                                        HoldDocEntry = order.DOCENTRY
        //                                    }).ToList();

        //                    if (holdeds.Count == 0) // no hold by some one 
        //                    {
        //                        // handler is IBT order
        //                        if (order.IsIBT)
        //                        {
        //                            var newHold = new FTAPP_OnHold_IBT_InPicking
        //                            {
        //                                HoldDocEntry = (int)order.DOCENTRY,
        //                                HoldByUserCode = dto.UserCode,
        //                                HoldByUserName = dto.UserName,
        //                                HoldStartDt = DateTime.Now,
        //                                HoldReason = "Picking"
        //                            };

        //                            var isCreated = PutIbtOrderOnHold(newHold, db);
        //                            if (!isCreated)
        //                            {
        //                                return BadRequest($"{db.COMPANYID} IBT#{order.DOCENTRY} " +
        //                                    $"can not ONHOLD, please contact support for help");
        //                            }

        //                            return Ok(order); // IBT
        //                        }

        //                        // handler invoice 
        //                        var newHold_so = new Dto_Pick // put the doc hold at server
        //                        {
        //                            Subsi = order.SubSi,
        //                            UserCode = dto.UserCode,
        //                            UserName = dto.UserName,
        //                            HoldReason = "Picking",
        //                            DocEntry = (int)order.DOCENTRY
        //                        };

        //                        var messge = PutOnHoldSo_InPicking(newHold_so); // by server when user press start
        //                        if (!string.IsNullOrWhiteSpace(messge))
        //                        {
        //                            return BadRequest(messge);
        //                        }

        //                        return Ok(order); // jump out all queue
        //                    }

        //                    // this doc hold and show hode information
        //                    if (holdeds.Count == 1 && $"{holdeds[0].HoldByUserCode}"
        //                                            .ToLower()
        //                                            .Equals(dto.UserCode.ToLower()))
        //                    {
        //                        return Ok(order); // jump out all queue
        //                    }

        //                    // replied order hold by any one 
        //                    var message = "";
        //                    holdeds.ForEach(u =>
        //                    {
        //                        message += $"{db.COMPANYNAME}\n#{u.HoldDocEntry} " +
        //                        $"currently picking by {u.HoldByUserCode}, {u.HoldByUserName}\n [PE2]";
        //                    });

        //                    return BadRequest(message);
        //                }
        //            }

        //            // after check no onhold order .. check new Q order
        //            if (AllCompSOs_Q.Count > 0)
        //            {
        //                var sortedEarlierQSos = AllCompSOs_Q.Where(x => x.LinesCount > 0).OrderBy(x => x.SortedDate).ToList();
        //                for (int q = 0; q < sortedEarlierQSos.Count; q++)
        //                {
        //                    var order = sortedEarlierQSos[q];
        //                    if (order == null) continue;

        //                    var db = dbhelper.GetDbInfo(_commDbConnStr, order.SubSi);
        //                    if (db == null) continue;

        //                    string checkDocHoldBySomeOne = "";
        //                    if (order.IsIBT)
        //                    {
        //                        checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //                    }
        //                    else // for ibt onhold checking 
        //                    {
        //                        checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  [{db.WEBDB}]..[FTAPP_OnHoldSoInPicking]
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //                    }

        //                    var holdeds = conn.Query<FTAPP_OnHoldSoInPicking>(checkDocHoldBySomeOne,
        //                                    new
        //                                    {
        //                                        HoldDocEntry = order.DOCENTRY
        //                                    }).ToList();

        //                    if (holdeds.Count == 0)
        //                    {
        //                        if (order.IsIBT)
        //                        {
        //                            var newHoldIbt = new FTAPP_OnHold_IBT_InPicking // put the doc hold at server
        //                            {
        //                                HoldDocEntry = (int)order.DOCENTRY,
        //                                HoldByUserCode = dto.UserCode,
        //                                HoldByUserName = dto.UserName,
        //                                HoldStartDt = DateTime.Now,
        //                                HoldReason = "Picking"
        //                            };

        //                            var isCreated = PutIbtOrderOnHold(newHoldIbt, db);
        //                            if (!isCreated)
        //                            {
        //                                return BadRequest($"{db.COMPANYID} IBT#{order.DOCENTRY} " +
        //                                      $"can not ONHOLD, please contact support for help");
        //                            }
        //                            return Ok(order);
        //                        }

        //                        // handler the sales order
        //                        var newHoldInv = new Dto_Pick // put the doc hold at server
        //                        {
        //                            Subsi = order.SubSi,
        //                            UserCode = dto.UserCode,
        //                            UserName = dto.UserName,
        //                            HoldReason = "Picking",
        //                            DocEntry = (int)order.DOCENTRY
        //                        };

        //                        var message1 = PutOnHoldSo_InPicking(newHoldInv); // by server when user press start
        //                        if (!string.IsNullOrWhiteSpace(message1))
        //                        {
        //                            return BadRequest(message1);
        //                        }
        //                        return Ok(order); // jump out all queue
        //                    }

        //                    // this doc hold and show hode information
        //                    if (holdeds.Count == 1 && $"{holdeds[0].HoldByUserCode}"
        //                                            .ToLower()
        //                                            .Equals(dto.UserCode.ToLower()))
        //                    {
        //                        return Ok(order); // jump out all queue
        //                    }

        //                    // replied order hold by any one 
        //                    var message = "";
        //                    holdeds.ForEach(u =>
        //                    {
        //                        message += $"{db.COMPANYNAME}\n#{u.HoldDocEntry} " +
        //                        $"currently picking by {u.HoldByUserCode}, {u.HoldByUserName}\n [PE2]";
        //                    });

        //                    return BadRequest(message);

        //                } // end of the Q order loop
        //            }
        //        }

        //        return BadRequest("No new available pick order");
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //    finally
        //    {
        //        IsBusyGetQueueSO3 = false;
        //    }
        //}

        //IActionResult GetQueueSO_NewPick(Dto_Pick dto, bool selectSingleSo = false)
        //{
        //    try
        //    {
        //        if (IsBusyNextOrder)
        //        {
        //            return BadRequest("Please try again, another picker in auto starting");
        //        }

        //        IsBusyNextOrder = true;

        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("The company name is empty");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.DocStatus))
        //        {
        //            return BadRequest("The doc status is empty");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.UserCode))
        //        {
        //            return BadRequest("User code can not empty");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.UserName))
        //        {
        //            return BadRequest("User name can not empty");
        //        }
        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("The compay info is empty");
        //        }

        //        if (dto.DocStatus == "H")
        //        {
        //            // query the SO doc in FTAPP_OnHOLD
        //            return GetQueueSO_Onhold_NewPick(dto, db);
        //        }

        //        // remove the onhold doc where is already invoice 
        //        //using var conn = new SqlConnection(_commDbConnStr);
        //        // conn.Open();

        //        //using (var trans0 = conn.BeginTransaction())
        //        //{
        //        //    try
        //        //    {
        //        //        var sql_deleteInvoicedONhold = @$"DELETE t0 
        //        //                        FROM  [{db.WEBDB}].[dbo].[FTAPP_OnHoldSoInPicking] t0 
        //        //                                INNER JOIN
        //        //                             [{db.WEBDB}].[dbo].[SO] t1 ON t1.DOCENTRY = t0.HoldDocEntry 
        //        //                     WHERE (t1.DOCSTATUS = 'I' or 
        //        //                               t1.DOCSTATUS = 'L' or 
        //        //                               t1.DOCSTATUS = 'R' or 
        //        //                               t1.DOCSTATUS = 'C' )";

        //        //        var cmd = conn.CreateCommand();
        //        //        cmd.Transaction = trans0;
        //        //        cmd.Connection = conn;
        //        //        cmd.CommandText = sql_deleteInvoicedONhold;
        //        //        cmd.ExecuteNonQuery();

        //        //        // 20230617
        //        //        // delete the ibt on hold 
        //        //        var sql_deleteIBTONhold = @$"DELETE t0 
        //        //                        FROM    [{db.WEBDB}]..[FTAPP_OnHold_IBT_InPicking] t0 INNER JOIN
        //        //                             [{db.WEBDB}]..[IBT] t1 ON t1.DOCENTRY = t0.HoldDocEntry 
        //        //                      WHERE (t1.DOCSTATUS = 'T' )";

        //        //        cmd.CommandText = sql_deleteIBTONhold;
        //        //        cmd.ExecuteNonQuery();
        //        //        trans0.Commit();
        //        //    }
        //        //    catch (Exception sqlDel)
        //        //    {
        //        //        trans0.Rollback();
        //        //        return BadRequest($"{sqlDel.Message}, {sqlDel.StackTrace}");
        //        //    }
        //        //}

        //        // doc statuc with Q and I go flow below
        //        //var sql = @"exec sp_SelectQueueSO @webDB, @erpDb, @subsi, @docStatus, @SubSiID";

        //        // 2021 07 15 new query script 

        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var sql = @"exec sp_SelectQueueSO_Version2 @webDB, @erpDb, @userCode, @subSi, @subSiId";
        //        var results = conn.Query<WSO>(sql, new
        //        {
        //            webDb = db.WEBDB,
        //            erpDb = db.SAPDB,
        //            userCode = dto.UserCode,
        //            subsi = db.COMPANYNAME,
        //            SubSiID = db.COMPANYID
        //        }).ToList();

        //        // 20230509
        //        // load in the ibt and match in
        //        if (IsSupportIBT)
        //        {
        //            var sqlIbt = @"exec sp_SelectQueueIBT @webDB, @userCode";
        //            var ibtRes = conn.Query<WSO>(sqlIbt, new
        //            {
        //                webDb = db.WEBDB,
        //                userCode = dto.UserCode
        //            }).ToList();

        //            if (ibtRes.Count > 0)
        //            {
        //                results.AddRange(ibtRes);
        //            }
        //        }

        //        // remove the checking of zero line 
        //        // show the line with zero to picker 
        //        // 2021 0806
        //        var sapNoOOSs = results.Where(x => x.LinesCount == 0 && x.IsIBT == false).ToList(); // set OOS from web api
        //        if (sapNoOOSs != null && sapNoOOSs.Count > 0) // intial own transaction
        //        {
        //            BatchUpdateForOOS_NewPick(sapNoOOSs, $"{dto.AppVersion}");
        //        }

        //        var returnList = results.Where(x => x.LinesCount > 0).ToList();

        //        // query the onhold doc and add into the list 
        //        // based on user code query 
        //        var sql_queryOnhold =
        //                    "exec sp_SelectHoldSO_ByUserCode @webDB, @erpDb, @subsi, @userCode, @docStatus, @SubSiID";

        //        var onholdDocs = conn.Query<WSO>(sql_queryOnhold, new
        //        {
        //            webDB = db.WEBDB,
        //            erpDb = db.SAPDB,
        //            subsi = db.COMPANYNAME,
        //            userCode = dto.UserCode,
        //            docStatus = "Q",
        //            SubSiID = db.COMPANYID
        //        }).ToList();

        //        // 20230617
        //        // add in the ibt query on hold 
        //        var sql_IBT_queryOnhold = "exec sp_SelectHoldIBT_ByUserCode @webDB, @userCode";
        //        var onholdDocs_IBT = conn.Query<WSO>(sql_IBT_queryOnhold, new
        //        {
        //            webDB = db.WEBDB,
        //            userCode = dto.UserCode,
        //        }).ToList();

        //        if (onholdDocs_IBT.Count > 0)
        //        {
        //            onholdDocs.AddRange(onholdDocs_IBT); // add in the ibt together
        //        }

        //        // add in the onhold doc based on the user code
        //        if (onholdDocs.Count > 0)
        //        {
        //            if (returnList == null) returnList = new List<WSO>();
        //            // reverse insert the doc
        //            for (int h = onholdDocs.Count - 1; h >= 0; h--)
        //            {
        //                if (onholdDocs[h].LinesCount == 0) continue; // remove all line is zero
        //                returnList.Insert(0, onholdDocs[h]);
        //            }
        //        }

        //        if (returnList == null) return NotFound();
        //        if (returnList.Count == 0) return NotFound();

        //        if (!selectSingleSo)
        //        {
        //            return Ok(returnList);
        //        }

        //        if (returnList.Count == 0)
        //        {
        //            return BadRequest("No new available pick order");
        //        }

        //        // loop each doc to see any hold, by other person
        //        // get the next new doc
        //        for (int d = 0; d < returnList.Count; d++)
        //        {
        //            var doc = returnList[d];
        //            if (doc == null) continue;
        //            if (doc.LinesCount == 0) continue;

        //            // check does this doc hold by some one not me
        //            // remove the select no lock
        //            // let db manage the table when some one may insert data
        //            // select statement can wait and the insert process
        //            string sp_checkDocHoldBySomeOne = "";
        //            if (!doc.IsIBT)
        //            {
        //                sp_checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  [{db.WEBDB}]..[FTAPP_OnHoldSoInPicking]
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //            }
        //            else // for ibt onhold checking 
        //            {
        //                sp_checkDocHoldBySomeOne = @$"SELECT * 
        //                                           FROM  {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
        //                                           WHERE HoldDocEntry = @HoldDocEntry ";
        //            }

        //            var holdeds = conn.Query<FTAPP_OnHoldSoInPicking>(sp_checkDocHoldBySomeOne,
        //                                        new
        //                                        {
        //                                            HoldDocEntry = doc.DOCENTRY
        //                                        }).ToList();

        //            if (holdeds.Count == 0) // no hold by some one 
        //            {
        //                if (doc.IsIBT)
        //                {
        //                    var newHold_Ibt = new FTAPP_OnHold_IBT_InPicking
        //                    {
        //                        HoldDocEntry = (int)doc.DOCENTRY,
        //                        HoldByUserCode = dto.UserCode,
        //                        HoldByUserName = dto.UserName,
        //                        HoldStartDt = DateTime.Now,
        //                        HoldReason = "Picking"
        //                    };
        //                    var isCreate = PutIbtOrderOnHold(newHold_Ibt, db);
        //                    if (isCreate)
        //                    {
        //                        LastError = $"Error puting IBT on hold, {db.COMPANYNAME}, IBT# {doc.DOCENTRY}";
        //                        _logger.LogError(LastError);
        //                        return BadRequest($"request not handler.\n{LastError}");
        //                    }

        //                    return Ok(doc);
        //                }

        //                // for invoice 
        //                var newHold_Inv = new Dto_Pick // put the doc hold at server
        //                {
        //                    Subsi = dto.Subsi,
        //                    UserCode = dto.UserCode,
        //                    UserName = dto.UserName,
        //                    HoldReason = "Picking",
        //                    DocEntry = (int)doc.DOCENTRY
        //                };

        //                var message1 = PutOnHoldSo_InPicking(newHold_Inv); // by server when user press start
        //                if (!string.IsNullOrWhiteSpace(message1))
        //                {
        //                    LastError = $"Error putting invoice on hold, {db.COMPANYNAME}," +
        //                                      $" IBT# {doc.DOCENTRY}\nError: {message1}";
        //                    _logger.LogError(LastError);
        //                    return BadRequest($"request not handler.\n{LastError}");  

        //                }
        //                return Ok(doc);
        //            }

        //            if (holdeds.Count == 1 && holdeds[0].HoldByUserCode.ToLower()
        //                                               .Equals(dto.UserCode.ToLower())) // it hold by me 
        //            {
        //                return Ok(doc);
        //            }

        //            // on hold doc ss                    
        //            var message = "";
        //            holdeds.ForEach(u =>
        //            {
        //                message += $"{db.COMPANYNAME}\n #{u.HoldDocEntry} currently picking by " +
        //                           $"{u.HoldByUserCode}, {u.HoldByUserName}[PE2]\n\n";
        //            });
        //            return BadRequest(message);
        //        }

        //        return BadRequest("No new available pick order");
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //    finally
        //    {
        //        IsBusyNextOrder = false; // open the entry for next person 
        //    }
        //}

        ///// <summary>
        ///// Create / insert a onhold record into the on hold
        ///// </summary>
        ///// <param name="dto"></param>
        ///// <returns></returns>
        //string PutOnHoldSo_InPicking(Dto_Pick dto)
        //{
        //    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //    if (db == null)
        //    {
        //        return "Error reading the DBI, please try again later. Thanks.";
        //    }

        //    using var conn = new SqlConnection(_commDbConnStr);
        //    conn.Open();
        //    using var trans = conn.BeginTransaction();

        //    // no create invoice 
        //    // no on hold 
        //    // then insert the onholad record
        //    try
        //    {
        //        // perform insert
        //        var insert_sql = @$"INSERT INTO [{db.WEBDB}].[dbo].[FTAPP_OnHoldSoInPicking] ( 
        //                                 HoldDocEntry
        //                               , HoldByUserCode
        //                               , HoldByUserName
        //                               , HoldStartDt
        //                               , HoldReason
        //                              ) VALUES (
        //                                 @HoldDocEntry
        //                               , @HoldByUserCode
        //                               , @HoldByUserName
        //                               , GETDATE()
        //                               , @HoldReason
        //                              )";

        //        var insert_result = conn.Execute(insert_sql,
        //            new
        //            {
        //                HoldDocEntry = dto.DocEntry,
        //                HoldByUserCode = dto.UserCode,
        //                HoldByUserName = dto.UserName,
        //                HoldReason = dto.HoldReason
        //            }, trans);

        //        trans.Commit();
        //        return "";
        //    }
        //    catch (Exception e)
        //    {
        //        trans.Rollback();
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return LastError;
        //    }
        //}

        /// <summary>
        /// Check is the invoice create 
        /// return true or false
        /// </summary>
        /// <param name="db"></param>
        /// <param name="docEntry"></param>
        /// <returns></returns>
        //bool IsCheckInvCreated(DbInfo db, int docEntry, out int? invNo)
        //{
        //    invNo = -1;
        //    try
        //    {
        //        var sp_CheckSoInvoice = $"Select docEntry, invno " +
        //                                $"from {db.WEBDB}..SO " +
        //                                $"where docEntry = @docEntry " +
        //                                $"and docStatus = 'I' ";

        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var foundSo = conn.Query<WSO>(sp_CheckSoInvoice, new { docEntry = docEntry }).FirstOrDefault();
        //        if (foundSo != null)
        //        {
        //            invNo = foundSo.INVNO;
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return false;
        //    }
        //}

        //bool PutIbtOrderOnHold(FTAPP_OnHold_IBT_InPicking dto, DbInfo db)
        //{
        //    using var conn = new SqlConnection(_commDbConnStr);
        //    conn.Open();
        //    using var trans = conn.BeginTransaction();

        //    try
        //    {
        //        var insertSql = $@"Insert into {db.WEBDB}..FTAPP_OnHold_IBT_InPicking (
        //                             HoldDocEntry 
        //                           , HoldByUserCode
        //                           , HoldByUserName
        //                           , HoldStartDt
        //                           , HoldReason 
        //                        ) values (
        //                              @HoldDocEntry 
        //                            , @HoldByUserCode
        //                            , @HoldByUserName
        //                            , GETDATE()
        //                            , @HoldReason 
        //                        )";

        //        conn.Execute(insertSql, dto, trans);
        //        trans.Commit();
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        trans.Rollback();
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return false;
        //    }
        //}

        //void BatchUpdateForOOS_NewPick(List<WSO> OosSos, string appVersion)
        //{
        //    if (OosSos == null) return;
        //    if (OosSos.Count == 0) return;

        //    // initial new connection and transaction for this delete OS SO
        //    using var conn = new SqlConnection(_commDbConnStr);
        //    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
        //    using var trans = conn.BeginTransaction();

        //    try
        //    {
        //        for (int x = 0; x < OosSos.Count; x++)
        //        {
        //            var so = OosSos[x];
        //            if (so == null) continue;
        //            if (so.LinesCount > 0) continue; // check again for the line total

        //            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, so.SubSi);
        //            if (db == null)
        //            {
        //                continue;
        //            }

        //            var sql_delete = @"exec sp_DeleteSoDraft @webDb, @docEntry";
        //            var res = conn.Execute(sql_delete, new { webDb = db.WEBDB, docEntry = so.DOCENTRY }, trans);

        //            var dto = new Dto_Pick
        //            {
        //                Subsi = so.SubSi,
        //                DocEntry = (int)so.DOCENTRY,
        //                CardCode = so.CARDCODE,
        //                UserCode = "SVR",
        //                AppVersion = appVersion
        //            };

        //            var successLog = new FTAPP_AppPostLog
        //            {
        //                AppModule = "Picked to Invoiced",
        //                UserCode = "SVR",
        //                CardCode = so.CARDCODE,
        //                SubSi = so.SubSi,
        //                Details = $"#{so.DOCENTRY}, No Picked Qty, Set OutofStock",
        //                PostResult = "Success",
        //                AppVersion = $"ServerUpdate, {dto.AppVersion}"
        //            };

        //            var logHelper = new AppPostLogHelper();
        //            var isCreated = logHelper.IsCreated(successLog, db, conn, trans);
        //            if (!isCreated)
        //            {
        //                _logger.LogError($"Delete OS fail, {so.SubSi} #{so.DOCENTRY} ");
        //            }
        //        }

        //        trans.Commit(); // comit at the last 
        //    }
        //    catch (Exception e)
        //    {
        //        trans.Rollback();
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //    }
        //}

        //IActionResult NewPick_SetSOOutStock(Dto_Pick dto)
        //{
        //    if (string.IsNullOrWhiteSpace(dto.Subsi))
        //    {
        //        return BadRequest("Subsi name empty");
        //    }

        //    var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
        //    if (db == null)
        //    {
        //        return BadRequest("db info empty");
        //    }

        //    using var conn = new SqlConnection(_commDbConnStr);
        //    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
        //    using var trans = conn.BeginTransaction();

        //    try
        //    {
        //        var sql_delete = @"exec sp_DeleteSoDraft @webDb, @docEntry";
        //        var res = conn.Execute(sql_delete, new { webDb = db.WEBDB, docEntry = dto.DocEntry }, trans);
        //        var successLog = new FTAPP_AppPostLog
        //        {
        //            AppModule = "Picked to Invoiced",
        //            UserCode = $"{dto.UserCode}",
        //            CardCode = $"{dto.CardCode}",
        //            SubSi = $"{dto.Subsi}",
        //            Details = $"#{dto.DocEntry}, No Picked Qty, Set OutofStock",
        //            PostResult = "Success",
        //            AppVersion = $"ServerUpdate, {dto.AppVersion}"
        //        };

        //        var logHelper = new AppPostLogHelper();
        //        var isCreated = logHelper.IsCreated(successLog, db, conn, trans);
        //        if (isCreated)
        //        {
        //            trans.Commit();
        //            return Ok();
        //        }

        //        trans.Rollback();
        //        return BadRequest(logHelper.LastError);
        //    }
        //    catch (Exception e)
        //    {
        //        trans.Rollback();
        //        LastError = $"{ e.Message}\n{e.StackTrace}";
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        //IActionResult GetQueueSO_Onhold_NewPick(Dto_Pick dto, DbInfo db)
        //{
        //    try
        //    {
        //        var sql = @"exec sp_SelectHoldSO @webDB, @erpDb, @subsi, @docStatus, @SubSiID";
        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var results = conn.Query<WSO>(sql, new
        //        {
        //            webDb = db.WEBDB,
        //            erpDb = db.SAPDB,
        //            subsi = db.COMPANYNAME,
        //            docStatus = "Q",
        //            SubSiID = db.COMPANYID
        //        }).ToList();

        //        if (results == null) return NotFound(); // save time cut off checking 
        //        if (results.Count == 0) return NotFound();

        //        // sub query the list with user agency 
        //        var queryUserAgency = "exec sp_SelectWhsUserAgency2 @webDb, @UserCode";
        //        var userAgencies = conn.Query<USERCARD>(queryUserAgency, new
        //        {
        //            webDb = db.WEBDB,
        //            UserCode = dto.UserCode
        //        }).ToList();

        //        if (userAgencies == null) return Ok(results);
        //        if (userAgencies.Count == 0) return Ok(results);

        //        // procee the new list 
        //        var returnList = results.Where(x => userAgencies.Any(y => y.CARDCODE == x.AgencyCode)).ToList();
        //        if (returnList == null) return NotFound();
        //        if (returnList.Count == 0) return NotFound();

        //        return Ok(returnList);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}
    }
}

