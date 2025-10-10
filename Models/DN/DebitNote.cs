using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.DN
{
    public class DebitNote
    {
        // app usage 

        public string Subsi { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }

        //original 
        public int DOCENTRY { get; set; }
        public int DOCNUM { get; set; }
        public string DOCSTATUS { get; set; }
        public DateTime DOCDATE { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string AGENCY { get; set; }
        public string PAFTYPE { get; set; }
        public string PAFNO { get; set; }
        public string REFNO { get; set; }
        public string REMARKS { get; set; }
        public string PAFFILE { get; set; }
        public string REFFILE { get; set; }
        public string BRAND { get; set; }
        public decimal AMOUNT { get; set; }
        public decimal SUPP { get; set; }
        public decimal OWN { get; set; }
        public int CNNO { get; set; }
        public DateTime CNDATE { get; set; }
        public int APCNNO { get; set; }
        public string APPRREM { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DCREATED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string INVNO { get; set; }
        public DateTime INVDATE { get; set; }
        public string ULNUMBER { get; set; }
        public DateTime ULDATE { get; set; }
        public string REFCARD { get; set; }
        public string NOGST { get; set; }
        public string FUNDTYPE { get; set; }
        public string PAFUSR { get; set; }
        public string PAFREV { get; set; }
        public string REVUSR { get; set; }
        public string PTYPE { get; set; }
        public string FINANCE { get; set; }

        public int DNNO { get; set; }
        public int APDNNO { get; set; }

        public List<DnApprovalDetail> ApprovalDetail { get; set; }
        public List<CNsDetail> chargeCodes { get; set; }

    }
}
