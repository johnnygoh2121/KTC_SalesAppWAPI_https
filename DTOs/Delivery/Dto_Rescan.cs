using KTC_SalesAppWAPI.Models.Delivery;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Delivery
{
    public class Dto_Rescan
    {
        public string Request { get; set; }
        public string DocNum { get; set; } // for invoice, cog and transfer
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public List<FTAPP_DLB1> Dlb1 { get; set; } //  
        public FTAPP_DLB Dlb { get; set; } // for invoice  head
        public string DriverName { get; set; }
        public string PlateNo { get; set; }
        public Guid SaveHeadGuid { get; set; }
        public string DocType { get; set; }

        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string Remarks { get; set; }
        public string Nric { get; set; }

        public Guid HeadGuid { get; set; }

        // 20240207
        public bool IsAgedInvoice { get; set; }

        public bool IsInterbranch { get; set; }
    }
}
