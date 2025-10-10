using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.Geofence;
using KTC_SalesAppWAPI.Models.Login;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs
{
    public class UserProfile_Dto
    {
        public string Request { get; set; }
        public string LoginName { get; set; }
        //public string Password { get; set; }

        public List<string> Companies { get; set; }
        public List<USERCARD> UserCards { get; set; }
        public List<FTAPP_SSO> UserProfiles { get; set; } // kept all belong company
        public List<OCRD_Ext> OCRDs { get; set; } // ERP company information
        public List<OCRD_Ext> Agencies { get; set; } // ERP company information
        public List<FTApp_Config> AppConfig { get; set; }

        //public List<FTApp_BaseRouteSchedule> SchedulesCalls { get; set; }

        public List<OCRD_Ext> Schedule_OCRDs { get; set; }

        // for query 
        public string QueryCompany { get; set; }
        public List<string> QueryCompanies { get; set; }
        public string QueryUserCode { get; set; }
        public string QueryCardType { get; set; }
        public DateTime QueryDate { get; set; }

        // query agency brand 
        public string QueryAgencyBrand { get; set; }


        // for app schedule 
        public string QuerySlpName { get; set; }
        public DateTime QueryScheduleDate { get; set; }

        // add in off schedule store
        public FTAppGeoTrack AddOffSch_Store {get; set;}
        public string CardCodes { get; set; }
        public string CardCode { get; set; }
        public string WildCode { get; set; }

        public List<AuthMenu> AutMenus { get; set; } // permission

        public void ResetRequest ()
        {
            Request = string.Empty;
            //Password = string.Empty;            
        }

        // 20240201
        public List<Booklet> Booklets { get; set; }


    }
}
