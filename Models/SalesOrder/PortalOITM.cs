using System;

namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class PortalOITM
    {
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string codeBars { get; set; }
        public double price { get; set; }
        public double uomQty { get; set; }
        public double onhand { get; set; }
        public string promoItemCode { get; set; }
        public object salesOrderQty { get; set; }
        public DateTime minSalesOrderDate { get; set; }
        public double suggIndex { get; set; }
        public object suppCatNum { get; set; }
        public double amsQty { get; set; }
        public double suggestQty { get; set; }

        // 20250108
        // for force case input 
        //public string U_MustCase { get; set; } = "";
        public bool MustSellSku { get; set; } 


        // 20250228
        // for DGP case
        public decimal invoiceQty { get; set; }
        public decimal returnQty { get; set; }

        public string isMustSell { get; set; }
    }
}
