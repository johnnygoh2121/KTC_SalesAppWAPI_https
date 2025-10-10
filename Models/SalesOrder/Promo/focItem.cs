namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class focItem
    {
        public string id { get; set; }
        public long PromoEntry { get; set; }
        public int PromoLine { get; set; }
        public int MBID { get; set; }
        public string PromoType { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string CodeBars { get; set; }
        public decimal FOCQty { get; set; }
        public decimal Onhand { get; set; }
        public decimal Price { get; set; }
        public decimal CartonUomRate { get; set; }
        public string BORNE { get; set; }
        public decimal BALANCE { get; set; }
        public decimal PACKINGFOCQTY { get; set; }
        public decimal SUPP { get; set; }
        public decimal OWN { get; set; }
        public string PACK { get; set; }
        public string SUPPCATNUM { get; set; }
    }
}
