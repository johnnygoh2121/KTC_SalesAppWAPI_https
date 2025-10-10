using Dapper;
using KTC_SalesAppWAPI.DTOs.BreadPayment;
using KTC_SalesAppWAPI.DTOs.Payment;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Aging;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadPay;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.Payment;
using KTC_SalesAppWAPI.Models.Payment.Log;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadPaymentController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadPaymentController> _logger;

        //readonly string APP_JSON = "application/json";

        string _webHostAddrEndPoint = string.Empty;
        string _lastError { get; set; } = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;

        public BreadPaymentController(IConfiguration configuration, ILogger<BreadPaymentController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
            _webHostAddrEndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult Post(PayBread_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "BreadPaymentDoc": // for seller ktc store usage
                    {
                        return BreadPaymentDoc(dto); // save for ktc store payment 
                    }
                case "GetBreadPayCustMaster":
                    {
                        return GetBreadPayCustMaster(dto);
                    }
                case "GetBreadPayCustMaster_Dist":
                    {
                        return GetBreadPayCustMaster_Dist(dto);
                    }
                case "GetBreadAging_NonKtcStore":
                    {
                        return GetBreadAging_NonKtcStore(dto);
                    }
                case "GetDistOwnStoreInv":
                    {
                        return GetDistOwnStoreInv(dto);
                    }
                case "GetDistOwnStoreCN":
                    {
                        return GetDistOwnStoreCN(dto);
                    }
                case "GetBreadPayCustMaster_DistSingle":
                    {
                        return GetBreadPayCustMaster_DistSingle(dto);
                    }
                case "GetBreadPayDocs_NonKtcStore":
                    {
                        return GetBreadPayDocs_NonKtcStore(dto);
                    }
                case "SaveBreadPay_NonKtcStore":
                    {
                        return SaveBreadPay_NonKtcStore(dto); // save payment for non ktc store
                    }
                case "GetPaymentDocs_Dist":
                    {
                        return GetPaymentDocs_Dist(dto);
                    }
                case "GetPaymentLines_NonKtcStore":
                    {
                        return GetPaymentLines_NonKtcStore(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetPaymentLines_NonKtcStore(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("The doc entry is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("The db info invalid");
                }

                // query the selected doc 
                // invoice, credit memo
                using var conn = new SqlConnection(_commDbConnStr_Bread);

                var sql = "exec sp_SelectChqCashBankTransfer_NonKtcStore @webDb, @subsi, @docEntry";
                var payments = conn.Query<Payment>(sql, new
                {
                    webDb = db.WEBDB,
                    subsi = db.COMPANYNAME,
                    docEntry = dto.DocEntry,
                }).ToList();

                // query the payment as cash, cheque, bank transfer
                sql = "exec sp_SelectDocInvCnDn_NonKtcStore @webDb, @subSi, @docEntry";
                var documents = conn.Query<Document>(sql, new
                {
                    webDb = db.WEBDB,
                    subSi = db.COMPANYNAME,
                    docEntry = dto.DocEntry
                }).ToList();

                var results = new { documents, payments }; // new dto 
                return Ok(results);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetPaymentDocs_Dist(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                var sp_query = @"exec sp_SelectPaymentDocs_NoKtcStore @webDb, @subSi, @companyId, @startDt, @endDt";

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var listing = conn.Query<PortalPayment>(sp_query, new
                {
                    webDb = db.WEBDB,
                    subSi = db.COMPANYNAME,
                    companyId = dto.UserCode,
                    startDt = dto.StartDt.Date,
                    endDt = dto.EndDt.Date
                }).ToList();

                if (listing.Count == 0) return NotFound();
                return Ok(listing);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult SaveBreadPay_NonKtcStore(PayBread_Dto dto)
        {
            try
            {
                #region check property
                if (dto.PayHead == null)
                {
                    return BadRequest("Payment head is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Pay update type is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Pay company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.WebPortalRequest))
                {
                    return BadRequest("Pay request name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Pay user code is empty");
                }
                var db_webPortal = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);
                if (db_webPortal == null)
                {
                    _logger.LogError(_lastError);
                    return BadRequest("Error reading the company database");
                }

                #endregion               
                // create pay into bread portal
                var breadPortalHead = CreateBreadPortalPay(dto);
                var db_BreadPortal = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);

                // save the portal paymen first 
                long breadPayDocEntry = -1;
                var breadPayDocHelper = new Bread_PAYDocHelper();
                if (dto.PayHead.docentry == 0)
                {
                    breadPayDocEntry = breadPayDocHelper.CreateBread_Pay(breadPortalHead, db_BreadPortal, _commDbConnStr_Bread);
                }
                else
                {
                    breadPayDocEntry = breadPayDocHelper.UpdateDraft_Pay(breadPortalHead, db_BreadPortal, _commDbConnStr_Bread);
                }

                if (dto.UpdateType.ToLower() == "draft") // and direct exit 
                {
                    //result.updateDocType = dto.UpdateType;
                    //result.docType = dto.Request;
                    var newRepliedD = new PayDocResult
                    {
                        actionSuccess = true,
                        errorMessage = "",
                        actionResult = $"{breadPayDocEntry}",
                        documentStatus = "D",
                        updateDocType = dto.UpdateType,
                        docType = dto.Request
                    };
                    return Ok(newRepliedD);
                }

                var newReplied = new PayDocResult
                {
                    actionSuccess = true,
                    errorMessage = "",
                    actionResult = $"{breadPayDocEntry}",
                    documentStatus = "C",
                    updateDocType = dto.UpdateType,
                    docType = dto.Request
                };

                return Ok(newReplied);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetBreadPayDocs_NonKtcStore(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // dist company id 
                {
                    return BadRequest("Invalid user code / dist company id");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCardCode)) // dist company id 
                {
                    return BadRequest("Invalid customer code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                var sp_query = @"exec sp_GetBreadPayDocs_NonKtcStore @webDb, @cardCode, @companyId";
                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var docs = conn.Query<PortalDocument>(sp_query, new
                {
                    webDb = db.WEBDB,
                    cardCode = dto.QueryCardCode,
                    companyId = dto.UserCode
                }).ToList();

                if (docs.Count == 0) return NotFound();
                return Ok(docs);

            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetBreadPayCustMaster_DistSingle(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // dist company id 
                {
                    return BadRequest("Invalid user code / dist company id");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCardCode)) // dist company id 
                {
                    return BadRequest("Invalid customer code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var sp_query = @"exec sp_GetBreadPayCustMaster_DistSingle @webDb, @companyId, @cardCode";
                var card = conn.Query<Bread_CUSTMASTER>(sp_query, new
                {
                    webDb = db.WEBDB,
                    companyId = dto.UserCode,
                    cardCode = dto.QueryCardCode
                }).FirstOrDefault();

                if (card == null) return NotFound();

                return Ok(card);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetDistOwnStoreCN(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid docEntry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var sp_query = @"exec sp_GetDistOwnStoreCn @webDb, @docEntry";
                var cn = conn.Query<Bread_OINV_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry
                }).FirstOrDefault();

                if (cn == null) return NotFound();
                // query the line 
                sp_query = @"exec sp_GetDistOwnStoreCn_Line @webDb, @docEntry";
                cn.Lines = conn.Query<Bread_INV1_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry
                }).ToList();

                // query the line batch (if any)
                for (int b = 0; b < cn.Lines.Count; b++)
                {
                    if (cn.Lines[b] == null) continue;

                    sp_query = @"exec sp_GetDistOwnStoreCn_LineBatch @webDb, @docEntry, @lineNum";
                    cn.Lines[b].Batches = conn.Query<Bread_Batch>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        docEntry = dto.DocEntry,
                        lineNum = cn.Lines[b].LINENUM
                    }).ToList();
                }
                return Ok(cn);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetDistOwnStoreInv(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid docEntry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var sp_query = @"exec sp_GetDistOwnStoreInv @webDb, @docEntry";
                var inv = conn.Query<Bread_OINV_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry
                }).FirstOrDefault();

                if (inv == null) return NotFound();
                // query the line 
                sp_query = @"exec sp_GetDistOwnStoreInv_Line @webDb, @docEntry";
                inv.Lines = conn.Query<Bread_INV1_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry
                }).ToList();

                // query the line batch (if any)
                for (int b = 0; b < inv.Lines.Count; b++)
                {
                    sp_query = @"exec sp_GetDistOwnStoreInv_LineBatch @webDb, @docEntry, @lineNum";
                    inv.Lines[b].Batches = conn.Query<Bread_Batch>(sp_query, new
                    {
                        webDb = db.WEBDB,
                        docEntry = dto.DocEntry,
                        lineNum = inv.Lines[b].LINENUM
                    }).ToList();
                }
                return Ok(inv);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        // for pay document store procedure 
        // sp_GetBreadPayDocs_NonKtcStore

        IActionResult GetBreadAging_NonKtcStore(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryCardCode))
                {
                    return BadRequest("Invalid card code");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Invalid card code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }

                var lastMonthDate = DateTime.Now.AddMonths(-1);

                var startDT = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(startDT.Year, startDT.Month);
                var endDt = new DateTime(startDT.Year, startDT.Month, daysInMonth);

                var sp_query = @"exec GetBreadAging_NonKtcStore @webDb, @cardCode, @companyId, @startDt, @endDt";
                var results = new SqlConnection(_commDbConnStr_Bread).Query<AgingStatement>(sp_query, new
                {
                    webDb = db.WEBDB,
                    cardCode = dto.QueryCardCode,
                    companyId = dto.CompanyId,
                    startDt = startDT,
                    endDt = endDt
                }).ToList();

                if (results.Count == 0) return NotFound();
                return Ok(results);

            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetBreadPayCustMaster_Dist(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }
                var sp_query = @"exec sp_GetCustMasterBreadPay_Dist @webDb, @userCode";
                var results = new SqlConnection(_commDbConnStr_Bread).Query<CUSTMASTER>(sp_query, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode
                }).ToList();

                if (results.Count == 0) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        IActionResult GetBreadPayCustMaster(PayBread_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid company name");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid dbi");
                }
                var sp_query = @"exec sp_GetCustMasterBreadPay @webDb, @userCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var results = conn.Query<CUSTMASTER>(sp_query, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode
                }).ToList();


                if (results.Count == 0) return NotFound();

                // prepare the address 
                //for (int c =  0; c < results.Count; c++)
                //{
                //    var sp_queryAddress = @$"SELECT * 
                //            FROM {db.SAPDB}..CRD1 WITH (NOLOCK) 
                //            WHERE CardCode = @SoDocStoreCard 
                //            AND AdresType ='B'";

                //    var bill_address = conn.Query<CRD1>(sp_queryAddress, new { SoDocStoreCard = results[c].CARDCODE }).FirstOrDefault();
                //    if (bill_address == null) continue;
                //    results[c].BILLADD1 = bill_address.GetAddress();
                //}

                return Ok(results);
            }
            catch (Exception e)
            {
                _lastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return BadRequest($"request not handler.\n{_lastError}");
            }
        }

        // save for payment for ktc store
        IActionResult BreadPaymentDoc(PayBread_Dto dto)
        {
            #region check property
            if (dto.PayHead == null)
            {
                return BadRequest("Payment head is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.CompanyId))
            {
                return BadRequest("Pay company id is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.QueryKey))
            {
                return BadRequest("Pay query key is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UpdateType))
            {
                return BadRequest("Pay update type is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                return BadRequest("Pay company name is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.WebPortalRequest))
            {
                return BadRequest("Pay request name is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Pay user code is empty");
            }
            var db_webPortal = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.CompanyName);
            if (db_webPortal == null)
            {
                _logger.LogError(_lastError);
                return BadRequest("Error reading the company database");
            }

            #endregion

            // do a massage to the payment documents
            // 2021 08 06
            var conn_webportal = new SqlConnection(_commDbConnStr);
            if (dto.PayHead.documents != null && dto.PayHead.documents.Count > 0)
            {
                // check the doc never being use in other payment document.                                         
                var found = false;
                var message = "";

                dto.PayHead.documents.ForEach(c =>
                {
                    var sql_duplUsage = @$"select top 1 t0.DOCENTRY 
                                                from {db_webPortal.WEBDB}..mc t0 with (nolock)
                                                inner join 
                                                     {db_webPortal.WEBDB}..mc3 t1 with (nolock) on t1.DOCENTRY = t0.DOCENTRY 
                                                Where (DOCSTATUS != 'P') and (DOCSTATUS != 'L')
                                                and SOURCEID = @SOURCEID 
                                                and SOURCEDOC = @SOURCEDOC
                                                and SOURCETYPE = @SOURCETYPE ";

                    var sid = conn_webportal.ExecuteScalar<string>(sql_duplUsage, new
                    {
                        SOURCEID = c.sourceid,
                        SOURCEDOC = c.sourcedoc,
                        SOURCETYPE = c.sourcetype
                    });

                    if (!string.IsNullOrWhiteSpace(sid))
                    {
                        message += $"{c.sourcetype} #{c.sourcedoc} being use in doc# {sid} pls recheck with the office\n";
                        found = true;
                    }
                });

                if (found) // if found any doc used in payment, reject the collection
                {
                    return BadRequest(message);
                }

                // gather the doc entry massage 
                const string _CreditMemo = "A/R Credit Memo";
                const string _Invoice = "A/R Invoice";
                const string _IncomingPayment = "Incoming Payment";

                for (int d = 0; d < dto.PayHead.documents.Count; d++)
                {
                    var doc = dto.PayHead.documents[d];
                    if (doc.objectcode == null)
                    {
                        if ($"{doc.sourcetype}".Equals(_Invoice))
                        {
                            dto.PayHead.documents[d].objectcode = "13";
                            var sql_inv = @$"SELECT DocEntry 
                                                 FROM {db_webPortal.SAPDB}..OINV WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                            var docEntry = conn_webportal.ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

                            if (docEntry > 0)
                            {
                                dto.PayHead.documents[d].sourceid = docEntry;
                            }
                            continue;
                        }

                        if ($"{doc.sourcetype}".Equals(_CreditMemo))
                        {
                            dto.PayHead.documents[d].objectcode = "14";
                            var sql_inv = @$"SELECT DocEntry 
                                                 FROM {db_webPortal.SAPDB}..ORIN WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                            var docEntry = conn_webportal.ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

                            if (docEntry > 0)
                            {
                                dto.PayHead.documents[d].sourceid = docEntry;
                            }
                            continue;
                        }

                        // Incoming Payment
                        if ($"{doc.sourcetype}".Equals(_IncomingPayment))
                        {
                            dto.PayHead.documents[d].objectcode = "24";
                            var sql_inv = @$"SELECT DocEntry 
                                                 FROM {db_webPortal.SAPDB}..ORCT WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                            var docEntry = conn_webportal.ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

                            if (docEntry > 0)
                            {
                                dto.PayHead.documents[d].sourceid = docEntry;
                            }
                            continue;
                        }

                        // Journal Entry
                        if ($"{doc.sourcetype}".Equals("Journal Entry"))
                        {
                            dto.PayHead.documents[d].objectcode = "30";
                            dto.PayHead.documents[d].sourceid = 0;
                            continue;
                        }
                    }
                }
            }

            // prepare the bread pay doc 

            // create pay into bread portal
            var breadPortalHead = CreateBreadPortalPay(dto);
            var db_BreadPortal = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);

            // save the portal paymen first 
            long breadPayDocEntry = -1;
            var breadPayDocHelper = new Bread_PAYDocHelper();
            if (dto.PayHead.docentry == 0)
            {
                breadPayDocEntry = breadPayDocHelper.CreateBread_Pay(breadPortalHead, db_BreadPortal, _commDbConnStr_Bread);
            }
            else
            {
                breadPayDocEntry = breadPayDocHelper.UpdateDraft_Pay(breadPortalHead, db_BreadPortal, _commDbConnStr_Bread);
            }

            if (dto.UpdateType.ToLower() == "draft") // and direct exit 
            {
                //result.updateDocType = dto.UpdateType;
                //result.docType = dto.Request;
                var newReplied = new PayDocResult
                {
                    actionSuccess = true,
                    errorMessage = "",
                    actionResult = $"{breadPayDocEntry}",
                    documentStatus = "D",
                    updateDocType = dto.UpdateType,
                    docType = dto.Request
                };
                return Ok(newReplied);
            }

            // 20220413
            var svrAdr = !string.IsNullOrWhiteSpace(db_webPortal.PostSvrAdressPort) ?
                                db_webPortal.PostSvrAdressPort : _webHostAddrEndPoint;

            // using rest api 
            var client = new RestClient($"{svrAdr}{dto.WebPortalRequest}/{dto.CompanyId}/{dto.UpdateType}");

            // for ktc store 
            dto.PayHead.docstatus = "S";

            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", $"Bearer {dto.QueryKey}");
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(dto.PayHead);

            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            // once success posted
            if (!response.IsSuccessful)
            {
                var content1 = response.Content; //await response.Content.ReadAsStringAsync();
                var result1 = JsonConvert.DeserializeObject<PortalReplied>(content1);
                if (result1 == null)
                {
                    return BadRequest("Error when posting to web portal");
                }

                if (dto.Line != null)
                {
                    dto.Line.PostResult = "Fail";
                    dto.Line.Details = result1.errorMessage;
                    new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                }
                return BadRequest($"{result1.errorMessage}\n{result1.actionResult}\n{content1}");
            }

            var content = response.Content;

            var result = JsonConvert.DeserializeObject<PayDocResult>(content);
            result.updateDocType = dto.UpdateType;
            result.docType = dto.Request;
            var guid = Guid.NewGuid();
            dto.PayHead.guid = $"{guid}";

            // build the web portal and bread portal link 

            using var breadPortal_Conn = new SqlConnection(_commDbConnStr_Bread);
            breadPortal_Conn.Open();
            using var trans = breadPortal_Conn.BeginTransaction();
            try
            {
                var insertPayMc = @$"insert into {db_BreadPortal.WEBDB}..FTAPP_PAyMCLink (PayEntry, McEntry, TransDt) 
                                            values (@PayEntry, @McEntry, GETDATE())";

                var insertres = breadPortal_Conn.Execute(insertPayMc,
                    new
                    {
                        PayEntry = breadPayDocEntry,
                        McEntry = result.actionResult
                    }, trans);

                // save a draft copy for collection
                dto.PayHead.docnum = int.Parse(result.actionResult);
                var documents = dto.PayHead.documents;
                if (documents != null)
                {
                    documents.ForEach(d => { d.headGuid = $"{guid}"; d.lineGuid = $"{Guid.NewGuid()}"; });
                }

                var payments = dto.PayHead.payments;
                if (payments != null)
                {
                    payments.ForEach(p => { p.headGuid = $"{guid}"; p.lineGuid = $"{Guid.NewGuid()}"; p.ChequeNo = p.lineref; });
                }

                // remove save in the porta payment draft
                // InsertWebDbTables(dto.PayHead, documents, payments, db.WEBDB);
                var resInsert = InsertPayment(dto.PayHead, payments, db_webPortal.WEBDB, db_webPortal.SAPDB, dto.CompanyName, dto.UserCode,
                              breadPortal_Conn, trans);


                trans.Commit();

                // add in the post success log 
                if (dto.Line != null)
                {
                    dto.Line.PostResult = result.actionResult;
                    dto.Line.Details = "Success";
                    new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                trans.Rollback();
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return null;
            }
        }

        Bread_Pay_Ext CreateBreadPortalPay(PayBread_Dto dto)
        {
            try
            {
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.CompanyName);
                if (db == null)
                {
                    _lastError = "Invalid bread web dbi";
                    _logger.LogError(_lastError);
                    return null;
                }

                var queryBreadPortalUser = @$"Select * 
                                                from {db.WEBDB}..USERS with (nolock) 
                                                where UserCode = @usercode";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var user = conn.Query<Bread_User>(queryBreadPortalUser,
                                                new { usercode = dto.UserCode }).FirstOrDefault();

                if (user == null)
                {
                    _lastError = $"Invalid user {dto.UserCode} - bread portal";
                    _logger.LogError(_lastError);
                    return null;
                }

                // create the pay head 
                var head = dto.PayHead;
                var bpHead = new Bread_Pay_Ext
                {
                    DOCENTRY = head.docentry > 0 ? head.docentry : 0,
                    COMPANYID = dto.UserCode,
                    DOCNUM = head.docnum > 0 ? $"{head.docnum}" : "0",
                    DOCSTATUS = head.docstatus,
                    CARDCODE = head.cardcode,
                    CARDNAME = head.cardname,
                    DOCDATE = head.docdate,
                    REFNO = head.refno,
                    REMARKS = "",
                    DOCTOTAL = (decimal)head.doctotal,
                    PAIDTOTAL = 0,
                    UCREATED = $"{user.USERID}",
                    DCREATED = head.dcreated,
                    UMODIFIED = $"{user.USERID}",
                    DMODIFIED = DateTime.Now,
                    PAIDTODATE = 0,
                    DOCTYPE = null,
                    FILES = head.docfile
                };

                var docLines = dto.PayHead.documents;
                if (docLines != null && docLines.Count > 0)
                {
                    var docs = new List<Bread_Pay1_Ext>();
                    var lineCounter = 1;
                    for (int d = 0; d < docLines.Count; d++)
                    {
                        var doc = docLines[d];
                        if (doc == null) continue;

                        string sourceType = string.Empty;
                        switch (doc.sourcetype)
                        {
                            case "A/R Invoice":
                                {
                                    sourceType = "INV";
                                    break;
                                }
                            case "A/R Credit Memo":
                                {
                                    sourceType = "CN";
                                    break;
                                }
                        }

                        var newDoc = new Bread_Pay1_Ext
                        {
                            DOCENTRY = doc.docentry > 0 ? doc.docentry : 0,
                            LINENUM = lineCounter,
                            BASEENTRY = doc.sourceid,
                            BASETYPE = sourceType,
                            BASEDOCNUM = string.IsNullOrWhiteSpace(doc.basedocnum)? $"{doc.sourcedoc}" : doc.basedocnum,
                            BASEDOCDATE = doc.sourcedate,
                            BASEREFNO = doc.sourceref,
                            BASETOTAL = (decimal)doc.sourceamt,
                            DOCAMOUNT = (decimal)doc.sourceamt,
                            TRANSID = doc.transid,
                            TRANSLINE = doc.transline,
                            OBJECTCODE = doc.objectcode,
                            SEL = "",
                            BANKAMT = 0, 
                           
                        };

                        docs.Add(newDoc);
                        lineCounter++;
                    }
                    bpHead.Documents = docs;
                }

                if (head.payments != null && head.payments.Count > 0)
                {
                    var payments = new List<Bread_Pay2_Ext>();
                    var lineCounter = 1;
                    foreach (var p in head.payments)
                    {
                        string linetype = "";
                        switch (p.linetype)
                        {
                            case "MCC":
                                {
                                    linetype = "Q"; // cheque 
                                    break;
                                }
                            case "MCB":
                                {
                                    linetype = "O"; // online 
                                    break;
                                }
                            case "MCA":
                                {
                                    linetype = "C"; // online 
                                    break;
                                }
                        }


                        var newPay = new Bread_Pay2_Ext
                        {
                            DOCENTRY = p.docentry > 0 ? p.docentry : 0,
                            LINENUM = lineCounter,
                            LINETYPE = linetype,
                            LINEREF = p.lineref,
                            LINEDATE = p.linedate,
                            BANK = p.bank1,
                            TOTAL = (decimal)p.lineamt,
                            REMARKS = p.lineref,
                            BANK2 = p.bank2,
                            BANKDATE = p.bankdate,
                            BANKUSER = p.confusr,
                            UPDDATE = DateTime.Now,
                            CANCEL = null,
                            CONFIRM = null,
                            FILES = p.filename
                        };

                        payments.Add(newPay);
                    }
                    bpHead.Payments = payments;
                }

                return bpHead;
            }
            catch (Exception e)
            {
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return null;
            }
        }

        int InsertPayment(PortalPayment head, List<Payment> payments,
            string webDb, string sapDb, string companyName, string seller,
            SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                var cash = payments.Where(c => c.linetype.Equals("MCA")).FirstOrDefault();
                if (cash != null)
                {
                    // insert into cash summary log
                    var cashlog = new FTApp_SellerCashLog
                    {
                        SubSi = companyName,
                        UserCode = seller,
                        Cash = cash.lineamt,
                        DocNum = $"{head.docnum}",
                        BankInStatus = "N",
                        CollectedDt = DateTime.Now,
                        CardCode = head.cardcode,
                        CardName = head.cardname,
                        HeaderGuid = head.guid,
                        CashStatus = "OnHand"
                    };

                    var res = InsertCashLog(cashlog, webDb, conn, trans);
                    if (res <= 0)
                    {
                        _lastError = $"Insert cash record fail. {head.docnum}";
                        _logger.LogError(_lastError);
                        return -1;
                    }
                }

                // cheque 
                int cheqCount = 0;
                var cheques = payments.Where(c => c.linetype.Equals("MCC")).ToList();
                if (cheques.Count > 0)
                {
                    cheques.ForEach(c =>
                    {
                        var newCheque = new FTApp_SellerChequeLog
                        {
                            Subsi = companyName,
                            UserCode = seller,
                            ChequeAmount = c.lineamt,
                            ChequeDt = c.linedate,
                            ChequeNo = c.ChequeNo,
                            DocNum = $"{head.docnum}",
                            BankInStatus = "N",
                            ColllectedDt = DateTime.Now,
                            CardCode = head.cardcode,
                            CardName = head.cardname,
                            HeaderGuid = head.guid,
                            ChequeStatus = "OnHand"
                        };

                        var chequeBank = GetBank(c.bankabs, sapDb);
                        if (chequeBank != null)
                        {
                            newCheque.ChequeBankCode = chequeBank.BankCode;
                            newCheque.ChequeBankName = chequeBank.BankName;
                        }

                        var cheInsertRes = InsertChequeLog(newCheque, webDb, conn, trans);
                        if (cheInsertRes <= 0)
                        {
                            _lastError = $"Insert cheque record fail. {head.docnum}, cheque# {c.ChequeNo}";
                            _logger.LogError(_lastError);
                            return;
                        }
                        cheqCount++;
                    });

                    if (cheqCount == cheques.Count)
                    {
                        return cheqCount;
                    }
                    return -1;
                }

                return 1; // no cheque insert .. just cash
            }
            catch (Exception e)
            {
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return -1;
            }
        }
        ODSC GetBank(int? bankAbs, string sapDb)
        {
            try
            {
                if (bankAbs == null) return null;

                var sql = @$"SELECT *                             
                            FROM [{sapDb}].[dbo].[ODSC] WITH (NOLOCK) 
                            WHERE AbsEntry = @bankabs";

                using var conn = new SqlConnection(_commDbConnStr);
                return conn.Query<ODSC>(sql, new { bankAbs }).FirstOrDefault();
            }
            catch (Exception e)
            {
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return null;
            }
        }

        int InsertChequeLog(FTApp_SellerChequeLog cheque, string webDb, SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                var insertCashLog = @$"INSERT INTO [{webDb}].[dbo].[FTApp_SellerChequeLog] (
                                           Subsi
                                          ,UserCode
                                          ,ChequeAmount
                                          ,ChequeDt
                                          ,ChequeNo
                                          ,DocNum
                                          ,BankInStatus
                                          ,ColllectedDt                                        
                                          ,CardCode
                                          ,CardName                                        
                                          ,HeaderGuid                                          
                                          ,ChequeStatus
                                          ,ChequeBankName
                                          ,ChequeBankCode
                                          ) VALUES (
                                           @Subsi
                                          ,@UserCode
                                          ,@ChequeAmount
                                          ,@ChequeDt
                                          ,@ChequeNo
                                          ,@DocNum
                                          ,@BankInStatus
                                          ,@ColllectedDt
                                          ,@CardCode
                                          ,@CardName
                                          ,@HeaderGuid
                                          ,@ChequeStatus
                                          ,@ChequeBankName
                                          ,@ChequeBankCode)";

                //var conn = new SqlConnection(_commDbConnStr).Execute(insertCashLog, cheque);
                return conn.Execute(insertCashLog, cheque, trans);
            }
            catch (Exception e)
            {
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return -1;
            }
        }

        int InsertCashLog(FTApp_SellerCashLog cash, string webDb, SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                var insertCashLog = @$"INSERT INTO [{webDb}].[dbo].[FTApp_SellerCashLog] (
                                           SubSi
                                          ,UserCode
                                          ,Cash
                                          ,DocNum
                                          ,BankInStatus
                                          ,CollectedDt
                                          ,CardCode
                                          ,CardName
                                          ,HeaderGuid    
                                          ,CashStatus
                                          ) VALUES (
                                           @SubSi
                                          ,@UserCode
                                          ,@Cash
                                          ,@DocNum
                                          ,@BankInStatus
                                          ,@CollectedDt
                                          ,@CardCode
                                          ,@CardName
                                          ,@HeaderGuid
                                          ,@CashStatus)";

                // var conn = new SqlConnection(_commDbConnStr).Execute(insertCashLog, cash);
                return conn.Execute(insertCashLog, cash, trans);
            }
            catch (Exception e)
            {
                _lastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(_lastError);
                return -1;
            }
        }

    }
}
