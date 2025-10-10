using Dapper;
using KTC_SalesAppWAPI.DTOs.Payment;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Aging;
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

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PaymentController> _logger;
        //readonly string APP_JSON = "application/json";

        string WebHostAddrEndPoint = string.Empty;
        string CommDbConnStr = string.Empty;
        string _commDbConnStr_bread = string.Empty;
        string LastError = string.Empty;

        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _configuration = configuration;

            CommDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");

            WebHostAddrEndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult Post(Pay_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "Document":
                case "Bank":
                case "HouseBank":
                case "Payment":
                    {
                        return HandlerPaymentQueries(dto);
                    }
                case "PaymentDoc":
                    {
                        return PaymentDoc(dto); // create, update pay document - draft and submit
                    }
                case "Aging":
                    {
                        return Aging(dto);
                    }
                case "CashSummary":
                    {
                        return CashSummaries(dto);
                    }
                case "CashSummary_Update":
                    {
                        return CashSummary_Update(dto);
                    }
                case "ChequesSummary":
                    {
                        return ChequesSummary(dto);
                    }
                case "ChequeSummary_Update":
                    {
                        return ChequeSummary_Update(dto);
                    }
                case "CashSummaries_BankIn":
                    {
                        return CashSummaries_BankIn(dto);
                    }
                case "ChequesSummary_BankIn":
                    {
                        return ChequesSummary_BankIn(dto);
                    }
                case "Cheques":
                    {
                        return Cheques(dto);
                    }
                case "Cash":
                    {
                        return Cash(dto);
                    }
                case "Cheques_Update_Receptionist":
                    {
                        return Cheques_Update_Receptionist(dto);
                    }
                case "Cash_Update_Cashier":
                    {
                        return Cash_Update_Cashier(dto);
                    }
                case "GetPaymentDocs":
                    {
                        return GetPaymentDocs(dto);
                    }
                case "GetPaymentLines":
                    {
                        return GetPaymentLines(dto);
                    }
                case "UpdatePaymentSupportDocs":
                    {
                        return UpdatePaymentSupportDocs(dto);
                    }
                default:
                    return BadRequest("Reques not recognised");
            }
        }

        IActionResult UpdatePaymentSupportDocs(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.SignedReceipt))
                {
                    return BadRequest("Invalid doc entry");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("Invalid company name for db infor");
                }

                // get the exiting support doc
                var query_existSuppDocNames = $@"SELECT DOCFILE FROM {db.WEBDB}..MC WITH (NOLOCK)
                                                 WHERE DOCENTRY =@DOCENTRY";

                var conn = new SqlConnection(CommDbConnStr);
                var toBeUpdate = conn.ExecuteScalar<string>(query_existSuppDocNames, new { DOCENTRY = dto.DocEntry });
                if (!string.IsNullOrWhiteSpace(toBeUpdate))
                {
                    toBeUpdate += "," + dto.SignedReceipt;
                }
                else
                {
                    toBeUpdate = dto.SignedReceipt;
                }

                // update back
                var update_sql = @$"UPDATE {db.WEBDB}..MC
                                    SET DOCFILE = @DOCFILE
                                    WHERE DOCENTRY = @DOCENTRY ";

                var res = conn.Execute(update_sql, new { DOCFILE = toBeUpdate, DOCENTRY = dto.DocEntry });
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult GetPaymentLines(Pay_Dto dto)
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
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("The db info invalid");
                }

                // query the selected doc 
                // invoice, credit memo
                var sql = "exec sp_SelectPaymentChqCashBankTransfer @webDb, @docEntry, @subSi";
                var conn = new SqlConnection(CommDbConnStr);
                var payments = conn.Query<Payment>(sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry,
                    subSi = db.COMPANYNAME
                }).ToList();

                // query the payment as cash, cheque, bank transfer
                sql = "exec sp_SelectPaymentInvCnDn @webDb, @docEntry, @subSi";
                var documents = conn.Query<Document>(sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry,
                    subSi = db.COMPANYNAME

                }).ToList();

                var results = new { documents, payments }; // new dto 
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


        IActionResult GetPaymentDocs(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("The company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("The user code is empty");
                }

                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }

                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("The db info invalid");
                }

                var sql = @"exec sp_SelectPaymentDocs @webDb ,@subSi, @userCode, @startDt, @endDt";
                var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<PortalPayment>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        subSi = db.COMPANYNAME,
                        userCode = dto.UserCode,
                        startDt = dto.StartDt.Date.ToString("yyyyMMdd"),
                        endDt = dto.EndDt.Date.ToString("yyyyMMdd"),
                    }).ToList();

                if (results == null) return NotFound();
                if (results.Count == 0) return NotFound();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult Cash_Update_Cashier(Pay_Dto dto)
        {
            try
            {
                if (dto.CashLogs == null)
                {
                    return BadRequest("cash log is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("cash log company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the db info is empty");
                }

                var conn = new SqlConnection(CommDbConnStr);

                dto.CashLogs.ForEach(x =>
                {
                    var updateSql = @$"UPDATE [{db.WEBDB}].[dbo].[FTApp_SellerCashLog]
                                        SET DeliveredToCashier = 'Y',
                                            DeliveredDt = GETDATE(), 
                                            CashStatus ='Cashier',
                                            BankInStatus = 'N'
                                        WHERE DocNum = @DocNum 
                                        AND UserCode = @UserCode
                                        AND CardCode = @CardCode";

                    var result = conn.Execute(updateSql, new { x.DocNum, x.UserCode, x.CardCode });
                });
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


        IActionResult Cheques_Update_Receptionist(Pay_Dto dto)
        {
            try
            {
                if (dto.ChequeLogs == null)
                {
                    return BadRequest("Cheques log is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Cheques log company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("the db info is empty");
                }

                var conn = new SqlConnection(CommDbConnStr);

                dto.ChequeLogs.ForEach(x =>
                {
                    var updateSql = @$"UPDATE [{db.WEBDB}].[dbo].[FTApp_SellerChequeLog]
                                        SET DeliveredToReceiption = 'Y',
                                            DeliveredDt = GETDATE(), 
                                            ChequeStatus ='Reception',
                                            BankInStatus = 'N'
                                        WHERE ChequeNo = @ChequeNo  
                                        AND DocNum = @DocNum 
                                        AND UserCode = @UserCode
                                        AND CardCode = @CardCode";

                    var result = conn.Execute(updateSql, new { x.ChequeNo, x.DocNum, x.UserCode, x.CardCode });
                });

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult Cash(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null) return BadRequest("Db name object is empty");

                var sql = @"exec sp_SelectSellerChashLog @webDb, @startDt,@endDt, @userCode";
                using var conn = new SqlConnection(CommDbConnStr);
                var cash = conn.Query<FTApp_SellerCashLog>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        startDt = $"{dto.ChequeStartDt:yyyyMMdd}",
                        endDt = $"{dto.ChequeEndDt:yyyyMMdd}",
                        UserCode = dto.UserCode
                    }).ToList();

                return Ok(cash);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult Cheques(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("user code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null) return BadRequest("Db name object is empty");

                var sql = @"exec sp_SelectSellerChequeLog @webDb, @chequeStartDt, @chequeEndDt, @userCode ";

                using var conn = new SqlConnection(CommDbConnStr);
                var cheques = conn.Query<FTApp_SellerChequeLog>(sql,
                    new
                    {
                        webDb = db.WEBDB,
                        ChequeStartDt = $"{dto.ChequeStartDt:yyyyMMdd}",
                        ChequeEndDt = $"{dto.ChequeEndDt:yyyyMMdd}",
                        UserCode = dto.UserCode
                    }).ToList();

                return Ok(cheques);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult ChequeSummary_Update(Pay_Dto dto)
        {
            try
            {
                if (dto.ChequeLog == null)
                {
                    return BadRequest("Cheque log is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryKey))
                {
                    return BadRequest("Query key is empty");
                }

                var c = dto.ChequeLog;

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, c.Subsi);
                if (db == null) return BadRequest("db info is empty");

                // check mc 2 with the exiting file name
                // if got then conbine, if not then overwrite
                var query_exiting = @$"SELECT FILENAME FROM  [{db.WEBDB}].[dbo].[MC2]  WITH (NOLOCK)                                        
                                       WHERE DOCENTRY = @DocNum
                                       AND LINEREF = @LineRef ";

                var backInSlipFiles = c.BankInSlip; // over write the file
                using var conn = new SqlConnection(CommDbConnStr);
                var files = conn.Query<string>(query_exiting, new { c.DocNum, LineRef = c.ChequeNo }).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(files))
                {
                    backInSlipFiles += $",{files}";
                }

                // 20220317
                // ensure the first char , if there is a comma
                if (!string.IsNullOrWhiteSpace(backInSlipFiles) && backInSlipFiles[0] == ',')
                {
                    backInSlipFiles = backInSlipFiles.Substring(1);
                }


                // update the portal file 
                var sql_updateMC = @$"UPDATE [{db.WEBDB}].[dbo].[MC2]
                                          SET FILENAME = @BankInSlip
                                          WHERE DOCENTRY = @DocNum 
                                          AND LINEREF = @ChequeNo
                                          AND LINETYPE = @Cheque ";

                var updateRes = conn.Execute(sql_updateMC, new
                {
                    BankInSlip = backInSlipFiles,
                    DocNum = c.DocNum,
                    Cheque = "MCC",
                    ChequeNo = c.ChequeNo
                });

                // update the cheque to log for bank in
                var update_sql = @$"UPDATE [{db.WEBDB}].[dbo].[FTApp_SellerChequeLog] 
                                        SET BankInSlip = @BankInSlip, 
                                            BankInStatus = @BankInStatus,  
                                            BankInDt = GETDATE(), 
                                            DeliveredToReceiption = 'N', 
                                            HouseBankAcctNo = @HouseBankAcctNo, 
                                            HouseBankCode = @HouseBankCode, 
                                            HouseBankName = @HouseBankName, 
                                            ChequeStatus = @ChequeStatus
                                        WHERE 
                                            DocNum = @DocNum
                                            AND ChequeNo = @ChequeNo";


                var result = conn.Execute(update_sql,
                    new
                    {
                        BankInSlip = backInSlipFiles,
                        c.BankInStatus,
                        c.HouseBankAcctNo,
                        c.HouseBankCode,
                        c.HouseBankName,
                        c.DocNum,
                        c.ChequeNo,
                        ChequeStatus = "BankIn"
                    });

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult CashSummary_Update(Pay_Dto dto)
        {
            try
            {
                if (dto.CashLog == null)
                {
                    return BadRequest("Cash log is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryKey))
                {
                    return BadRequest("Query key is empty");
                }

                var c = dto.CashLog;

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, c.SubSi);
                if (db == null) return BadRequest("db info is empty");


                // check mc 2 with the exiting file name
                // if got then conbine, if not then overwrite
                var query_exiting = @$"SELECT FILENAME 
                                       FROM [{db.WEBDB}].[dbo].[MC2] WITH (NOLOCK)                                        
                                       WHERE DOCENTRY = @DocNum
                                       AND LINETYPE = 'MCA'";

                using var conn = new SqlConnection(CommDbConnStr);
                var files = conn.Query<string>(query_exiting, new { c.DocNum }).FirstOrDefault();

                var backInSlipFiles = c.BankInSlip; // over write the file
                if (!string.IsNullOrWhiteSpace(files))
                {
                    backInSlipFiles += $",{files}";
                }

                // update portal
                var sql_updateMC = @$"UPDATE [{db.WEBDB}].[dbo].[MC2]
                                      SET FILENAME = @BankInSlip
                                      WHERE DOCENTRY = @DocNum 
                                      AND LINETYPE = @Cash";

                var updateRes = conn.Execute(sql_updateMC, new { BankInSlip = backInSlipFiles, DocNum = c.DocNum, Cash = "MCA" });

                // update the log for cash bank in 
                var update_sql = @$"UPDATE [{db.WEBDB}].[dbo].[FTApp_SellerCashLog] 
                                        SET BankInSlip = @BankInSlip, 
                                            BankInStatus = @BankInStatus,  
                                            BankInDt = GETDATE(), 
                                            DeliveredToCashier = 'N', 
                                            HouseBankAcctNo = @HouseBankAcctNo, 
                                            HouseBankCode = @HouseBankCode, 
                                            HouseBankName = @HouseBankName,
                                            CashStatus = @CashStatus
                                        WHERE 
                                            DocNum = @DocNum";

                var result = conn.Execute(update_sql,
                    new
                    {
                        c.BankInSlip,
                        c.BankInStatus,
                        c.HouseBankAcctNo,
                        c.HouseBankCode,
                        c.HouseBankName,
                        c.DocNum,
                        c.CashStatus
                    });


                return Ok();

                #region posting and sync code

                // check for available remaining cheque
                // if yes , resave the doc as draft
                // if no, save doc as submit (no more changes) 
                //var sql_checkDueCheque = @$"SELECT [id]
                //                                  ,[Subsi]
                //                                  ,[UserCode]
                //                                  ,[ChequeAmount]
                //                                  ,[ChequeDt]
                //                                  ,[ChequeNo]
                //                                  ,[DocNum]
                //                                  ,[BankInStatus]
                //                                  ,[ColllectedDt]
                //                                  ,[BankInDt]
                //                                  ,[CardCode]
                //                                  ,[CardName]
                //                                  ,[HouseBankCode]
                //                                  ,[HouseBankName]
                //                                  ,[HouseBankAcctNo]
                //                                  ,Convert(nvarchar(50), [HeaderGuid]) [HeaderGuid]
                //                                  ,[Remarks]
                //                                  ,[DeliveredToReceiption]
                //                                  ,[DeliveredDt]
                //                                  ,[BankInSlip] 
                //                                  ,[ChequeBankName]
                //                                  ,[ChequeBankCode]
                //                            FROM [{db.WEBDB}].[dbo].[FTApp_SellerChequeLog]
                //                                WHERE DocNum = @DocNum 
                //                                AND BankInStatus = 'N' 
                //                                AND Convert(date, chequeDt) <=  Convert(date, GETDATE())";

                //var due_cheques = conn.Query<FTApp_SellerChequeLog>(sql_checkDueCheque, new { c.DocNum }).ToList();

                //var docUpdateType = "submit";
                //if (due_cheques != null && due_cheques.Count > 0) // had remaining pending cheque not bank in
                //{                         
                //    docUpdateType = "draft";
                //}

                //// query the portal payment head
                //// update the cash entries and bank slip
                //var sql_header = $@"SELECT [id]
                //                      ,[docentry]
                //                      ,[docnum]
                //                      ,[docdate]
                //                      ,[docstatus]
                //                      ,[cardcode]
                //                      ,[cardname]
                //                      ,[refno]
                //                      ,[refnofile]
                //                      ,[doc]
                //                      ,[docfile]
                //                      ,[collector]
                //                      ,[doctotal]
                //                      ,[checkedamt]
                //                      ,[confirmamt]
                //                      ,[cancelamt]
                //                      ,[postamt]
                //                      ,[ucreated]
                //                      ,[dcreated]
                //                      ,[umodified]
                //                      ,[dmodified]
                //                      ,[initno]
                //                      ,[checkedby]
                //                      ,[checkeddate]
                //                      ,[batchid]
                //                      ,convert(nvarchar(50), [guid]) [guid] 
                //                FROM [{db.WEBDB}].[dbo].[FTAPP_PortalPayment] WHERE docnum = @docnum";

                //var payhead = conn.Query<PortalPayment>(sql_header, new { docnum = c.DocNum }).FirstOrDefault();

                //// once pay document head found
                //if (payhead != null)
                //{
                //    var headGuid = payhead.guid;

                //    // update the cash entry file name and bank abs and bank entry
                //    var sql_UpdateCash = @$"UPDATE [{db.WEBDB}].[dbo].[FTApp_PortalPayment_Payments] 
                //                               SET linedate = GETDATE(),
                //                                   bankdate = GETDATE(),
                //                                   filename = @BankInSlip
                //                               WHERE linetype ='MCA' 
                //                               AND HeadGuid = @headGuid";

                //    result = conn.Execute(sql_UpdateCash, new { c.BankInSlip, HeadGuid = headGuid });
                //    // ---------------------

                //    var sql_queryPayments = $@"SELECT 
                //                               [id]
                //                              ,[docentry]
                //                              ,[linetype]
                //                              ,[linecode]
                //                              ,[lineamt]
                //                              ,[linedate]
                //                              ,[lineref]
                //                              ,[bank1]
                //                              ,[bank2]
                //                              ,[filename]
                //                              ,[ChequeNo]
                //                              ,[confirmed]
                //                              ,[canceled]
                //                              ,[bankdate]
                //                              ,[batchid]
                //                              ,[confusr]
                //                              ,[bankentry]
                //                              ,[bankabs]
                //                              ,convert(nvarchar(50), [headGuid]) [headGuid]
                //                              ,convert(nvarchar(50), [lineGuid]) [lineGuid]
                //                        FROM [{db.WEBDB}].[dbo].[FTApp_PortalPayment_Payments] 
                //                               WHERE headGuid = @headGuid";

                //    payhead.payments = conn.Query<Payment>(sql_queryPayments, new { headGuid }).ToList();
                //    // ----------------------

                //    var sql_queryDocs = $@"SELECT 
                //                               [id]
                //                              ,[docentry]
                //                              ,[linenum]
                //                              ,[transid]
                //                              ,[transline]
                //                              ,[sourceid]
                //                              ,[sourcetype]
                //                              ,[sourcedate]
                //                              ,[sourcedoc]
                //                              ,[sourceref]
                //                              ,[sourceamt]
                //                              ,[appliedamt]
                //                              ,[sourceamtfc]
                //                              ,[objectcode]
                //                              ,convert(nvarchar(50), [headGuid]) [headGuid]
                //                              ,convert(nvarchar(50), [lineGuid]) [lineGuid] 
                //                        FROM [{db.WEBDB}].[dbo].[FTApp_PortalPayment_Docs] 
                //                               WHERE headGuid = @headGuid ";

                //    payhead.documents = conn.Query<Document>(sql_queryDocs, new { headGuid }).ToList();
                //    // -------------------- 

                //    // post to portal first
                //    using var httpclient = new HttpClient();
                //    var json = JsonConvert.SerializeObject(payhead); // include line of payment and document
                //    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);
                //    var uri = new Uri($"{WebHostAddrEndPoint}payment/{db.COMPANYID}/{docUpdateType}");

                //    httpclient.DefaultRequestHeaders.Authorization =
                //            new AuthenticationHeaderValue("Bearer", dto.QueryKey);

                //    var response = await httpclient.PostAsync(uri, stringContent);
                //    var isSuccessStatusCode = response.IsSuccessStatusCode;
                //    var lastStatusCode = response.StatusCode;

                //    var content = await response.Content.ReadAsStringAsync();
                //    var portalResult = JsonConvert.DeserializeObject<PayDocResult>(content);
                //}
                #endregion

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult ChequesSummary(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request summary company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Request summary user code is empty");
                }
                if (dto.QuerySummaryDate.Equals(default))
                {
                    return BadRequest("Request summary date is invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("DB name helper return for this company name");
                }

                var sql = @$"SELECT    t0.id
                                      ,t0.Subsi
                                      ,t0.UserCode
                                      ,t0.ChequeAmount
                                      ,t0.ChequeDt
                                      ,t0.ChequeNo
                                      ,t0.DocNum
                                      ,t0.BankInStatus
                                      ,t0.ColllectedDt
                                      ,t0.BankInDt
                                      ,t0.CardCode
                                      ,t0.CardName
                                      ,t0.HouseBankCode
                                      ,t0.HouseBankName
                                      ,t0.HouseBankAcctNo
                                      , CONVERT (nvarchar(50) ,t0.HeaderGuid) HeaderGuid
                                      ,t0.Remarks
                                      ,t0.DeliveredToReceiption
                                      ,t0.DeliveredDt
                                      ,t0.BankInSlip
                                      ,t0.ChequeBankName
                                      ,t0.ChequeBankCode
                            FROM [{db.WEBDB}].[dbo].[FTApp_SellerChequeLog] t0 WITH (NOLOCK) 
                                INNER JOIN 
                                 [{db.WEBDB}].[dbo].[MC] t1 WITH (NOLOCK) ON t1.DOCENTRY = t0.DocNum
                            WHERE t0.UserCode = @UserCode
                            AND t0.SubSi = @CompanyName 
                            AND Convert(date, t0.ChequeDt) <= @QuerySummaryDate 
                            AND t0.BankInStatus = 'N'
                            AND t1.DocStatus != 'L' 
                            and ISNULL(t0.DeliveredToReceiption , '') != 'Y'";

                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<FTApp_SellerChequeLog>(sql,
                                new
                                {
                                    dto.UserCode,
                                    dto.CompanyName,
                                    QuerySummaryDate = $"{dto.QuerySummaryDate:yyyyMMdd}"
                                }).ToList();

                if (results == null) return NotFound();
                return Ok(results);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult ChequesSummary_BankIn(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request summary company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Request summary user code is empty");
                }
                if (dto.QuerySummaryDate.Equals(default))
                {
                    return BadRequest("Request summary date is invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("DB name helper return for this company name");
                }

                var sql = @$"SELECT [id]
                                      ,[Subsi]
                                      ,[UserCode]
                                      ,[ChequeAmount]
                                      ,[ChequeDt]
                                      ,[ChequeNo]
                                      ,[DocNum]
                                      ,[BankInStatus]
                                      ,[ColllectedDt]
                                      ,[BankInDt]
                                      ,[CardCode]
                                      ,[CardName]
                                      ,[HouseBankCode]
                                      ,[HouseBankName]
                                      ,[HouseBankAcctNo]
                                      ,CONVERT (nvarchar(50) ,[HeaderGuid])
                                      ,[Remarks]
                                      ,[DeliveredToReceiption]
                                      ,[DeliveredDt] 
                                      ,[BankInSlip]
                                      ,[ChequeBankName]
                                      ,[ChequeBankCode]
                            FROM [{db.WEBDB}].[dbo].[FTApp_SellerChequeLog] WITH (NOLOCK) 
                            WHERE UserCode = @UserCode
                            AND SubSi = @CompanyName 
                            AND Convert(date, ChequeDt) <= @QuerySummaryDate 
                            AND BankInStatus = 'Y' ";

                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<FTApp_SellerChequeLog>(sql,
                                new { dto.UserCode, dto.CompanyName, QuerySummaryDate = $"{dto.QuerySummaryDate:yyyyMMdd}" }).ToList();

                if (results == null) return NotFound();
                return Ok(results);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


        IActionResult CashSummaries(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request summary company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Request summary user code is empty");
                }
                if (dto.QuerySummaryDate.Equals(default))
                {
                    return BadRequest("Request summary date is invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("DB name helper return for this company name");
                }

                var sql = $@"SELECT t0.Id
                              ,t0.SubSi
                              ,t0.UserCode
                              ,t0.Cash
                              ,t0.DocNum
                              ,t0.BankInStatus
                              ,t0.CollectedDt
                              ,t0.BankInDt
                              ,t0.CardCode
                              ,t0.CardName
                              ,t0.HouseBankCode
                              ,t0.HouseBankName
                              ,t0.HouseBankAcctNo
                              ,CONVERT (nvarchar(50) ,t0.HeaderGuid) [HeaderGuid]
                              ,t0.Remarks
                              ,t0.DeliveredToCashier
                              ,t0.DeliveredDt
                              ,t0.BankInSlip
                              ,t0.CashStatus
                            FROM [{db.WEBDB}].[dbo].[FTApp_SellerCashLog] t0 WITH (NOLOCK)  
                                 INNER JOIN 
                                 [{db.WEBDB}].[dbo].[MC] t1 WITH (NOLOCK) on t1.DocEntry = t0.DocNum 
                            WHERE t0.UserCode= @UserCode 
                            AND t0.SubSi = @CompanyName 
                            AND Convert(date, t0.CollectedDt) <= @QuerySummaryDate 
                            AND t0.BankInStatus = 'N'
                            AND t1.DocStatus != 'L'
                            AND ISNULL(t0.DeliveredToCashier , '') != 'Y'";

                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<FTApp_SellerCashLog>(sql,
                                new { dto.UserCode, dto.CompanyName, QuerySummaryDate = $"{dto.QuerySummaryDate:yyyyMMdd}" }).ToList();

                if (results == null) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult CashSummaries_BankIn(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Request summary company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Request summary user code is empty");
                }
                if (dto.QuerySummaryDate.Equals(default))
                {
                    return BadRequest("Request summary date is invalid");
                }

                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    return BadRequest("DB name helper return for this company name");
                }

                var sql = $@"SELECT [Id]
                              ,[SubSi]
                              ,[UserCode]
                              ,[Cash]
                              ,[DocNum]
                              ,[BankInStatus]
                              ,[CollectedDt]
                              ,[BankInDt]
                              ,[CardCode]
                              ,[CardName]
                              ,[HouseBankCode]
                              ,[HouseBankName]
                              ,[HouseBankAcctNo]
                              ,CONVERT (nvarchar(50) ,[HeaderGuid])
                              ,[Remarks]
                              ,[DeliveredToCashier]
                              ,[DeliveredDt]
                              ,[BankInSlip]
                              ,[CashStatus]
                            FROM [{db.WEBDB}].[dbo].[FTApp_SellerCashLog] WITH (NOLOCK) 
                            WHERE UserCode= @UserCode 
                            AND SubSi = @CompanyName 
                            AND Convert(date, CollectedDt) <= @QuerySummaryDate 
                            AND BankInStatus = 'Y'";

                using var conn = new SqlConnection(CommDbConnStr);
                var results = conn.Query<FTApp_SellerCashLog>(sql,
                                new { dto.UserCode, dto.CompanyName, QuerySummaryDate = $"{dto.QuerySummaryDate:yyyyMMdd}" }).ToList();

                if (results == null) return NotFound();
                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult Aging(Pay_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                {
                    return BadRequest("Aging request company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCardCode))
                {
                    return BadRequest("Aging request uery card code is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (dbInfo == null)
                {
                    return BadRequest("The db info error, please try again later.");
                }

                // run store procedure
                //@sapDb nvarchar(120), 
                //@storeCode as nvarchar(120), 
                //@startDt as datetime, 
                //@endDt As datetime

                var query = "exec sp_SelectStoreAging @sapDb, @storeCode";
                using (var conn = new SqlConnection(CommDbConnStr))
                {
                    var agings = conn.Query<AgingStatement>(query,
                        new
                        {
                            sapDb = dbInfo.SAPDB,
                            storeCode = dto.QueryCardCode,
                        }).ToList();
                    //return Ok(agings);

                    // 20230310 massage cn and invoice to get the picture url 
                    if (agings != null)
                    {
                        // get the web attachment path 
                        var query_webAttchPath = @"select SetupValue from KTCW_COMMON..FTApp_Config 
                                                    Where SetupName = 'AppWebAttachmentPath'";

                        var webPath = conn.ExecuteScalar<string>(query_webAttchPath);


                        for (int d = 0; d < agings.Count; d++)
                        {
                            var doc = agings[d];
                            if (doc == null) continue;

                            if (doc.Type == "CN")
                            {
                                // get the cn docEntry 
                                var query_cnDocEntry = @$"Select docentry from {dbInfo.SAPDB}..ORIN 
                                                          where docnum = @docnum ";

                                var sap_cmentry = conn.ExecuteScalar<int>(query_cnDocEntry, new { docnum = doc.DocNum });
                                if (sap_cmentry <= 0) continue;

                                // seller trcn - create cn at store, return to whs for charge
                                // driver trcn - create cn at store, return whs for charge 
                                var query_Rtn = $@"select top 1 SIGNDOC from {dbInfo.WEBDB}..RTN where CMENTRY = @cmentry";
                                var signedFiles = conn.ExecuteScalar<string>(query_Rtn, new { cmentry = sap_cmentry });
                                if (!string.IsNullOrWhiteSpace(signedFiles))
                                {
                                    agings[d].WebPrefixUrl = webPath;
                                    agings[d].DocTypeDesc = "TRCN";
                                    agings[d].SignedDocNames = signedFiles;
                                    continue;
                                }

                                // seller paf create service cn at store, and upload sign doc
                                var query_paf = $@"select top 1 SIGNFILE from {dbInfo.WEBDB}..PAF where CNNO = @docNum";
                                signedFiles = conn.ExecuteScalar<string>(query_paf, new { docNum = doc.DocNum });

                                if (!string.IsNullOrWhiteSpace(signedFiles))
                                {
                                    agings[d].WebPrefixUrl = webPath;
                                    agings[d].DocTypeDesc = "Seller_CDN_PAF";
                                    agings[d].SignedDocNames = signedFiles;
                                    continue;
                                }

                                // return thru invoice via whs app - create cn at warehouse (cn no signed)
                                // only for reconcile the invoice. 

                                // check bread cn attached file 
                                // check bread sign invoice 
                                // get the inv docEntry 

                                var breadDb1 = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.CompanyName);
                                if (breadDb1 == null) continue;

                                query = $@"Select FILES 
                                                from {breadDb1.WEBDB}..CN with (nolock) 
                                                where CNENTRY = @num ";

                                var docfiles = conn.ExecuteScalar<string>(query, new
                                {
                                    num = sap_cmentry
                                });

                                if (!string.IsNullOrWhiteSpace(docfiles))
                                {
                                    agings[d].WebPrefixUrl = webPath;
                                    agings[d].DocTypeDesc = "Bread_CN";
                                    agings[d].SignedDocNames = docfiles; // file may separated by comma
                                }

                                continue;
                            }

                            if (doc.Type == "IN")
                            {
                                // delivered invoice with DLB only 
                                // dlb 1
                                var query1 = @$"select top 1 SignedFiles from {dbInfo.WEBDB}..FTAPP_DLB1 
                                                Where DocNum = @docNum 
                                                order by id desc ";

                                var signedFiles = conn.ExecuteScalar<string>(query1,
                                    new
                                    {
                                        docNum = doc.DocNum
                                    });

                                if (!string.IsNullOrWhiteSpace(signedFiles))
                                {
                                    agings[d].WebPrefixUrl = webPath;
                                    agings[d].DocTypeDesc = "Seller_Invoice";
                                    agings[d].SignedDocNames = signedFiles; // file may separated by comma
                                    continue;
                                }

                                // check bread sign invoice 
                                // get the inv docEntry 

                                var breadDb = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.CompanyName);
                                if (breadDb == null) continue;

                                var query_invDocEntry = @$"Select docentry from {dbInfo.SAPDB}..OINV 
                                                          where docnum = @docnum ";

                                var sap_invEntry = conn.ExecuteScalar<int>(query_invDocEntry, new { docnum = doc.DocNum });
                                if (sap_invEntry <= 0) continue;

                                query = $@"Select FILES 
                                                from {breadDb.WEBDB}..INV with (nolock) 
                                                where INVENTRY = @num ";

                                var docfiles = conn.ExecuteScalar<string>(query, new
                                {
                                    num = sap_invEntry
                                });

                                if (!string.IsNullOrWhiteSpace(docfiles))
                                {
                                    agings[d].WebPrefixUrl = webPath;
                                    agings[d].DocTypeDesc = "Bread_Invoice";
                                    agings[d].SignedDocNames = docfiles; // file may separated by comma
                                }
                                continue;
                            }
                        }

                        return Ok(agings);
                    }

                    return NotFound();
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult PaymentDoc(Pay_Dto dto)
        {
            try
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
                var db = new DbNameHelper().GetDbInfo(CommDbConnStr, dto.CompanyName);
                if (db == null)
                {
                    _logger.LogError(LastError);
                    return BadRequest("Error reading the company database");
                }

                #endregion

                // do a massage to the payment documents
                // 2021 08 06
                if (dto.PayHead.documents != null && dto.PayHead.documents.Count > 0)
                {
                    // check the doc never being use in other payment document.                     
                    using var conn1 = new SqlConnection(CommDbConnStr);
                    var found = false;
                    var message = "";

                    dto.PayHead.documents.ForEach(c =>
                    {
                        var sql_duplUsage = @$"select top 1 t0.DOCENTRY 
                                                from {db.WEBDB}..mc t0 with (nolock)
                                                inner join 
                                                     {db.WEBDB}..mc3 t1 with (nolock) on t1.DOCENTRY = t0.DOCENTRY 
                                                Where (DOCSTATUS != 'P') and (DOCSTATUS != 'L')
                                                and SOURCEID = @SOURCEID 
                                                and SOURCEDOC = @SOURCEDOC
                                                and SOURCETYPE = @SOURCETYPE ";

                        var sid = conn1.ExecuteScalar<string>(sql_duplUsage, new
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
                                                 FROM {db.SAPDB}..OINV WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                                var docEntry = new SqlConnection(CommDbConnStr)
                                                .ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

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
                                                 FROM {db.SAPDB}..ORIN WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                                var docEntry = conn1.ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

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
                                                 FROM {db.SAPDB}..ORCT WITH (NOLOCK)
                                                 Where DocNum = @DocNum ";

                                var docEntry = new SqlConnection(CommDbConnStr)
                                                .ExecuteScalar<int>(sql_inv, new { DocNum = doc.sourcedoc });

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

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // post with rest client 
                // 20220409
                var client = new RestClient($"{svrAdr}{dto.WebPortalRequest}/{dto.CompanyId}/{dto.UpdateType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKey}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.PayHead);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                // post to portal first
                //using var httpclient = new HttpClient();
                //var json = JsonConvert.SerializeObject(dto.PayHead); // include line of payment and document
                //var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);
                //var uri = new Uri($"{svrAdr}{dto.WebPortalRequest}/{dto.CompanyId}/{dto.UpdateType}");
                //httpclient.DefaultRequestHeaders.Authorization =
                //        new AuthenticationHeaderValue("Bearer", dto.QueryKey);
                //var response = await httpclient.PostAsync(uri, stringContent);
                //var isSuccessStatusCode = response.IsSuccessStatusCode;
                //var lastStatusCode = response.StatusCode;

                // once success posted
                if (!response.IsSuccessful)
                {
                    var content1 = response.Content;
                    var result1 = JsonConvert.DeserializeObject<PortalReplied>(content1);
                    if (result1 == null)
                    {
                        return BadRequest("Error when posting to web portal");
                    }

                    if (dto.Line != null)
                    {
                        dto.Line.PostResult = "Fail";
                        dto.Line.Details = result1.errorMessage;
                        new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result1.errorMessage}\n{result1.actionResult}\n{content1}");
                }

                var content = response.Content; //  await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PayDocResult>(content);
                result.updateDocType = dto.UpdateType;
                result.docType = dto.Request;

                //RemoveApSubRecsSave(result.actionResult, db.WEBDB);
                // remove the sub save rec
                //if (!dto.UpdateType.ToLower().Equals("draft")) return Ok(result);

                // save draft a copy to App db tables 
                var guid = Guid.NewGuid();
                dto.PayHead.guid = $"{guid}";

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
                var insertRes = InsertPayment(dto.PayHead, payments, db.WEBDB, db.SAPDB, dto.CompanyName,
                    dto.UserCode, result.actionResult);

                var messageInsertLog = string.Empty;
                if (insertRes == false)
                {
                    messageInsertLog = "But, Insert Cash / Cheque log fail, please contact support for reupdate.";
                }

                // add in the post success log 
                if (dto.Line != null)
                {
                    dto.Line.PostResult = result.actionResult;
                    dto.Line.Details = $"Success {messageInsertLog}";
                    new AppPostLogHelper().Create(CommDbConnStr, dto.Line);
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        //void RemoveApSubRecsSave(string docNum, string webDb)
        //{
        //    try
        //    {
        //        var sqlPayhead = @$"SELECT convert(nvarchar(50), [guid]) [guid] 
        //                            FROM [{webDb}].[dbo].[FTAPP_PortalPayment] 
        //                            WHERE docnum = @docNum";

        //        using var conn = new SqlConnection(CommDbConnStr);
        //        var head_guid = conn.ExecuteScalar<string>(sqlPayhead, new { docNum });
        //        if (!string.IsNullOrWhiteSpace(head_guid))
        //        {
        //            //// delete the 
        //            //select* from  FTAPP_PortalPayment
        //            //select* from  FTApp_PortalPayment_Docs
        //            //select* from  FTApp_PortalPayment_Payments
        //            //select* from  FTApp_SellerCashLog
        //            //select* from  FTApp_SellerChequeLog

        //            var sql_delete = @$"DELETE FROM [{webDb}].[DBO].[FTAPP_PortalPayment] WHERE guid= @head_guid";
        //            var result = conn.Execute(sql_delete, new { head_guid });

        //            sql_delete = @$"DELETE FROM [{webDb}].[DBO].[FTApp_PortalPayment_Docs] WHERE headGuid= @head_guid";
        //            result = conn.Execute(sql_delete, new { head_guid });


        //            sql_delete = @$"DELETE FROM [{webDb}].[DBO].[FTApp_PortalPayment_Payments] WHERE headGuid= @head_guid";
        //            result = conn.Execute(sql_delete, new { head_guid });


        //            sql_delete = @$"DELETE FROM [{webDb}].[DBO].[FTApp_SellerCashLog] WHERE HeaderGuid= @head_guid";
        //            result = conn.Execute(sql_delete, new { head_guid });


        //            sql_delete = @$"DELETE FROM [{webDb}].[DBO].[FTApp_SellerChequeLog] WHERE HeaderGuid= @head_guid";
        //            result = conn.Execute(sql_delete, new { head_guid });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return;
        //    }
        //}

        // insert for app usage 
        // based on portal data 
        bool InsertPayment(PortalPayment head, List<Payment> payments, string webDb, string sapDb,
            string companyName, string seller, string docNum)
        {
            using var conn = new SqlConnection(CommDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sp_query = @$"exec KTCW_COMMON..sp_ReInsertSellerChequeLog @webDb, @docEntry ;
                                  exec KTCW_COMMON..sp_ReInsertSellerCashLog   @webDb, @docEntry ; ";

                var res = conn.Execute(sp_query, new
                {
                    webDb = webDb,
                    docEntry = int.Parse(docNum)
                }, trans);

                if (res <= 0)
                {
                    trans.Rollback();
                    return false;
                }

                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
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

                using var conn = new SqlConnection(CommDbConnStr);
                return conn.Query<ODSC>(sql, new { bankAbs }).FirstOrDefault();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }


        //void InsertCashLog(FTApp_SellerCashLog cash, string webDb, SqlConnection conn, SqlTransaction trans)
        //{
        //    try
        //    {
        //        var insertCashLog = @$"INSERT INTO [{webDb}].[dbo].[FTApp_SellerCashLog] (
        //                                   SubSi
        //                                  ,UserCode
        //                                  ,Cash
        //                                  ,DocNum
        //                                  ,BankInStatus
        //                                  ,CollectedDt
        //                                  ,CardCode
        //                                  ,CardName
        //                                  ,HeaderGuid    
        //                                  ,CashStatus
        //                                  ) VALUES (
        //                                   @SubSi
        //                                  ,@UserCode
        //                                  ,@Cash
        //                                  ,@DocNum
        //                                  ,@BankInStatus
        //                                  ,@CollectedDt
        //                                  ,@CardCode
        //                                  ,@CardName
        //                                  ,@HeaderGuid
        //                                  ,@CashStatus)";

        //        conn.Execute(insertCashLog, cash, trans);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return;
        //    }
        //}

        //void InsertChequeLog(FTApp_SellerChequeLog cheque, string webDb)
        //{
        //    try
        //    {
        //        var insertCashLog = @$"INSERT INTO [{webDb}].[dbo].[FTApp_SellerChequeLog] (
        //                                   Subsi
        //                                  ,UserCode
        //                                  ,ChequeAmount
        //                                  ,ChequeDt
        //                                  ,ChequeNo
        //                                  ,DocNum
        //                                  ,BankInStatus
        //                                  ,ColllectedDt                                        
        //                                  ,CardCode
        //                                  ,CardName                                        
        //                                  ,HeaderGuid                                          
        //                                  ,ChequeStatus
        //                                  ,ChequeBankName
        //                                  ,ChequeBankCode
        //                                  ) VALUES (
        //                                   @Subsi
        //                                  ,@UserCode
        //                                  ,@ChequeAmount
        //                                  ,@ChequeDt
        //                                  ,@ChequeNo
        //                                  ,@DocNum
        //                                  ,@BankInStatus
        //                                  ,@ColllectedDt
        //                                  ,@CardCode
        //                                  ,@CardName
        //                                  ,@HeaderGuid
        //                                  ,@ChequeStatus
        //                                  ,@ChequeBankName
        //                                  ,@ChequeBankCode)";

        //        var conn = new SqlConnection(CommDbConnStr).Execute(insertCashLog, cheque);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return;
        //    }
        //}

        IActionResult HandlerPaymentQueries(Pay_Dto dto)
        {
            try
            {
                // mandatory field checking
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("Pay doc company id invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Pay doc type invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    return BadRequest("Pay doc code invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKey))
                {
                    return BadRequest("Pay doc query key invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.WebPortalRequest))
                {
                    return BadRequest("The portal request not specify");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(CommDbConnStr, dto.CompanyId);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                var address = $"{svrAdr}{dto.WebPortalRequest}/{dto.CompanyId}/{dto.DocType}/{dto.Code}";
                var client = new RestClient(address);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKey}");
                IRestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    var content = response.Content;
                    return BadRequest($"Request unsuccessful, please try again later." +
                                      $"\nReposnse content :{content}");
                }

                switch (dto.Request)
                {
                    case "Document":
                        {
                            var docs = JsonConvert.DeserializeObject<List<PortalDocument>>(response.Content);
                            if (docs?.Count == 0) return NotFound();
                            docs.ForEach(doc =>
                            {
                                doc.objectcode = doc.transType;

                                // 20241023
                                // cater copy the journal entry as doc num
                                if ($"{doc.documentTypeDesc}".ToLower().Contains("journal") && 
                                    string.IsNullOrWhiteSpace($"{doc.docNum}".Trim()))
                                {
                                    doc.docNum = $"{doc.transId}";
                                }

                            });

                            return Ok(docs);
                        }
                    case "Bank":
                        {
                            var banks = JsonConvert.DeserializeObject<List<PortalBank>>(response.Content);
                            if (banks?.Count == 0) return NotFound();
                            return Ok(banks);
                        }
                    case "HouseBank":
                        {
                            var houseBanks = JsonConvert.DeserializeObject<List<PortalHouseBank>>(response.Content);
                            if (houseBanks?.Count == 0) return NotFound();
                            return Ok(houseBanks);
                        }
                    case "Payment":
                        {
                            var payment = JsonConvert.DeserializeObject<PortalPayment>(response.Content);
                            if (payment == null) return NotFound();
                            return Ok(payment);
                        }
                    default:
                        {
                            return BadRequest("request no found");
                        }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }
    }
}
