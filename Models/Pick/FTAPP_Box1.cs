using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class FTAPP_Box1
    {
        public int id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public string Packaging { get; set; }
        public Guid BoxGuid { get; set; }
        public Guid ContentGuid { get; set; }
        public int BaseEntry { get; set; }
        public int BaseLine { get; set; }
    }
}
