using System;

namespace KTC_SalesAppWAPI.Models.BreadRequest
{
    public class BreadRequestHead
    {
        public int Id { get; set; }
        public DateTime DeliveryDate { get; set; } // date of DO
        public DateTime DocDate { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public Guid HeadGuid { get; set; }
        public string Remarks { get; set; }
        public string SubSi { get; set; }
        public string SubSiId { get; set; }

        public int LineCount { get; set; }

    }
}
