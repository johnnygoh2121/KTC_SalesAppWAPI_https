using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class BreadDocDetail
    {
        public int Id { get; set; }
        public int LineNum { get; set; }
        public Guid HeaderGuid { get; set; }
        public string DocNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double OrderQty { get; set; }
        public double Price { get; set; }
        public double LineAmount { get; set; }
        public double TotalBeforeDiscount { get; set; }
        public double DisByPercent { get; set; }
        public double DisByValue { get; set; }
        public string TaxCode { get; set; }
        public double TaxAmt { get; set; }
        public string ToWhsCode { get; set; }
        public string FromWhsCode { get; set; }
        public string SelectedUom { get; set; }

        public DateTime DeliveryDate { get; set; }
        public string Pricelist { get; set; }
        public double TotalBeforeDis { get; set; }
        public string TaxName { get; set; }
        public double TaxRate { get; set; }
        public double GrossPrice { get; set; }
        public double LineTotal { get; set; }
        public Guid ItemGuid { get; set; }
        public DateTime ExpiredDate { get; set; }

        public string Remarks { get; set; }
        public string Batch { get; set; }
    }
}
