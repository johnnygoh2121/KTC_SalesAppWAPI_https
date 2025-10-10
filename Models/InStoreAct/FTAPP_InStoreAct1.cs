using System;

namespace KTC_SalesAppWAPI.Models.InStoreAct
{
    public class FTAPP_InStoreAct1
    {
        public int id { get; set; }
        public Guid HeadGuid { get; set; }
        public Guid LineGuid { get; set; }
        public string ActName { get; set; }
        public string ActDesc { get; set; }
        public string ActFile { get; set; }
        public string Remarks { get; set; }

        public DateTime FileDt { get; set; }

        // 20230501
        public string PhotoName { get; set; }
        public string PhotoDesc { get; set; }
        public string ItemGroup { get; set; }
        public string Reason { get; set; }


    }
}
