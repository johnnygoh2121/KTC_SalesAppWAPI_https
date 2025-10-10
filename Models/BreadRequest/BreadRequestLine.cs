using System;

namespace KTC_SalesAppWAPI.Models.BreadRequest
{
    public class BreadRequestLine
    {
        public int Id { get; set; }
        public Guid HeadGuid { get; set; }
        public Guid LineGuid { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal RequestQty { get; set; }
        public string Remarks { get; set; }
        public int LineNum { get; set; }
    }
}
