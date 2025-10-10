using Dapper;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Login;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers
{
    public class DataRefreshHelper
    {
        public string LastError { get; set; }
        string ConnectionString { get; set; }
        ILogger Logger { get; set; }

        public DataRefreshHelper(ILogger logger, string DbConnectionString)
        {
            Logger = logger; 
            ConnectionString = DbConnectionString;
        }

        public void LoadUserToFromAllSubsiDb()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                // truncate the table first                 
                conn.Execute("TRUNCATE TABLE FTAPP_SSO");

                // load all db connection
                var companies = conn.Query<DbInfo>("SELECT * FROM DBINFOView WITH (NOLOCK) ").ToList();
                if (companies == null) return;

                // for each load each company
                for (int c = 0; c < companies.Count; c++)
                {
                    var company = companies[c];
                    // query active user
                    // preset the password to 1234
                    //var spName = "exec sp_SelectSSOUsers 'Kim Teck Cheong Sdn Bhd', 'KTCW_KK', 'KTC100101_SAP', '5555'"
                    var spName = "exec sp_SelectSSOUsers @companyName, @webDb, @erpDb, @appPassword, @userCompanyId";
                    var users = conn.Query<USERS>(spName, new
                    {
                        companyName = company.COMPANYNAME,
                        webDb = company.WEBDB,
                        erpDb = company.SAPDB,
                        appPassword = "",
                        userCompanyId = company.COMPANYID
                    }).ToList();

                    // insert into sso table
                    if (users != null)
                    {
                        var insertSql = "INSERT INTO FTAPP_SSO ( " +
                                        "UserCode" +
                                        ", UserName" +
                                        ", UserPassword" +
                                        ", UserDisplayName" +
                                        ", UserCompany" +
                                        ", UserCompanyRef" +
                                        ", UserCompanyErpRef " +
                                        ", SlpCode " +
                                        ", SlpName " +
                                        ", Memo " +
                                        ", SourceType" +
                                        ", LastUpdate" +
                                        ", UserCompanyID" +
                                        ") VALUES (" +
                                        "  @UserCode" +
                                        ", @UserName" +
                                        ", @UserPassword " +
                                        ", @UserName" +
                                        ", @UserCompany" +
                                        ", @UserCompanyRef" +
                                        ", @UserCompanyErpRef" +
                                        ",@SlpCode" +
                                        ",@SlpName" +
                                        ",@Memo" +
                                        ",@SourceType" +
                                        ", GETDATE()" +
                                        ",@UserCompanyID" +
                                        ") ";

                        conn.Execute(insertSql, users);
                    }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                Logger.LogError(LastError);
            }
        }

        // 20121208T1637
        public void LoadUserToFromAllSubsiDb_Bread()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                // truncate the table first                 
                conn.Execute("TRUNCATE TABLE FTAPP_SSO");

                // load all db connection
                var companies = conn.Query<DbInfo>("SELECT * FROM DBINFOView WITH (NOLOCK) ").ToList();
                if (companies == null) return;

                // for each load each company
                for (int c = 0; c < companies.Count; c++)
                {
                    var company = companies[c];
                    // query active user
                    // preset the password to 1234
                    //var spName = "exec sp_SelectSSOUsers 'Kim Teck Cheong Sdn Bhd', 'KTCW_KK', 'KTC100101_SAP', '5555'"
                    var spName = "exec sp_SelectSSOUsers @companyName, @webDb, @erpDb, @appPassword, @userCompanyId";
                    var users = conn.Query<USERS>(spName, new
                    {
                        companyName = company.COMPANYNAME,
                        webDb = company.WEBDB,
                        erpDb = company.SAPDB,
                        appPassword = "",
                        userCompanyId = company.COMPANYID
                    }).ToList();

                    // insert into sso table
                    if (users != null)
                    {
                        var insertSql = "INSERT INTO FTAPP_SSO ( " +
                                        "UserCode" +
                                        ", UserName" +
                                        ", UserPassword" +
                                        ", UserDisplayName" +
                                        ", UserCompany" +
                                        ", UserCompanyRef" +
                                        ", UserCompanyErpRef " +
                                        ", SlpCode " +
                                        ", SlpName " +
                                        ", Memo " +
                                        ", SourceType" +
                                        ", LastUpdate" +
                                        ", UserCompanyID" +
                                        ", UCompanyId" +
                                        ", UUserGroup" +
                                        ", UUserName" +
                                        ", UIsActive" +
                                        ", UDefWhs" +
                                        ") VALUES (" +
                                        "  @UserCode" +
                                        ", @UserName" +
                                        ", @UserPassword " +
                                        ", @UserName" +
                                        ", @UserCompany" +
                                        ", @UserCompanyRef" +
                                        ", @UserCompanyErpRef" +
                                        ",@SlpCode" +
                                        ",@SlpName" +
                                        ",@Memo" +
                                        ",@SourceType" +
                                        ", GETDATE()" +
                                        ",@UserCompanyID" +
                                        ",@UCompanyId" +
                                        ",@UUserGroup" +
                                        ",@UUserName" +
                                        ",@UIsActive" +
                                        ",@UDefWhs" +
                                        ") ";

                        conn.Execute(insertSql, users);
                    }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                Logger.LogError(LastError);
            }
        }
    }
}

