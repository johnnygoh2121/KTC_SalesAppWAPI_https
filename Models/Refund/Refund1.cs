using System;


namespace KTC_SalesAppWAPI.Models.Refund
{
    public class Refund1
    {
        public long DocEntry { get; set; }
        public int LineNum { get; set; }
        public string LineType { get; set; }
        public string LineBankCode { get; set; }
        public string BankAcNo { get; set; }
        public string LineRefNo { get; set; }
        public DateTime? LineDate { get; set; }
        public decimal? LineAmt { get; set; }
        public int? HouseBankAbs { get; set; }
        public string GlAccount { get; set; }
    }
}
