using System;

namespace KTC_SalesAppWAPI.Models.BreadReturn
{
    public class TrcnLineDetails
    {
        public int Id { get; set; }
        public long DocEntry { get; set; }
        public int LineNum { get; set; }
        public DateTime ExpDate { get; set; }
        public DateTime MfrDt { get; set; }
        public string Remark { get; set; }
        public string LotNo { get; set; }
        public string Module { get; set; }

        public string ReasonCode { get; set; }
        public string WhsCode { get; set; }

        public string Source { get; set; }
    }
}
