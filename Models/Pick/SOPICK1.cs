using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class SOPICK1
    {
        public long ID { get; set; }
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string REFITEM { get; set; }
        public string PICKLISTNO { get; set; }
        public string BIN { get; set; }
        public DateTime EXPIRED { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal WEIGHT { get; set; }
    }
}
