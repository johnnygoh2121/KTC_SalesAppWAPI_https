using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread;
using KTC_SalesAppWAPI.DTOs.PortalLogin;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.AppUpdate;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class PortalLoginBreadController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PortalLoginBreadController> _logger;
        //readonly string APP_JSON = "application/json";
        string LastError { get; set; } = string.Empty;
        //string WebHostAddrEndPoint = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;

        //readonly string AppType = "SalesApp";

        public PortalLoginBreadController(IConfiguration configuration, ILogger<PortalLoginBreadController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
            //WebHostAddrEndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPointLogin").Value;
        }

        [HttpPost]
        public IActionResult Post(PortalLogin_dto dto)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.appVersion) &&
                    !string.IsNullOrWhiteSpace(dto.appType))
                {
                    // check the latest version number
                    var sql = @"SELECT TOP 1 * 
                            FROM FTAPP_Update WITH (NOLOCK)    
                            Where AppType = @appType
                            ORDER by id DESC";

                    var conn = new SqlConnection(_commDbConnStr);
                    var result = conn.Query<FTAPP_Update>(sql, new { dto.appType }).FirstOrDefault();

                    if (result == null)
                    {
                        goto ProcessNormal;
                    }

                    if (!$"{result.AppVersion}".Equals(dto.appVersion))
                    {
                        var message = $"Pls login with latest version {result.AppVersion}, " +
                                      $"current version {dto.appVersion} is outdated !";
                        return BadRequest(message);
                    }
                }

            ProcessNormal:

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.company);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // origin
                var query = @$"SELECT ISNULL(T1.CARDNAME,'') AS [companyId] 
                                    FROM {db.WEBDB}..USERS T0 with (nolock) 
                                    LEFT OUTER JOIN {db.SAPDB}..OCRD T1  with (nolock) ON 
                                        T1.CARDCODE = T0.COMPANYID AND t1.CARDTYPE = 'C'
                                    WHERE
                                        ISNULL(T0.INACTIVE,'') <> 'I'
                                    AND T0.USERCODE = @USERCODE 
                                    AND ISNULL(T0.USRPASS,'') = @USRPASS";

                // final return BearerToken 
                var encrytedPw = new EnDecriptHelper().EncriptMD5String(dto.password);
                var conn1 = new SqlConnection(_commDbConnStr_Bread);

                var newToken = conn1.Query<BearerToken>(query, new
                {
                    USERCODE = dto.usercode,
                    USRPASS = encrytedPw
                }).FirstOrDefault();

                if (newToken == null) // KTC van sell
                {
                    query = @$"SELECT T0.CompanyId [companyId] 
                            FROM {db.WEBDB}..USERS T0 with (nolock) 
                            WHERE
                                ISNULL(T0.INACTIVE,'') <> 'I'
                                AND T0.USERCODE = @USERCODE 
                                AND ISNULL(T0.USRPASS,'') = @USRPASS";

                    newToken = conn1.Query<BearerToken>(query, new
                    {
                        USERCODE = dto.usercode,
                        USRPASS = encrytedPw
                    }).FirstOrDefault();
                }

                var replied = new Dto_BreadLogin();

                // query the user object 
                query = @$"Select '{db.COMPANYNAME}' [Subsi] , * from {db.WEBDB}..USERS Where UserCode = @UserCode";
                replied.User = conn1.Query<Bread_User>(query,
                    new { UserCode = dto.usercode }).FirstOrDefault();

                // query the list list if SSOs from db 
                //query = $@"select * from {db.COMMONDB}..FTAPP_SSO where UserCode = @UserCode";

                // loop each company to find user code
                // for ease swaping 
                var companies = new DbNameHelper().GetDbInfos(_commDbConnStr_Bread);
                var ssoList = new List<Bread_UserSSO>();
                query = @"exec sp_QueryUserSSO @webDb, @userCode ";

                for (int c = 0; c < companies.Count; c++)
                {
                    var company = companies[c];
                    if (company == null) continue;

                    var sso = conn1.Query<Bread_UserSSO>(query,
                        new
                        {
                            webDb = company.WEBDB,
                            userCode = dto.usercode
                        }).FirstOrDefault();

                    if (sso == null) continue;
                    ssoList.Add(sso);
                }

                replied.Sso = ssoList;

                // get the app setting from ktcw_common
                var configQuery = "Select * from FTApp_Config with (nolock)";

                var conn2 = new SqlConnection(_commDbConnStr);
                replied.AppConfigs = conn2.Query<FTApp_Config>(configQuery).ToList();
                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}



