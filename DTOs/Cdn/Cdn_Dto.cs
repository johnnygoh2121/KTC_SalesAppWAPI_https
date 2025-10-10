using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Cdn;
using System;

namespace KTC_SalesAppWAPI.DTOs
{
    public class Cdn_Dto
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }

        public string CardCode { get; set; }

        //public string UserCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string CardType { get; set; } = "S";
        public string ValidFor { get; set; } = "Y";

        public string CodeType { get; set; }

        // for save paf 
        public string QueryKeys { get; set; }
        public string QueryCompanyID { get; set; }
        public string UpdateType { get; set; }
        public PAF Paf { get; set; }

        public int QueryDocEntry { get; set; }

        public Gondola Gon { get; set; }

        public int CNNumber { get; set; } // for credit memo print out 

        public int PafDocEntry { get; set; }
        public string StoreCard { get; set; }
        public string PAFFiles { get; set; }

        public int GonDocEntry { get; set; }
        public string GonFiles { get; set; }

        // for user code agency filter
        public string UserCode { get; set; }

        public FTAPP_AppPostLog Line { get; set; }

        public string SIGNFILE { get; set; }

        public string LastDocEntry { get; set; }
        public string TableName { get; set; }

        // for PAF files update 

        public int PafEntry { get; set; }
        public string PafFiles1 { get; set; }
        public string RefFiles1 { get; set; }


    }
}
