using System;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.DN;

namespace KTC_SalesAppWAPI.DTOs.DN
{
    public class Dn_Dto
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string CardCode { get; set; }
        public string CodeType{ get; set; }
        public string CardType { get; set; }
        public string ValidFor { get; set; }

        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }

        public string QueryKeys { get; set; }
        public string QueryCompanyID { get; set; }
        public string UpdateType { get; set; }

        public DebitNote dn { get; set; }

        public int DnDocEntry { get; set; }
        public int InvNo { get; set; }

        public int docNumber { get; set; }
        public int pafDocEntry { get; set; }
        public int PafDnDocEntry { get; set; }

        public string DnFiles { get; set; }

        public string UserCode { get; set; }

        public FTAPP_AppPostLog Line { get; set; }

        public string LastDocEntry { get; set; }
    }

}
