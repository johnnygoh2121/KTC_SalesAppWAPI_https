using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.NewPick
{
    public class PORTAL_SO
    {
        // content / lines 
        public List<PORTAL_SO1> Lines { get; set; }

        // for app
        public string SubSi { get; set; }
        public string SubSiID { get; set; }
        public string WhsName { get; set; }
        public int LinesCount { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string InPickOnhold { get; set; }
        public string DocStatusDisplay { get; set; }
        public string TerritryID { get; set; }
        public string TerritryName { get; set; }
        public DateTime SortedDate { get; set; }
        public bool IsIBT { get; set; } = false;

        // original
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string BILLTO { get; set; }
        public string BILLTOADD { get; set; }
        public string SHIPTO { get; set; }
        public string SHIPTOADD { get; set; }
        public string COLLECTOR { get; set; }
        public DateTime DELDATE { get; set; }
        public string WHSCODE { get; set; }
        public string NODEL { get; set; }
        public string PONO { get; set; }
        public string POFILE { get; set; }
        public string ADDHOC { get; set; }
        public decimal DOCTOTAL { get; set; }
        public decimal INVTOTAL { get; set; }
        public string REMARKS { get; set; }
        public string POSTREM { get; set; }
        public string APPR { get; set; }
        public string APPRUSER { get; set; }
        public DateTime APPRDATE { get; set; }
        public int APPRLEV { get; set; }
        public string APPRREM { get; set; }
        public string INITNO { get; set; }
        public string SLPCODE { get; set; }
        public int INVENTRY { get; set; }
        public int INVNO { get; set; }
        public decimal INVAMT { get; set; }
        public decimal INVAMTFC { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string GEOCODE { get; set; }
        public string HOLDREM { get; set; }
        public int APPRLEVEL { get; set; }
        public string SUPPCODE { get; set; }
        public string SAMPLING { get; set; }
        public int CURRLEVEL { get; set; }
        public string REFCARD { get; set; }
        public string REFNO { get; set; }
        public string REFTYPE { get; set; }
        public string ODRTYPE { get; set; }
        public DateTime ODRDATE { get; set; }
        public string SELLER { get; set; }
        public string LOCATION { get; set; }
        public int BATCHID { get; set; }
        public string ONHOLD { get; set; }
        public string DELRTE { get; set; }
        public decimal DOCDISC { get; set; }
        public string ADDHOCUSR { get; set; }
        public decimal ADDHOCAMT { get; set; }
        public string CONFIRMED { get; set; }
        public DateTime CONFIRMEDDATE { get; set; }
        public string CONFRMEDBY { get; set; }
        public int SAPSELLER { get; set; }
        public string GS { get; set; }
        public string POWERROOT { get; set; }
        public DateTime EXPDATE { get; set; }
        public DateTime SPENDATE { get; set; }
        public string PICKED { get; set; }
        public DateTime PICKEDDATE { get; set; }
        public long PICKSEQ { get; set; }
        public string TOPICK { get; set; }
        public string AVONFILE { get; set; }
        public string AVONCTN { get; set; }
        public DateTime AVONCTNDATE { get; set; }
        public string AVONSHIP { get; set; }
        public DateTime AVONSHIPDATE { get; set; }
        public string AVONDLB { get; set; }
        public DateTime AVONDLBDATE { get; set; }
        public string ACK { get; set; }
        public string COLA { get; set; }
        public string AVONADD { get; set; }
        public string AVONADDID { get; set; }
        public string AVONTYPE { get; set; }
        public string ISIOH { get; set; }
        public string TPSHIPCUST { get; set; }
        public string TPNAME { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; }
        public string ADD3 { get; set; }
        public string ADD4 { get; set; }
        public string ADD5 { get; set; }
        public string ADD6 { get; set; }
        public string ADD7 { get; set; }
        public string ADD8 { get; set; }
        public string TPID { get; set; }
        public string OOS { get; set; }
        public DateTime POEXPDATE { get; set; }
        public string PICKLISTNO { get; set; }
        public string FROMTRCN { get; set; }
        public string SOURCETYPE { get; set; }
    }
}
