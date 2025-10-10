using System;

namespace KTC_SalesAppWAPI.Models.COG.ReturnMemoF
{
    public class ReturnCogResult
    {
        // for app usage
        public int id { get; set; }
        public string Guid { get; set; }
        public string Subsi { get; set; }

        // portal return result
        public bool actionSuccess { get; set; }
        public string errorMessage { get; set; }
        public string actionResult { get; set; }
        public string documentStatus { get; set; }
        public string updateDocType { get; set; } // draft, submit update o
        public string docType { get; set; } // indicate so, return or payment 

        // app usage
        public string storeName { get; set; } // app usage
        public string storeCard { get; set; } // app usage
        public string companyName { get; set; } // app usage
        public DateTime docDate { get; set; } // app usage

        // for return credit memo 
        public string CreditMemoDocNum { get; set; }

        // for GI 
        public string GIDocNum { get; set; }
        public decimal HrChargeAmt { get; set; }
        public string ChargedUserCode { get; set; }
        public string ChargedUserName { get; set; }
    }
}
