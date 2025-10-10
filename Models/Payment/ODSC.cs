using System;
namespace KTC_SalesAppWAPI.Models.Payment
{
     public class ODSC
     {
         #region fields for table ODSC
         public string BankCode { get; set; }
         public string BankName { get; set; }
         public string DfltAcct { get; set; }
         public string DfltBranch { get; set; }
         public int NextChckNo { get; set; }
         public string Locked { get; set; }
         public string DataSource { get; set; }
         public short UserSign { get; set; }
         public string SwiftNum { get; set; }
         public string IBAN { get; set; }
         public string CountryCod { get; set; }
         public string PostOffice { get; set; }
         public string AliasName { get; set; }
         public int AbsEntry { get; set; }
         public int DfltActKey { get; set; }
         public int NextNum { get; set; }
         public string BsPstDate { get; set; }
         public string BsValDate { get; set; }
         public int BnkOpCode { get; set; }
         public string BsDocDate { get; set; }
         public string TaxIdNum { get; set; }
         #endregion fields for table ODSC
     }
}

