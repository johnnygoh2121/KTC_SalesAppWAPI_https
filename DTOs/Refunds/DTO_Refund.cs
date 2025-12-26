using KTC_SalesAppWAPI.Models.Refund;
using System;

namespace KTC_SalesAppWAPI.DTOs.Refunds
{
    public class DTO_Refund
    {
        public string Request {  get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public string SubSi { get; set; }      
        public string UserCode { get; set; }

        // for posting 
        public string RequestName { get; set; }
        public string DocUpdateType { get; set; }
        public string QueryKeys { get; set; }
        public Refund RefundDoc { get; set; }

    }
}
