using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class ArCnCdnChargeCodeListing
    {
        public string BOP { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerGroup { get; set; }
        public string SubChannel { get; set; }
        public string GlobalChannel { get; set; }
        public string ForeignName { get; set; }
        public string ChargeCode { get; set; }
        public string PAFType { get; set; }
        public string DescriptionLine { get; set; }
        public string BrandDetails { get; set; }
        public string FundType { get; set; }
        public string GLAccountCode { get; set; }
        public DateTime ARCNDate { get; set; }
        public decimal LineAmount { get; set; }
        public string Description { get; set; }
        public string PAFNo { get; set; }
        public string ManualPAFNo { get; set; }
        public string SalePersonSap { get; set; }
        public string SalePersonSFA { get; set; }
        public string DocStatus { get; set; }
        public string BaseInvoice { get; set; }
        public DateTime BaseInvoiceDate { get; set; }
        public string CustomerReference { get; set; }
        public string PeriodActMth { get; set; }
        public string Brand { get; set; }
        public string SKUName { get; set; }
        public string Rebate_Pcs { get; set; }
        public string Quantity_Pcs { get; set; }

    }
}
