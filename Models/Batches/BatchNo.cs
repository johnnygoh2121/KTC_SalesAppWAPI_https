using System;

namespace KTC_SalesAppWAPI.Models.Batches
{
    /// <summary>
    /// For posting the pick with batch number 
    /// </summary>
    public class BatchNo // for posting 
    {
        public long Docentry { get; set; }
        public int Linenum { get; set; }
        public int Linenum2 { get; set; }
        public string Batchno { get; set; }
        public decimal Quantity { get; set; }
        public  DateTime? ExpiredDate { get; set; }
        public DateTime? MfrDate { get; set; }
    }
}
