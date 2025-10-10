using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.BreadReturn;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System;

namespace KTC_SalesAppWAPI.DTOs.Bread.BreadCog
{
    public class Dto_BreadCog
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string CardCode { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public string ItemCode { get; set; }
        public long DocEntry { get; set; }
        public string UserCode { get; set; }
        // for cog item query 
        public PortalQueryItemMaster QueryItems { get; set; }
        public string QueryKeys { get; set; }
        public string CompanyID { get; set; }
        public string QueryCompanyID { get; set; }
        public string Code { get; set; }

        // for create the cog
        public COG_Doc NewCog { get; set; }
        public string UpdateType { get; set; } // indicate draft or summit        

        // for create direct CN
        public ReturnMemo NewDirectCn { get; set; }
        public FTAPP_AppPostLog Line { get; set; }
        public string SignDoc { get; set; }

        // for create cn portal 
        public Bread_CN_Ext CnDoc { get; set; }
        //public Bread_OINV_Ext InvDoc { get; set; }

        public string DocType { get; set; }
        public string IsKtcStore { get; set; }

        public string UserType { get; set; }

    }
}
