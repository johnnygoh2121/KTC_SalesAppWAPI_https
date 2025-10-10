using KTC_SalesAppWAPI.Models.CreditApplication;
using System;

namespace KTC_SalesAppWAPI.DTOs.CreditApplication
{
    public class Dto_CreditApplication
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string UserCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public CUST SavedCust { get; set; } // for saving credit app for submit or draft 
        public string UpdateType { get; set; } // save submit or draft
        public string QueryKeys { get; set; } // for posting to pb portal

    }
}
