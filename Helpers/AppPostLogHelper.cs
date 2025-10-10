using Dapper;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.CommonDb;
using System;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Helpers
{
    public class AppPostLogHelper
    {
        public string LastError { get; set; }

        public void Create(string connStr, FTAPP_AppPostLog line)
        {
            var Db = new DbNameHelper().GetDbInfo(connStr, line.SubSi);
            if (Db == null)
            {
                return;
            }

            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var insertSql = @$"INSERT INTO [{Db.WEBDB}].[dbo].[FTAPP_AppPostLog] (
                                     AppModule
                                   , UserCode
                                   , CardCode
                                   , SubSi
                                   , TransDt
                                   , Details
                                   , PostResult 
                                   , AppVersion
                                    ) values (
                                    @AppModule
                                   ,@UserCode
                                   ,@CardCode
                                   ,@SubSi
                                   ,GETDATE()
                                   ,@Details
                                   ,@PostResult 
                                   ,@AppVersion
                                )";

                var insertRes = conn.Execute(insertSql, line, trans);
                trans.Commit();
                return;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                return;
            }
        }

        public bool IsCreated(FTAPP_AppPostLog line, DbInfo db, SqlConnection conn, SqlTransaction trans)
        {                        
            try
            {
                var insertSql = @$"INSERT INTO [{db.WEBDB}..FTAPP_AppPostLog (
                                     AppModule
                                   , UserCode
                                   , CardCode
                                   , SubSi
                                   , TransDt
                                   , Details
                                   , PostResult 
                                   , AppVersion
                                    ) values (
                                    @AppModule
                                   ,@UserCode
                                   ,@CardCode
                                   ,@SubSi
                                   ,GETDATE()
                                   ,@Details
                                   ,@PostResult 
                                   ,@AppVersion
                                )";

                var insertRes = conn.Execute(insertSql, line, trans);
                if (insertRes == 1) return true;

                LastError = "Insert log record with return 0 row effected";
                return false;
            }
            catch (Exception e)
            {   
                LastError = $"{e.Message}\n{e.StackTrace}";
                return false;
            }
        }

    }
}
