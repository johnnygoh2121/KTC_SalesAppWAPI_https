using KTC_SalesAppWAPI.Models.BreadTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.DTOs.Bread.Trade
{
    public class Dto_BreadTrade
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string UserCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public int DocEntry { get; set; }
        public string AddrType { get; set; }

        // for batch 
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }

        public Bread_OINV_Ext InvDoc { get; set; }
        public string SaveType { get; set; }

        public string UserType { get; set; }
        public string IsKTCStore { get; set; } // Y for DI represent sell to KTC store

        public Bread_CN_Ext CnDoc { get; set; }

        public long InvNum { get; set; }
        public long CnNum { get; set; }

        public string CardCode { get; set; }

    }
}
