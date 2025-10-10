using System;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class COGResult
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
    }
}
