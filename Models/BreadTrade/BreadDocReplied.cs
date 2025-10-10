namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class BreadDocReplied
    {
        public long DocEntry { get; set; }
        public bool IsSuccess { get; set; }
        public string DocNum { get; set; }
        public string LastErrorMessage { get; set; }
    }
}
