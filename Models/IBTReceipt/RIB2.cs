using System;

namespace KTC_SalesAppWAPI.Models.IBTReceipt
{
    public class RIB2
    {
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public int LINENUM2 { get; set; }
        public string BATCHNO { get; set; }
        public decimal QUANTITY { get; set; }
        public DateTime EXPDATE { get; set; }
        public DateTime MFRDATE { get; set; }
        public DateTime EXPIREDDATE { get; set; }

    }
}
