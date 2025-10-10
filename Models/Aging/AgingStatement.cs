using System;

namespace KTC_SalesAppWAPI.Models.Aging
{
    public class AgingStatement
    {
        public string StoreCode { get; set; }
        public string StoreName { get; set; }        
        public double DebitAmount { get; set; }
        public double CreditAmount { get; set; }
        public double Balance { get; set; }
        public string Type { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string SeriesName { get; set; }
        public string BeginStr { get; set; }
        public string Currency { get; set; }
        public DateTime PostingDate  { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime DocDate { get; set; }
        public int Below_30 {get; set;}
        public int Between30_60 { get; set; }
        public int Betweenn60_90 { get; set; }
        public int Over_90 { get; set; }
        public int Aged { get; set; }
        public int DocNum { get; set; }

        // 20230310
        // for looping the doc to look for cn 

        public string WebPrefixUrl { get; set; }
        public string DocTypeDesc { get; set; }
        public string SignedDocNames { get; set; } // separated by comma

    }
}
