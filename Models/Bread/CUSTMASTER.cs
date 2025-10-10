using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class CUSTMASTER
    {
        // app added in columns 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public string IsKTCClient { get; set; }  // to deter mine the create of inoice and cn
        public string SapFrozenFor { get; set; }
        public string ClientOwner { get; set; }
        public string IsKTCClientParent { get; set; }

        // orig column

        public string COMPANYID { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string REGNO { get; set; }
        public string GSTNO { get; set; }
        public string CUSTGRP { get; set; }
        public string TERRITRY { get; set; }
        public string TEL { get; set; }
        public string FAX { get; set; }
        public string EMAILADD { get; set; }
        public string CONTACT { get; set; }
        public string MOBILE { get; set; }
        public string CURRENCY { get; set; }
        public string COUNTRY { get; set; }
        public string STATE { get; set; }
        public string DEFTAX { get; set; }
        public string BILLADD1 { get; set; }
        public string BILLADD2 { get; set; }
        public string BILLADD3 { get; set; }
        public string BILLADD4 { get; set; }
        public string BILLADD5 { get; set; }
        public string SHIPADD1 { get; set; }
        public string SHIPADD2 { get; set; }
        public string SHIPADD3 { get; set; }
        public string SHIPADD4 { get; set; }
        public string SHIPADD5 { get; set; }
        public string REMARKS { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public decimal ACCBAL { get; set; }
        public string USECR { get; set; }
        public string NOCN { get; set; }

       
    }
}
