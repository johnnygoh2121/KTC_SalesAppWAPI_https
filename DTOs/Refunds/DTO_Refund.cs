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

    }
}
