using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class FTAPP_WRTN
    {
        // for app usage 
        public string SubSi { get; set; }
        public string SubSiID { get; set; }
        
        public int id { get; set; }
        public int CnDocEntry { get; set; }
        public int CnDocNum { get; set; }
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
        public bool EmailSent { get; set; }
        public DateTime EmailSentDt { get; set; }
        public string AppVersion { get; set; }
        public string DateSource { get; set; }
        public int GIDocNum { get; set; }
        public int GIDocEntry { get; set; }

        // the line 
        public List<FTAPP_WRTN1> Lines { get; set; }

        public string Remark { get; set; } // rtn remark from sales app 

    }
}
