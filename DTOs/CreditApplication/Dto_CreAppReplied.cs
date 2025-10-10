namespace KTC_SalesAppWAPI.DTOs.CreditApplication
{
    public class Dto_CreAppReplied
    {
        public bool ActionSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ActionResult { get; set; }
        public string DocumentStatus { get; set; }

        public int DocEntry { get; set; }
    }
}
