using KTC_SalesAppWAPI.Models.Pack;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.PackIBT
{
    public class Dto_Pack_IBT
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public string QueryBoxid { get; set; }
        public List<FTAPP_Box> PackedBoxes { get; set; }

        public FTAPP_Box PackedBox { get; set; }

        // for pack print invoice 
        public string CardCode { get; set; }
        public string SubSi { get; set; }
        public string AddrType { get; set; }

        public string SOID { get; set; }

        // for Tupperware carton running number query
        // base on biz center and week of the order processing 
        public string Tp_WeekOfProcess { get; set; }
        public string Tp_BizCenterCode { get; set; }
        public string Tp_OrderDate { get; set; } // for consultant label

        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string AppVersion { get; set; }

        // for tp tally sheet 
        public string InvoiceNo { get; set; }
        public string PackedBoxId { get; set; }

        // for gamification record svaing 
        public FTAPP_PickPackAvgTime PickPackSecData { get; set; }


        // 2023 03 24
        public string PackId { get; set; }
        public string PackerCode { get; set; }
        public string PackerName { get; set; }

        public SO DeviceSo { get; set; }
    }
}