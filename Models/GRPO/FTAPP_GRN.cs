using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class FTAPP_GRN
    {
        // for app 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public List<FTAPP_GRN1> Lines { get; set; }

        public string FILES { get; set; } // 20230526

        public int ID { get; set; }
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string REFNO { get; set; }
        public string WHSCODE { get; set; }
        public string TOWHS { get; set; }
        public string REMARKS { get; set; }
        public int POSTENTRY { get; set; }
        public DateTime POSTDATE { get; set; }
        public string APPRREM { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DCREATED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string BOP { get; set; }
        public string GRNTYPE { get; set; }
        public string AVREASONCODE { get; set; }
        public string DRAFT_STATUS { get; set; }
        public Guid GUID { get; set; }
        public string DELIVERY_ORDER { get; set; }

        public string ApproveUser { get; set; }
    }
}
