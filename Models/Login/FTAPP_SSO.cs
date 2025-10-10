using System;

namespace KTC_SalesAppWAPI
{
    public class FTAPP_SSO
    {
        public int id { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }

        //public string UserPassword { get; set; }
        public string UserDisplayName { get; set; }
        public string UserCompany { get; set; }
        public string UserCompanyRef { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UserCompanyErpRef { get; set; }
        public string SlpCode { get; set; } = "0";
        public string SlpName { get; set; }
        public string Memo { get; set; }
        public string SourceType { get; set; }
        public string UserCompanyID { get; set; }
    }
}
