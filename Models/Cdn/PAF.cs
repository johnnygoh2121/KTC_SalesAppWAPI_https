using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class PAF
    {
        // app usage property

        public string UpdateDocType { get; set; }
        public string DocType  { get; set; }

        public string Subsi { get; set; }
        public string AgencyName { get; set; }
        public string AgencyCode { get; set; }

        // orginal property
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
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
        public string MTH { get; set; }
        public DateTime REFDATE { get; set; }
        public string SUPPREF { get; set; }
        public DateTime SPENDATE { get; set; }

        public List<PAF1> CHARGECODES { get; set; }
        public string SIGNFILE { get; set; }

        // for paf charge code
        // ktcb 
        // 20211112
        public string SKUNAME { get; set; } //SKU Name
        public decimal REBATE { get; set; } //Rebate
        public decimal QTY { get; set; } //Quantity
        public string INVDESC { get; set; } //Invoice Description
        public string PICTURE { get; set; } //Attached picture file name

        // 20211216 ktcw am 
        public string PRDMTHACT { get; set; } // to save period data

    }
}
