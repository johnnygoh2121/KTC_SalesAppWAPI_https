using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_HoldDlvryDocs // holding the invoice to prevent other scan in while in out -transist time
    {
        public int id { get; set; }
        public string DocNum { get; set; }
        public string DocType { get; set; }
        public DateTime HoldDt { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string Reason { get; set; }
        public Guid HeadGuid { get; set; } 
        public int DlbEntry { get; set; }
    }
}
