using KTC_SalesAppWAPI.Models.BreadTrade;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Bread_Return
{
    public class Dto_BreadReturn
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserType { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        // for query the return ktc cn 
        public int CnDocEntry { get; set; }

        public Bread_CN_Ext CnDoc { get; set; }
        public List<Bread_CN1_Ext> BreadLines { get; set; }

        // for saving the dist return draft line
        public string DiCardCode { get; set; }

        public string SaveType { get; set; }

        public string SignFiles { get; set; }
        public string DocType { get; set; }
        public int DocEntry { get; set; }

        public int CNNumber { get; set; }

    }
}
