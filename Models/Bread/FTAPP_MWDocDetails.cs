using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class FTAPP_MWDocDetails
    {
        public int Id { get; set; }
        public int LineNum { get; set; }
        public Guid HeaderGuid { get; set; }
        public string DocNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal OrderQty { get; set; }
        public decimal Price { get; set; }
        public decimal LineAmount { get; set; }
        public decimal TotalBeforeDiscount { get; set; }
        public decimal DisByPercent { get; set; }
        public decimal DisByValue { get; set; }
        public string TaxCode { get; set; }
        public decimal TaxAmt { get; set; }
        public string FromWhsCode { get; set; } // from warehouse 
        public string ToWhsCode { get; set; }   // to whs        
        public string SelectedUom { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Pricelist { get; set; }
        public decimal TotalBeforeDis { get; set; }
        public string TaxName { get; set; }
        public decimal TaxRate { get; set; }
        public decimal GrossPrice { get; set; }
        public decimal LineTotal { get; set; }
        public Guid ItemGuid { get; set; }
        public DateTime ExpiredDate { get; set; }

        public string Remarks { get; set; }
        public string Batch { get; set; }
    }
}
