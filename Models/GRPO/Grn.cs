using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class Grn
    {
        // for app 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }

        /// <summary>
        /// Document Entry, 0 = Create new, > 0 = update existing
        /// </summary>
        public long Docentry { get; set; }

        /// <summary>
        ///Document No unuse, will be same as Docentry
        /// </summary>
        public long? Docnum { get; set; }

        /// <summary>
        /// Entry Date
        /// </summary>
        public DateTime? Docdate { get; set; }

        /// <summary>
        /// Document Status (D - Draft, S - Submit for approval, P = Posted)
        /// </summary>
        public string Docstatus { get; set; }

        /// <summary>
        /// Agency Code
        /// </summary>
        public string Cardcode { get; set; }

        /// <summary>
        /// Agency Name
        /// </summary>
        public string Cardname { get; set; }

        /// <summary>
        /// Vendor Ref No
        /// </summary>
        public string Refno { get; set; }

        /// <summary>
        ///Unuse
        /// </summary>
        public string Whscode { get; set; }

        /// <summary>
        ///Unuse
        /// </summary>
        public string Towhs { get; set; }

        /// <summary>
        ///Remarks
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        ///System Auto update
        /// </summary>
        public int? Postentry { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public DateTime? Postdate { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public string Apprrem { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public int? Apprlevel { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public int? Currlevel { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public string Ucreated { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public string Umodified { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public DateTime? Dcreated { get; set; }
        /// <summary>
        ///System Auto update
        /// </summary>
        public DateTime? Dmodified { get; set; }

        /// <summary>
        ///Unuse
        /// </summary>
        public string Bop { get; set; }

        /// <summary>
        /// empty - post to SAP as GRN Draft, A - post to SAP as GRN (System auto update base on submit type)
        /// </summary>
        public string Grntype { get; set; }

        /// <summary>
        ///Avon Reason Code (Required for AVON agency)
        /// </summary>
        public string Avreasoncode { get; set; }

        public List<Grn1> Lines { get; set; }
        public List<FTAPP_GRN1> Lines1 { get; set; }

        public string DeliveryOrderNo { get; set; }

        public DateTime? PostDt { get; set; } // 20230111
        public DateTime? ApprovedDt { get; set; }
        public string ApproveUser { get; set; }

        public string Files { get; set; } // 20230526 for files attachments
    }
}
