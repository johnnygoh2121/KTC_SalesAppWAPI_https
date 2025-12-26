namespace KTC_SalesAppWAPI.Models.Refund
{
    public class RefundResult
    {


        public bool ActionSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ActionResult { get; set; }
        public string DocumentStatus { get; set; } // nullable, so string is fine
        public int DocEntry { get; set; }



    }
}
