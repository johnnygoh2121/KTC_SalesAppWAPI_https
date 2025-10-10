
using System;

namespace KTC_SalesAppWAPI.Models.WebPortal
{
    public class WFTAPP_Box1
    {
        // 20240415
        // index key link to SO and SO1 table
        
        public int DOCENTRY { get; set; } // So docentry

        
        public int LINENUM { get; set; } // So line
                                         // 
        
        public string BOXID { get; set; }

        
        public string LINENUM1 { get; set; }

        public string BatchNo { get; set; }

        // old 

        public int id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public string Packaging { get; set; }
        public Guid BoxGuid { get; set; }
        public Guid ContentGuid { get; set; }
        public int BaseEntry { get; set; }
        public int BaseLine { get; set; }

    }
}
