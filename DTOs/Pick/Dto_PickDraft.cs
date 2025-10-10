using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Pick
{
    public class Dto_PickDraft
    {   public string Request { get; set; }
        public string Subsi { get; set; }
        public SO1 Line { get; set; }
        public List<FTAPP_Box> Boxes { get; set; }
        public List<FTAPP_Box1> BoxContents { get; set; }
        public List<FTAPP_Batch> Batches { get; set; }
    }
}
