using Dapper;
using KTC_SalesAppWAPI.DTOs;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Geofence;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SSOController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly ILogger<SSOController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;
        string _attachmentFolder { get; set; } = string.Empty; // 20240201
        string _webAccessPath { get; set; } = string.Empty;

        bool IsTesting { get; set; } = false;
        public SSOController(IConfiguration configuration, ILogger<SSOController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            _attachmentFolder = _configuration.GetSection("WebAttachmentPath").Value;
            _webAccessPath = _configuration.GetSection("WebAccessPath").Value;
        }

        [HttpPost]
        public IActionResult Post(UserProfile_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "Login":
                    {
                        return Login(dto);
                    }
                case "LoadErpUserCustomer":
                    {
                        return LoadErpUserCustomer(dto);
                    }
                //case "LoadErpUserCustomer_Seller":
                //    {
                //        return LoadErpUserCustomer_BySeller(dto); // for seller 
                //    }
                case "LoadErpUserCustomer_ByCards":
                    {
                        return LoadErpUserCustomer_ByCards(dto);
                    }
                case "LoadErpUserCustomer_SingleCard":
                    {
                        return LoadErpUserCustomer_SingleCard(dto);
                    }
                case "LoadSchedule":
                    {
                        return LoadErpCustomer(dto);
                    }
                case "LoadSchedule_multipleTrip":
                    {
                        return LoadSchedule_MultipleTrip(dto);
                    }
                case "AddOffScheduleStore":
                    {
                        return AddOffScheduleStore(dto);
                    }
                case "LoadErpUserCustomer_WildCard":
                    {
                        return LoadErpUserCustomer_WildCard(dto);
                    }
                case "LoadErpUserCustomer_WildCard_BySeller":
                    {
                        return LoadErpUserCustomer_WildCard_BySeller(dto);
                    }
                case "RefreshAuthMenus":
                    {
                        return RefreshAuthMenus(dto);
                    }
                case "MapViewDemoOcrd":
                    {
                        return MapViewDemoOcrd();
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        // 20230811
        IActionResult MapViewDemoOcrd()
        {
            try
            {
                using var conn = new SqlConnection(_commDbConnStr);

                var cards = conn.Query<OCRD_Ext>("exec sp_MapViewDemoOcrd ").ToList();
                if (cards.Count == 0) return NotFound();

                var repliedCards = new List<OCRD_Ext>();

                var subsis = cards.Select(c => c.CompanyName).Distinct().ToList();
                var helper = new DbNameHelper();

                for (int s = 0; s < subsis.Count; s++)
                {
                    var dbInfo = helper.GetDbInfo(_commDbConnStr, subsis[s]);
                    if (dbInfo == null) continue;
                    var compCards = cards.Where(c => c.CompanyName == dbInfo.COMPANYNAME).ToList();

                    var newCards = ProcessCardGLN(compCards, conn, dbInfo);
                    if (newCards.Count > 0)
                    {
                        repliedCards.AddRange(newCards);
                    }
                }
                return Ok(repliedCards);
            }
            catch (Exception exp)
            {
                LastError = $"{exp.Message}\n{exp.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult RefreshAuthMenus(UserProfile_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QueryUserCode))
                {
                    return BadRequest("Invalid userid, field can not be empty, please try again later.");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompany))
                {
                    return BadRequest("Invalid company, field can not be empty, please try again later.");
                }

                var res = GetAuthMenu(dto.QueryUserCode, dto.QueryCompany);
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult LoadErpUserCustomer_WildCard(UserProfile_Dto dto)
        {
            try
            {
                List<OCRD_Ext> returnList = null;
                var dbhelper = new DbNameHelper();

                var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
                if (dbInfo == null) return BadRequest("Invalid company.");

                //   @CompanyName as nvarchar(120), 
                //@CompanyID as nvarchar(120), 
                //@ErpDb as nvarchar(120), 
                //@WebDb as nvarchar(120), 
                //@QueryUserCode as nvarchar(120), 
                //@CardType as nvarchar(1), 
                //@wildCode as nvarchar(100)
                var sql_sp = @"exec sp_SelectErpCustomer_Wildcard 
                                    @CompanyName, 
                                    @CompanyID,
	                                @ErpDb,
	                                @WebDb,
	                                @QueryUserCode,
	                                @CardType, 
                                    @wildCode";

                var param = new
                {
                    CompanyName = dbInfo.COMPANYNAME,
                    CompanyId = dbInfo.COMPANYID,
                    ErpDB = dbInfo.SAPDB,
                    WebDb = dbInfo.WEBDB,
                    QueryUserCode = dto.QueryUserCode,
                    CardType = dto.QueryCardType,
                    wildCode = dto.WildCode
                };

                using var conn = new SqlConnection(_commDbConnStr);
                var tempList = conn.Query<OCRD_Ext>(sql_sp, param).ToList();

                if (tempList?.Count == 0)
                {
                    return NotFound();
                }

                tempList = ProcessCardGLN(tempList, conn, dbInfo);

                if (returnList == null) returnList = new List<OCRD_Ext>();
                returnList.AddRange(tempList);
                return Ok(returnList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult LoadErpUserCustomer_WildCard_BySeller(UserProfile_Dto dto)
        {
            try
            {
                List<OCRD_Ext> returnList = null;
                var dbhelper = new DbNameHelper();

                var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
                if (dbInfo == null) return BadRequest("Invalid company.");

                //   @CompanyName as nvarchar(120), 
                //@CompanyID as nvarchar(120), 
                //@ErpDb as nvarchar(120), 
                //@WebDb as nvarchar(120), 
                //@QueryUserCode as nvarchar(120), 
                //@CardType as nvarchar(1), 
                //@wildCode as nvarchar(100)
                var sql_sp = @"exec sp_SelectErpCustomer_Wildcard_BySeller 
                                    @CompanyName, 
                                    @CompanyID,
	                                @ErpDb,
	                                @WebDb,
	                                @QueryUserCode,
	                                @CardType, 
                                    @wildCode";

                var param = new
                {
                    CompanyName = dbInfo.COMPANYNAME,
                    CompanyId = dbInfo.COMPANYID,
                    ErpDB = dbInfo.SAPDB,
                    WebDb = dbInfo.WEBDB,
                    QueryUserCode = dto.QueryUserCode,
                    CardType = dto.QueryCardType,
                    wildCode = dto.WildCode
                };

                using var conn = new SqlConnection(_commDbConnStr);
                var tempList = conn.Query<OCRD_Ext>(sql_sp, param).ToList();

                if (tempList?.Count == 0)
                {
                    return NotFound();
                }

                tempList = ProcessCardGLN(tempList, conn, dbInfo);

                if (returnList == null) returnList = new List<OCRD_Ext>();
                returnList.AddRange(tempList);
                return Ok(returnList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult AddOffScheduleStore(UserProfile_Dto dto)
        {
            try
            {
                if (dto.AddOffSch_Store == null)
                {
                    return BadRequest("Add in store is null");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompany))
                {
                    return BadRequest("Add in store company name is empty");
                }

                var newhead = new FTAppGeoTrack
                {
                    HeaderGuid = Guid.NewGuid(),
                    StoreCode = dto.AddOffSch_Store.StoreCode,
                    StoreName = dto.AddOffSch_Store.StoreName,
                    ScheduleDate = DateTime.Now.Date,
                    UserCode = dto.AddOffSch_Store.UserCode,
                    SlpName = dto.AddOffSch_Store.SlpName,
                    Latitude = dto.AddOffSch_Store.Latitude,
                    Longitude = dto.AddOffSch_Store.Longitude,
                    Address = dto.AddOffSch_Store.Address,
                    IsOffRoute = true // set this as off route
                };

                var isAdded = InsertScehdule(newhead, dto.QueryCompany); // Add off route
                if (!isAdded)
                {
                    return BadRequest("Error add in off route to db, please try again.");
                }

                var companies = GetUserCompanies(dto.AddOffSch_Store.UserCode);
                if (companies == null) return NotFound();
                if (companies.Count == 0) return NotFound();

                var query = new UserProfile_Dto
                {
                    Companies = companies,
                    QueryUserCode = dto.AddOffSch_Store.UserCode
                };

                return LoadSchedule_MultipleTrip(query); // from add off route 
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
                var sql = @"select UserCompany from [FTAPP_SSO] WITH (NOLOCK) where UserCode = @userCode";
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

        IActionResult LoadSchedule_MultipleTrip(UserProfile_Dto dto)
        {
            try
            {
                if (dto.Companies == null)
                {
                    return BadRequest("Invalid query companies");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryUserCode))
                {
                    return BadRequest("Invalid user code");
                }

                var results = new List<FTAppGeoTrack>();
                using var conn = new SqlConnection(_commDbConnStr);

                dto.Companies.ForEach(Subsi =>
                {
                    var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, Subsi);
                    if (dbInfo == null) return; // next

                    var query_sp = "exec sp_SelectGeoTrack @WebDb, @QueryUserCode, @QueryDate, @Subsi";
                    var param = new
                    {
                        WebDb = dbInfo.WEBDB,
                        dto.QueryUserCode,
                        QueryDate = DateTime.Now.Date,
                        Subsi
                    };

                    var result = conn.Query<FTAppGeoTrack>(query_sp, param).ToList();
                    if (result?.Count == 0) return;

                    // message the line (if any)
                    if (result == null) return; // next
                    for (int r = 0; r < result.Count; r++)
                    {
                        var query_sp1 = "exec sp_SelectGeoTrackLine @WebDb, @HeaderGuid";
                        var param1 = new
                        {
                            WebDb = dbInfo.WEBDB,
                            result[r].HeaderGuid
                        };
                        result[r].Line = conn.Query<FTAppGeoTrackLine>(query_sp1, param1).ToList();
                    }
                    results.AddRange(result);
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

        IActionResult Login(UserProfile_Dto request)
        {
            try
            {
                var loginName = $"{request.LoginName}"; // UserCode
                if (string.IsNullOrWhiteSpace(loginName))
                {
                    return BadRequest("Invalid userid, field can not be empty, please try again later.");
                }

                using var conn = new SqlConnection(_commDbConnStr);

                // login with user login name
                var founds = conn.Query<FTAPP_SSO>(
                  @"SELECT *
                    FROM FTAPP_SSOView WITH (NOLOCK) 
                    WHERE UserName = @loginName OR UserCode = @loginName", new { loginName }).ToList();

                // prepare the needed data to app

                if (founds == null)
                {
                    return NotFound(request);
                }

                if (founds.Count == 0)
                {
                    return NotFound(request);
                }

                var companies = founds.Select(c => c.UserCompany).Distinct().ToList();
                request.Companies = companies;
                request.UserProfiles = founds;
                request.Schedule_OCRDs = LoadSchedule(founds, DateTime.Now);
                request.AppConfig = LoadAppConfigSetup();
                request.ResetRequest();
                request.AutMenus = GetAuthMenu(loginName, $"{request.QueryCompany}");

                // 20240201
                // get list of boolet from year mon folder 
                request.Booklets = GetBookLet();

                return Ok(request);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        List<Booklet> GetBookLet()
        {
            try
            {
                var bookletFolder = "booklet";

                var yrMthFolder = DateTime.Now.ToString("yyyy") + DateTime.Now.ToString("MM");
                var targetDir = Path.Combine(_attachmentFolder, bookletFolder, yrMthFolder);

                //C:\KTCWEB1\Attachment\booklet\202402
                if (!Directory.Exists(targetDir))
                {
                    return null;
                }


                var filesPath = Directory.GetFiles(targetDir);
                var returnList = new List<Booklet>();

                for (int x = 0; x < filesPath.Length; x++)
                {
                    var curFile = new FileInfo(filesPath[x]);
                    var addFile = new Booklet
                    {
                        Url = $"{_webAccessPath}{bookletFolder}/{yrMthFolder}/{curFile.Name}",
                        FileName = curFile.Name,
                        FileType = curFile.Extension.Replace(".", ""),
                        DownloadPath = $"{_webAccessPath}{bookletFolder}/{yrMthFolder}/",
                    };

                    returnList.Add(addFile);
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

        List<AuthMenu> GetAuthMenu(string userCode, string company)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(company))
                {
                    return null;
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, company);
                if (db == null)
                {
                    return null;
                }
                var sql = "exec sp_SelectAuthMenu @webDb, @commDb, @userCode, @subSi";

                //create procedure[sp_SelectAuthMenu]
                //@webDb as nvarchar(100), 
                //@commDb as nvarchar(100), 
                //@userCode as nvarchar(100), 
                //@subSi as nvarchar(300)

                var conn = new SqlConnection(_commDbConnStr);
                return conn.Query<AuthMenu>(sql, new
                {
                    webDb = db.WEBDB,
                    commDb = db.COMMONDB,
                    userCode = userCode,
                    subSi = company
                }).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }


        // load all user code bp card type c   
        IActionResult LoadErpCustomer(UserProfile_Dto dto)
        {
            try
            {
                if (dto.QueryCompanies == null)
                {
                    return BadRequest("Companies can not be empty");
                }

                if (dto.QueryCompanies.Count == 0)
                {
                    return BadRequest("Companies can not be empty");
                }

                List<OCRD_Ext> returnList = null;

                var dbhelper = new DbNameHelper();
                using var conn = new SqlConnection(_commDbConnStr);
                for (int c = 0; c < dto.QueryCompanies.Count; c++)
                {
                    var company = dto.QueryCompanies[c];

                    var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, company);
                    if (dbInfo == null) continue; // next company

                    var sql_sp = @"exec sp_SelectErpCustomer 
                                    @CompanyName, 
                                    @CompanyID,
	                                @ErpDb,
	                                @WebDb,
	                                @QueryUserCode,
	                                @CardType";

                    var param = new
                    {
                        CompanyName = dbInfo.COMPANYNAME,
                        CompanyId = dbInfo.COMPANYID,
                        ErpDB = dbInfo.SAPDB,
                        WebDb = dbInfo.WEBDB,
                        QueryUserCode = dto.QueryUserCode,
                        CardType = "C"
                    };

                    using var conn1 = new SqlConnection(_commDbConnStr);
                    var tempList = conn1.Query<OCRD_Ext>(sql_sp, param).ToList();

                    if (tempList?.Count == 0) continue;

                    tempList = ProcessCardGLN(tempList, conn1, dbInfo);

                    if (returnList == null) returnList = new List<OCRD_Ext>();
                    returnList.AddRange(tempList);
                }

                return Ok(returnList); //<--- accumulated list item 
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        List<OCRD_Ext> LoadSchedule(List<FTAPP_SSO> users, DateTime queryDate)
        {
            try
            {
                List<OCRD_Ext> returnList = null;
                List<OCRD_Ext> tempList = null;
                using var conn = new SqlConnection(_commDbConnStr);

                for (int u = 0; u < users.Count; u++)
                {
                    var user = users[u];
                    if (user == null) continue;

                    // load the seller scehdule base on Slpname (sap)
                    // using dapper store procedure
                    var date = $"{queryDate:yyyyMMdd}";
                    var sql = @"exec sp_SelectSchedule
                                @UserCompany,
                                @UserCompanyID,     
	                            @UserCode,          
	                            @SlpName,          
	                            @erpDb,             
	                            @webDb,         
                                @queryUserCode,
	                            @date";

                    var values = new
                    {
                        UserCompany = user.UserCompany,
                        UserCompanyID = user.UserCompanyID,
                        UserCode = user.UserCode,
                        SlpName = user.SlpName,
                        erpDb = user.UserCompanyErpRef,
                        webDb = user.UserCompanyRef,
                        queryUserCode = user.SlpName, // < query based on slpname
                        date = date
                    };

                    tempList = conn.Query<OCRD_Ext>(sql, values).ToList();

                    if (tempList?.Count == 0) // query by portal user code
                    {
                        var param = new
                        {
                            UserCompany = user.UserCompany,
                            UserCompanyID = user.UserCompanyID,
                            UserCode = user.UserCode,
                            SlpName = user.SlpName,
                            erpDb = user.UserCompanyErpRef,
                            webDb = user.UserCompanyRef,
                            queryUserCode = user.UserCode, // < query based on portal user
                            date = date
                        };
                        tempList = conn.Query<OCRD_Ext>(sql, param).ToList();
                    }

                    if (tempList?.Count == 0) continue;
                    tempList = ProcessCardGLN(tempList, conn, new DbInfo { SAPDB = user.UserCompanyErpRef });

                    if (returnList == null) returnList = new List<OCRD_Ext>();
                    returnList.AddRange(tempList);
                }

                if (returnList != null)
                {
                    for (int i = 0; i < returnList.Count; i++)
                    {
                        var r = returnList[i];
                        var newhead = new FTAppGeoTrack
                        {
                            HeaderGuid = Guid.NewGuid(),
                            StoreCode = r.CardCode,
                            StoreName = r.CardName,
                            ScheduleDate = queryDate,
                            UserCode = r.UserCode,
                            SlpName = r.SlpName,
                            Latitude = r.Latitude, // GLN from SAP
                            Longitude = r.Longitude, // GLN from SAP
                            Address = r.Address, 
                            IsOffRoute = false
                        };
                        InsertScehdule(newhead, r.CompanyName); // LoadSchedule
                    }
                }

                //if (IsTesting)
                //{
                //    var newhead = new FTAppGeoTrack
                //    {
                //        HeaderGuid = Guid.NewGuid(),
                //        StoreCode = "12345678",
                //        StoreName = "House for testing",
                //        ScheduleDate = queryDate,
                //        UserCode = "1001000237",
                //        SlpName = "1001000237",
                //        Latitude = 3.053182, // GLN from SAP // 3.053182043131528, 101.64269984310401
                //        Longitude = 101.64255, // GLN from SAP
                //        Address = @"6,Jalan BK 2 / 12b,Bandar Kinrara 2,47180 Puchong,Selangor"
                //    };
                //    InsertScehdule(newhead, "TEST - KTC Loreal"); // LoadSchedule

                //    var newCard = new OCRD_Ext
                //    {
                //        CardCode = "12345678",
                //        CardName = "J1 Store",
                //        CompanyName = "TEST - KTC Loreal",
                //        Latitude = 3.05302,
                //        Longitude = 101.64271
                //    };

                //    var newhead1 = new FTAppGeoTrack
                //    {
                //        HeaderGuid = Guid.NewGuid(),
                //        StoreCode = "123456789",
                //        StoreName = "J3 Store",
                //        ScheduleDate = queryDate,
                //        UserCode = "1001000237",
                //        SlpName = "1001000237",
                //        Latitude = 3.05302, // GLN from SAP
                //        Longitude = 101.64271, // GLN from SAP
                //        Address = @"7,Jalan BK 2 / 12b,Bandar Kinrara 2,47180 Puchong,Selangor"
                //    };
                //    InsertScehdule(newhead1, "TEST - KTC Loreal"); // LoadSchedule

                //    var newCard1 = new OCRD_Ext
                //    {
                //        CardCode = "123456789",
                //        CardName = "J3 Store",
                //        CompanyName = "TEST - KTC Loreal",
                //        Latitude = 3.05302,
                //        Longitude = 101.64271
                //    };

                //    var newhead2 = new FTAppGeoTrack
                //    {
                //        HeaderGuid = Guid.NewGuid(),
                //        StoreCode = "1234567890",
                //        StoreName = "FT",
                //        ScheduleDate = queryDate,
                //        UserCode = "1001000237",
                //        SlpName = "1001000237",
                //        Latitude = 3.031838543292382,
                //        Longitude = 101.61623212626326,
                //        Address = @"SetiaWalk, Unit H-05-01, Block H"
                //    };
                //    InsertScehdule(newhead2, "TEST - KTC Loreal"); // LoadSchedule

                //    var newCard2 = new OCRD_Ext
                //    {
                //        CardCode = "1234567890",
                //        CardName = "FT",
                //        CompanyName = "TEST - KTC Loreal",
                //        Latitude = 3.031838543292382,
                //        Longitude = 101.61623212626326,
                //    };

                //    if (returnList == null) returnList = new List<OCRD_Ext>();

                //    returnList.Add(newCard);
                //    returnList.Add(newCard1);
                //    returnList.Add(newCard2);
                //}

                return returnList;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        List<OCRD_Ext> ProcessCardGLN(List<OCRD_Ext> list, SqlConnection conn, DbInfo db)
        {
            try
            {
                if (list == null) return list;

                for (int x = 0; x < list.Count; x++)
                {
                    if (string.IsNullOrWhiteSpace(list[x].GlblLocNum)) continue; // cont next
                    var glnArray = list[x].GlblLocNum.Split(',');
                    if (glnArray?.Length < 2) continue; // cont. next

                    list[x].Latitude = SafeGetDouble(glnArray[0]); // actual code
                    list[x].Longitude = SafeGetDouble(glnArray[1]);

                    // get the bill address 
                    // get the s address type 
                    // get shipment address
                    var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                    var bill_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (bill_address != null)
                    {
                        list[x].Address = bill_address.GetAddress();
                    }

                    // get the s address type 
                    // get shipment address
                    sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='S'";

                    var ship_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (ship_address != null)
                    {
                        list[x].ShipAdd = ship_address.GetAddress();
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return list;
            }
        }

        bool InsertScehdule(FTAppGeoTrack head, string companName)
        {
            var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, companName);
            if (dbInfo == null) return false;

            var checkSql = @$"SELECT * FROM {dbInfo.WEBDB}..FTAppGeoTrack WITH (NOLOCK) 
                                 Where (UserCode = @UserCode OR  SlpName = @UserCode)
                                 AND StoreCode = @StoreCode
                                 AND Convert(date, ScheduleDate) = @ScheduleDate";

            using var conn = new SqlConnection(_commDbConnStr);
            var isFound = conn.Query(checkSql, new
            {
                UserCode = head.UserCode,
                StoreCode = head.StoreCode,
                ScheduleDate = head.ScheduleDate.Date
            }).FirstOrDefault();

            if (isFound != null)
            {
                return true;
            }

            var sqlInsert = @$"INSERT INTO {dbInfo.WEBDB}..FTAppGeoTrack ( 
                                    StoreCode
                                   ,StoreName
                                   ,ScheduleDate
                                   ,UserCode
                                   ,SlpName
                                   ,Latitude
                                   ,Longitude
                                   ,HeaderGuid
                                   ,Address
                                   ,IsOffRoute
                                    ) VALUES (
                                    @StoreCode
                                   ,@StoreName
                                   ,@ScheduleDate
                                   ,@UserCode
                                   ,@SlpName
                                   ,@Latitude
                                   ,@Longitude
                                   ,@HeaderGuid
                                   ,@Address
                                   ,@IsOffRoute)";

            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                int result = conn.Execute(sqlInsert, head, trans);
                if (result <= 0)
                {
                    LastError = $"{dbInfo.COMPANYNAME}, Error insert Off Route {head.StoreCode} {head.StoreName}," +
                                $" {head.UserCode} {head.SlpName}";
                    _logger.LogError(LastError);
                    trans.Rollback();
                    return false;
                }

                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        bool IsDuplicated(FTAppGeoTrack head, DbInfo dbInfo)
        {
            try
            {
                var checkSql = @$"SELECT * FROM [{dbInfo.WEBDB}].[dbo].[FTAppGeoTrack] WITH (NOLOCK) 
                                 Where (UserCode = @UserCode OR  SlpName = @UserCode)
                                 AND StoreCode = @StoreCode
                                 AND Convert(date, ScheduleDate) = @ScheduleDate";

                using var conn = new SqlConnection(_commDbConnStr);
                var isFound = conn.Query(checkSql, new
                {
                    UserCode = head.UserCode,
                    StoreCode = head.StoreCode,
                    ScheduleDate = head.ScheduleDate.Date
                }).FirstOrDefault();

                if (isFound == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return true;
            }
        }

        IActionResult LoadErpUserCustomer_SingleCard(UserProfile_Dto dto)
        {
            try
            {
                //List<OCRD_Ext> returnList = null;
                var dbhelper = new DbNameHelper();

                var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
                if (dbInfo == null) return BadRequest("Invalid company.");

                var sql_sp = @"exec sp_SelectErpCustomer_SingleCard @CompanyName, @CompanyID, @ErpDb, @CardCode";

                var param = new
                {
                    CompanyName = dbInfo.COMPANYNAME,
                    CompanyId = dbInfo.COMPANYID,
                    ErpDB = dbInfo.SAPDB,
                    CardCode = dto.CardCode
                };

                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<OCRD_Ext>(sql_sp, param).FirstOrDefault();
                if (result == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrWhiteSpace(result.GlblLocNum)) return Ok(result);

                var glnArray = result.GlblLocNum.Split(',');
                if (glnArray?.Length < 2) return Ok(result);

                result.Latitude = SafeGetDouble(glnArray[0]); // actual code
                result.Longitude = SafeGetDouble(glnArray[1]);

                // handler the ship address and bill address

                // get the bill address 
                // get the s address type 
                // get shipment address
                var sql = @$"SELECT * FROM {dbInfo.SAPDB}..CRD1 WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                var bill_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = result.CardCode }).FirstOrDefault();
                if (bill_address != null)
                {
                    result.Address = bill_address.GetAddress();
                }

                // get the s address type 
                // get shipment address
                sql = @$"SELECT * FROM {dbInfo.SAPDB}..CRD1 WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='S'";

                var ship_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = result.CardCode }).FirstOrDefault();
                if (ship_address != null)
                {
                    result.ShipAdd = ship_address.GetAddress();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }


        IActionResult LoadErpUserCustomer_ByCards(UserProfile_Dto dto)
        {
            try
            {
                List<OCRD_Ext> returnList = null;
                var dbhelper = new DbNameHelper();

                var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
                if (dbInfo == null) return BadRequest("Invalid company.");

                //@CompanyName as nvarchar(120), 
                //@CompanyID as nvarchar(120), 
                //@ErpDb as nvarchar(120), 
                //@WebDb as nvarchar(120), 
                //@QueryUserCode as nvarchar(120), 
                //@CardType as nvarchar(1)
                var sql_sp = @"exec sp_SelectErpCustomer_ByCards 
                                    @CompanyName, 
                                    @CompanyID,
	                                @ErpDb,
	                                @WebDb,
	                                @QueryUserCode,
	                                @CardType, 
                                    @CardCodes";

                var param = new
                {
                    CompanyName = dbInfo.COMPANYNAME,
                    CompanyId = dbInfo.COMPANYID,
                    ErpDB = dbInfo.SAPDB,
                    WebDb = dbInfo.WEBDB,
                    QueryUserCode = dto.QueryUserCode,
                    CardType = dto.QueryCardType,
                    CardCodes = dto.CardCodes
                };

                using var conn = new SqlConnection(_commDbConnStr);
                var tempList = conn.Query<OCRD_Ext>(sql_sp, param).ToList();

                if (tempList?.Count == 0)
                {
                    return NotFound();
                }

                tempList = ProcessCardGLN(tempList, conn, dbInfo);

                if (returnList == null) returnList = new List<OCRD_Ext>();
                returnList.AddRange(tempList);
                return Ok(returnList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        // load all user card from svr to app 
        IActionResult LoadErpUserCustomer(UserProfile_Dto dto)  //string companies, string userCode, string cardType)
        {
            try
            {
                List<OCRD_Ext> returnList = null;
                var dbhelper = new DbNameHelper();

                var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
                if (dbInfo == null) return BadRequest("Invalid company.");

                //@CompanyName as nvarchar(120), 
                //@CompanyID as nvarchar(120), 
                //@ErpDb as nvarchar(120), 
                //@WebDb as nvarchar(120), 
                //@QueryUserCode as nvarchar(120), 
                //@CardType as nvarchar(1)
                var sql_sp = @"exec sp_SelectErpCustomer @CompanyName, 
                                    @CompanyID,
	                                @ErpDb,
	                                @WebDb,
	                                @QueryUserCode,
	                                @CardType";

                var param = new
                {
                    CompanyName = dbInfo.COMPANYNAME,
                    CompanyId = dbInfo.COMPANYID,
                    ErpDB = dbInfo.SAPDB,
                    WebDb = dbInfo.WEBDB,
                    QueryUserCode = dto.QueryUserCode,
                    CardType = dto.QueryCardType
                };

                using var conn = new SqlConnection(_commDbConnStr);
                var tempList = conn.Query<OCRD_Ext>(sql_sp, param).ToList();

                if (tempList?.Count == 0) return NotFound();

                tempList = ProcessCardGLN(tempList, conn, dbInfo);

                if (returnList == null) returnList = new List<OCRD_Ext>();
                returnList.AddRange(tempList);
                return Ok(returnList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        // load all user card from svr to app 
        // seller 
        //IActionResult LoadErpUserCustomer_BySeller(UserProfile_Dto dto)  //string companies, string userCode, string cardType)
        //{
        //    try
        //    {
        //        List<OCRD_Ext> returnList = null;
        //        var dbhelper = new DbNameHelper();

        //        var dbInfo = dbhelper.GetDbInfo(_commDbConnStr, dto.QueryCompany);
        //        if (dbInfo == null) return BadRequest("Invalid company.");

        //        //@CompanyName as nvarchar(120), 
        //        //@CompanyID as nvarchar(120), 
        //        //@ErpDb as nvarchar(120), 
        //        //@WebDb as nvarchar(120), 
        //        //@QueryUserCode as nvarchar(120), 
        //        //@CardType as nvarchar(1)

        //        var sql_sp = @"exec sp_SelectErpCustomer @CompanyName, 
        //                            @CompanyID,
	       //                         @ErpDb,
	       //                         @WebDb,
	       //                         @QueryUserCode,
	       //                         @CardType";

        //        var param = new
        //        {
        //            CompanyName = dbInfo.COMPANYNAME,
        //            CompanyId = dbInfo.COMPANYID,
        //            ErpDB = dbInfo.SAPDB,
        //            WebDb = dbInfo.WEBDB,
        //            QueryUserCode = dto.QueryUserCode,
        //            CardType = dto.QueryCardType
        //        };

        //        using var conn = new SqlConnection(_commDbConnStr);
        //        var tempList = conn.Query<OCRD_Ext>(sql_sp, param).ToList();

        //        if (tempList?.Count == 0) return NotFound();

        //        tempList = ProcessCardGLN(tempList, conn, dbInfo);

        //        if (returnList == null) returnList = new List<OCRD_Ext>();
        //        returnList.AddRange(tempList);
        //        return Ok(returnList);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return null;
        //    }
        //}

        List<FTApp_Config> LoadAppConfigSetup()
        {
            try
            {
                var sql = "SELECT * FROM FTAPP_ConfigView WITH (NOLOCK) ";
                using var conn = new SqlConnection(_commDbConnStr);
                return conn.Query<FTApp_Config>(sql).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        double SafeGetDouble(string _value)
        {
            try
            {
                var isNumeric = Double.TryParse(_value, out double result);
                if (isNumeric) return result;
                return -1;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

    }
}

// reserved code
/// <summary>
/// return list of the schedule today
/// load in by each companies
/// </summary>
/// <returns></returns>        
//-- link with SAP
//select* from[KTCW_COMMON].[dbo].FTAPP_SSO t0
//inner join[KTCW_TW].dbo.FTApp_BaseRouteSchedule t1 on t0.UserCode = t1.UserCode

//-- query for web portal
//select* from [KTCW_COMMON].[dbo].FTAPP_SSO t0
//inner join [KTCW_KK].dbo.FTApp_BaseRouteSchedule t1 on t0.SlpName = t1.UserCode
//List<FTApp_BaseRouteSchedule> GetUserShecdule(List<string> companies, List<FTAPP_SSO> users)
//{
//    try
//    {
//        if (companies == null) return null;
//        if (companies.Count == 0) return null;

//        List<FTApp_BaseRouteSchedule> returnList = null;
//        var dbHelper = new DbNameHelper();
//        var date = $"{DateTime.Now:yyyyMMdd}";

//        users.ForEach(user =>
//        {
//            var dbInfo = dbHelper.GetDbInfo(_commDbConnStr, user.UserCompany);
//            if (dbInfo == null) return;

//            using var conn = new SqlConnection(_commDbConnStr);

//            var userCodeValue = (user.SourceType.Equals("OSLP")) ? user.SlpName : user.UserCode;

//            var sql = @$"SELECT t1.*, t0.UserCode, t0.Latitute, t0.Longitude 
//                        , (ISNULL(t2.StreetNo, '') +' '+ ISNULL(t2.Street, '')+' '+ ISNULL(t2.Block, '')+ 
//                        ' '+ ISNULL(t2.ZipCode, '')+' '+ ISNULL(t2.City, '')+' '+ ISNULL(t2.County, '') +
//                        ' '+ ISNULL(t2.Country, '')+' '+ISNULL(t2.State, '')) AS [Address]                      
//                        FROM [{dbInfo.WEBDB}].[dbo].[FTApp_BaseRouteUserCard] t0 
//                        INNER JOIN 
//                             [{dbInfo.WEBDB}].[dbo].[FTApp_BaseRouteSchedule] t1 on t0.RouteCode = t1.RouteCode         
//                        LEFT JOIN [{dbInfo.SAPDB}].dbo.CRD1 t2 on t1.StoreCode = t2.CardCode
//                        WHERE 
//                         t1.UserCode = @userCodeValue
//                            AND t0.StoreCode = t1.StoreCode 
//                            AND t1.ScheduleDate =  @date
//                            AND t1.IsActive = 1
//                            AND t2.AdresType = 'B'
//                            ORDER BY t1.SeqNo";

//            var list = conn.Query<FTApp_BaseRouteSchedule>(sql, new { userCodeValue, date }).ToList();

//            if (list == null) return;
//            if (list.Count == 0) return;

//            // pre process the adress 
//            list.ForEach(addr =>
//            {
//                if (!string.IsNullOrEmpty(addr.Address))
//                {
//                    addr.Address = addr.Address.Trim();
//                }
//            });

//            if (returnList == null) returnList = new List<FTApp_BaseRouteSchedule>();
//            returnList.AddRange(list);
//        });

//        return returnList?.Distinct().ToList();
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return null;
//    }
//}

