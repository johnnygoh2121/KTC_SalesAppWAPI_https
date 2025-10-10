namespace KTC_SalesAppWAPI.Models.Pick
{
    public class PickQtyVsBathQty
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity {get; set;} 
        public decimal InvQty { get; set; }
        public decimal PickedQty { get; set; }
        public decimal BatchQty { get; set; }
    }
}
