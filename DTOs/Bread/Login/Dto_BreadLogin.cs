using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.Bread;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Bread
{
    public class Dto_BreadLogin
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string UserCode { get; set; }
        public string CardType { get; set; }
        public string WildCode { get; set; }
        public Bread_User User { get; set; }
        public List< Bread_UserSSO> Sso { get; set; } // single sign on 
        public List<FTApp_Config> AppConfigs { get; set; }
    }
}
