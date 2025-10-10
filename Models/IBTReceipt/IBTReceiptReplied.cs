namespace KTC_SalesAppWAPI.Models.IBTReceipt
{
    public class IBTReceiptReplied
    {
        public bool ActionSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public string ActionResult { get; set; }
        public string DocumentStatus { get; set; }

        public string DocEntry { get; set; }
    }
}
