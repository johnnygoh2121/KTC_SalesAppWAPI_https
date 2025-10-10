namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    // for fill in the inv lines details
    public class Bread_ItemDetail
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string  BillAdd1 { get; set; }
        public string BillAdd2 { get; set; }
        public string BillAdd3 { get; set; }
        public string  BillAdd4 { get; set; }
        public string BillAdd5 { get; set; }
        public string Contact { get; set; }
        public string Currency { get; set; }
        public string Fax { get; set; }
        public string Tel { get; set; }
        public int PriceId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public string TaxCode { get; set; }
        public int TaxRate { get; set; }
    }
}
