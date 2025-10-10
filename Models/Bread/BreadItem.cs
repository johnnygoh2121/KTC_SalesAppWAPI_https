using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class BreadItem
    {
        public int ItemCounts { get; set; } // reuse the object as line count header
        public string SubSi { get; set; }
        public string SubSiId { get; set; }

        public int Id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal UOMQty { get; set; }
        public string SuppCatNum { get; set; }
        public string CodeBars { get; set; }
        public int LineNum { get; set; }              
        public decimal QtyInPcs { get; set; }        
        public decimal TrayQty { get; set; }
        public decimal PcsQty { get; set; }        
        public string Remarks { get; set; }
        public Guid LineGuid { get; set; }        
        public string ScanInCode { get; set; }
        public string Batch { get; set; }
        public string BarCodeStr { get; set; }

        public string WhsCode { get; set; }
        public string FromWhsCode { get; set; }

        // for draft refenrence
        public string UserCode { get; set; } // owner whs user
        public string UserName { get; set; } // owner whs user
        public string UserSubsi { get; set; }


        public string ReqUserCode { get; set; } // van, distributor, and transportor 
        public string ReqUserName { get; set; } // van, distributor, and transportor  
        public string ReqUserSubsi { get; set; } // van, distributor, and transportor 

        public DateTime TransDt { get; set; }

        public Guid HeadGuid { get; set; }

        public string TransferStatus { get; set; }


    }
}