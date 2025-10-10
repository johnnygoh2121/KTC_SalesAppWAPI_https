namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class MustBuyItem
    {
        public string id { get; set; }
        public int promoEntry { get; set; }
        public int promoLine { get; set; }
        public int mbid { get; set; }
        public string promoType { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string codeBars { get; set; }
        public double mustBuyQty { get; set; }
        public double onhand { get; set; }
        public double price { get; set; }
        public double cartonUomRate { get; set; }
        public double discount { get; set; }
        public double bornedBySupplier { get; set; }
        public double bornedByOwn { get; set; }
        public double packageQty { get; set; }
        public string isPackage { get; set; }
        public string suppCatNum { get; set; }
    }
}
