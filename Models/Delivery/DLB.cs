using SAPbobsCOM;
using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class DLB
    {
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string TRUCKNO { get; set; }
        public string BOP { get; set; }
        public string REMARKS { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }

        public string DocStatusDisplay { get; set; }

        // 20250808
        
        public bool ISINTERBRANCH { get; set; }

        // 20250930
        public string PICKEDWHS { get; set; }
    }
}
