using System;

namespace KTC_SalesAppWAPI.Models.TrcukInspection
{
    public class FTAPP_TruckInspection
    {
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public string DriverCode { get; set; }
        public string DriverName { get; set; }
        public string TruckNo { get; set; }
        public DateTime Date { get; set; }
        public string DocStatus { get; set; }

        public string Files { get; set; } // for signed doc 
        public string Remarks { get; set; }

    }
}
