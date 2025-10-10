using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;

namespace KTC_SalesAppWAPI.DTOs.Delivery
{
    public class Dto_Driver
    {
        public string DocType { get; set; }
        public OINV Invoice { get; set; }
        public COG_Doc Cog { get; set; }
        public OWTR_Ext Transfer { get; set; }
    }
}
