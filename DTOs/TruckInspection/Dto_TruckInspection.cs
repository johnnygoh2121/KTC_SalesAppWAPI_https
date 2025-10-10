using KTC_SalesAppWAPI.Models.TrcukInspection;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.TruckInspection
{
    public class Dto_TruckInspection
    {
        public string Request { get; set; }

        public string Subsi { get; set; }

        public string DriverCode { get; set; }

        public string TruckNo { get; set; }

        public DateTime StartDt { get; set; }

        public DateTime EndDt { get; set; }

        public int DocEntry { get; set; } // Inspection

        public string SignedFiles { get; set; } // files in comma

        public FTAPP_TruckInspection  Head { get; set; }

        public List<FTAPP_TruckInspection1> Details { get; set; }
    }
}
