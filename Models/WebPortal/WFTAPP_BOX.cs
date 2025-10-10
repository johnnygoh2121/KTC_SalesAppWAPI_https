
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WebPortal
{
    public class WFTAPP_BOX
    {
        // 20240415
        // index key link to SO and SO1 table
        
        public int DOCENTRY { get; set; } // So docentry

        
        public int LINENUM { get; set; } // So line
                                         
        
        public string BOXID { get; set; } // uniquebox line no

        public List<WFTAPP_Box1> BoxContents  {get; set;}
      
        // old structure 
        public int id { get; set; }       
        
        public string PickerCode { get; set; }
        public string PickerName { get; set; }
        public DateTime PickDt { get; set; }
        public string PackId { get; set; }
        public DateTime PackDt { get; set; }
        public string PackerCode { get; set; }
        public string PackerName { get; set; }
        public int BaseEntry { get; set; }
        public Guid BoxGuid { get; set; }
        public long TimeStampSeq { get; set; }
        public string AppVersion { get; set; }
        public string BoxSize { get; set; }
        public string OrderProcessWeek { get; set; }
        public string BusinessCenterCode { get; set; }
        public int CurrentCartonNo { get; set; }
        public string OrderNo { get; set; }
        public int LabelConsistTotalBoxes { get; set; }
    }
}
