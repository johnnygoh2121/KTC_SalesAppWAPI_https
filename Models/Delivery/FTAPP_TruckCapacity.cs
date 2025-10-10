namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_TruckCapacity
    {
        public int id { get; set; }
        public string Plate { get; set; }
        public string Company { get; set; } // transporter card name
        public string CompanyCode { get; set; } // transporter card code
        public string SubSi { get; set; }
        public string SubsiID { get; set; }
        public string Capacity { get; set; }
        public int MaxItemQty { get; set; }
        public int TolerantQty { get; set; }
        public int AvailItemQty { get; set; }
    }
}
