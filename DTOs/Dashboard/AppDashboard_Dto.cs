using KTC_SalesAppWAPI.Models.Dashboard;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Dashboard
{
    public class AppDashboard_Dto
    {
        // request part
        public string SlpName { get; set; }
        public string Request { get; set; }
        public string UserIdCode { get; set; }   
        public string QueryCurrency { get; set; } // for sales and collection in near future
        public List<string> Companies { get; set; } // indicate the sap db name
    }
}
