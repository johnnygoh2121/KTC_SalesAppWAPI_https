using System;

namespace KTC_SalesAppWAPI.Models.StockCount
{
    public class FTAPP_InStoreStockCountLine
    {
        public int id { get; set; }
        public string ProdCode { get; set; }
        public string ProdName { get; set; }
        public int ExpiryInterval { get; set; }
        public DateTime ManufactureDt { get; set; }
        public DateTime ExpiryDt { get; set; }
        public decimal DamageQty { get; set; }
        public decimal SellableQty { get; set; }
        public decimal TotalQty { get; set; }
        public string LineGuid { get; set; }
        public string HeadGuid { get; set; }
        public int CountSeq { get; set; }
        public DateTime CountedDt { get; set; }
        public string CounterName { get; set; }
        public string Remarks { get; set; }
    }
}
