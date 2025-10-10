using System;

namespace KTC_SalesAppWAPI.DTOs.DriverHelperInvoices
{
    public class Dto_HelperSignInv
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string FilePath { get; set; }
        public long InvoiceNo { get; set; }
        public long SONo { get; set; }
        public long DLBNo { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Agency { get; set; }
        public string WhsCode { get; set; }
        public string AttachmentStatus { get; set; }
        public string CostCenter { get; set; }
        public string UserCode { get; set; }

        public string DLBTruckNo { get; set; }

        //  @StartDAte DATETIME
        //, @EndDate DATETIME
        //, @FilePath nvarchar(200)
        //, @InvoiceNo int 
        //, @SONo int
        //, @DlbNo int
        //, @CardCode NVARCHAR(20)
        //, @CardName nvarchar(100)
        //, @Agency NVARCHAR(20)
        //, @WhsCode nvarchar(10)
        //, @AttachmentStatus nvarchar(20)
        //, @CostCenter nvarchar(20)
        //, @UserCode nvarchar(50)
    }
}
