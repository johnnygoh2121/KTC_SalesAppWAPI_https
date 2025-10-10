using KTC_SalesAppWAPI.Models.BreadRequest;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Bread
{
    public class Dto_BreaRequest
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public Guid HeadGuid { get; set; } // for line query
        public BreadRequestHead RequestHead { get; set; }
        public List<BreadRequestLine> RequestLine { get; set; }
        public string UserCode { get; set; }
    }
}
