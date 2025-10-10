using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BreadPay
{
    public class Bread_Pay_Ext
    {
        public long DOCENTRY { get; set; }
        public string COMPANYID { get; set; }
        public string DOCNUM { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public DateTime DOCDATE { get; set; }
        public string REFNO { get; set; }
        public string REMARKS { get; set; }
        public decimal DOCTOTAL { get; set; }
        public decimal PAIDTOTAL { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public decimal PAIDTODATE { get; set; }
        public string DOCTYPE { get; set; }
        public DateTime POSTEDDATE { get; set; }
        public string POSTEDUSER { get; set; }
        public long POSTENTRY { get; set; }
        public string POSTED { get; set; }

        public string FILES { get; set; }

        public List<Bread_Pay1_Ext> Documents { get; set; }
        public List<Bread_Pay2_Ext> Payments { get; set; }
    }
}
