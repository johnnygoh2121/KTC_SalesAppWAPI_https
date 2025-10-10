using KTC_SalesAppWAPI.Models.InStoreAct;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.InStoreAct
{
    public class InStoreAct_DTO
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string CardCode { get; set; }      
        public string UserCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        // query line 
        public string HeadGuid { get; set; }

        // for svae 
        public FTAPP_InStoreAct Head { get; set; }
        public List<FTAPP_InStoreAct1> Lines { get; set; }

        // for query agency 
        public string CardType { get; set; }
        public string AgencyCode { get; set; } // 20230501


    }
}
