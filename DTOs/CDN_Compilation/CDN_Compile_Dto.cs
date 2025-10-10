using System;

namespace KTC_SalesAppWAPI.DTOs.CDN_Compilation
{
    public class CDN_Compile_Dto
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
