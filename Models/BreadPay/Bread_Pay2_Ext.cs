using System;

namespace KTC_SalesAppWAPI.Models.BreadPay
{
    public class Bread_Pay2_Ext
    {
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string LINETYPE { get; set; }
        public string LINEREF { get; set; }
        public DateTime LINEDATE { get; set; }
        public string BANK { get; set; }
        public decimal TOTAL { get; set; }
        public string REMARKS { get; set; }
        public string BANK2 { get; set; }
        public DateTime? BANKDATE { get; set; }
        public string BANKUSER { get; set; }
        public DateTime UPDDATE { get; set; }
        public string CANCEL { get; set; }
        public string CONFIRM { get; set; }
        public string FILES { get; set; }
    }
}
