namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_Batch
    {
        public long DocEntry { get; set; }
        public int LineNum { get; set; }
        public int LineNum2 { get; set; }
        public string BatchNo { get; set; }
        public decimal Quantity { get; set; }
        public string TableName { get; set; }

    }
}
