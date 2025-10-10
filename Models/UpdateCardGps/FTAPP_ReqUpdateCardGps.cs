using System;

namespace KTC_SalesAppWAPI.Models.UpdateCardGps
{
    public class FTAPP_ReqUpdateCardGps
    {

        public int id { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public decimal CurrentLatitude { get; set; }
        public decimal CurrentLogitude { get; set; }
        public decimal NewLatitude { get; set; }
        public decimal NewLongitude { get; set; }
        public DateTime RequestDt { get; set; }
        public string UpdateStatus { get; set; }
        public string CompanyName { get; set; }
        public string CompanyId { get; set; }
    }
}
