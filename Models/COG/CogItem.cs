using System;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class CogItem
    {
        // copy from ocrd property setup
        // 20220110
        public string AllowRescanIInv { get; set; }
        public string MustBatchLotAtGrpo { get; set; }
        public string MustBatchLotAtTrcn { get; set; }
        public string MustSelectBoxSize { get; set; }

        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string CodeBars { get; set; }
        public string FozenFor { get; set; }
        public decimal UomQty { get; set; }
        public decimal ListPrice { get; set; } = -1;
        // for inv line
        public decimal LowestInvoicePrice { get; set; }
        public DateTime LastInvDate { get; set; }
        public string LastDocNum { get; set; }
        // for agency code grouping 
        public string AgencyCode { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ListName  {get; set; }

        public string SuppCatNum { get; set; }

        // 20211216
        //public CogItemInfo Info { get; set; }
    }
}
