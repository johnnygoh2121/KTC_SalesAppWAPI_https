using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DriverLog
    {
        public int Id { get; set; }
        public string ReportAs { get; set; }
        public DateTime TransDt { get; set; }
        public long DlbEntry { get; set; }
        public long DocNum { get; set; }
        public string DocType { get; set; }
        public string Reason { get; set; }
        public string TruckNo { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DriverName { get; set; }
    }
}
