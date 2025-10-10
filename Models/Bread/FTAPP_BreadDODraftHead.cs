using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class FTAPP_BreadDODraftHead
    {
        public int Id { get; set; }
        public string SubSi { get; set; }
        public string SubSiID { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime DocDate { get; set; }
        public string DocStatus { get; set; }
        public Guid HeadGuid { get; set; }

        // for receiver
        public string CardCode { get; set; } // for receiver
        public string CardName { get; set; } // for receiver

        public string ODCardCode { get; set; } // for receiver
        public string ODCardName { get; set; } // for receiver

        public List<BreadItem> Lines { get; set; }
        public string Comments { get; set; }

        public int BaseITEntry { get; set; }

        public string TruckNo { get; set; }   

        public int UsedTrayQty { get; set; } // 20220417

        // 20250809
        public int ITDocNum { get; set; }    
    }
}
