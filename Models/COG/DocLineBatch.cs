using System;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class DocLineBatch
    {
        public int LineNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BatchNum { get; set; }
        public decimal BatchQty { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime ManufacturingDate { get; set; }
    }
}