//IActionResult CheckIsTransporterLogin(PortalLogin_dto dto)
//{
//    try
//    {
//        var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.company);
//        if (db == null)
//        {
//            return BadRequest("invalid dbi, bread login with KIM portal");
//        }

//        var query = $@"exec sp_LoadTransportersTruck @webDb_bread, @truckNo, @cardCode";
//        var conn = new SqlConnection(_commDbConnStr_Bread);
//        var truck = conn.Query<Bread_Truck>(query,
//            new
//            {
//                webDb_bread = db.WEBDB,
//                truckNo = dto.password,
//                cardCode = dto.usercode
//            }).FirstOrDefault();

//        if (truck == null) return NotFound();

//        // create the Bread_User 

//        var newUser = new Bread_User
//        {
//            //USERID = truck.
//        };

//        // create the Bread_UserSSO 
//        var newUserSSo = new Bread_UserSSO
//        {
//            UserCode = truck.CardCode,
//            UserName = truck.CardName,
//            UserPassword = "",
//            UserDisplayName = truck.CardName,
//            UserCompany = db.COMPANYID,
//            UserCompanyRef = db.WEBDB,
//            LastUpdate = DateTime.Now,
//            UserCompanyErpRef = db.SAPDB,
//            SlpCode = -1,
//            SlpName = "",
//            Memo = "",
//            SourceType ="SVR",
//            UserCompanyID = db.COMPANYID, 
//            UCompanyId = db.COMPANYID,
//            UUserGroup = "U",
//            UUserName = truck.CardName,
//            UIsActive = "Y",
//            UDefWhs = "", // get the transporter default whs
//            UserType = "TR",
//            DICardCode = ""
//        };

//        var replied = new Dto_BreadLogin();

//        replied.User = newUser;
//        var ssolist = new List<Bread_UserSSO>();

//        ssolist.Add(newUserSSo);
//        replied.Sso = ssolist;

//        // get the app setting from ktcw_common
//        var configQuery = "Select * from FTApp_Config with (nolock)";
//        var conn2 = new SqlConnection(_commDbConnStr);
//        replied.AppConfigs = conn2.Query<FTApp_Config>(configQuery).ToList();
//        return Ok(replied);
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}


// sample code for checking bread user login 
//protected void btnLogin_Click(object sender, EventArgs e)
//{
//    try
//    {
//        string sapdb = AppUtilities.getSAPDB();
//        if (txtUser.Value == "")
//        {
//            lblError.Text = "User ID is empty";
//            return;
//        }
//        using (SqlConnection conn = new SqlConnection(AppUtilities.getConnectionString()))
//        {
//            using (SqlDataAdapter dat = new SqlDataAdapter("", conn))
//            {
//                string userid = txtUser.Value;
//                string pass = txtPass.Value;
//                if (pass.Length > 0) pass = AppUtilities.encriptMD5String(pass);

//                dat.SelectCommand.CommandText = "SELECT T0.*, ISNULL(T1.CARDNAME,'') AS [CARDNAME], " +
//                    "(SELECT TOP 1 ComPnyName FROM [" + sapdb + "].[DBO].[OADM]) AS [CRNAME] " +
//                    "FROM USERS T0 " +
//                    "LEFT OUTER JOIN [" + sapdb + "].[DBO].[OCRD] T1 ON T1.CARDCODE = T0.COMPANYID AND t1.CARDTYPE = 'C' " +
//                    "WHERE T0.USERCODE = @1 AND ISNULL(T0.USRPASS,'') = @2 AND ISNULL(T0.INACTIVE,'') <> 'I' ";
//                dat.SelectCommand.Parameters.Clear();
//                dat.SelectCommand.Parameters.AddWithValue("@1", userid);
//                dat.SelectCommand.Parameters.AddWithValue("@2", pass);
//                DataTable dt = new DataTable();
//                dat.Fill(dt);
//                if (dt.Rows.Count > 0)
//                {
//                    Session["USERCODE"] = dt.Rows[0]["USERCODE"].ToString();
//                    Session["COMPANYID"] = dt.Rows[0]["COMPANYID"].ToString();
//                    Session["USERGROUP"] = dt.Rows[0]["USERGROUP"].ToString();
//                    Session["USERID"] = dt.Rows[0]["USERID"].ToString();
//                    string cardname = dt.Rows[0]["CARDNAME"].ToString();
//                    if (cardname == "") cardname = dt.Rows[0]["CRNAME"].ToString();
//                    Session["COMPANYNAME"] = cardname;

//                    Response.Redirect("~/Default.aspx");
//                }
//                else
//                {
//                    lblError.Text = "Invalid user and/or Password";
//                }
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        lblError.Text = ex.Message;
//    }
//}