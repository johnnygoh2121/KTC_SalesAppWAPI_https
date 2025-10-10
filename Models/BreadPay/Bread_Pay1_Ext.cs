using System;

namespace KTC_SalesAppWAPI.Models.BreadPay
{
    public class Bread_Pay1_Ext
    {
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public long BASEENTRY { get; set; }
        public string BASETYPE { get; set; }
        public string BASEDOCNUM { get; set; }
        public DateTime BASEDOCDATE { get; set; }
        public string BASEREFNO { get; set; }
        public decimal BASETOTAL { get; set; }
        public decimal DOCAMOUNT { get; set; }
        public long TRANSID { get; set; }
        public int TRANSLINE { get; set; }
        public string OBJECTCODE { get; set; }
        public string SEL { get; set; }
        public decimal BANKAMT { get; set; }
    }
}
