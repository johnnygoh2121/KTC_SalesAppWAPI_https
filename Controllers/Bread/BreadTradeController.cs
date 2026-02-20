using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread.Trade;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.DiApi;
using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadTradeController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadTradeController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;
        string _fileSavePath;

        public BreadTradeController(IConfiguration configuration, ILogger<BreadTradeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
            _fileSavePath = _configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult Post(Dto_BreadTrade dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadInvoices":
                    {
                        return LoadInvoices(dto);
                    }
                case "LoadInvoiceLines":
                    {
                        return LoadInvoiceLines(dto);
                    }
                case "Load_Dist_InvoiceLines":
                    {
                        return Load_Dist_InvoiceLines(dto);
                    }
                case "Avail_BreadBatches_Whs_Seller":
                    {
                        return Avail_BreadBatches_Whs_Seller(dto);
                    }
                case "Avail_BreadBatches_Whs_Dist":
                    {
                        return Avail_BreadBatches_Whs_Dist(dto);
                    }
                case "GetCustMaster":
                    {
                        return GetCustMaster(dto);
                    }
                case "GetTaxCode":
                    {
                        return GetTaxCode(dto);
                    }
                case "CreateSellerInvoice":
                    {
                        return CreateSellerInvoice(dto);
                    }
                case "CreateDistOwnStoreInvoice":
                    {
                        return CreateDistOwnStoreInvoice(dto);
                    }
                case "CreateDistCN_KStoreInv":
                    {
                        return CreateDistCN_KStoreInv(dto);
                    }
                case "BreadInvoice_AgingV2":
                    {
                        return BreadInvoice_AgingV2(dto);
                    }
                case "BreadCn_AgingV2":
                    {
                        return BreadCn_AgingV2(dto);
                    }
                case "GetCustomer2":
                    {
                        return GetCustomer2(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetCustomer2(Dto_BreadTrade dto) // get address based on address Type
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("Subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.AddrType))
                {
                    return BadRequest("address type is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db information retrieve error");
                }

                var sql_getcard = @"exec sp_SelectOCRD @erpDB, @cardCode";
                var conn = new SqlConnection(_commDbConnStr);
                var card = conn.Query<OCRD_Ext>(sql_getcard, new
                {
                    erpDB = db.SAPDB,
                    cardCode = dto.CardCode
                }).FirstOrDefault();

                if (card == null) return NotFound();

                var sql_address = @"exec sp_SelectCRD1 @erpDB, @cardCode, @adresType";
                var ship_address = conn.Query<CRD1>(sql_address,
                        new
                        {
                            erpDB = db.SAPDB,
                            cardCode = dto.CardCode,
                            adresType = dto.AddrType // S for ship, b for billing address
                        }).FirstOrDefault();

                if (ship_address == null)
                {
                    return Ok(card);
                }

                // get the ship address;
                card.ShipAdd = ship_address.GetAddress();
                // add to get the address field from CRD1 Address name 
                card.CardName = ship_address.Address;
                return Ok(card);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult BreadCn_AgingV2(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CnNum <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                //var sql = @"exec sp_SelectInvoice_Aging @sapDb, @DocNum";

                var sql = @"exec sp_SelectCN_AgingV2_KStore @webDb, @sapDb, @docNum";
                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var result = conn.Query<ORIN>(sql, new
                {
                    webDb = db.WEBDB,
                    sapDb = db.SAPDB,
                    DocNum = dto.CnNum
                }).FirstOrDefault();

                if (result == null) return NotFound();
                sql = "exec sp_CnLine_KStore @sapDb, @docEntry";
                result.Lines = conn.Query<RIN1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load the batch, if any 
                // load line batch is any 
                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch_KStore @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 14 // credit memo line
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult BreadInvoice_AgingV2(Dto_BreadTrade dto) // for packing invoice printing 
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.InvNum <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                //var sql = @"exec sp_SelectInvoice_Aging @sapDb, @DocNum";

                var sql = @"exec sp_SelectInvoice_AgingV2_KStore @webDb,  @sapDb, @docNum";
                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var result = conn.Query<OINV>(sql, new
                {
                    webDb = db.WEBDB,
                    sapDb = db.SAPDB,
                    DocNum = dto.InvNum
                }).FirstOrDefault();

                // get the bill address 
                // get the s address type 
                // get shipment address
                var sp_queryAddress = @$"SELECT * 
                            FROM {db.SAPDB}..CRD1 WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                var bill_address = conn.Query<CRD1>(sp_queryAddress, new { SoDocStoreCard = result.CardCode }).FirstOrDefault();
                result.Address = bill_address.GetAddress();

                if (result == null) return NotFound();
                sql = "exec sp_SelectInvoiceLine_KStore @sapDb, @docEntry";
                result.Lines = conn.Query<INV1>(sql, new { sapDb = db.SAPDB, docEntry = result.DocEntry }).ToList();

                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch_KStore @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 13 // invoice
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreateDistOwnStoreInvoice(Dto_BreadTrade dto)
        {
            try
            {
                #region validation
                if (dto.InvDoc == null)
                {
                    return BadRequest("Invalid doc");
                }
                if (dto.InvDoc.Lines == null)
                {
                    return BadRequest("Invalid doc lines");
                }
                if (dto.InvDoc.Lines.Count == 0)
                {
                    return BadRequest("Invalid doc lines (0) lines");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveType))
                {
                    return BadRequest("Invalid save type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                #endregion 

                var breadDocHelper = new Bread_INVDocHelper();
                long docEntry = -1;

                if (dto.InvDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBreadInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraftInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }

                if (docEntry == -1)
                {
                    return BadRequest(breadDocHelper.LastErrorMessage);
                }

                // if save draft then return doc entry
                if (dto.SaveType == "draft")
                {
                    var draft_replied = new BreadDocReplied
                    {
                        DocEntry = docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };

                    return Ok(draft_replied);
                }

                // update the doc status as complete
                var updateSql = $@"Update {db.WEBDB}..INV Set DocStatus = 'C' Where DocEntry = @docEntry";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                conn.Execute(updateSql, new { docEntry = docEntry });

                var sql3 = $@"select DOCNUM from {db.WEBDB}..INV with (nolock)
                                    Where DOCENTRY = @docentry";

                // replied the success create of the INv doc 
                // but wait posting to SAP
                var invoiceNum = conn.ExecuteScalar<string>(sql3, new { docentry = docEntry });

                var replied3 = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = invoiceNum
                };

                return Ok(replied3);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreateSellerInvoice(Dto_BreadTrade dto)
        {
            try
            {
                // prevent double post 
                // UserTransToken_PostPick

                // check the user in dlb creation 
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    goto ByPassTransCheck;
                    //return BadRequest("bad user login, please log out app, " +
                    //    "and login again to refresh login token. Thanks");
                }

                // check the memory for the key exist
                if (Program.UserTransToken_BreadCreateInv == null)
                    Program.UserTransToken_BreadCreateInv = new Dictionary<string, bool>();

                // check user token in list
                var isListed = Program.UserTransToken_BreadCreateInv.ContainsKey(dto.UserCode);

                if (isListed) // yes in 
                {
                    bool inTran = Program.UserTransToken_BreadCreateInv[dto.UserCode];
                    if (inTran)
                    {
                        return BadRequest("Post picking in process, please wait for moment. Thanks.");
                    }
                    else
                    {
                        Program.UserTransToken_BreadCreateInv[dto.UserCode] = true;
                    }
                }
                else // no then add in and set true 
                {
                    Program.UserTransToken_BreadCreateInv.Add(dto.UserCode, true); // add and set to intrans
                }

            ByPassTransCheck:

                #region validation
                if (dto.InvDoc == null)
                {
                    return BadRequest("Invalid doc");
                }
                if (dto.InvDoc.Lines == null)
                {
                    return BadRequest("Invalid doc lines");
                }
                if (dto.InvDoc.Lines.Count == 0)
                {
                    return BadRequest("Invalid doc lines (0) lines");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid SubSi");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveType))
                {
                    return BadRequest("Invalid save type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                #endregion 

                var breadDocHelper = new Bread_INVDocHelper();
                long docEntry = -1;

                if (dto.InvDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBreadInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraftInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }

                if (docEntry == -1)
                {
                    return BadRequest(breadDocHelper.LastErrorMessage);
                }

                // if save draft then return doc entry
                if ($"{dto.SaveType}".ToLower() == "draft")
                {
                    var draft_replied = new BreadDocReplied
                    {
                        DocEntry = docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };
                    return Ok(draft_replied);
                }

                var conn = new SqlConnection(_commDbConnStr_Bread);

                // create SAP invoice 
                var seriesName = "";
                var errorMsg = HandlerSellerCreateInvoice(conn, db, docEntry, out seriesName);

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    return BadRequest($"Doc# {docEntry} saved draft,\n" + errorMsg);
                }

                // add int he check for create invoice
                BreadInvCnChecker.CommDbConnStr_Bread = _commDbConnStr_Bread;
                var found_Inv = BreadInvCnChecker.GetPostedInv(db, docEntry); 
                var found_Cn  = BreadInvCnChecker.GetPostedCN(db, docEntry);

                // update the inv table as draft
                if (found_Inv == null)
                {
                    var update_portal_inv = @$"Update {db.WEBDB}..INV 
                                         set DOCSTATUS = 'D',
                                         DOCNUM = '', 
                                         SAPINV = null , 
                                         INVENTRY = null , 
                                         CNENTRY = null
                                         WHERE DOCENTRY = @docEntry;";

                    var upd = conn.Execute(update_portal_inv, new { docEntry });
                    return BadRequest("Server busy, please try submit again. doc save as draft Thanks. [E963IN]");
                }

                // reupdate the inv object 
                // reupdate the cn cm entry again 
                var update_inv1 = @$"Update {db.WEBDB}..INV 
                                        set DOCSTATUS = 'C',
                                        DOCNUM = @portalDocNum, 
                                        SAPINV = 'Y' , 
                                        INVENTRY = @sapInvDocEntry,
                                        CNENTRY = @sapCnDocEntry
                                        WHERE DOCENTRY = @docEntry;";

                var cnDocEntry = found_Cn == null ? null : $"{found_Cn.DocEntry}";

                var newConn = new SqlConnection(_commDbConnStr_Bread);
                var updateRes1 = newConn.Execute(update_inv1, new
                {
                    portalDocNum = $"{seriesName}{found_Inv.DocNum}",
                    sapInvDocEntry = found_Inv.DocEntry,
                    sapCnDocEntry = cnDocEntry,
                    docEntry = docEntry
                });

                // create sap invoice 
                var replied3 = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = $"{found_Inv.DocNum}"
                };

                return Ok(replied3);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(dto.UserCode) && Program.UserTransToken_BreadCreateInv.Count > 0)
                {
                    Program.UserTransToken_BreadCreateInv.Remove(dto.UserCode);
                }
            }
        }

        //OINV GetPostedInv(DbInfo db, long portalDocEntry)
        //{
        //    try
        //    {
        //        using var conn = new SqlConnection(_commDbConnStr_Bread);
        //        // by pass current locked row with READPAST
        //        var sp_QuerySapInv = $@"Select top 1 * 
        //                                From {db.SAPDB}..OINV  with (READPAST)
        //                                Where U_SOENTRY = @portalDocEntry
        //                                ORDER BY DocEntry DESC ; ";

        //        for (int i = 0; i < 3; i++)
        //        {
        //            var found_Inv = conn.Query<OINV>(sp_QuerySapInv, new { portalDocEntry }).FirstOrDefault();                    
        //            if (found_Inv != null)
        //            {
        //                return found_Inv;
        //            }
        //            Thread.Sleep(500);
        //        }
        //        return null;
        //    }
        //    catch (Exception except)
        //    {
        //        LastError = $"{except.Message}\n{except.StackTrace}";
        //        _logger.LogError(LastError);
        //        return null;
        //    }
        //}

        //ORIN GetPostedCN(DbInfo db, long portalDocEntry)
        //{
        //    try
        //    {
        //        using var conn = new SqlConnection(_commDbConnStr_Bread);
        //        // by pass current locked row with READPAST
        //        var sp_QuerySapInv = $@"Select top 1 * 
        //                                From {db.SAPDB}..ORIN  with (READPAST)
        //                                Where U_SOENTRY = @portalDocEntry
        //                                ORDER BY DocEntry DESC ; ";

        //        for (int i = 0; i < 3; i ++ )
        //        {
        //            var found_Cn = conn.Query<ORIN>(sp_QuerySapInv, new { portalDocEntry }).FirstOrDefault();
        //            // first time 
        //            if (found_Cn != null)
        //            {
        //                return found_Cn;
        //            }
        //            Thread.Sleep(500);
        //        }                      
        //        return null;

        //    }
        //    catch (Exception except)
        //    {
        //        LastError = $"{except.Message}\n{except.StackTrace}";
        //        _logger.LogError(LastError);
        //        return null;
        //    }
        //}

        string HandlerSellerCreateInvoice(SqlConnection conn, DbInfo db, long docEntry, out string seriesName)
        {
            seriesName = "";
            LastError = "";
            try
            {
                // parepare the invoice data table 
                var query = $@"
                                SELECT T0.*
                                , T1.CARDCODE AS [SAPCARDCODE]
                                , T2.INVARINVSERIES AS [INVSERIES]
                                , T2.INVARCNSERIES AS [CNSERIES]
                                , CASE WHEN ISNULL(U0.DEFWHS,'') = '' 
                                       THEN T2.WHSCODE 
                                       ELSE U0.DEFWHS END AS [WHSCODE]
                                , T3.SERIESNAME
                                , U0.SLPCODE AS [SAPSLPCODE]
                                , T4.PRCCODE AS [DIM1]
                                FROM {db.WEBDB}..INV T0 with (nolock)
                                INNER JOIN {db.SAPDB}..OCRD T1 with (nolock) ON 
                                            CASE WHEN ISNULL(T1.U_PORTALID,'') = '' THEN T1.CardCode 
                                                 ELSE T1.U_PORTALID END = T0.CARDCODE 
                                            AND T1.CARDTYPE = 'C' 
                                            AND T1.FrozenFor = 'N'
                                LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 with (nolock) ON T2.RECID = 1 
                                LEFT OUTER JOIN {db.WEBDB}..USERS U0 with (nolock) ON U0.USERID = T0.UMODIFIED 
                                LEFT OUTER JOIN {db.SAPDB}..NNM1 T3 with (nolock) ON T3.SERIES = T2.INVARINVSERIES 
                                LEFT OUTER JOIN {db.SAPDB}..OPRC T4 with (nolock) ON T4.PRCCODE = T1.U_COSTCTR 
                                           AND T4.DimCode = '1'
                                WHERE T0.DOCENTRY = '{docEntry}'";

                var inv_dt = GetDataTable(conn, query);
                if (inv_dt == null)
                {
                    return LastError;
                }

                var sapcard = inv_dt.Rows[0]["SAPCARDCODE"].ToString();
                seriesName = inv_dt.Rows[0]["SERIESNAME"].ToString();

                if (string.IsNullOrWhiteSpace(sapcard))
                {

                    return LastError + "\nInvalid empty SAP CardCode when query inv header";
                }

                var priceList = db.DEF_PRICELIST;
                if (priceList < 0)
                {
                    return LastError + "\nInvalid peice list setup, when query inv header";
                }

                query = $@"SELECT T1.*
                            , T1.PRICE AS [CUSTPRICE]
                            , T5.Price AS [SUPPLIERPRICE]
                            , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
                                   ELSE T6.U_CSUS_UOM END AS [CSUOM]
                            , ISNULL(T7.GLCN,'') AS [CNGL]
                            , T8.PRCCODE AS [DIM2] 
                            , ISNULL(T6.ManBtchNum, 'N') [MANBATCHNUM] 

                            FROM {db.WEBDB}..INV T0 with (nolock) 
                                INNER JOIN {db.WEBDB}..INV1 T1 with (nolock) ON T1.DOCENTRY = T0.DOCENTRY 

                                INNER JOIN {db.SAPDB}..OCRD T2 with (nolock) ON T2.U_PORTALID = T0.CARDCODE 
                                    AND T2.CARDTYPE = 'C' 
                                    AND T2.FrozenFor = 'N' 
                                    AND T2.CardCode = '{sapcard}' 

                            LEFT OUTER JOIN {db.SAPDB}..OCRD T3 with (nolock) ON T3.CardCode = T0.COMPANYID
                            LEFT OUTER JOIN {db.SAPDB}..ITM1 T4 with (nolock) ON T4.ItemCode = T1.ITEMCODE 
                                                                             AND T4.PriceList = T2.ListNum 

                            LEFT OUTER JOIN {db.SAPDB}..ITM1 T5 with (nolock) ON T5.ItemCode = T1.ITEMCODE 
                                                                             AND T5.PriceList = 
                                                                                        CASE WHEN '{db.DEF_PRICELIST}' = 0 THEN T3.ListNum 
                                                                                        ELSE '{db.DEF_PRICELIST}' END

                            LEFT OUTER JOIN {db.SAPDB}..OITM T6 with (nolock) ON T6.ItemCode = T1.ITEMCODE 

                            LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 with (nolock) ON T7.ITEMCODE = T1.ITEMCODE

                            LEFT OUTER JOIN {db.SAPDB}..OPRC T8 with (nolock) ON T8.PRCCODE = T6.CardCode 
                                                                             AND T8.DimCode = '2' 
                            WHERE T0.DOCENTRY = {docEntry}";

                var inv1_dt = GetDataTable(conn, query);
                if (inv_dt == null)
                {
                    return LastError + "\nInvalid peice list setup, when query inv details.";
                }

                var diapiHelper = new BreadDiApi_Trade(db, _fileSavePath);
                var noCn = true;

                return diapiHelper.CreateInvCN($"{docEntry}", inv_dt, inv1_dt, noCn); // just create invoice , no cn
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                seriesName = "";
                return LastError;
            }
        }

        DataTable GetDataTable(SqlConnection conn, string query)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd); // create data adapter                
                DataTable dt = new DataTable();
                da.Fill(dt); // this will query your database and return the result to your datatabled
                da.Dispose();
                return dt;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }

        IActionResult CreateDistCN_KStoreInv(Dto_BreadTrade dto)
        {
            try
            {
                #region validation
                if (dto.InvDoc == null)
                {
                    return BadRequest("Invalid doc");
                }
                if (dto.InvDoc.Lines == null)
                {
                    return BadRequest("Invalid doc lines");
                }
                if (dto.InvDoc.Lines.Count == 0)
                {
                    return BadRequest("Invalid doc lines (0) lines");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveType))
                {
                    return BadRequest("Invalid save type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                #endregion

                // create portal cn
                var breadDocHelper = new Bread_INVDocHelper();
                long docEntry = -1;
                if (dto.InvDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBreadInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraftInvoice(dto.InvDoc, db, _commDbConnStr_Bread);
                }

                if (docEntry == -1)
                {
                    return BadRequest(breadDocHelper.LastErrorMessage);
                }

                // if save draft then return doc entry
                if (dto.SaveType == "draft")
                {
                    var draft_replied = new BreadDocReplied
                    {
                        DocEntry = docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };

                    return Ok(draft_replied);
                }

                // create SAP CN then KTC store invoice
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var errorMsg = HandlerDistCreateCN_KStore_Invoice(conn, db, docEntry);
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    return BadRequest(errorMsg);
                }

                // replied the success create of the inv doc 
                // but wait posting to SAP
                var sql3 = $@"select t0.DocNum 
                                    from {db.SAPDB}..OINV t0 with (nolock)
                                    inner join 
                                        {db.WEBDB}..INV t1 with (nolock) on t1.INVENTRY = t0.DocEntry 
                                    Where t1.DOCENTRY = @docentry";

                var invoiceNum = conn.ExecuteScalar<string>(sql3, new { docentry = docEntry });
                var replied = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = invoiceNum
                };

                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        string HandlerDistCreateCN_KStore_Invoice(SqlConnection conn, DbInfo db, long docEntry)
        {
            try
            {
                LastError = "";
                // parepare the invoice data table 
                var query = @$"SELECT T0.*
                            , T1.CARDCODE AS [SAPCARDCODE]
                            , T2.INVARINVSERIES AS [INVSERIES]
                            , T2.INVARCNSERIES AS [CNSERIES]
                            , CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE 
                                   ELSE U0.DEFWHS END AS [WHSCODE]
                            , T3.SERIESNAME
                            , U0.SLPCODE AS [SAPSLPCODE]
                            , T4.PRCCODE AS [DIM1] 
                            FROM {db.WEBDB}..INV T0 INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T1 ON 
                                    CASE WHEN ISNULL(T1.U_PORTALID,'') = '' THEN T1.CardCode 
                                    ELSE T1.U_PORTALID END = T0.CARDCODE 
                                        AND T1.CARDTYPE = 'C' AND T1.FrozenFor = 'N' 
                            LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 ON T2.RECID = 1 
                            LEFT OUTER JOIN {db.WEBDB}..USERS U0 ON U0.USERID = T0.UMODIFIED 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[NNM1] T3 ON T3.SERIES = T2.INVARINVSERIES 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T4 ON T4.PRCCODE = T1.U_COSTCTR AND T4.DimCode = '1' 
                            WHERE T0.DOCENTRY = '{docEntry}' ";

                var inv_dt = GetDataTable(conn, query);
                if (inv_dt == null)
                {
                    return LastError;
                }

                var sapcard = inv_dt.Rows[0]["SAPCARDCODE"].ToString();
                var priceList = db.DEF_PRICELIST;

                query = $@"SELECT T1.*
                            , T1.PRICE AS[CUSTPRICE]
                            , T5.Price AS[SUPPLIERPRICE]
                            , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
                                   ELSE T6.U_CSUS_UOM END AS [CSUOM]
                            , ISNULL(T7.GLCN,'') AS [CNGL]
                            , T8.PRCCODE AS [DIM2] 
                            , T6.ManBtchNum [MANBATCHNUM]
                            FROM {db.WEBDB}..INV T0 
                            INNER JOIN {db.WEBDB}..INV1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
                            INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T2 ON T2.U_PORTALID = T0.CARDCODE 
                                    AND T2.CARDTYPE = 'C' 
                                    AND T2.FrozenFor = 'N' AND T2.CardCode = '{sapcard}' 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
                                    AND T4.PriceList = T2.ListNum 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
                                    AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum 
                                                            ELSE '{priceList}' END
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
                            LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T8 ON T8.PRCCODE = T6.CardCode AND T8.DimCode = '2' 
                            WHERE T0.DOCENTRY = '{docEntry}'";

                var inv1_dt = GetDataTable(conn, query);
                if (inv1_dt == null)
                {
                    return LastError;
                }

                var diapiHelper = new BreadDiApi_Trade(db, _fileSavePath);
                return diapiHelper.CreateInvCN($"{docEntry}", inv_dt, inv1_dt, false); // create invoice for ktc store , and cn for dist
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return LastError;
            }
        }

        IActionResult GetTaxCode(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"exec sp_GetTaxCode @webDb";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var taxcodes = conn.Query<Bread_TAXGROUP>(sql, new
                {
                    webDb = db.WEBDB,
                    companyId = dto.UserCode
                }).ToList();

                if (taxcodes.Count == 0) return NotFound();
                return Ok(taxcodes);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetCustMaster(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // cardcode
                {
                    return BadRequest("Invalid item code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"exec sp_GetCustMaster @webDb , @companyId";
                var conn = new SqlConnection(_commDbConnStr_Bread);

                var custs = conn.Query<Bread_CUSTMASTER>(sql, new
                {
                    webDb = db.WEBDB,
                    companyId = dto.UserCode
                }).ToList();

                if (custs.Count == 0) return NotFound();

                return Ok(custs);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //IActionResult GetCustMaster_Single(Dto_BreadTrade dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.Subsi))
        //        {
        //            return BadRequest("Invalid subsi");
        //        }
        //        if (string.IsNullOrWhiteSpace(dto.CardCode)) // cardcode
        //        {
        //            return BadRequest("Invalid card code");
        //        }

        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
        //        if (db == null)
        //        {
        //            return BadRequest("Invalid db info");
        //        }

        //        var sql = @$"Select * from {db.WEBDB}..CUSTMASTER Where Cardcode = @cardCode";
        //        var conn = new SqlConnection(_commDbConnStr_Bread);

        //        var custs = conn.Query<Bread_CUSTMASTER>(sql, new
        //        {
        //            webDb = db.WEBDB,
        //            companyId = dto.UserCode
        //        }).ToList();

        //        if (custs.Count == 0) return NotFound();

        //        return Ok(custs);

        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult Avail_BreadBatches_Whs_Dist(Dto_BreadTrade dto) // for seller
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("Invalid item code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // cardcode
                {
                    return BadRequest("Invalid item code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"exec sp_Avail_BreadBatches_Dist @webDb, @itemCode, @userCode";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var batches = conn.Query<FTAPP_Batch>(sql, new
                {
                    webDb = db.WEBDB,
                    itemCode = dto.ItemCode,
                    userCode = dto.UserCode
                }).ToList();

                if (batches.Count == 0) return NotFound();

                return Ok(batches);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Avail_BreadBatches_Whs_Seller(Dto_BreadTrade dto) // for seller
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("Invalid item code");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("invalid whs code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = @"exec sp_Avail_BreadBatches_Seller @webDb, @itemCode, @whsCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var batches = conn.Query<FTAPP_Batch>(sql, new
                {
                    webDb = db.WEBDB,
                    itemCode = dto.ItemCode,
                    whsCode = dto.WhsCode
                }).ToList();

                if (batches.Count == 0) return NotFound();

                return Ok(batches);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Load_Dist_InvoiceLines(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("invalid user type");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var sql = $@"exec sp_GetInvoiceLine_Seller @webDb, @docEntry";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var invoiceLines = conn.Query<Bread_INV1_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry,
                }).ToList();

                if (invoiceLines.Count == 0) return NotFound();
                return Ok(invoiceLines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInvoiceLines(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }

                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("invalid user type");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }


                var sql = dto.UserType == "DI" ? $@"exec sp_GetInvoiceLine_Dist @webDb, @docEntry" :
                                                 $@"exec sp_GetInvoiceLine_Seller @webDb, @docEntry";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var invoiceLines = conn.Query<Bread_INV1_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry,
                }).ToList();

                if (invoiceLines.Count == 0) return NotFound();
                return Ok(invoiceLines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadInvoices(Dto_BreadTrade dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }
                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("Invalid user type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var sql = dto.UserType == "DI" ? $@"exec [sp_GetInvoices_Distv2] @webDb, @userCode, @startDt, @endDt" :
                                                 $@"exec sp_GetInvoices_Seller  @webDb, @userCode, @startDt, @endDt";


                var conn = new SqlConnection(_commDbConnStr_Bread);
                var invoices = conn.Query<Bread_OINV_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();
                if (invoices.Count == 0) return NotFound();
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
