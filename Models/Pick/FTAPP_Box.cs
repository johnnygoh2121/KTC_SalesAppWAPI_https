using KTC_SalesAppWAPI.Models.Pack;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class FTAPP_Box
    {
        public int id { get; set; }
        public string BoxId { get; set; }
        public string PickerCode { get; set; }
        public string PickerName { get; set; }
        public DateTime PickDt { get; set; }

        // for packer later
        public string PackId { get; set; }
        public DateTime PackDt { get; set; }
        public string PackerCode { get; set; }
        public string PackerName { get; set; }
        public int BaseEntry { get; set; }
        public Guid BoxGuid { get; set; }
        public List<FTAPP_Box1> Contents { get; set; }

        // for app 
        public string PickMode { get; set; }

        public bool IsLooseBox { get; set; } = true; // or a pcs container, true for loose , false for full box
        public DateTime CreatedDt { get; set; }

        // for packing 
        //public SO BaseDoc { get; set; }
        //public decimal TotalBoxes { get; set; }

        public int TimeStampSeq { get; set; }
        public string AppVersion { get; set; }

        // for retrive packid 
        public int PackSeqId { get; set; }

        // for box size 
        public string BoxSize { get; set; }

        // for tupperware property 
        public string OrderProcessWeek { get; set; }
        public string BusinessCenterCode { get; set; }
        public int CurrentCartonNo { get; set; }
        public string OrderNo { get; set; }

        // for tupperware 
        public List<Tp_BoxContent> TpBoxContents { get; set; }

        public int LabelConsistTotalBoxes { get; set; }


    }
}
