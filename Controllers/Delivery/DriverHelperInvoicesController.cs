using Dapper;
using KTC_SalesAppWAPI.DTOs.DriverHelperInvoices;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.DriverHelperInvoices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Delivery
{
    [Route("[controller]")]
    [ApiController]
    public class DriverHelperInvoicesController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<DriverHelperInvoicesController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";

        public DriverHelperInvoicesController(IConfiguration configuration,
            ILogger<DriverHelperInvoicesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_HelperSignInv dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetSellerInvoice":
                    {
                        return GetSellerInvoice(dto);
                    }

                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetSellerInvoice(Dto_HelperSignInv dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name needed");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db info");
                }

                // 20231227
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    dto.UserCode = "manager";
                }

                if ($"{dto.AttachmentStatus}".ToLower().Equals("pending"))
                {
                    dto.AttachmentStatus = "";
                }

                if ($"{dto.AttachmentStatus}".ToLower().Equals("all"))
                {
                    dto.AttachmentStatus = "";
                }

                var sp_query1 = $@"DECLARE @StartDAte DATETIME = '{dto.StartDate:yyyy-MM-dd}'
                                DECLARE @EndDate DATETIME = '{dto.EndDate:yyyy-MM-dd}'
                                DECLARE @FilePath nvarchar(200) = '{dto.FilePath}'
                                DECLARE @InvoiceNo int = {dto.InvoiceNo}, @SONo int = {dto.SONo}, @DlbNo int = {dto.DLBNo}
                                DECLARE @CardCode NVARCHAR(20) = '{dto.CardCode}', @CardName nvarchar(100) = '{dto.CardName}', @Agency NVARCHAR(20) = '{dto.Agency}'
                                DECLARE @WhsCode nvarchar(10) = '{dto.WhsCode}',@AttachmentStatus nvarchar(20) = '{dto.AttachmentStatus}', @CostCenter nvarchar(20) = '{dto.CostCenter}'
                                DECLARE @UserCode nvarchar(50) = '{dto.UserCode}'
                                DECLARE @DLBTruckNo nvarchar(50) = '{dto.DLBTruckNo}'

                                SELECT
                                '{db.COMPANYID}' as [SubsiID],
                                '{db.COMPANYNAME}' as [Subsi],
                                T1.DocEntry, T1.U_SOID as [SONo],  T1.DocNum as [InvoiceNo], 
                                T1.DocNum,
                                --CONVERT(NVARCHAR(20),T1.DocDate,103) as [InvoiceDate],
                                T1.DocDate as  [InvoiceDate],
                                T1.CardCode as [CustomerCode], T1.CardName as [CustomerName],
                                T1.DOCTOTAL AS [InvoiceAmount], 
                                (SELECT TOP 1 D0.DOCENTRY FROM [{db.WEBDB}].[DBO].[DLB] D0 INNER JOIN [{db.WEBDB}].[DBO].[DLB1] D1 ON D1.DOCENTRY = D0.DOCENTRY 
                                WHERE D0.DOCSTATUS = 'C' AND D1.DOCTYPE = 'I' AND D1.DOCNUM = T1.DOCNUM ORDER BY D0.DOCENTRY DESC) AS [DLBNo],

                               -- (SELECT CONVERT(NVARCHAR(20),MAX(D0.DOCDATE),103) FROM [{db.WEBDB}].[DBO].[DLB] D0 INNER JOIN [{db.WEBDB}].[DBO].[DLB1] D1 ON D1.DOCENTRY = D0.DOCENTRY 
                               -- WHERE D0.DOCSTATUS = 'C' AND D1.DOCTYPE = 'I' AND D1.DOCNUM = T1.DOCNUM) AS [DLBDate],

                                (SELECT MAX(D0.DOCDATE) FROM [{db.WEBDB}].[DBO].[DLB] D0 INNER JOIN [{db.WEBDB}].[DBO].[DLB1] D1 ON D1.DOCENTRY = D0.DOCENTRY 
                                WHERE D0.DOCSTATUS = 'C' AND D1.DOCTYPE = 'I' AND D1.DOCNUM = T1.DOCNUM) AS [DLBDate],

                                (SELECT TOP 1 D0.TRUCKNO FROM [{db.WEBDB}].[DBO].[DLB] D0 INNER JOIN [{db.WEBDB}].[DBO].[DLB1] D1 ON D1.DOCENTRY = D0.DOCENTRY 
                                WHERE D0.DOCSTATUS = 'C' AND D1.DOCTYPE = 'I' AND D1.DOCNUM = T1.DOCNUM ORDER BY D0.DOCENTRY DESC) AS [TruckNo],
                                T8.CARDCODE AS [Agency], T1.NumAtCard AS [RefNo] , T9.descript AS [Territory],
                                (SELECT TOP 1 Case D1.STATUS WHEN 'R' THEN 'Returned' WHEN 'C' THEN 'Cancelled' WHEN 'O' THEN 'Out' ELSE  D1.STATUS END FROM [{db.WEBDB}].[DBO].[DLB] D0 WITH (NOLOCK) INNER JOIN [{db.WEBDB}].[DBO].[DLB1] D1 WITH (NOLOCK) ON D1.DOCENTRY = D0.DOCENTRY WHERE D0.DOCSTATUS = 'C' AND D1.DOCNUM = T1.DOCNUM AND D1.DOCTYPE = 'I' ORDER BY D0.DOCENTRY DESC) AS [DLBStatus], 
                                T10.WhsCode as [Warehouse], 
                                --CONVERT(NVARCHAR(20),T1.U_DELDATE,103) as [DeliveryDate],
                                T1.U_DELDATE  as [DeliveryDate],
                                @FilePath as [FilePath],
                                case when isnull(T2.Attachments,'') = '' THEN 
                                (SELECT TOP 1 SignedFiles FROM [{db.WEBDB}]..FTAPP_DLB1 T4 WITH (NOLOCK)  WHERE T4.DocNum = T1.DocNum AND T4.DocType = 'I' ORDER BY SignedFiles desc ) 
                                ELSE T2.Attachments END as [SignFile],
                                CASE WHEN ISNULL(T2.AttchmentStatus,'') = '' THEN 'Pending' ELSE T2.AttchmentStatus END as [AttachmentStatus], 
                                ISNULL(T2.ApprSts,'') as [Action], T2.ApprRem as [Remarks],
                                CAST(0 as bit) AS [Change], cast('' as nvarchar(1000)) as [Attachments]
                                INTO #TEMP
                                FROM [{db.SAPDB}].[DBO].[OINV] T1 WITH (NOLOCK) 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[INV1] T10 WITH (NOLOCK) ON T10.DOCENTRY = T1.DOCENTRY AND T10.LineNum = (SELECT MIN(LineNum) FROM [{db.SAPDB}].[DBO].[INV1] WITH (NOLOCK) WHERE DocEntry = T1.DocEntry)
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OITM] T8 WITH (NOLOCK) ON T8.ItemCode = T10.ItemCode
                                INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T5 WITH (NOLOCK) ON T5.CARDCODE =T1.CardCode AND T5.CARDTYPE = 'C' 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OTER] T9 WITH (NOLOCK) ON T9.territryID = T5.Territory 
                                LEFT OUTER JOIN [{db.WEBDB}]..InvoiceAttach T2 WITH (NOLOCK) ON T2.DocEntry = T1.DocEntry
                                WHERE  T1.DocType = 'I' AND T1.DocDate >= @StartDate AND T1.DocDate <= @EndDate
                                AND T5.U_COSTCTR IN (SELECT COSTCTR FROM [{db.WEBDB}]..USERCENTER WITH (NOLOCK)  WHERE USERCODE = @UserCode)
                                
                                AND (T1.DocNum = @InvoiceNo OR ISNULL(@InvoiceNo,0) = 0)
                                AND (T1.CardCode = @CardCode OR ISNULL(@CardCode,'') = '')
                                AND (T1.U_SOID = @SONo OR ISNULL(@SONo,0) = 0)
                                AND (T1.CardName LIKE @CardName OR ISNULL(@CardName,'') = '')
                                AND (T8.CardCode = @Agency OR ISNULL(@Agency,'') = '')
                                AND (T10.WhsCode = @WhsCode OR ISNULL(@WhsCode,'') = '')
                                AND (T2.AttchmentStatus = @AttachmentStatus OR ISNULL(@AttachmentStatus,'') = '')
                                AND (T5.U_COSTCTR = @CostCenter OR ISNULL(@CostCenter,'') = '')

                                SELECT * FROM #TEMP T0 WHERE 0 = 0
                                AND (T0.[DLBNo] = @DlbNo OR ISNULL(@DlbNo,0) = 0) order by DocEntry
                                DROP TABLE #TEMP";

                var conn = new SqlConnection(_commDbConnStr);
                var invoices = conn.Query<HelperSigned_Inv>(sp_query1).ToList();

                if (invoices.Count == 0)
                {
                    return NotFound();
                }

                // 202312
                // RE FILTER THE BY TRUCK NO 
                if (!string.IsNullOrWhiteSpace(dto.DLBTruckNo))
                {
                    var newList = invoices.Where(i => $"{i.TruckNo}".ToLower().Equals($"{dto.DLBTruckNo}".ToLower())).ToList();
                    if (newList.Count == 0) return NotFound();
                    return Ok(newList);
                }

                return Ok(invoices);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}
