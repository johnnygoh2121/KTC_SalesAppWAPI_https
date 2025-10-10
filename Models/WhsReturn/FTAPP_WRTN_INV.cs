using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class FTAPP_WRTN_INV
    {
        public string SubSi { get; set; }
        public string SubSiID { get; set; }

        public int id { get; set; }
        public int InvDocEntry { get; set; }
        public int InvDocNum { get; set; }
        public string Files { get; set; }
        public DateTime TransDt { get; set; }
        public string Signed { get; set; }
        public string OwnerCode { get; set; }
        public string OwnerName { get; set; }
        public string Remarks { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public string AuthUserCode { get; set; }
        public string AuthUserName { get; set; }
        public bool HasVarient { get; set; }
        public int EmailSent { get; set; }
        public DateTime EmailSentDt { get; set; }
        public string AppVersion { get; set; }
        public string DateSource { get; set; }
        public int GIDocNum { get; set; }
        public int GIDocEntry { get; set; }
        public string LastMessage { get; set; }
        public string Remark { get; set; }

        public List<FTAPP_WRTN1_INV> Lines { get; set; }

        public int DlbEntry { get; set; }
        public int InvNum { get; set; }
        public int CnNum { get; set; }
        public int RtnEntry { get; set; }

        public int CnEntry { get; set; }

        public string Reason { get; set; }
    }
}
