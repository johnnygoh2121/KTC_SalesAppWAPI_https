using System;

namespace KTC_SalesAppWAPI.DTOs.WebSo
{
    public class WebSO_Dto
    {
        public string Request { get; set; }  
        public string CompanyName { get; set; }
        public string UserCode { get; set; }
        public string SlpCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public int DocEntry { get; set; }
    }
}
