using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.COG.ReturnMemoF
{
    public class ReturnMemo
    {

        public long Docentry { get; set; }
        public long? Docnum { get; set; }

        /// <summary>
        /// Base document, normal COG DOCENTRY no, Invoice Docentry no, -1 for COG direct invoice 
        /// </summary>
        public long? Basedocnum { get; set; }
        public DateTime? Docdate { get; set; } = null;

        /// <summary>
        /// Status (D - Draft, P - Posted (SAP CN Created))
        /// </summary>
        public string Docstatus { get; set; }

        /// <summary>
        /// Base Document Type (C - COG, I - Invoice)
        /// </summary>
        public string Doctype { get; set; }

        public string Cardcode { get; set; }
        public string Cardname { get; set; }
        public string Shipadd { get; set; }

        /// <summary>
        /// Collection Type - Selection : (Assigned/Personal)
        /// </summary>
        public string Coltype { get; set; }

        /// <summary>
        /// Manual COG No
        /// </summary>
        public string Cogno { get; set; }

        /// <summary>
        /// Customer Ref No
        /// </summary>
        public string Refno { get; set; }
        public string Remarks { get; set; }

        /// <summary>
        /// SAP Credit Memo DocEntry No
        /// </summary>
        public int? Cmentry { get; set; }
        public string Ucreated { get; set; }
        public string Umodified { get; set; }
        public DateTime? Dcreated { get; set; } = null;
        public DateTime? Dmodified { get; set; } = null;
        public string Lastinvrem { get; set; }
        public string Gstrem { get; set; }
        public List<ReturnMemoLine> Lines { get; set; }
        public string ItemDoc { get; set; }
        public string SignDoc { get; set; }
        public string Salesperson { get; set; } //20210915 add for cn sales person tagging
        public int Graceperiod { get; set; }

        // 20220531
        public string LorryNo { get; set; }
        public string Driver { get; set; }
        public string Transporter { get; set; }


        // AgencyCode
        // 20240718
        public string AgencyCode { get; set; }
    }

}

