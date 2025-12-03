using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.Pick;
using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DLB1
    {

        // 20241024 
        // for checking draft 
        public string DriverName { get; set; }
        public string TruckNo { get; set; }
        public int DraftID { get; set; }

        public DateTime OutTransDt { get; set; }



        // 20230518
        // for transger property saving the toWhsCode and toWhsName 
        public string ToWhsCode { get; set; }
        public string ToWhsName { get; set; }

        public int IBTEntry { get; set; } // for trasfer reference  // 20230518

        public string SubSi { get; set; }
        public int id { get; set; }
        public int DocNum { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public int DocEntry { get; set; }
        public string DocStatus { get; set; }
        public string StatusDesc { get; set; }
        public Guid HeadGuid { get; set; }
        public string DocType { get; set; }
        public string BoxStatusDesc { get; set; }
        public int BoxesCount { get; set; }

        public DateTime DocDate { get; set; }
        public decimal DocTotal { get; set; }
        public int CartonNo { get; set; }
        public string RefNo { get; set; }
        public string ConsigmentNo { get; set; }

        public bool IsBoxLabelVis { get; set; }

        public OINV Invoice { get; set; } // for app display
        public COG_Doc Cog { get; set; }
        public OWTR_Ext Transfer { get; set; }

        public string SaveAs { get; set; } // indicate the svr to save, N = new / delete insert, E/ U = Update, D = Delete 

        public string Currency { get; set; }

        public string CustRef { get; set; }
        public string Territory  { get; set; } // TERRITORY

        public string SignedFiles { get; set; }

        public DateTime OutDt { get; set; }
        
        public DateTime TransInDt { get; set; }

        public bool IsReScan { get; set; } // for add in rescan feature indicator
        public int LastDlbEntry { get; set; } // indicate the last dlb being copy from 

        // for server update usage 
        //public long LastDlbDocEntry { get; set; }

        // for allow the continue scan in the box 
        public ScanAddBoxesInfo AddBoxInfo { get; set; }
        public bool IsCompleted_AddBoxes { get; set; } = false;



        public string U_DROPPOINT { get; set; }
        public string Warehouse { get; set; }
    }
}
