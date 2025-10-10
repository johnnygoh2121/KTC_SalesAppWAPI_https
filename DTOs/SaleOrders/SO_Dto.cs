using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs
{
    public class SO_Dto
    {
        public string Request { get; set; }
        public string UserCompany { get; set; }
        public string AgencyCardCode { get; set; }
        public string AgencyName { get; set; }

        //public string AgencyBrandCode { get; set; }
        public string WarehouseCode { get; set; }

        // for portal request 
        public PortalQueryItemMaster QueryItems { get; set; }
        public string QueryKeys { get; set; }
        public string QueryCompanyID { get; set; }
        public string QueryCardType { get; set; }

        // for portal sales order posting
        public SoDocHeader SoDoc { get; set; }
        public string SoDocCompanyID { get; set; }
        public string SoDocUpdateType { get; set; }
        public string SoDocUpdateTypeShort { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime PoExpDate { get; set; }

        // process SO Line to create the header doc 
        public List<SoDocLine> SoDocLines { get; set; }
        public string SoDocSubSi { get; set; }
        public string SoDocWarehouse { get; set; }
        public string SoDocStoreCard { get; set; }
        public string SoDocSlpName { get; set; }
        public string SoDocPurchaseOrder { get; set; }
        public string SoDocRemarks { get; set; }

        // for sales order line 
        public int SoDocEntry { get; set; }

        public SoDocHeader SoDocUpdate {get; set;}

        public string SoDocFilesAttached { get; set; }

        public FTAPP_AppPostLog Line { get; set; }

        // for update the po file 
        public string POFile { get; set; }

        public string IsNewPromoteList { get; set; }


        // for promotion query 
        //public string PromoAgencyCode { get; set; }
        //public string PromoAgencyBrand { get; set; }
        //public string PromoCardCode { get; set; }
        //public string PromoWhsCode { get; set; }
    }   
}
