using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class PortalPayment
    {
        public string SubSi { get; set; }

        // original
        public int docentry { get; set; }
        public int docnum { get; set; }
        public DateTime docdate { get; set; }
        public string docstatus { get; set; }
        public string cardcode { get; set; }
        public string cardname { get; set; }
        public string refno { get; set; }
        public string refnofile { get; set; }
        public string doc { get; set; }
        public string docfile { get; set; }
        public string collector { get; set; }        
        public double doctotal { get; set; }
        public double? checkedamt { get; set; }
        public double? confirmamt { get; set; }
        public double? cancelamt { get; set; }
        public double? postamt { get; set; }
        public string ucreated { get; set; }
        public DateTime dcreated { get; set; }
        public string umodified { get; set; }
        public DateTime dmodified { get; set; }
        public string initno { get; set; }
        public string checkedby { get; set; }
        public DateTime? checkeddate { get; set; }
        public int? batchid { get; set; }
        public List<Payment> payments { get; set; }
        public List<Document> documents { get; set; }

        // for app usage 
        public string guid { get; set; }
    }
}
