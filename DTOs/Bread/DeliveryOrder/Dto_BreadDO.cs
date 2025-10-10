using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadTrade;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Bread.DeliveryOrder
{
    public class Dto_BreadDO
    {

        public int UsedTrayQty { get; set; } // user specified used tray qty // 20220417
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public List<BreadItem> DoLines { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        // 
        public string DoStatus { get; set; }
        public string ReqUserCode { get; set; }
        public string ReqUserName { get; set; }
        public string ReqUserSubsi { get; set; }

        // for do , invoice documents 
        public string UserType { get; set; }
        public string DiCardCode { get; set; }

        public int DocEntry { get; set; }

        // for create invoice 
        // 20220121
        public FTAPP_MWDocHeader Head { get; set; }
        public FTAPP_MWRequest DocRequest { get; set; }
        
        // for query the card store based on usercode
        public string CardCode { get; set; } // for single query to bread store card
        public string CardName { get; set; }
        public Guid HeadGuid { get; set; }
        public string SaveDocAsStatus { get; set; }
        public string ODCardCode { get; set; }
        public string ODCardName { get; set; }


        public Guid PickedGuid { get; set; }

        // for batch 
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }

        // for doc comments
        public string Comments { get; set; }
        public string TruckNo { get; set; }

        public string QueryDocStatus { get; set; }

        // for delivery , bakary DO 
        // create ktc invoice for dist
        public Bread_OINV_Ext InvDoc { get; set; }

        public string SaveType { get; set; }
        public FTAPP_BreadDODraftHead DoDraftHead { get; set; }
        public int AttachFileCnt { get; set; }

        public string Files { get; set; }

        public int InvntryTrnsfrDocEntry { get; set; }

        public string DocType { get; set; }

        // ReceiverCode 
        // FROM IT TO IT 
        public string ReceiverCode { get; set; }
        public int ItDocNum { get; set; }
    }
}
