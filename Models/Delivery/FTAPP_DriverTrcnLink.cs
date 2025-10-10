using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DriverTrcnLink
    {

        public int id { get; set; }
        public int DLbEntry { get; set; }
        public int BaseDocNum { get; set; }
        public string BaseDocType { get; set; }
        public int CnDocNum { get; set; }
        public DateTime TransDt { get; set; }
        public int RtnEntry { get; set; }

    }
}
