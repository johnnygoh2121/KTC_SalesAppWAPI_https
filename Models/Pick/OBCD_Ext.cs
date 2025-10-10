using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class OBCD_Ext
    {
        public int BcdEntry { get; set; }
        public string BcdCode { get; set; }
        public string BcdName { get; set; }
        public string ItemCode { get; set; }
        public int UomEntry { get; set; }
        public string DataSource { get; set; }
        public short UserSign { get; set; }
        public int LogInstanc { get; set; }
        public short UserSign2 { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
