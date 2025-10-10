using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class PAFResult
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

        // 20211029
        // add in the created CN number
        public int CnDocNum { get; set; }
        public int CnDocEntry { get; set; }
    }
}
