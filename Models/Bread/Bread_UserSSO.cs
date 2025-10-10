using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class Bread_UserSSO
    {
        public int id { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string UserDisplayName { get; set; }
        public string UserCompany { get; set; }
        public string UserCompanyRef { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UserCompanyErpRef { get; set; }
        public int SlpCode { get; set; }
        public string SlpName { get; set; }
        public string Memo { get; set; }
        public string SourceType { get; set; }
        public string UserCompanyID { get; set; }
        public string UCompanyId { get; set; }
        public string UUserGroup { get; set; }
        public string UUserName { get; set; }
        public string UIsActive { get; set; }
        public string UDefWhs { get; set; }

        public string UserType { get; set; }
        public string DICardCode { get; set; }
    }
}
