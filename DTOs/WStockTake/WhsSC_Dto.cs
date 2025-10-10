using KTC_SalesAppWAPI.Models.WStockCount;
using System;

namespace KTC_SalesAppWAPI.DTOs.WStockTake
{
    public class WhsSC_Dto
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string UserCode { get; set; }
        public string WhsCode { get; set; }
        public string SpaceID { get; set; }
        public string ItemCode { get; set; }
        public DateTime MfrDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string UomType { get; set; }
        public string ScanCode { get; set; }
        public SC1 CountLine { get; set; }
        public string CountMode { get; set; }
        public string OriginalInCode { get; set; }
    }

}
