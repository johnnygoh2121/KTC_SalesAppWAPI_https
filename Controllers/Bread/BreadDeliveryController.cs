using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread.DeliveryOrder;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.DiApi;
using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.Bread;
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

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadDeliveryController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadDeliveryController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;
        string _fileSavePath;
        public BreadDeliveryController(IConfiguration configuration, ILogger<BreadDeliveryController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
            _fileSavePath = _configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult Post(Dto_BreadDO dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "SaveBreadDoLinesDraft":
                    {
                        return SaveBreadDoLinesDraft(dto);
                    }
                case "GetBreadDoLinesDraft":
                    {
                        return GetBreadDoLinesDraft(dto);
                    }
                case "LoadDoList":
                    {
                        return LoadDoList(dto);
                    }
                case "LoadDoListLines":
                    {
                        return LoadDoListLines(dto);
                    }
                case "UpdateDoStatus":
                    {
                        return UpdateDoStatus(dto);
                    }
                case "GetBreadCard":
                    {
                        return GetBreadCard(dto);
                    }
                case "GetBreadCards":
                    {
                        return GetBreadCards(dto);
                    }
                case "GetInnerCards":
                    {
                        return GetInnerCards(dto);
                    }
                case "GetListOfTransfer":
                    {
                        return GetListOfTransfer(dto);
                    }
                case "GetPickedDoLines":
                    {
                        return GetPickedDoLines(dto);
                    }
                case "GetPickedDoLines1":
                    {
                        return GetPickedDoLines1(dto);
                    }
                case "ClearLastDraftLines":
                    {
                        return ClearLastDraftLines(dto);
                    }
                case "GetDistributerCards": // for all transporter selection
                    {
                        return GetDistributerCards(dto);
                    }
                case "GetDistributer_SellerCards":
                    {
                        return GetDistributer_SellerCards(dto);
                    }
                case "CreateRequest": // create local distributer + outstation dist invoice
                    {
                        return CreateRequest(dto);
                    }
                case "CreateDistKTC_Inv":
                    {
                        return CreateDistKTC_Inv(dto);
                    }
                case "CreateDistKTC_TransporterToDistInv":
                    {
                        return CreateDistKTC_TransporterToDistInv(dto);
                    }
                case "Avail_BreadBatches":
                    {
                        return Avail_BreadBatches(dto);
                    }
                case "IsReceiverSeller":
                    {
                        return IsReceiverSeller(dto);
                    }
                case "CreateITFromIT":
                    {
                        return CreateITFromIT(dto);
                    }
                //case "LoadTransportersTruck":
                //    {
                //        return LoadTransportersTruck(dto);
                //    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        //IActionResult LoadTransportersTruck(Dto_BreadDO dto)
        //{
        //    try
        //    {               
        //        if (string.IsNullOrWhiteSpace(dto.SubSi))
        //        {
        //            return BadRequest("invalid receivercode");
        //        }

        //        var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
        //        if (db == null)
        //        {
        //            return BadRequest("Invalid dbi");
        //        }

        //        var sql = $@"exec sp_LoadTransportersTruck @webDb_bread";

        //        var founds = new SqlConnection(_commDbConnStr_Bread).Query<Bread_Truck>(sql, new
        //        {
        //            webDb_bread = db.WEBDB
        //        }).ToList();

        //        if (founds == null) return NotFound();
        //        return Ok(founds);
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return BadRequest($"request not handler.\n{LastError}");
        //    }
        //}

        IActionResult IsReceiverSeller(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ReceiverCode))
                {
                    return BadRequest("invalid receivercode");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid receivercode");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sql = $@"select * 
                            from {db.WEBDB}..USERS with (nolock)
                            Where COMPANYID = @companyId";

                var found = new SqlConnection(_commDbConnStr_Bread).Query<Bread_User>(sql, new
                {
                    companyId = dto.ReceiverCode
                }).FirstOrDefault();

                if (found == null) return NotFound();
                return Ok(found);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreateITFromIT(Dto_BreadDO dto)
        {
            if (dto.DocRequest == null)
            {
                return BadRequest("Invalid request doc");
            }
            if (dto.Head == null)
            {
                return BadRequest("Invalid request doc head");
            }
            if (dto.Head.Lines == null)
            {
                return BadRequest("INvalid request doc lines");
            }
            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid subsi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid user code");
            }
            if (string.IsNullOrWhiteSpace(dto.UserType))
            {
                return BadRequest("Invalid user type");
            }
            if (dto.PickedGuid == default)
            {
                return BadRequest("Invalid picked GUID");
            }
            if (dto.PickedGuid == null)
            {
                return BadRequest("Invalid picked GUID");
            }
            if (dto.ItDocNum < 0)
            {
                return BadRequest("Invalid IT doc num");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid db info");
            }

            // clear the last save                 
            using var conn = new SqlConnection(_commDbConnStr_Bread);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var delSql = $@"Delete from {db.WEBDB}..FTAPP_MWRequest Where Guid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);

                delSql = $@"Delete from {db.WEBDB}..FTAPP_MWDocHeader Where Guid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);

                delSql = $@"Delete from {db.WEBDB}..FTAPP_MWDocDetails Where HeaderGuid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);

                trans.Commit();

                // insert the request 
                // insert the doc head
                // insert the doc lines 
                var reqHelper = new BreadRequestHelper
                {
                    MidwareDbConnectStr = db.GetWebDbConnStr()
                };

                reqHelper.BeginTransaction();
                var res = reqHelper.InsertRequest(dto.DocRequest);

                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }

                res = reqHelper.InsertDocHeader(dto.Head);
                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }

                res = reqHelper.InsertDocDetailsLine(dto.Head.Lines);

                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }

                // finally
                reqHelper.Commit();

                var sql = $@"SELECT * FROM {db.WEBDB}..FTAPP_MWDocHeader WHERE Guid = @guid";
                var docHead = conn.Query<BreadDocHeader>(sql, new { guid = dto.PickedGuid }).FirstOrDefault();

                sql = $@"SELECT * FROM {db.WEBDB}..FTAPP_MWDocDetails WHERE HeaderGuid = @guid";
                var docDetails = conn.Query<BreadDocDetail>(sql, new { guid = dto.PickedGuid }).ToList();

                if (dto.DocRequest.Request == "Inventory Transfer")
                {
                    //// then inventory transfer 
                    var docRemark = $"BASE IT#{dto.ItDocNum}, {DateTime.Now:dd-MMM-yyyy}";
                    var apiHelper = new BreadDiApi_Delivery(
                                          db
                                        , docHead
                                        , docDetails
                                        , _commDbConnStr_Bread
                                        , docHead.NumberFileAttached
                                        , "OWTR"
                                        , SAPbobsCOM.BoObjectTypes.oStockTransfer
                                        , docRemark
                                        , $"{dto.ItDocNum}");

                    var err = apiHelper.CreateInventryTransfer_ITT2It(dto.UserType);
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        _logger.LogError(err);
                        return BadRequest(err);
                    }

                    return Ok(apiHelper.PostedDocNum);
                }

                return BadRequest("Request no handler, pls contact support or help.");

            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreateDistKTC_TransporterToDistInv (Dto_BreadDO dto)
        {
            try
            {
                #region validation
                if (dto.DoDraftHead == null)
                {
                    return BadRequest("Invalid DO head");
                }
                if (dto.DoLines == null)
                {
                    return BadRequest("Invalid DO lines");
                }
                if (dto.DoLines.Count == 0)
                {
                    return BadRequest("Invalid DO lines (0l)");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("in valid head guid");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.InvntryTrnsfrDocEntry <= 0)
                {
                    return BadRequest("Invalid inventory entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                #endregion

                // massage the delivery line 
                // covert the data into the portal invoice 

                // get cardcode currency 
                var distCardQuery = @$"Select * from {db.SAPDB}..OCRD with (nolock)
                                    Where CardCode = @CardCode";

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var distCard = conn.Query<OCRD_Ext>(distCardQuery, new
                {
                    CardCode = dto.DoDraftHead.CardCode
                }).FirstOrDefault();

                if (distCard == null)
                {
                    return BadRequest("Invalid card code, no card code no exist");
                }

                // query the user id from bread portal 
                var queryUser = $@"select * from {db.WEBDB}..USERS with (nolock)
                                   Where UserCode = @UserCode ";

                var user = conn.Query<Bread_User>(queryUser, new
                {
                    UserCode = dto.DoDraftHead.UserCode
                }).FirstOrDefault();
                
                if (user == null)
                {
                    return BadRequest("Invalid card code, no user no exist");
                }

                var transporterUserWhsCode = user.DEFWHS; // for invoice creation

                var newInvDoc = new Bread_OINV_Ext
                {
                    Subsi = dto.DoDraftHead.SubSi,
                    SubsiId = dto.DoDraftHead.SubSiID,
                    DOCENTRY = 0,
                    DOCSTATUS = "D",
                    DOCNUM = "0",
                    COMPANYID = distCard.CardCode,
                    CARDCODE = distCard.CardCode,
                    CARDNAME = distCard.CardName,
                    DOCDATE = DateTime.Now,
                    CURRENCY = distCard.Currency,
                    DOCRATE = 0,
                    CUSTREF = dto.DoDraftHead.Comments,
                    TOTALBD = 0,
                    TAXSUM = 0,
                    ROUNDING = 0,
                    DOWNPAYMENT = 0,
                    DOCTOTAL = 0,
                    PRICEID = 0,
                    REMARKS = dto.DoDraftHead.Comments,
                    UCREATED = $"{user.USERID}",
                    DCREATED = DateTime.Now,
                    UMODIFIED = $"{user.USERID}",
                    DMODIFIED = DateTime.Now,
                    PAIDTODATE = 0,
                    FILES = dto.Files
                };

                // prepare the inv lines 
                var invLines = new List<Bread_INV1_Ext>();
                var invBatchLines = new List<Bread_Batch>();
                for (int i = 0; i < dto.DoLines.Count; i++)
                {
                    var line = dto.DoLines[i];
                    if (line == null) continue;

                    var queryPrice = $@"exec sp_GetDistItemPrice @webDb, @cardCode, @itemCode";
                    decimal price = conn.Query<decimal>(queryPrice, new
                    {
                        webDb = db.WEBDB,
                        cardCode = distCard.CardCode,
                        itemCode = line.ItemCode
                    }).FirstOrDefault();

                    var newInvLine = new Bread_INV1_Ext
                    {
                        DOCENTRY = 0,
                        LINENUM = i,
                        ITEMCODE = line.ItemCode,
                        ITEMNAME = line.ItemName,
                        QUANTITY = line.QtyInPcs,
                        PRICE = price,
                        TAXCODE = "",
                        TAXPERC = 0,
                        TAXSUM = 0,
                        LINETOTAL = 0,
                        LINETYPE = ""
                    };

                    // massage the tax and line 
                    var totalAmt = (newInvLine.QUANTITY * newInvLine.PRICE);
                    var taxSum = totalAmt * newInvLine.TAXPERC;
                    var linesTotal = taxSum + totalAmt;

                    newInvLine.TAXSUM = taxSum;
                    newInvLine.LINETOTAL = linesTotal;
                    invLines.Add(newInvLine);

                    // crate the batch object
                    if (!string.IsNullOrWhiteSpace(line.Batch))
                    {
                        var newBatch = new Bread_Batch
                        {
                            DocEntry = 0,
                            LineNum = i,
                            LineNum2 = 0,
                            Quantity = line.QtyInPcs
                        };
                        invBatchLines.Add(newBatch);
                    }
                }

                newInvDoc.Lines = invLines;
                newInvDoc.Batches = invBatchLines;
                var sumLines = invLines.Sum(x => x.LINETOTAL);
                var sumTax = invLines.Sum(x => x.TAXSUM);
                newInvDoc.DOCTOTAL = sumLines;
                newInvDoc.TAXSUM = sumTax;
                newInvDoc.TOTALBD = sumLines - sumTax;

                // create portal cn
                var breadDocHelper = new Bread_INVDocHelper();
                long docEntry = -1;
                if (newInvDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBreadInvoice(newInvDoc, db, _commDbConnStr_Bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraftInvoice(newInvDoc, db, _commDbConnStr_Bread);
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

                // 20220421
                // using single transation for doc creation 
                var helper = new BreadDiApi_Delivery_Dist_Transport2Inv(_commDbConnStr_Bread)
                {
                    Db = db,
                    SvrPath = _fileSavePath,
                    Docentry = $"{docEntry}",
                    CurSapTableName = "OINV"
                };

                var errMsg = helper.CreateInvoice_Dist(newInvDoc, newInvDoc.Lines, transporterUserWhsCode);
                if (!string.IsNullOrWhiteSpace(errMsg))
                {
                    return BadRequest(errMsg);
                }

                // replied the success create of the inv doc 
                // but wait posting to SAP
                var sql3 = $@"select t0.DocNum
                            from {db.SAPDB}..OINV t0 with (nolock)
                            inner join 
                                {db.WEBDB}..INV t1 with (nolock) on t1.INVENTRY = t0.DocEntry 
                            Where t1.DOCENTRY = @docentry";

                var invDocNum = conn.ExecuteScalar<string>(sql3, new { docentry = docEntry });
                var replied = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = !string.IsNullOrWhiteSpace(helper.PostedDocNum) ? helper.PostedDocNum : invDocNum 
                };

                if (conn.State == ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    var updateDraft_sql = @"Update CR_COMMON..FTAPP_BreadDODraftHead 
                                        Set DocStatus = 'Invoiced' Where HeadGuid = @guid";
                    conn.Execute(updateDraft_sql, new { guid = dto.HeadGuid }, trans);

                    if (dto.InvntryTrnsfrDocEntry > 0)
                    {
                        var updateOWTRDocEntry = @$"Update {db.SAPDB}..OWTR 
                                            Set U_SOENTRY = @invEntry_portal
                                            Where DocEntry = @invntryTrnsfrDocEntry";

                        conn.Execute(updateOWTRDocEntry, new
                        {
                            invEntry_portal = replied.DocEntry,
                            invntryTrnsfrDocEntry = dto.InvntryTrnsfrDocEntry
                        }, trans);
                    }

                    trans.Commit();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }

                return Ok(replied);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CreateDistKTC_Inv(Dto_BreadDO dto)
        {
            try
            {
                #region validation
                if (dto.DoDraftHead == null)
                {
                    return BadRequest("Invalid DO head");
                }
                if (dto.DoLines == null)
                {
                    return BadRequest("Invalid DO lines");
                }
                if (dto.DoLines.Count == 0)
                {
                    return BadRequest("Invalid DO lines (0l)");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("in valid head guid");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }
                #endregion

                // massage the delivery line 
                // covert the data into the portal invoice 

                // get cardcode currency 
                var distCardQuery = @$"Select * from {db.SAPDB}..OCRD with (nolock)
                                    Where CardCode = @CardCode";

                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var distCard = conn.Query<OCRD_Ext>(distCardQuery, new
                {
                    CardCode = dto.DoDraftHead.CardCode
                }).FirstOrDefault();

                if (distCard == null)
                {
                    return BadRequest("Invalid card code, no card code no exist");
                }

                // query the user id from bread portal 
                var queryUser = $@"select * from {db.WEBDB}..USERS with (nolock)
                                   Where UserCode = @UserCode ";

                var user = conn.Query<Bread_User>(queryUser, new
                {
                    UserCode = dto.DoDraftHead.UserCode
                }).FirstOrDefault();

                if (user == null)
                {
                    return BadRequest("Invalid card code, no user no exist");
                }

                var newInvDoc = new Bread_OINV_Ext
                {
                    Subsi = dto.DoDraftHead.SubSi,
                    SubsiId = dto.DoDraftHead.SubSiID,
                    DOCENTRY = 0,
                    DOCSTATUS = "D",
                    DOCNUM = "0",
                    COMPANYID = distCard.CardCode,
                    CARDCODE = distCard.CardCode,
                    CARDNAME = distCard.CardName,
                    DOCDATE = DateTime.Now,
                    CURRENCY = distCard.Currency,
                    DOCRATE = 0,
                    CUSTREF = dto.DoDraftHead.Comments,
                    TOTALBD = 0,
                    TAXSUM = 0,
                    ROUNDING = 0,
                    DOWNPAYMENT = 0,
                    DOCTOTAL = 0,
                    PRICEID = 0,
                    REMARKS = dto.DoDraftHead.Comments,
                    UCREATED = $"{user.USERID}",
                    DCREATED = DateTime.Now,
                    UMODIFIED = $"{user.USERID}",
                    DMODIFIED = DateTime.Now,
                    PAIDTODATE = 0,
                    FILES = dto.Files
                };

                // prepare the inv lines 
                var invLines = new List<Bread_INV1_Ext>();
                var invBatchLines = new List<Bread_Batch>();
                for (int i = 0; i < dto.DoLines.Count; i++)
                {
                    var line = dto.DoLines[i];
                    if (line == null) continue;

                    var queryPrice = $@"exec sp_GetDistItemPrice @webDb, @cardCode, @itemCode";
                    decimal price = conn.Query<decimal>(queryPrice, new
                    {
                        webDb = db.WEBDB,
                        cardCode = distCard.CardCode,
                        itemCode = line.ItemCode
                    }).FirstOrDefault();

                    var newInvLine = new Bread_INV1_Ext
                    {
                        DOCENTRY = 0,
                        LINENUM = i,
                        ITEMCODE = line.ItemCode,
                        ITEMNAME = line.ItemName,
                        QUANTITY = line.QtyInPcs,
                        PRICE = price,
                        TAXCODE = "",
                        TAXPERC = 0,
                        TAXSUM = 0,
                        LINETOTAL = 0,
                        LINETYPE = ""
                    };

                    // massage the tax and line 
                    var totalAmt = (newInvLine.QUANTITY * newInvLine.PRICE);
                    var taxSum = totalAmt * newInvLine.TAXPERC;
                    var linesTotal = taxSum + totalAmt;

                    newInvLine.TAXSUM = taxSum;
                    newInvLine.LINETOTAL = linesTotal;
                    invLines.Add(newInvLine);

                    // crate the batch object
                    if (!string.IsNullOrWhiteSpace(line.Batch))
                    {
                        var newBatch = new Bread_Batch
                        {
                            DocEntry = 0,
                            LineNum = i,
                            LineNum2 = 0,
                            Quantity = line.QtyInPcs
                        };
                        invBatchLines.Add(newBatch);
                    }
                }

                newInvDoc.Lines = invLines;
                newInvDoc.Batches = invBatchLines;
                var sumLines = invLines.Sum(x => x.LINETOTAL);
                var sumTax = invLines.Sum(x => x.TAXSUM);
                newInvDoc.DOCTOTAL = sumLines;
                newInvDoc.TAXSUM = sumTax;
                newInvDoc.TOTALBD = sumLines - sumTax;

                // create portal cn
                var breadDocHelper = new Bread_INVDocHelper();
                long docEntry = -1;
                if (newInvDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBreadInvoice(newInvDoc, db, _commDbConnStr_Bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraftInvoice(newInvDoc, db, _commDbConnStr_Bread);
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


                //// create the good received base on the portal inv table
                //var grHelper = new BreadDiApi_Delivery(db, _commDbConnStr_Bread, "OIGN");
                //string remark = $@"GOODS RECEIPT FROM PRODUCTION {DateTime.Now:dd-MMM-yyyy} (GR-IN)";
                //var err = grHelper.CreateGoodsReceive_Dist(newInvDoc, newInvDoc.Lines, remark);

                //if (!string.IsNullOrWhiteSpace(err))
                //{
                //    _logger.LogError(err);
                //    return BadRequest(err);
                //}

                //var GrRemarks = $"GR # {grHelper.PostedDocNum}, {DateTime.Now:dd-MMM-yyyy}"; // postedDocNum GR infor, for internal checking  
                //var GrDocNum = grHelper.PostedDocNum;
                //int GrDocEntry = int.Parse(grHelper.PostedDocEntry);

                //// create SAP CN then KTC store invoice                
                //// create a production entry 
                //// before create the invoice
                //var errorMsg = HandlerDistCreateCN_KStore_Invoice(conn, db, docEntry, GrRemarks, GrDocNum);
                //if (!string.IsNullOrWhiteSpace(errorMsg))
                //{
                //    return BadRequest(errorMsg);
                //}

                // 20220421
                // using single transation for doc creation 
                var helper = new BreadDiApi_Delivery_Dist(_commDbConnStr_Bread)
                {
                    Db = db,
                    SvrPath = _fileSavePath,
                    Docentry = $"{docEntry}",
                    CurSapTableName = "OIGN"
                };

                var errMsg = helper.CreateGoodsReceiveInvoice_Dist(newInvDoc, newInvDoc.Lines);
                if (!string.IsNullOrWhiteSpace(errMsg))
                {
                    return BadRequest(errMsg);
                }

                // replied the success create of the inv doc 
                // but wait posting to SAP
                var sql3 = $@"select t0.DocNum
                            from {db.SAPDB}..OINV t0 with (nolock)
                            inner join 
                                {db.WEBDB}..INV t1 with (nolock) on t1.INVENTRY = t0.DocEntry 
                            Where t1.DOCENTRY = @docentry";

                var invDocNum = conn.ExecuteScalar<string>(sql3, new { docentry = docEntry });
                var replied = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = invDocNum
                };

                if (conn.State == ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    var updateDraft_sql = @"Update CR_COMMON..FTAPP_BreadDODraftHead 
                                        Set DocStatus = 'Invoiced' Where HeadGuid = @guid";
                    conn.Execute(updateDraft_sql, new { guid = dto.HeadGuid }, trans);

                    if (dto.InvntryTrnsfrDocEntry > 0)
                    {
                        var updateOWTRDocEntry = @$"Update {db.SAPDB}..OWTR 
                                            Set U_SOENTRY = @invEntry_portal
                                            Where DocEntry = @invntryTrnsfrDocEntry";

                        conn.Execute(updateOWTRDocEntry, new
                        {
                            invEntry_portal = replied.DocEntry,
                            invntryTrnsfrDocEntry = dto.InvntryTrnsfrDocEntry
                        }, trans);
                    }

                    trans.Commit();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }

                return Ok(replied);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        //string HandlerDistCreateCN_KStore_Invoice(SqlConnection conn, DbInfo db, long docEntry,
        //    string remarks, string ref2)
        //{
        //    try
        //    {
        //        LastError = "";
        //        // parepare the invoice data table 
        //        var query = @$"SELECT T0.*
        //                    , T1.CARDCODE AS [SAPCARDCODE]
        //                    , T2.INVARINVSERIES AS [INVSERIES]
        //                    , T2.INVARCNSERIES AS [CNSERIES]
        //                    , CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE 
        //                           ELSE U0.DEFWHS END AS [WHSCODE]
        //                    , T3.SERIESNAME
        //                    , U0.SLPCODE AS [SAPSLPCODE]
        //                    , T4.PRCCODE AS [DIM1] 
        //                    FROM {db.WEBDB}..INV T0 
        //                                INNER JOIN 
        //                        [{db.SAPDB}].[DBO].[OCRD] T1 ON 
        //                                --CASE WHEN ISNULL(T1.U_PORTALID,'') = '' THEN T1.CardCode 
        //                                --ELSE T1.U_PORTALID END = T0.CARDCODE 
        //                                T1.CARDCODE  = T0.CARDCODE    
        //                                AND T1.CARDTYPE = 'C' 
        //                                AND T1.FrozenFor = 'N' 
        //                    LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 ON T2.RECID = 1 
        //                    LEFT OUTER JOIN {db.WEBDB}..USERS U0 ON U0.USERID = T0.UMODIFIED 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[NNM1] T3 ON T3.SERIES = T2.INVARINVSERIES 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T4 ON T4.PRCCODE = T1.U_COSTCTR AND T4.DimCode = '1' 
        //                    WHERE T0.DOCENTRY = '{docEntry}' ";

        //        var inv_dt = GetDataTable(conn, query);
        //        if (inv_dt == null)
        //        {
        //            return LastError;
        //        }

        //        var sapcard = inv_dt.Rows[0]["SAPCARDCODE"].ToString();
        //        var priceList = db.DEF_PRICELIST;

        //        query = $@"SELECT T1.*
        //                    , T1.PRICE AS[CUSTPRICE]
        //                    , T5.Price AS[SUPPLIERPRICE]
        //                    , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
        //                           ELSE T6.U_CSUS_UOM END AS [CSUOM]
        //                    , ISNULL(T7.GLCN,'') AS [CNGL]
        //                    , T8.PRCCODE AS [DIM2] 
        //                    , T6.ManBtchNum [MANBATCHNUM]
        //                    FROM {db.WEBDB}..INV T0 
        //                    INNER JOIN {db.WEBDB}..INV1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
        //                    INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T2 ON T2.CARDCODE = T0.CARDCODE 
        //                            AND T2.CARDTYPE = 'C' 
        //                            AND T2.FrozenFor = 'N' AND T2.CardCode = '{sapcard}' 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
        //                            AND T4.PriceList = T2.ListNum 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
        //                            AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum 
        //                                                    ELSE '{priceList}' END
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
        //                    LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
        //                    LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T8 ON T8.PRCCODE = T6.CardCode AND T8.DimCode = '2' 
        //                    WHERE T0.DOCENTRY = '{docEntry}'";

        //        var inv1_dt = GetDataTable(conn, query);
        //        if (inv1_dt == null)
        //        {
        //            return LastError;
        //        }

        //        var diapiHelper = new BreadDiApi_Trade(db, _fileSavePath, remarks, ref2);
        //        return diapiHelper.createInvCN($"{docEntry}", inv_dt, inv1_dt, true); // create invoice for ktc store , and cn for dist
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return LastError;
        //    }
        //}

        //DataTable GetDataTable(SqlConnection conn, string query)
        //{
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        SqlDataAdapter da = new SqlDataAdapter(cmd); // create data adapter                
        //        DataTable dt = new DataTable();
        //        da.Fill(dt); // this will query your database and return the result to your datatabled
        //        da.Dispose();
        //        return dt;
        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //        return null;
        //    }
        //}

        IActionResult Avail_BreadBatches(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("invalid item code");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("invalid item db info");
                }

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var sp_query = "exec sp_Avail_BreadBatches @webDb, @itemCode ";
                var results = conn.Query<FTAPP_Batch>(sp_query, new
                {
                    webDb = db.WEBDB,
                    itemCode = dto.ItemCode
                }).ToList();

                return Ok(results);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetDistributerCards(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("inalid db info");
                }

                //var sql = @"exec sp_GetBreadCards_Distributers @webDB";
                var sql = @"exec sp_GetBreadCards_DistAndSeller @webDB";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var distributers = conn.Query<OCRD_Ext>(sql, new { webDB = db.WEBDB }).ToList();
                if (distributers.Count == 0) return NotFound();

                if (distributers.Count > 0)
                {
                    distributers = ProcessCardGLN(distributers, conn, db);
                }

                return Ok(distributers);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetDistributer_SellerCards(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("inalid db info");
                }

                //var sql = @"exec sp_GetBreadCards_Distributers @webDB";
                var sql = @"exec sp_GetBreadCards_DistAndSeller @webDB";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var distributers = conn.Query<OCRD_Ext>(sql, new { webDB = db.WEBDB }).ToList();
                if (distributers.Count == 0) return NotFound();

                if (distributers.Count > 0)
                {
                    distributers = ProcessCardGLN(distributers, conn, db);
                }

                return Ok(distributers);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearLastDraftLines(Dto_BreadDO dto)
        {

            if (dto.HeadGuid == null)
            {
                return BadRequest("Invalid head guid");
            }
            if (dto.HeadGuid == null)
            {
                return BadRequest("Invalid head guid [0]");
            }

            using var conn = new SqlConnection(_commDbConnStr_Bread);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var delSql = @"delete from FTAPP_BreadDODraftHead
                               Where HeadGuid = @headGuid 
                               and DocStatus = 'Picking'";
                conn.Execute(delSql, new { headGuid = dto.HeadGuid }, trans);

                var delSqlLines = @"delete from FTAPP_BreadDODraft Where HeadGuid = @headGuid";
                conn.Execute(delSqlLines, new { headGuid = dto.HeadGuid }, trans);

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearLastDraftLines(Dto_BreadDO dto, SqlConnection conn, SqlTransaction trans)
        {
            try
            {
                if (dto.HeadGuid == null)
                {
                    return BadRequest("Invalid head guid");
                }
                if (dto.HeadGuid == null)
                {
                    return BadRequest("Invalid head guid [0]");
                }

                var delSql = @"delete from FTAPP_BreadDODraftHead
                                Where HeadGuid = @headGuid and DocStatus = 'Picking'";
                conn.Execute(delSql, new { headGuid = dto.HeadGuid }, trans);

                var delSqlLines = @"delete from FTAPP_BreadDODraft Where HeadGuid = @headGuid";
                conn.Execute(delSqlLines, new { headGuid = dto.HeadGuid }, trans);

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetListOfTransfer(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid query start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid query end date");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid User code");
                }

                var sql = @" exec sp_GetBreadDoList @startDt, @endDt, @userCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var list = conn.Query<FTAPP_BreadDODraftHead>(sql,
                    new
                    {
                        startDt = dto.StartDt,
                        endDt = dto.EndDt,
                        userCode = dto.UserCode
                    }).ToList();

                if (list.Count == 0) return NotFound();
                return Ok(list);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetInnerCards(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                var sp_query = @"exec sp_QueryBreadCardsV2 @webDB";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var stores = conn.Query<OCRD_Ext>(sp_query,
                    new
                    {
                        webDB = db.WEBDB,
                        userCode = dto.UserCode
                    }).ToList();

                if (stores.Count > 0)
                {
                    stores = ProcessCardGLN(stores, conn, db);
                }

                return Ok(stores);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetBreadCards(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                var sp_query = @"exec sp_GetBreadCards @webDB, @userCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var stores = conn.Query<OCRD_Ext>(sp_query,
                    new
                    {
                        webDB = db.WEBDB,
                        userCode = dto.UserCode
                    }).ToList();

                if (stores.Count > 0)
                {
                    stores = ProcessCardGLN(stores, conn, db);
                }

                return Ok(stores);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        List<OCRD_Ext> ProcessCardGLN(List<OCRD_Ext> list, SqlConnection conn, DbInfo db)
        {
            try
            {
                if (list == null) return list;

                for (int x = 0; x < list.Count; x++)
                {
                    if (string.IsNullOrWhiteSpace(list[x].GlblLocNum)) continue; // cont next
                    var glnArray = list[x].GlblLocNum.Split(',');
                    if (glnArray?.Length < 2) continue; // cont. next

                    list[x].Latitude = SafeGetDouble(glnArray[0]); // actual code
                    list[x].Longitude = SafeGetDouble(glnArray[1]);

                    // get the bill address 
                    // get the s address type 
                    // get shipment address
                    var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                    var bill_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (bill_address != null)
                    {
                        list[x].Address = bill_address.GetAddress();
                    }

                    // get the s address type 
                    // get shipment address
                    sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='S'";

                    var ship_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (ship_address != null)
                    {
                        list[x].ShipAdd = ship_address.GetAddress();
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return list;
            }
        }

        IActionResult GetBreadCard(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                var sp_query = @"exec sp_GetBreadCard @webDB, @cardCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var store = conn.Query<OCRD_Ext>(sp_query,
                    new
                    {
                        webDB = db.WEBDB,
                        cardCode = dto.CardCode
                    }).FirstOrDefault();

                if (store != null)
                {
                    var singleItemList = new List<OCRD_Ext>(); // fake list to hold 1 store 
                    singleItemList.Add(store);

                    var resList = ProcessCardGLN(singleItemList, conn, db);
                    return Ok(resList[0]);
                }

                return BadRequest("Store no found");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        double SafeGetDouble(string _value)
        {
            try
            {
                var isNumeric = double.TryParse(_value, out double result);
                if (isNumeric) return result;
                return -1;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        IActionResult CreateRequest(Dto_BreadDO dto)
        {

            // 20240816 
            // add in memeroy control 
            // check the user in dlb creation 
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                goto ByPassTransCheck;
                //return BadRequest("bad user login, please log out app, " +
                //    "and login again to refresh login token. Thanks");
            }

            // check the memory for the key exist
            if (Program.UserTransToken_BreadCreateTransfer == null)
                Program.UserTransToken_BreadCreateTransfer = new Dictionary<string, bool>();

            // check user token in list
            var isListed = Program.UserTransToken_BreadCreateTransfer.ContainsKey(dto.UserCode);

            if (isListed) // yes in 
            {
                bool inTran = Program.UserTransToken_BreadCreateTransfer[dto.UserCode];
                if (inTran)
                {
                    return BadRequest("Creation in process, please wait for moment. Thanks.");
                }
                else
                {
                    Program.UserTransToken_BreadCreateTransfer[dto.UserCode] = true;
                }
            }
            else // no then add in and set true 
            {
                Program.UserTransToken_BreadCreateTransfer.Add(dto.UserCode, true); // add and set to intrans
            }

        ByPassTransCheck:

            if (dto.DocRequest == null)
            {
                return BadRequest("Invalid request doc");
            }
            if (dto.Head == null)
            {
                return BadRequest("Invalid request doc head");
            }
            if (dto.Head.Lines == null)
            {
                return BadRequest("Invalid request doc lines");
            }
            if (string.IsNullOrWhiteSpace(dto.SubSi))
            {
                return BadRequest("Invalid SubSi");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("Invalid user code");
            }
            if (string.IsNullOrWhiteSpace(dto.UserType))
            {
                return BadRequest("Invalid user type");
            }
            if (dto.PickedGuid == default)
            {
                return BadRequest("Invalid picked GUID");
            }
            if (dto.PickedGuid == null)
            {
                return BadRequest("Invalid picked GUID");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
            if (db == null)
            {
                return BadRequest("Invalid db info");
            }

            // clear the last save                 
            using var conn = new SqlConnection(_commDbConnStr_Bread);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var delSql = $@"Delete from {db.WEBDB}..FTAPP_MWRequest Where Guid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);

                delSql = $@"Delete from {db.WEBDB}..FTAPP_MWDocHeader Where Guid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);

                delSql = $@"Delete from {db.WEBDB}..FTAPP_MWDocDetails Where HeaderGuid = @guid ";
                conn.Execute(delSql, new { guid = dto.PickedGuid }, trans);
                trans.Commit();

                // insert the request 
                // insert the doc head
                // insert the doc lines 
                var reqHelper = new BreadRequestHelper
                {
                    MidwareDbConnectStr = db.GetWebDbConnStr()
                };

                reqHelper.BeginTransaction(); // intial own connect abd trans
                var res = reqHelper.InsertRequest(dto.DocRequest);

                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }

                res = reqHelper.InsertDocHeader(dto.Head);
                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }

                res = reqHelper.InsertDocDetailsLine(dto.Head.Lines);

                if (res == -1)
                {
                    return BadRequest($"{reqHelper.LastErrorMessage}");
                }
                // finally
                reqHelper.Commit();               

                // launch api 
                // get teh doc head

                var sql = $@"SELECT * FROM {db.WEBDB}..FTAPP_MWDocHeader WHERE Guid = @guid";
                var docHead = conn.Query<BreadDocHeader>(sql, new { guid = dto.PickedGuid }).FirstOrDefault();

                sql = $@"SELECT * FROM {db.WEBDB}..FTAPP_MWDocDetails WHERE HeaderGuid = @guid";
                var docDetails = conn.Query<BreadDocDetail>(sql, new { guid = dto.PickedGuid }).ToList();
                                               
                if (dto.DocRequest.Request == "Inventory Transfer")
                {
                    // good received to good bin
                    var remarks = $"GOODD RECEIPT FROM PRODUCTION {DateTime.Now:dd-MMM-yyyy} (GR-IT)";
                    var apiHelper = new BreadDiApi_Delivery(db, docHead, docDetails,
                           _commDbConnStr_Bread, docHead.NumberFileAttached, "OIGN", SAPbobsCOM.BoObjectTypes.oInventoryGenEntry, remarks);


                    var err = apiHelper.CreateGoodsReceive_InvtTransfer(dto.UserType);
                   if (!string.IsNullOrWhiteSpace(err))
                    {
                        _logger.LogError(err);
                        return BadRequest(err);
                    }
                    return Ok(apiHelper.PostedDocNum);
                }

                if (dto.DocRequest.Request == "Create Credit Memo")
                {
                    // inti the helper class
                    var apiHelper = new BreadDiApi_Delivery(db, docHead, docDetails,
                                              _commDbConnStr_Bread, -1, "ORIN", SAPbobsCOM.BoObjectTypes.oCreditNotes);


                    // check does the it docEntry create in invoice 
                    var checkInvSql = $@"Select * from {db.SAPDB}..ORIN with (nolock) Where U_SOENTRY = @itDocEntry";
                    var cn = conn.Query<ORIN>(checkInvSql, new { itDocEntry = dto.Head.BaseITEntry }).FirstOrDefault();
                    if (cn != null)
                    {
                        // perform update to the request table
                        apiHelper.PostedDocEntry = $"{cn.DocEntry}";
                        apiHelper.PostedDocNum = $"{cn.DocNum}";

                        //apiHelper.UpdateCrPickedStatus($"{dto.PickedGuid}", "Invoiced");
                        //apiHelper.UpdateRequest($"{dto.PickedGuid}");
                        //apiHelper.UpdateHeader($"{dto.PickedGuid}");
                        return Ok($"{cn.DocNum}");
                    }

                    // else 
                    // create new invoice posting

                    var err = apiHelper.CreateInv_Cn(); // create the cn
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        _logger.LogError(err);
                        return BadRequest(err);
                    }

                    return Ok(apiHelper.PostedDocNum);
                }

                if (dto.DocRequest.Request == "Create Invoice")
                {
                    // inti the helper class
                    var apiHelper = new BreadDiApi_Delivery(db, docHead, docDetails,
                                              _commDbConnStr_Bread, -1, "OINV", SAPbobsCOM.BoObjectTypes.oInvoices);


                    // check does the it docentry create in invoice 
                    var checkInvSql = $@"Select * from {db.SAPDB}..OINV with (nolock) Where U_SOENTRY = @itDocEntry";
                    var inv = conn.Query<OINV>(checkInvSql, new { itDocEntry = dto.Head.BaseITEntry }).FirstOrDefault();
                    if (inv != null)
                    {
                        // perform update to the request table
                        apiHelper.PostedDocEntry = $"{inv.DocEntry}";
                        apiHelper.PostedDocNum = $"{inv.DocNum}";
                        apiHelper.UpdateCrPickedStatus($"{dto.PickedGuid}", "Invoiced");
                        apiHelper.UpdateRequest($"{dto.PickedGuid}");
                        apiHelper.UpdateHeader($"{dto.PickedGuid}");

                        return Ok($"{inv.DocNum}");
                    }

                    // else 
                    // create new invoice posting
                    var err = apiHelper.CreateInv_Cn();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        _logger.LogError(err);
                        return BadRequest(err);
                    }

                    return Ok(apiHelper.PostedDocNum);
                }

                return BadRequest("Request no handler, pls contact support or help.");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(dto.UserCode) && Program.UserTransToken_BreadCreateTransfer.Count > 0)
                {
                    Program.UserTransToken_BreadCreateTransfer.Remove(dto.UserCode);
                }
            }
        }


        IActionResult SaveBreadPicked(Dto_BreadDO dto)
        {
            try
            {
                // seperate the pick by company 
                // save the picked record into company Delivery Draft

                // based on user login (van, dist , transportor)
                // create inventory transfer for van 
                // create invoice for distributor
                // create do for transportor







                return Ok();

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateDoStatus(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.DoStatus)) // reserver for future use
                {
                    return BadRequest("Invalid Do Status");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserCode)) // reserver for future use
                {
                    return BadRequest("Invalid Req User Code");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserName)) // reserver for future use
                {
                    return BadRequest("Invalid Req User Name");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserSubsi)) // reserver for future use
                {
                    return BadRequest("Invalid Req User Subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // reserver for future use
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var update = @"UPDATE FTAPP_BreadDODraft 
                                SET 
                                    TransferStatus = @doStatus ,                                     
                                    ReqUserCode =    @reqUserCode,
                                    ReqUserName =    @reqUserName,
                                    ReqUserSubsi =   @reqUserSubsi,
                                    transferedDT = GETDATE()
                                WHERE UserCode =     @userCode       ";

                var res = new SqlConnection(_commDbConnStr_Bread).Execute(update, new
                {
                    doStatus = dto.DoStatus,
                    reqUserCode = dto.ReqUserCode,
                    reqUserName = dto.ReqUserName,
                    reqUserSubsi = dto.ReqUserSubsi,
                    userCode = dto.UserCode
                });

                if (res <= 0)
                {
                    return BadRequest($"Update DO status fail , {dto.UserCode}");
                }

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadDoListLines(Dto_BreadDO dto)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(dto.SubSi)) // reserver for future use
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("Invalid user type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = string.Empty;
                var res = new List<BreadItem>();
                switch (dto.UserType)
                {
                    case "DI":
                        {
                            // query the invoice list                             
                            sql = @"exec CR_COMMON..sp_QueryBreadInv_Line @webb, @docEntry";
                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadItem>(sql, new
                                                    {
                                                        webb = db.WEBDB,
                                                        docEntry = dto.DocEntry
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                    case "TR": // transporter
                        {
                            // get list of inventory transfer                            
                            sql = dto.DocType == "INV" ? @"exec CR_COMMON..sp_QueryBreadINV_Line @webb, @docEntry" :
                                                         @"exec CR_COMMON..sp_QueryBreadITSeller_Line @webb, @docEntry";

                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadItem>(sql, new
                                                    {
                                                        webb = db.WEBDB,
                                                        docEntry = dto.DocEntry
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                    case "SE": // van seller 
                        {
                            // get list of inventory transfer                            
                            sql = @"exec CR_COMMON..sp_QueryBreadITSeller_Line @webb, @docEntry";
                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadItem>(sql, new
                                                    {
                                                        webb = db.WEBDB,
                                                        docEntry = dto.DocEntry
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// Load the DO list based on user selected subsi 
        /// </summary>
        /// <returns></returns>
        IActionResult LoadDoList(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // reserver for future use
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi)) // reserver for future use
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }
                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("Invalid user type");
                }
                if (string.IsNullOrWhiteSpace(dto.DiCardCode))
                {
                    return BadRequest("Invalid distribution card code.");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sql = string.Empty;
                var res = new List<BreadIItemHead>();
                switch (dto.UserType)
                {
                    case "DI":
                        {
                            // query the invoice list                             
                            sql = @"exec sp_QueryBreaHeadInv @webDb, @cardCode, @startDt, @endDt";
                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadIItemHead>(sql, new
                                                    {
                                                        webDb = db.WEBDB,
                                                        cardCode = dto.DiCardCode,
                                                        startDt = $"{dto.StartDt:yyyy-MM-dd}",
                                                        endDt = $"{dto.EndDt:yyyy-MM-dd}"
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                    case "TR": // transporter                        
                    case "SE": // van seller 
                        {
                            // get list of inventory transfer                            
                            sql = @"exec sp_QueryBreaHeadIT @webDb, @userCode, @startDt, @endDt";
                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadIItemHead>(sql, new
                                                    {
                                                        webDb = db.WEBDB,
                                                        userCode = dto.UserCode,
                                                        startDt = $"{dto.StartDt:yyyy-MM-dd}",
                                                        endDt = $"{dto.EndDt:yyyy-MM-dd}"
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                    case "TR_INV":
                        {
                            // query the invoice list                             
                            sql = @"exec sp_QueryBreaHeadInv_Tr @webDb, @cardCode, @startDt, @endDt";
                            res = new SqlConnection(_commDbConnStr_Bread)
                                                    .Query<BreadIItemHead>(sql, new
                                                    {
                                                        webDb = db.WEBDB,
                                                        cardCode = dto.DiCardCode,
                                                        startDt = $"{dto.StartDt:yyyy-MM-dd}",
                                                        endDt = $"{dto.EndDt:yyyy-MM-dd}"
                                                    }).ToList();

                            if (res.Count == 0) return NotFound();
                            return Ok(res);
                        }
                }

                return NotFound();

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for bread app to get the picked line
        IActionResult GetPickedDoLines(Dto_BreadDO dto)
        {
            try
            {
                // get the lines based on head guid
                if (dto.HeadGuid == null)
                {
                    return BadRequest("Invalid header guid [0]");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid header guid [1]");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryDocStatus))
                {
                    return BadRequest("Invalid doc status");
                }
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }


                // picked -> transfered -> Invoiced, Credit Noted or ITransfered
                // this code oinly handler transfered noted 
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var sqlHead = @"Select * from FTAPP_BreadDODraftHead with (nolock) 
                                Where HeadGUid = @headguid 
                                and DocStatus = @queryDocStatus";

                var head = conn.Query<FTAPP_BreadDODraftHead>(sqlHead,
                    new
                    {
                        headguid = dto.HeadGuid,
                        queryDocStatus = dto.QueryDocStatus
                    }).FirstOrDefault();

                if (head == null) return NotFound();

                var sql = @"Select * from FTAPP_BreadDODraft with (nolock) Where HeadGUid = @headguid";
                head.Lines = conn.Query<BreadItem>(sql,
                    new
                    {
                        headguid = dto.HeadGuid
                    }).ToList();

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                sql = @$"Select ISNULL(BaseITEntry, '-1') [BaseITEntry] 
                       from {db.WEBDB}..FTAPP_MWDocHeader with (nolock) 
                       Where Guid = @headguid";

                head.BaseITEntry = conn.Query<int>(sql, new { headguid = dto.HeadGuid }).FirstOrDefault();

                return Ok(head);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for whs app to get the picked lin
        IActionResult GetPickedDoLines1(Dto_BreadDO dto)
        {
            try
            {
                // get the lines based on head guid
                if (dto.HeadGuid == null)
                {
                    return BadRequest("Invalid header guid [0]");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid header guid [1]");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryDocStatus))
                {
                    return BadRequest("Invalid doc status");
                }

                var conn = new SqlConnection(_commDbConnStr_Bread);

                var sql = @"Select * from FTAPP_BreadDODraft with (nolock) 
                            Where HeadGUid = @headguid";
                var lines = new SqlConnection(_commDbConnStr_Bread).Query<BreadItem>(sql,
                    new
                    {
                        headguid = dto.HeadGuid
                    }).ToList();

                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetBreadDoLinesDraft(Dto_BreadDO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // reserver for future use
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserCode)) // reserver for future use
                {
                    return BadRequest("Invalid ReqUserCode");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserName)) // reserver for future use
                {
                    return BadRequest("Invalid ReqUserName");
                }
                if (string.IsNullOrWhiteSpace(dto.ReqUserSubsi)) // reserver for future use
                {
                    return BadRequest("Invalid ReqUserSubsi");
                }
                if (dto.HeadGuid == null)
                {
                    return BadRequest("Invalid Head doc guid [1]");
                }
                if (dto.HeadGuid == default)
                {
                    return BadRequest("Invalid Head doc guid [0]");
                }

                var query = $@" Select * 
                                from FTAPP_BreadDODraft t0  with (nolock) 
                                left join FTAPP_BreadDODraftHead t1  with (nolock) on t0.HeadGuid = t1.HeadGuid 
                                Where t0.UserCode = @UserCode 
                                and t0.ReqUserCode = @ReqUserCode 
                                and t0.ReqUserName = @ReqUserName 
                                and t0.ReqUserSubsi = @ReqUserSubsi 
                                and t1.DocStatus = 'Picking'";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var res = conn.Query<BreadItem>(query, new
                {
                    UserCode = dto.UserCode,
                    ReqUserCode = dto.ReqUserCode,
                    ReqUserName = dto.ReqUserName,
                    ReqUserSubsi = dto.ReqUserSubsi
                }).ToList();

                if (res.Count == 0) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveBreadDoLinesDraft(Dto_BreadDO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserCode)) // reserver for future use
            {
                return BadRequest("Invalid user code");
            }
            if (string.IsNullOrWhiteSpace(dto.CardCode)) // reserver for future use
            {
                return BadRequest("Invalid card code");
            }
            if (string.IsNullOrWhiteSpace(dto.CardName)) // reserver for future use
            {
                return BadRequest("Invalid card code");
            }
            if (string.IsNullOrWhiteSpace(dto.SubSi)) // reserver for future use
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DoLines == null)
            {
                return BadRequest("invalid save draft lines");
            }
            if (dto.HeadGuid == null || dto.HeadGuid == default)
            {
                return BadRequest("invalid save draft head guid");
            }
            if (string.IsNullOrWhiteSpace(dto.SaveDocAsStatus))
            {
                dto.SaveDocAsStatus = "Picking";
            }
            if (dto.DoLines.Count <= 0)
            {
                return BadRequest("invalid save draft, line count zero");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
            if (db == null)
            {
                return BadRequest("invalid db info");
            }

            var headGuid = dto.HeadGuid;
            var dto1 = new Dto_BreadDO
            {
                HeadGuid = headGuid
            };

            using var conn = new SqlConnection(_commDbConnStr_Bread);
            conn.Open();
            using var trans = conn.BeginTransaction();
            ClearLastDraftLines(dto1, conn, trans);

            try
            {
                // create the head rcord 
                var newDoc = new FTAPP_BreadDODraftHead
                {
                    SubSi = db.COMPANYNAME,
                    SubSiID = db.COMPANYID,
                    UserCode = dto.UserCode,
                    UserName = dto.UserName,
                    DocDate = DateTime.Now,
                    DocStatus = dto.SaveDocAsStatus,
                    HeadGuid = headGuid,
                    CardCode = dto.CardCode,
                    CardName = dto.CardName,
                    ODCardCode = dto.ODCardCode,
                    ODCardName = dto.ODCardName,
                    Comments = dto.Comments,
                    TruckNo = dto.TruckNo,
                    UsedTrayQty = dto.UsedTrayQty
                };

                // insert the head
                var insertHeadSql = @"Insert into FTAPP_BreadDODraftHead  (
                                    SubSi,
                                    SubSiID,
                                    UserCode,
                                    UserName,
                                    DocDate,
                                    DocStatus,
                                    HeadGuid , CardCode, CardName,  ODCardCode , ODCardName, Comments, TruckNo, UsedTrayQty
                                ) values (                           
                                    @SubSi,
                                    @SubSiID,
                                    @UserCode,
                                    @UserName,
                                    @DocDate,
                                    @DocStatus,
                                    @HeadGuid , @CardCode, @CardName,  @ODCardCode , @ODCardName, @Comments, @TruckNo, @UsedTrayQty
                                )";

                conn.Execute(insertHeadSql, newDoc, trans);

                // insert the line
                for (int l = 0; l < dto.DoLines.Count; l++)
                {
                    var line = dto.DoLines[l];
                    if (line == null) continue;

                    line.HeadGuid = headGuid;
                    line.TransferStatus = dto.SaveDocAsStatus;
                    line.LineNum = l;

                    var sqlInsert = $@"INSERT INTO CR_COMMON..FTAPP_BreadDODraft (
                                                  ItemCode
                                                , ItemName
                                                , UOMQty
                                                , SuppCatNum
                                                , CodeBars
                                                , LineNum
                                                , QtyInPcs
                                                , TrayQty
                                                , PcsQty
                                                , Remarks
                                                , LineGuid
                                                , ScanInCode
                                                , Batch
                                                , BarCodeStr
                                                , UserCode
                                                , UserName
                                                , UserSubsi
                                                , ReqUserCode
                                                , ReqUserName
                                                , ReqUserSubsi
                                                , TransDt 
                                                , HeadGuid
                                                    ) Values ( 
                                                      @ItemCode
                                                     ,@ItemName
                                                     ,@UOMQty
                                                     ,@SuppCatNum
                                                     ,@CodeBars
                                                     ,@LineNum
                                                     ,@QtyInPcs
                                                     ,@TrayQty
                                                     ,@PcsQty
                                                     ,@Remarks
                                                     ,@LineGuid
                                                     ,@ScanInCode
                                                     ,@Batch
                                                     ,@BarCodeStr
                                                     ,@UserCode
                                                     ,@UserName
                                                     ,@UserSubsi
                                                     ,@ReqUserCode
                                                     ,@ReqUserName
                                                     ,@ReqUserSubsi
                                                     ,GETDATE()
                                                     ,@HeadGuid )";
                    conn.Execute(sqlInsert, line, trans);
                }
                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }
    }
}


