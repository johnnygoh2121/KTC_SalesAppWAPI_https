using KTC_SalesAppWAPI.Models.Pack;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.TPShipping
{
    public class Dto_TpShipping
    {
        public string SOID { get; set; }
        public string InvoiceNo { get; set; } // query invoice 
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string Studio { get; set; }
        public string OrderDate { get; set; }
        public string AppVersion { get; set; }

        public List<TP_PackedBoxInfo> Lines { get; set; }

        // for checking 
        public string PackedId { get; set; }
        public string OrderNo { get; set; }

        // for update 
        public string ShippingCartonNo { get; set; } // also for query the user card

        public string ScanInCode { get; set; }

        public int SoDocEntry { get; set; }

    }
}
