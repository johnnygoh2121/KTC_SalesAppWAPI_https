using Dapper;
using KTC_SalesAppWAPI.Models.CommonDb;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers
{
    // vater better web security on direct using the db name
    public class DbNameHelper : IDisposable
    {
        public string LastError { get; set; }
        public DbNameHelper() { }
        public DbInfo GetDbInfoById(string commnDbConstr, string companyId)
        {
            try
            {
                var sql = @" select *
                                FROM DBInfoView WITH (NOLOCK) 
                              where CompanyID = @companyId";

                using var conn = new SqlConnection(commnDbConstr);
                return conn.Query<DbInfo>(sql, new { companyId }).FirstOrDefault();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        public DbInfo GetDbInfo(string commnDbConstr, string companyName)
        {
            try
            {
                var sql = @"select *
                                FROM DBInfoView WITH (NOLOCK) 
                              where CompanyName = @companyName";

                using var conn = new SqlConnection(commnDbConstr);
                return conn.Query<DbInfo>(sql, new { companyName }).FirstOrDefault();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        public List<DbInfo> GetDbInfo_DeliveryApp(string commnDbConstr)
        {
            var returnList = new List<DbInfo>();
            try
            {
                var sql = @"select *
                            from DBInfo WITH (NOLOCK)                               
                            where DeliveryAppDeployed = @DeliveryAppDeployed";

                using var conn = new SqlConnection(commnDbConstr);
                var results =  conn.Query<DbInfo>(sql, new
                {                    
                    DeliveryAppDeployed = "Y"
                }).ToList();

                if (results.Count == 0)
                {
                    return returnList;
                }

                returnList.AddRange(results);
                return returnList;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return returnList;
            }
        }

        public List<DbInfo> GetDbInfos(string commnDbConstr)
        {
            var returnList = new List<DbInfo>(); // always return a list without null
            try
            {
                var sql = @"SELECT  * FROM DBInfoView WITH (NOLOCK) ";

                using var conn = new SqlConnection(commnDbConstr);
                var list = conn.Query<DbInfo>(sql).ToList();
                if (list.Count == 0) return returnList;

                returnList.AddRange(list);
                return returnList;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return returnList;
            }
        }

        public void Dispose() { }
    }
}
