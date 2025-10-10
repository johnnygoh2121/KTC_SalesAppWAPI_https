using Dapper;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Models.CommonDb
{
    public class DbInfo
    {
        public string COMPANYID { get; set; }
        public string COMPANYNAME { get; set; }
        public string WEBSERVER { get; set; }
        public string WEBDB { get; set; }
        public string WEBDBUSR { get; set; }
        public string WEBDBPASS { get; set; }
        public string COMMONDB { get; set; }
        public string SAPSERVER { get; set; }
        public string SAPDB { get; set; }
        public string SAPDBUSR { get; set; }
        public string SAPDBPASS { get; set; }
        public string SAPUSERNAME { get; set; }
        public string SAPPASSWORD { get; set; }
        public string SAPLICENSE { get; set; }
        public string TOPRINT { get; set; }
        public string INVFORMAT { get; set; }
        public string BUNDLEEMAIL { get; set; }
        public int SSLDAY { get; set; }
        public string NRA { get; set; }
        public int LP { get; set; }
        public int CP { get; set; }
        public string RECACCT { get; set; }
        public string WATUSER { get; set; }
        public string WATSLP { get; set; }
        public string GURUSER { get; set; }
        public string GURSLP { get; set; }
        public string CNACC { get; set; }

        public int DEF_PRICELIST { get; set; }
        public int SAP_DbType { get; set; } = 6;

        public string PostSvrAdressPort { get; set; }

        public string DeliveryAppDeployed { get; set; } // Y or N 

        public int LastDlbEntry { get; set; } // use for rescan the dlb entry

        // 20240331 , for checking whs jam condition 
        // Y to perform check 
        // N to by pass the jam check and work as per normal
        public string ISJAMWHS_CHECK { get; set; } 

        // 20220112
        // control the use of the end point in CDN auto cn {submit / budget}
        public string AUTOCDN_DEPLOYED { get; set; }

        // model with it method
        [JsonIgnore]
        public string LastError { get; set; }

        public DbInfo[] GetWebCompany (string dbConnStr)
        {
            try
            {              
                using var conn = new SqlConnection(dbConnStr);
                return conn.Query<DbInfo>("SELECT * FROM DbInfo").ToArray();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        public string GetErpDbConnStr() =>
        $"Server={SAPSERVER};Database={SAPDB};User Id={SAPDBUSR};Password={SAPDBPASS};";

        public string GetWebDbConnStr() =>
            $"Server={WEBSERVER};Database={WEBDB};User Id={WEBDBUSR};Password={WEBDBPASS};";

    }
}
