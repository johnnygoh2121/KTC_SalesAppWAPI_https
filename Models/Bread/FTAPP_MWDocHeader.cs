using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class FTAPP_MWDocHeader
    {
        public int Id { get; set; }
        public string DocType { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal DocAmt { get; set; }
        public string DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime TaxDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Ref2 { get; set; }
        public string Comments { get; set; }
        public string JrnlMemo { get; set; }
        public string NumberAtCard { get; set; }
        public Guid Guid { get; set; }
        public short Submitted { get; set; }
        public string SapDocNo { get; set; }
        public string ContactPerson { get; set; }
        public string ShipAddress { get; set; }
        public string BillAddress { get; set; }
        public decimal DiscountByPercent { get; set; }
        public decimal DiscountByValue { get; set; }
        public decimal TaxAmt { get; set; }
        public int NumberFileAttached { get; set; }
        public string DocSeries { get; set; }
        public string Currency { get; set; }
        public string SalesPerson { get; set; }
        public decimal TotalBeforeDis { get; set; }
        public short IsPostDraft { get; set; }

        public string ODCardCode { get; set; } // for receiver
        public string ODCardName { get; set; } // for receiver

        public List<FTAPP_MWDocDetails> Lines { get; set; }

        public int BaseITEntry { get; set; }

        public string TruckNo { get; set; }

        public int UsedTrayQty { get; set; }
    }
}
