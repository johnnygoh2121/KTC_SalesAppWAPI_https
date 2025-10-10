using System;

namespace KTC_SalesAppWAPI.DTOs.BaseRoute
{
    public class DTO_BaseRoute
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserCode { get; set; } // portal user code
 
    }
}
