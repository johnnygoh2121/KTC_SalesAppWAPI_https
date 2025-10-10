using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.CreditApplication
{
    public class CUST
    {
        public List<CUST1> Sellers { get; set; }
        public List<CUST1> SalesPerson { get; set; } // for posting
        public List<CUST3> Attachments { get; set; } // for posting

        public int SellerCount { get; set; }
        public string Files { get; set; }

        // orig

        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime ?DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string BADD1 { get; set; }
        public string BADD2 { get; set; }
        public string BADD3 { get; set; }
        public string BADD4 { get; set; }
        public string BCOUNTRY { get; set; }
        public string BSTATE { get; set; }
        public string SADD1 { get; set; }
        public string SADD2 { get; set; }
        public string SADD3 { get; set; }
        public string SADD4 { get; set; }
        public string SCOUNTRY { get; set; }
        public string SSTATE { get; set; }
        public string PADD1 { get; set; }
        public string PADD2 { get; set; }
        public string PADD3 { get; set; }
        public string PADD4 { get; set; }
        public string PCOUNTRY { get; set; }
        public string PSTATE { get; set; }
        public string TELNO { get; set; }
        public string FAXNO { get; set; }
        public string COMPANYNO { get; set; }
        public string LICNO { get; set; }
        public string NATUREBUS { get; set; }
        public DateTime ?BUSDATE { get; set; }
        public string BUSSTART { get; set; }
        public string ORGANIZATION { get; set; }
        public string GEO1 { get; set; }
        public string GEO2 { get; set; }
        public string CONTACT1 { get; set; }
        public string CONTACT2 { get; set; }
        public string CONTACT3 { get; set; }
        public string CONTACT4 { get; set; }
        public string MOBILENO { get; set; }
        public string PHONE1 { get; set; }
        public string PHONE2 { get; set; }
        public string PHONE3 { get; set; }
        public string PHONE4 { get; set; }
        public string CADD1 { get; set; }
        public string CADD2 { get; set; }
        public string CADD3 { get; set; }
        public string CADD4 { get; set; }
        public string POSITION1 { get; set; }
        public string SUPP1 { get; set; }
        public string SUPP2 { get; set; }
        public string SUPP3 { get; set; }
        public string SUPPTERM1 { get; set; }
        public string SUPPTERM2 { get; set; }
        public string SUPPTERM3 { get; set; }
        public decimal SUPPLIMIT1 { get; set; }
        public decimal SUPPLIMIT2 { get; set; }
        public decimal SUPPLIMIT3 { get; set; }
        public string SUPPYEAR1 { get; set; }
        public string SUPPYEAR2 { get; set; }
        public string SUPPYEAR3 { get; set; }
        public string TSTYPE { get; set; }
        public decimal TSAMOUNT { get; set; }
        public string BANKGURR { get; set; }
        public DateTime ?GURRFROM { get; set; }
        public DateTime? GURRTO { get; set; }
        public string TRNO { get; set; }
        public DateTime ?TRDATE { get; set; }
        public string CHEQUEBANK { get; set; }
        public DateTime ?CHEQUEDATE { get; set; }
        public string CHEQUENO { get; set; }
        public string COLLECTOR { get; set; }
        public string SLPCODE { get; set; }
        public string BOP { get; set; }
        public string TERRITORY { get; set; }
        public int CARDGROUP { get; set; }
        public string CALLFREQ { get; set; }
        public string PAYTERM { get; set; }
        public decimal CREDITLIMIT { get; set; }
        public string UCREATED { get; set; }
        public DateTime ?DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime ?DMODIFIED { get; set; }
        public string GSTNO { get; set; }
        public string BOWNER { get; set; }
        public string NRIC { get; set; }
        public string LOCATION { get; set; }
    }
}
