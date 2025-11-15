using KTC_SalesAppWAPI.Models.SalesOrder;
using System;

using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.AppPostLog;

namespace KTC_SalesAppWAPI.DTOs.COG
{
    public class Dto_Cog
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string  CardCode { get; set; }
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

       public int DlbNum { get; set; } // use when return from dlb
       public int BasedDocNum { get; set; } // return from dlb base document
       public string BasedDoctype { get; set; }  // return based doc type

        public Guid BoxGuid { get; set; } // for delivery app seelct item to trcn

        public bool IsStandAloneTrcn { get; set; } // 20230613 for tracking the stand alone trcn

        public string DriverName { get; set; }
        public string PlateNum { get; set; }

        // for cn and invoice return 
        public int LineNumber { get; set; }        
        public string ItemName { get; set; }
        public string BatchNum { get; set; }
        public decimal BatchQty { get; set; }

    }
}
