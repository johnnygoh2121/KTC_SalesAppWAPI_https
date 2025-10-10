using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class Charge_Code
    {
        public string Sel { get; set; }
        public int DocEntry { get; set; }
        public string Agency { get; set; }
        public string Brand { get; set; }
        public string ChargeCode { get; set; }
        public string BOP { get; set; }
        public double Budget { get; set; }
        public double Usage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Terminate { get; set; }
        public string UCreated { get; set; }
        public DateTime DCreated { get; set; }
        public string UModified { get; set; }
        public DateTime DModified { get; set; }
        public string GroupCode {get; set;}
        public string BrandName { get; set; }
        public double Balance { get; set; }
    }
}
