using System;

namespace KTC_SalesAppWAPI.Models.WebPortal
{
    public class FTAPP_JsSentLog
    {
        public int Id { get; set; }
        public string Endpoint { get; set; }
        public string JSonValue { get; set; }
        public string Module { get; set; }
        public string DocType { get; set; }
        public int DocNum { get; set; }
        public int DocEntry { get; set; }
        public string Token { get; set; }
        public DateTime TransDt { get; set; }
    }
}
