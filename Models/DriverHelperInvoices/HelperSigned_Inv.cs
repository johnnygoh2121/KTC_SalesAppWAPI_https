using System;

namespace KTC_SalesAppWAPI.Models.DriverHelperInvoices
{
    public class HelperSigned_Inv
    {
        public string Subsi { get; set; }
        public string SubsiId { get; set; }

        public long DocEntry { get; set; }
        public long SoNo { get; set; }
        public long InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public decimal InvoiceAmount { get; set; }
        public long DLBNo { get; set; }
        public DateTime DLBDate { get; set; }
        public string TruckNo { get; set; }
        public string Agency { get; set; } // agency code
        public string RefNo {get; set;}
        public string Territory { get; set; }
        public string DLBStatus { get; set; }
        public string Warehouse { get; set; } // warehouse code
        public DateTime DeliveryDate { get; set; }
        public string FilePath { get; set; }
        public string SignFile { get; set; }
        public string AttachmentStatus { get; set; }
        public string Action { get; set; }
        public string Remarks { get; set; }
        public int Change { get; set; }
        public string Attachments { get; set; }

    }
}
