namespace KTC_SalesAppWAPI.Models.Payment
{
    public class PayDocResult
    {
        public bool actionSuccess { get; set; }
        public string errorMessage { get; set; }
        public string actionResult { get; set; }
        public string documentStatus { get; set; }
        public string updateDocType { get; set; } // draft, submit update o
        public string docType { get; set; } // indicate so, return or payment 
    }
}
