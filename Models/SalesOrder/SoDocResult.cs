namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class SoDocResult
    {
        public bool actionSuccess { get; set; }
        public string errorMessage { get; set; }
        public string actionResult { get; set; }
        public string documentStatus { get; set; }
        public string updateDocType { get; set; } // draft, submit update o
        public string docType { get; set; } // indicate so, return or payment 

        // for picking 
        public string INVNO { get; set; } // get the invoice number

        // 20211005
        public SO SalesOrder { get; set; } // for print the sales order and it line 
    }
}
