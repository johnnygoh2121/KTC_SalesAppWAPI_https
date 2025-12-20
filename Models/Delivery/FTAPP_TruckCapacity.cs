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

        // 20251220
        public string Driver1_Device_Id { get; set; }
        public string Driver2_Device_Id { get; set; }
        public string Driver1_Guid { get; set; }
        public string Driver2_Guid { get; set; }
        public string Pass { get; set; }
        public string Pass2 { get; set; }
    }
}
