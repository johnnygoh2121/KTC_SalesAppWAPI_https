using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class SoDocHeader
    {
        public int docentry { get; set; }
        public int docnum { get; set; }
        public string docdate { get; set; }
        public string docstatus { get; set; }
        public string cardcode { get; set; }
        public string cardname { get; set; }
        public string billto { get; set; }
        public string billtoadd { get; set; }
        public string shipto { get; set; }
        public string shiptoadd { get; set; }
        public string collector { get; set; }
        public string deldate { get; set; }
        public string whscode { get; set; }
        public string nodel { get; set; }
        public string pono { get; set; }
        public string pofile { get; set; }
        public string addhoc { get; set; }
        public double doctotal { get; set; }
        public double invtotal { get; set; }
        public string remarks { get; set; }
        public string postrem { get; set; }
        public string appr { get; set; }
        public string appruser { get; set; }
        public DateTime? apprdate { get; set; }
        public int? apprlev { get; set; }
        public string apprrem { get; set; }
        public string initno { get; set; }
        public string slpcode { get; set; }
        public int? inventry { get; set; }
        public int? invno { get; set; }
        public double? invamt { get; set; }
        public double? invamtfc { get; set; }
        public string ucreated { get; set; }
        public DateTime? dcreated { get; set; }
        public string umodified { get; set; }
        public DateTime? dmodified { get; set; }
        public string geocode { get; set; }
        public string holdrem { get; set; }
        public int? apprlevel { get; set; }
        public string suppcode { get; set; }
        public string sampling { get; set; }
        public int? currlevel { get; set; }
        public string refcard { get; set; }
        public string refno { get; set; }
        public string reftype { get; set; }
        public string odrtype { get; set; }
        public DateTime? odrdate { get; set; }
        public string seller { get; set; }
        public string location { get; set; }
        public int? batchid { get; set; }
        public string onhold { get; set; }
        public string delrte { get; set; }
        public double? docdisc { get; set; }
        public string addhocusr { get; set; }
        public double? addhocamt { get; set; }
        public string confirmed { get; set; }
        public DateTime? confirmeddate { get; set; }
        public string confrmedby { get; set; }
        public int? sapseller { get; set; }
        public string gs { get; set; }
        public string powerroot { get; set; }
        public DateTime? expdate { get; set; }
        public DateTime? spendate { get; set; }
        public List<SoDocLine> lines { get; set; }

        public DateTime? poexpdate { get; set; } // 20220722 , PO exp date add in the posting 
    }
}
