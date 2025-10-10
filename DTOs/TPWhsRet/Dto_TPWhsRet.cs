using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.TPWhsRet;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.TPWhsRet
{
    public class Dto_TPWhsRet
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public int CogEntry { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public List<FTAPP_COG1> CogLines { get; set; }

    }
}
