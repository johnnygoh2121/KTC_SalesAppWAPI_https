using System;

namespace KTC_SalesAppWAPI.Models.Refund
{
    public class Refund2
    {
        public long DocEntry { get; set; }

        public int LineNum { get; set; }
        
        public int? TransId { get; set; }

        public int? TransLine { get; set; }

        public int? SourceId { get; set; }

        public string SourceType { get; set; }

        public DateTime? SourceDate { get; set; }

        public int? SourceDoc { get; set; }

        public string SourceRef { get; set; }

        public decimal? SourceAmt { get; set; }

        public decimal? AppliedAmt { get; set; }

        public decimal? SourceAmtFc { get; set; }

        public string ObjectCode { get; set; }
    }

}
