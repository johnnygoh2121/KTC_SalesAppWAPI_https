using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.CommonDb;
using System.Data.SqlClient;
using System.Threading;
using System;
using KTC_SalesAppWAPI.Models.DN;
using Dapper;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Helpers
{
    public  static class BreadInvCnChecker
    {   public static string LastError { get; set; }
        public static string CommDbConnStr_Bread { get; set; }
        static int CheckLoopTime { get; set; } = 3;

        public static OINV GetPostedInv(DbInfo db, long portalDocEntry)
        {
            try
            {
                using var conn = new SqlConnection(CommDbConnStr_Bread);

                // by pass current locked row with READPAST
                var sp_QuerySapInv = $@"Select top 1 * 
                                        From {db.SAPDB}..OINV with (READPAST)
                                        Where U_SOENTRY = @portalDocEntry
                                        ORDER BY DocEntry DESC ; ";

                for (int i = 0; i < CheckLoopTime; i++)
                {
                    var found_Inv = conn.
                        Query<OINV>(sp_QuerySapInv, new { portalDocEntry }).FirstOrDefault(); ;

                    if (found_Inv != null)
                    {
                        return found_Inv;
                    }
                    Thread.Sleep(500);
                }
                return null;
            }
            catch (Exception except)
            {
                LastError = $"{except.Message}\n{except.StackTrace}";                
                return null;
            }
        }

        public static ORIN GetPostedCN(DbInfo db, long portalDocEntry)
        {
            try
            {
                using var conn = new SqlConnection(CommDbConnStr_Bread);
                // by pass current locked row with READPAST
                var sp_QuerySapInv = $@"Select top 1 * 
                                        From {db.SAPDB}..ORIN with (READPAST)
                                        Where U_SOENTRY = @portalDocEntry
                                        ORDER BY DocEntry DESC ; ";

                for (int i = 0; i < CheckLoopTime; i++)
                {
                    var found_Cn =  conn.Query<ORIN>(sp_QuerySapInv, new { portalDocEntry }).FirstOrDefault();

                    // first time 
                    if (found_Cn != null)
                    {
                        return found_Cn;
                    }
                    Thread.Sleep(500);
                }
                return null;
            }
            catch (Exception except)
            {
                LastError = $"{except.Message}\n{except.StackTrace}";
                return null;
            }
        }

    }
}
