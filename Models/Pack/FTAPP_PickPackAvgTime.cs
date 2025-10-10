using System;

namespace KTC_SalesAppWAPI.Models.Pack
{
    public class FTAPP_PickPackAvgTime
    {
        public int id { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime TransDt { get; set; }
        public decimal TotalSecond { get; set; }
        public decimal TotalSku { get; set; }
        public decimal AvgValue { get; set; }
        public int DocEntry { get; set; }
        public string DataType { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
    }
}
