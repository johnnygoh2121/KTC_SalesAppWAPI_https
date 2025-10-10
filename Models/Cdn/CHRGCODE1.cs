using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class CHRGCODE1
    {
        public int DOCENTRY { get; set; }
        public string AGENCY { get; set; }
        public string BRAND { get; set; }
        public string CHARGECODE { get; set; }
        public string BOP { get; set; }
        public decimal BUDGET { get; set; }
        public decimal USAGE { get; set; }
        public DateTime STARTDATE { get; set; }
        public DateTime ENDDATE { get; set; }
        public string TERMINATE { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string GROUPCODE { get; set; }
    }
}
