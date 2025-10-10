namespace KTC_SalesAppWAPI.Models.Login
{
    public class USERS
    {
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string UserDisplayName { get; set; }
        public string UserCompany { get; set; }
        public string UserCompanyRef { get; set; }
        public string UserCompanyErpRef { get; set; }        
        public string SlpCode { get; set; }
        public string SlpName { get; set; }
        public string Memo { get; set; }
        public string SourceType { get; set; }
        public string UserCompanyID { get; set; }
        // for bread biz 
        // 230121208
        public string UCompanyId { get; set; }
        public string UUserGroup { get; set; }
        public string UUserName { get; set; }
        public string UIsActive { get; set; }
        public string UDefWhs { get; set; }
    }
}
