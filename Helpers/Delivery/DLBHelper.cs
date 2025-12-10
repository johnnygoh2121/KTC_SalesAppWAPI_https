using Dapper;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Transfer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers.Delivery
{
    public class DLBHelper
    {
        public DbInfo Db { get; set; }
        public Guid HeadGuid { get; set; }
        public string Error { get; set; }

        // add in the property to set the current company card code and card name
        public string TruckCardCode { get; set; } = "";
        public string TruckCardName { get; set; } = "";

        public DLBHelper(DbInfo db)
        {
            Db = db;
            //HeadGuid = headGuid;

            //var truckInfo = GetTruckCompany(truckNo);
            //TruckCardCode = truckInfo?.TruckCardCode;
            //TruckCardName = truckInfo?.TruckCardName;
        }

        public DLBHelper(DbInfo db, Guid headGuid, string truckNo)
        {
            Db = db;
            HeadGuid = headGuid;

            var truckInfo = GetTruckCompany(truckNo);
            TruckCardCode = truckInfo?.TruckCardCode;
            TruckCardName = truckInfo?.TruckCardName;
        }

        public string GetPickedWhs(string userCode, string connStr, string webDb)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(userCode)) return "";
                if (string.IsNullOrWhiteSpace(connStr)) return "";
                if (string.IsNullOrWhiteSpace(webDb)) return "";

                var so_Query = @$"select ORIWHS from {webDb}..USERS with (nolock) 
                                    where USERCODE = @userCode ";

                using (var conn = new SqlConnection(connStr))
                {
                    var orgWhs = conn.ExecuteScalar<string>(so_Query, new
                    {
                        userCode = userCode
                    });

                    if (string.IsNullOrWhiteSpace(orgWhs))
                    {
                        return "";
                    }
                    return orgWhs;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public long CreateDLB(FTAPP_DLB head, List<FTAPP_DLB1> lines, string userCode, string userName,
            bool isInterbranch, bool isRescan = false)
        {
            var connStr = Db.GetWebDbConnStr();

            // 20250930 
            // get the picked whs code 
            var pickedWhs = GetPickedWhs(userCode, connStr, Db.WEBDB);
            using var conn = new SqlConnection(connStr);

            // get the max doc entry
            var maxDocEntry_query = @$"select isnull(max(docentry) +1, 1) from {Db.WEBDB}..DLB ";
            long docEntry = conn.Query<long>(maxDocEntry_query).FirstOrDefault();

            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
            }


            #region create DLB, DLB1 
            using var trans = conn.BeginTransaction();
            try
            {
                var costCt = GetUserCostCenter(head.WhsUserCode);
                // create the dlb head 
                var newHead = new DLB
                {
                    DOCENTRY = docEntry,
                    DOCNUM = docEntry,
                    DOCDATE = head.OutTransDt,
                    DOCSTATUS = "C", // C= confirmed, D = draft

                    // 20221027
                    // query to get the latest
                    CARDCODE = TruckCardCode, //head.TruckCardCode,
                    CARDNAME = TruckCardName,  //head.TruckCardName,

                    TRUCKNO = head.TruckNo,
                    BOP = costCt,
                    REMARKS = head.Remarks,
                    UCREATED = head.WhsUserCode,
                    DCREATED = DateTime.Now,
                    UMODIFIED = head.WhsUserCode,
                    DMODIFIED = DateTime.Now,
                    ISINTERBRANCH =  isRescan == true ? false : isInterbranch, // 20250808
                    PICKEDWHS = pickedWhs,
                };

                var lineCnt = 0;
                List<DLB1> newDlb1 = new List<DLB1>();
                for (int i = 0; i < lines.Count; i++) // loop the ftapp_dlb1 to create dlb1 record
                {
                    var doc = lines[i];
                    if (doc == null) continue;

                    // 20240103
                    var actualTotalBox = QueryActualBoxes(doc.DocNum, doc.DocType);
                    if (actualTotalBox == -1)
                    {
                        actualTotalBox = doc.CartonNo;
                    }

                    var terAndGeo = GetTerritoryAndGeo(doc.StoreCode);
                    var newLines = new DLB1
                    {
                        DOCENTRY = docEntry,
                        LINENUM = lineCnt,
                        DOCTYPE = doc.DocType,
                        DOCNUM = doc.DocNum,
                        DOCDATE = doc.DocDate,
                        CARDCODE = doc.StoreCode,
                        CARDNAME = doc.StoreName,
                        DOCTOTAL = doc.DocTotal,
                        TERRITORY = terAndGeo == null ? "" : terAndGeo.Territory,
                        GEOCODE = terAndGeo == null ? "" : string.IsNullOrWhiteSpace(terAndGeo.U_DELGLN) ? terAndGeo.GeoCode : terAndGeo.U_DELGLN,
                        TOTALPAGES = 1,
                        CARTONNO = actualTotalBox,
                        REFNO = doc.RefNo,
                        STATUS = "O",
                        RETDATE = default,
                        PAGES = "[1]",
                        UMODIFIED = head.WhsUserCode,
                        DMODIFIED = DateTime.Now,
                        RECDATE = default,
                        CONSIGNMENTNO = doc.ConsigmentNo
                    };

                    // try to build sql insert
                    var sp_insert_line_head = @$"INSERT INTO {Db.WEBDB}..DLB1 (
                                             DOCENTRY
                                           , LINENUM
                                           , DOCTYPE
                                           , DOCNUM
                                           , CARDCODE
                                           , CARDNAME
                                           , DOCTOTAL
                                           , TERRITORY
                                           , GEOCODE
                                           , TOTALPAGES
                                           , CARTONNO
                                           , REFNO
                                           , STATUS
                                           , PAGES
                                           , UMODIFIED
                                           , CONSIGNMENTNO  ";

                    var sp_insert_line_tail = @$"@DOCENTRY
                                           ,@LINENUM
                                           ,@DOCTYPE
                                           ,@DOCNUM
                                           ,@CARDCODE
                                           ,@CARDNAME
                                           ,@DOCTOTAL
                                           ,@TERRITORY
                                           ,@GEOCODE
                                           ,@TOTALPAGES
                                           ,@CARTONNO
                                           ,@REFNO
                                           ,@STATUS
                                           ,@PAGES
                                           ,@UMODIFIED
                                           ,@CONSIGNMENTNO  ";

                    if (newLines.DOCDATE != default)
                    {
                        sp_insert_line_head += ", DOCDATE";
                        sp_insert_line_tail += ", @DOCDATE";
                    }

                    if (newLines.RETDATE != default)
                    {
                        sp_insert_line_head += ", RETDATE";
                        sp_insert_line_tail += ", @RETDATE";
                    }
                    if (newLines.DMODIFIED != default)
                    {
                        sp_insert_line_head += ", DMODIFIED";
                        sp_insert_line_tail += ", @DMODIFIED";
                    }
                    if (newLines.RECDATE != default)
                    {
                        sp_insert_line_head += ", RECDATE";
                        sp_insert_line_tail += ", @RECDATE";
                    }

                    var insertLineSql = $"{sp_insert_line_head} ) values ( {sp_insert_line_tail}) ";
                    var insertDLbLineRes = conn.Execute(insertLineSql, newLines, trans);
                    if (insertDLbLineRes <= 0)
                    {
                        trans.Rollback();
                        Error = "Error insert the dlb line";
                        return -1;
                    }

                    if (doc.DocType == "I")
                    {
                        // 20251203
                        // to ensure the FTAPP_DLB1 and FTAPP_DLB2 was create for this
                        var sp_InsertFTAP_DLB2 = @"exec KTCW_COMMON..sp_RepairFTAPP_DLB2_SingleInvNo @webDb,  @dlbNum, @invNum ";
                        var reInsertDlb2 = conn.Execute(sp_InsertFTAP_DLB2,
                                new
                                {
                                    webDb = Db.WEBDB,
                                    dlbNum = docEntry,
                                    invNum = doc.DocNum,
                                }, trans);

                        var sp_InsertFTAP_DLB1 = $@"exec KTCW_COMMON..sp_RepairFTAPP_DLB1_SingleInvNo @webDb,  @dlbEntry, @invNum ,@docType";
                        var reInsertDlb1 = conn.Execute(sp_InsertFTAP_DLB1,
                                 new
                                 {
                                     webDb = Db.WEBDB,
                                     dlbEntry = docEntry,
                                     invNum = doc.DocNum,
                                     docType = doc.DocType
                                 }, trans);
                    }
                    
                    lineCnt++;
                }
                // --------------------------------------

                // prepare head insert 
                var sp_insert_head = @$"INSERT INTO  {Db.WEBDB}..DLB ( 
                                             DOCENTRY
                                           , DOCNUM
                                           , DOCDATE
                                           , DOCSTATUS
                                           , CARDCODE
                                           , CARDNAME
                                           , TRUCKNO
                                           , BOP
                                           , REMARKS
                                           , UCREATED
                                           , UMODIFIED , ISINTERBRANCH , PICKEDWHS ";
                var sp_insert_head_tail = @$" @DOCENTRY
                                           ,@DOCNUM
                                           ,@DOCDATE
                                           ,@DOCSTATUS
                                           ,@CARDCODE
                                           ,@CARDNAME
                                           ,@TRUCKNO
                                           ,@BOP
                                           ,@REMARKS
                                           ,@UCREATED
                                           ,@UMODIFIED , @ISINTERBRANCH , @PICKEDWHS ";

                if (newHead.DCREATED != default)
                {
                    sp_insert_head += ", DCREATED";
                    sp_insert_head_tail += ", @DCREATED";
                }
                if (newHead.DMODIFIED != default)
                {
                    sp_insert_head += ", DMODIFIED";
                    sp_insert_head_tail += ", @DMODIFIED";
                }
                var sp_headInsert = $"{sp_insert_head} ) values ({sp_insert_head_tail})";
                var insertDLBHeadres = conn.Execute(sp_headInsert, newHead, trans);
                if (insertDLBHeadres <= 0)
                {
                    Error = "Error insert the DLB head";
                    trans.Rollback();
                    return -1;
                }

                // 20240513 
                // check onhold doc exit if not then ignore 
                var sp_CheckExit = $@"select * from  {Db.WEBDB}..FTAPP_HoldDlvryDocs where DlbEntry = @dlbEntry ";
                var found = conn.Query<FTAPP_HoldDlvryDocs>(sp_CheckExit, new
                {
                    dlbEntry = docEntry
                }, trans).FirstOrDefault();

                if (found != null)
                {
                    // update the on hold table with dlb entry linked 
                    var updateInvHoldReason = @$"Update {Db.WEBDB}..FTAPP_HoldDlvryDocs
                                                Set Reason = @reason, 
                                                    DlbEntry = @dlbEntry
                                                Where HeadGuid = @headGuid";
                    var updateHoldTableRes = conn.Execute(updateInvHoldReason,
                        new
                        {
                            reason = "Intransit",
                            dlbEntry = docEntry,
                            headGuid = HeadGuid
                        }, trans);

                    if (updateHoldTableRes <= 0)
                    {
                        trans.Rollback();
                        Error = $"Error update on DLB hold table {Db.COMPANYNAME}, Dlb Entry : {docEntry}";
                        return -1;
                    }
                }                
                
                // perform update to the FTAPP_DLB 
                var update_FTAPP_DLB = @$"Update {Db.WEBDB}..FTAPP_DLB
                                         Set DLBEntry = @docEntry 
                                             ,WhsUserCode = @userCode
                                             ,WhsUserName = @userName
                                             ,DLBStatus = @dlbStatus 
                                             ,NRIC = @nric
                                             ,Remarks = @remarks
                                             ,IsInterbranch = @IsInterbranch
                                            where HeadGuid = @HeadGuid";

                var updateFTAPP_DLB_Res = conn.Execute(update_FTAPP_DLB, new
                {
                    docEntry,
                    userCode,
                    userName,
                    HeadGuid,
                    dlbStatus = "O",
                    nric = head.NRIC,
                    remarks = head.Remarks,
                    IsInterbranch = isRescan ? false:  isInterbranch,
                }, trans);

                if (updateFTAPP_DLB_Res < 0)
                {
                    trans.Rollback();
                    Error = "Error update on FTAPP_DLB table ";
                    return -1;
                }

                #endregion insert DLB , DLB1 

                #region insert transfer
                // ------------------------------------------
                // prepare create the transfer invoice here 
                // get all the invoice out from the DLB list
                var invs = lines.Where(d => d.DocType == "I").ToList();
                if (invs.Count == 0)
                {
                    // no invoice to insert 
                    trans.Commit();
                    return docEntry;
                }

                // check any close of connection
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }

                var groupGuid = HeadGuid; // using the same guid as FTAPP DLB // Guid.NewGuid();  // grouping guid for transfer

                // use for later insert the whole list
                var tLines = new List<FTAPP_Transfer1>();
                var tLinesBoxes = new List<FTAPP_Transfer2>();

                for (int i = 0; i < invs.Count; i++)
                {
                    var inv = invs[i];
                    if (inv == null) continue;

                    // return ok when all status 
                    // get the invoice from sap                     
                    var query_inv = @$"select * from {Db.SAPDB}..OINV with (nolock) where docnum = @docnum";
                    OINV sapInv = conn.Query<OINV>(query_inv, new { docnum = inv.DocNum }, trans).FirstOrDefault();
                    if (sapInv == null)
                    {
                        continue; // next invoice
                    }

                    // create the line
                    var newT = new FTAPP_Transfer1
                    {
                        InvNo = (int)inv.DocNum,
                        TransDt = DateTime.Now,
                        GroupGuid = groupGuid
                    };
                    tLines.Add(newT);

                    var query_box = $@"select DISTINCT  
                                              t0.BoxId 
                                           , '{inv.DocNum}' [DocNum]
                                           ,  GETDATE()     [TransDt]
                                           ,  CONVERT(uniqueidentifier, '{groupGuid}' )    [GroupGuid]        
                                FROM {Db.WEBDB}..FTAPP_Box t0 WITH (NOLOCK)
                                LEFT JOIN  {Db.WEBDB}..FTAPP_Box1 t1  WITH (NOLOCK) on t0.BoxGuid = t1.BoxGuid
                                WHERE t0.BaseEntry = @baseentry 
                                AND t1.BoxGuid IS NOT NULL";

                    var boxes = conn.Query<FTAPP_Transfer2>(query_box, new { baseentry = sapInv.U_SOID }, trans).ToList();
                    if (boxes.Count == 0)
                    {
                        continue; // next invoice
                    }

                    tLinesBoxes.AddRange(boxes);

                } // transfer invoice loop end

                // insert the invoice | transfer 1
                // insert the lines 
                if (tLines.Count > 0)
                {
                    var insert_lines = $@"insert into {Db.WEBDB}..FTAPP_Transfer1 ( 
                                          InvNo
                                        , TransDt
                                        , GroupGuid 
                                    ) values (  
                                          @InvNo
                                         , GETDATE()
                                         , @GroupGuid 
                                    ) ";

                    var insertLinesRes = conn.Execute(insert_lines, tLines, trans);
                    if (insertLinesRes <= 0)
                    {
                        Error = "Error insert transfer lines";
                        trans.Rollback();
                        return -1;
                    }
                }

                if (tLinesBoxes.Count > 0)
                {
                    // insert the box 
                    var insert_boxes_sql = @$"insert into {Db.WEBDB}..FTAPP_Transfer2 (
                                         BoxId
                                        ,InvNo
                                        ,TransDt
                                        ,GroupGuid 
                                    ) values (
                                         @BoxId
                                        ,@InvNo
                                        ,GETDATE()
                                        ,@GroupGuid  ) ";

                    var resInsertTransBoxes = conn.Execute(insert_boxes_sql, tLinesBoxes, trans);
                    if (resInsertTransBoxes <= 0)
                    {
                        Error = "Error insert transfer lines boxes";
                        trans.Rollback();
                        return -1;
                    }
                }

                var tHead = new FTAPP_Transfer
                {
                    // 20221027
                    // query to get the latest
                    ReceiverCode = TruckCardCode, //head.TruckCardCode,
                    ReceiverName = TruckCardName,  //head.TruckCardName,
                    LocationCode = head.TruckNo,
                    LocationName = head.TruckNo,
                    TransDt = DateTime.Now,
                    DocStatus = "T",
                    DriverName = head.DriverName,
                    GroupGuid = groupGuid,
                    Module = "CreateDLB",
                    DLBEntry = (int)docEntry
                };

                // lastly insert the transfer head
                // insert the transfer head
                // insert the transfer
                var insert_transfer = @$"insert into {Db.WEBDB}..FTAPP_Transfer (
                                              ReceiverCode
                                            , ReceiverName
                                            , LocationCode
                                            , LocationName
                                            , TransDt
                                            , GroupGuid
                                            , DocStatus  
                                            , DriverName 
                                            , Module 
                                            , DLBEntry                                        
                                            ) values (
                                               @ReceiverCode
                                              ,@ReceiverName
                                              ,@LocationCode
                                              ,@LocationName
                                              ,GETDATE()
                                              ,@GroupGuid
                                              ,@DocStatus
                                              ,@DriverName 
                                              ,@Module
                                              ,@DLBEntry )";

                var insertTranHead = conn.Execute(insert_transfer, tHead, trans);
                if (insertTranHead <= 0)
                {
                    trans.Rollback();
                    Error += "\nError insert transfer head ";
                    return -1;
                }

                #endregion end of insert transfer

                Error = "";
                trans.Commit();
                return docEntry;
            }
            catch (Exception e)
            {
                trans.Rollback();
                Error = $"{e.Message}\n{e.Message}";
                return -1;
            }
        }

        // create the dlb without auto create the transfer
        public long CreateDLB_WNoTransfer(FTAPP_DLB head, List<FTAPP_DLB1> lines, string userCode, string userName,
            SqlTransaction trans,
            SqlConnection conn,
            string connString)
        {
            // 20250930
            var pickedWhs = "";
            if (lines.Count > 0)
            {
                var lastDlbEntry = lines.LastOrDefault().LastDlbEntry;

                var sp_query = $@"select distinct t1.ORIWHS 
                                    from {Db.WEBDB}..DLB t0 inner join 
                                         {Db.WEBDB}..USERS t1 on t0.UCREATED = t1.USERCODE
                                    Where docentry = @lastDlbEntry ";

                pickedWhs = new SqlConnection(connString).ExecuteScalar<string>(sp_query, new
                {
                    lastDlbEntry
                });
            }

            // get the max doc entry
            var maxDocEntry_query = @$"select max(docentry) +1 from {Db.WEBDB}..DLB";

            var cmd = new SqlCommand(maxDocEntry_query, conn);
            cmd.Transaction = trans;
            long docEntry = (long)cmd.ExecuteScalar();

            try
            {
                // create the dlb head 
                var newHead = new DLB
                {
                    DOCENTRY = docEntry,

                    DOCNUM = docEntry,
                    DOCDATE = head.OutTransDt,
                    DOCSTATUS = "C", // C= confirmed, D = draft

                    // 20221027
                    // query to get the latest
                    CARDCODE = TruckCardCode, //head.TruckCardCode,
                    CARDNAME = TruckCardName,  //head.TruckCardName,

                    //CARDCODE = head.TruckCardCode,
                    //CARDNAME = head.TruckCardName,
                    TRUCKNO = head.TruckNo,
                    BOP = GetUserCostCenter(head.WhsUserCode),
                    REMARKS = head.Remarks,
                    UCREATED = head.WhsUserCode,
                    DCREATED = DateTime.Now,
                    UMODIFIED = head.WhsUserCode,
                    DMODIFIED = DateTime.Now,
                    PICKEDWHS = pickedWhs
                };

                var lineCnt = 0;
                List<DLB1> newDlb1 = new List<DLB1>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var doc = lines[i];
                    if (doc == null) continue;
                    var terAndGeo = GetTerritoryAndGeo(doc.StoreCode);
                    var newLines = new DLB1
                    {
                        DOCENTRY = docEntry,
                        LINENUM = lineCnt,
                        DOCTYPE = doc.DocType,
                        DOCNUM = doc.DocNum,
                        DOCDATE = doc.DocDate,
                        CARDCODE = doc.StoreCode,
                        CARDNAME = doc.StoreName,
                        DOCTOTAL = doc.DocTotal,
                        TERRITORY = terAndGeo == null ? "" : terAndGeo.Territory,
                        //GEOCODE = terAndGeo == null ? "" : terAndGeo.U_DELGLN,
                        GEOCODE = terAndGeo == null ? "" : string.IsNullOrWhiteSpace(terAndGeo.U_DELGLN) ? terAndGeo.GeoCode : terAndGeo.U_DELGLN,
                        TOTALPAGES = 1,
                        CARTONNO = doc.CartonNo,
                        REFNO = doc.RefNo,
                        STATUS = "O",
                        RETDATE = default,
                        PAGES = "[1]",
                        UMODIFIED = head.WhsUserCode,
                        DMODIFIED = DateTime.Now,
                        RECDATE = default,
                        CONSIGNMENTNO = doc.ConsigmentNo
                    };

                    newDlb1.Add(newLines);
                    lineCnt++;
                }
                // prepare head insert 
                var sp_insert_head = @$"INSERT INTO  {Db.WEBDB}..DLB ( 
                                             DOCENTRY
                                           , DOCNUM
                                           , DOCDATE
                                           , DOCSTATUS
                                           , CARDCODE
                                           , CARDNAME
                                           , TRUCKNO
                                           , BOP
                                           , REMARKS
                                           , UCREATED
                                           , UMODIFIED , PICKEDWHS ";

                var sp_insert_head_tail = @$" @DOCENTRY
                                           ,@DOCNUM
                                           ,@DOCDATE
                                           ,@DOCSTATUS
                                           ,@CARDCODE
                                           ,@CARDNAME
                                           ,@TRUCKNO
                                           ,@BOP
                                           ,@REMARKS
                                           ,@UCREATED
                                           ,@UMODIFIED , @PICKEDWHS ";

                if (newHead.DCREATED != default)
                {
                    sp_insert_head += ", DCREATED";
                    sp_insert_head_tail += ", @DCREATED";
                }

                if (newHead.DMODIFIED != default)
                {
                    sp_insert_head += ", DMODIFIED";
                    sp_insert_head_tail += ", @DMODIFIED";
                }

                var sp_headInsert = $"{sp_insert_head} ) values ({sp_insert_head_tail})";
                conn.Execute(sp_headInsert, newHead, trans);

                // --------------------------------------
                // inser DLB line 
                for (int i = 0; i < newDlb1.Count; i++)
                {
                    var line = newDlb1[i];
                    if (line == null) continue;

                    var sp_insert_line_head = @$"INSERT INTO {Db.WEBDB}..DLB1 (
                                             DOCENTRY
                                           , LINENUM
                                           , DOCTYPE
                                           , DOCNUM
                                           , CARDCODE
                                           , CARDNAME
                                           , DOCTOTAL
                                           , TERRITORY
                                           , GEOCODE
                                           , TOTALPAGES
                                           , CARTONNO
                                           , REFNO
                                           , STATUS
                                           , PAGES
                                           , UMODIFIED
                                           , CONSIGNMENTNO  ";

                    var sp_insert_line_tail = @$"@DOCENTRY
                                           ,@LINENUM
                                           ,@DOCTYPE
                                           ,@DOCNUM
                                           ,@CARDCODE
                                           ,@CARDNAME
                                           ,@DOCTOTAL
                                           ,@TERRITORY
                                           ,@GEOCODE
                                           ,@TOTALPAGES
                                           ,@CARTONNO
                                           ,@REFNO
                                           ,@STATUS
                                           ,@PAGES
                                           ,@UMODIFIED
                                           ,@CONSIGNMENTNO  ";

                    if (line.DOCDATE != default)
                    {
                        sp_insert_line_head += ", DOCDATE";
                        sp_insert_line_tail += ", @DOCDATE";
                    }

                    if (line.RETDATE != default)
                    {
                        sp_insert_line_head += ", RETDATE";
                        sp_insert_line_tail += ", @RETDATE";
                    }
                    if (line.DMODIFIED != default)
                    {
                        sp_insert_line_head += ", DMODIFIED";
                        sp_insert_line_tail += ", @DMODIFIED";
                    }
                    if (line.RECDATE != default)
                    {
                        sp_insert_line_head += ", RECDATE";
                        sp_insert_line_tail += ", @RECDATE";
                    }

                    var insertLineSql = $"{sp_insert_line_head} ) values ( {sp_insert_line_tail}) ";
                    conn.Execute(insertLineSql, line, trans);                   
                }

                // update the on hold table with dlb entry linked 
                var updateInvHoldReason = @$"Update {Db.WEBDB}..FTAPP_HoldDlvryDocs
                                                Set Reason = @reason, 
                                                    DlbEntry = @dlbEntry
                                                Where HeadGuid = @headGuid";

                conn.Execute(updateInvHoldReason,
                    new
                    {
                        reason = "In transit",
                        dlbEntry = docEntry,
                        headGuid = HeadGuid
                    }, trans);


                // update the dlb 2 box with new dlb entry 
                var update_dlb2 = @$"update {Db.WEBDB}..FTAPP_DLB2 
                                     set DlbEntry = @dlbEntry
                                     Where headGuid  = @headGuid";
                conn.Execute(update_dlb2, new
                {
                    dlbEntry = docEntry,
                    headGuid = HeadGuid
                }, trans);

                // perform update to the FTAPP_DLB 
                var update_FTAPP_DLB = @$"Update {Db.WEBDB}..FTAPP_DLB
                                         Set DLBEntry = @docEntry 
                                             ,WhsUserCode = @userCode
                                             ,WhsUserName = @userName
                                             ,DLBStatus = @dlbStatus 
                                             ,NRIC = @nric
                                             ,Remarks = @remarks
                                            where HeadGuid = @HeadGuid";

                conn.Execute(update_FTAPP_DLB, new
                {
                    docEntry,
                    userCode,
                    userName,
                    HeadGuid,
                    dlbStatus = "O",
                    nric = head.NRIC,
                    remarks = head.Remarks
                }, trans);

                return docEntry;
            }
            catch (Exception e)
            {
                Error = $"{e.Message}\n{e.Message}";
                return -1;
            }
        }

        TruckCompany GetTruckCompany(string truckNo)
        {
            var sp_query = @$"select top 1 t0.CARDCODE [TruckCardcode], t1.CardName [TruckCardName] , t0.TRUCKNO [TruckNo]
                            from {Db.WEBDB}..TRUCK1 t0 with (nolock) inner join 
                                 {Db.SAPDB}..OCRD t1 with (nolock) on t1.CardCode = t0.CARDCODE
                            Where t0.TRUCKNO = @truckNo";

            using var conn = new SqlConnection(Db.GetErpDbConnStr());
            var result = conn.Query<TruckCompany>(sp_query, new
            {
                truckNo = truckNo
            }).FirstOrDefault();

            return result;
        }

        TerritoryGeo GetTerritoryAndGeo(string cardCode)
        {
            var sp_query = @$"select t2.descript[TERRITORY], t1.GlblLocNum [GEOCODE] , t1.U_DELGLN  [U_DELGLN]
                                from                               
                                {Db.SAPDB}..OCRD t1 with (NOLOCK)
                                left join 
                                {Db.SAPDB}..OTER t2 with (NOLOCK) on t2.territryID = t1.Territory
                                where t1.CardCode = @cardCode";

            using var conn = new SqlConnection(Db.GetErpDbConnStr());
            var result = conn.Query<TerritoryGeo>(sp_query, new
            {
                cardCode
            }).FirstOrDefault();

            return result;
        }

        string GetUserCostCenter(string userCode)
        {
            try
            {
                var sp_query = @$"select top 1 COSTCTR, * 
                                from {Db.WEBDB}..USERCENTER 
                                where USERCODE = @userCode";

                using var conn = new SqlConnection(Db.GetWebDbConnStr());
                var costCtr = conn.ExecuteScalar<string>(sp_query, new
                {
                    userCode = userCode
                });

                return costCtr;
            }
            catch (Exception e)
            {
                Error = $"{e.Message}\n{e.Message}";
                return "";
            }
        }

        int QueryActualBoxes(int docNum, string docType)
        {
            try
            {
                string query = "";
                if (docType == "T") // transfer
                {
                    query = @$"select sum(tt1.LabelConsistTotalBoxes) 
			                    from {Db.WEBDB}..IBT tt0          with (nolock) inner join  
			                         {Db.WEBDB}..FTAPP_IBTBox tt1 with (nolock) on tt1.BaseEntry = tt0.DocEntry  
			                    where tt0.TRANSITNO = @docNum ";
                }
                else // if (docType == "I") // invoice
                {
                    query = @$"select sum(ttt2.LabelConsistTotalBoxes)
			                    from {Db.WEBDB}..SO ttt1        with (nolock) inner join
			                         {Db.WEBDB}..FTAPP_Box ttt2 with (nolock) on ttt1.DocEntry = ttt2.BaseEntry  
				               where ttt1.INVNO = @docNum ";
                }

                using var conn = new SqlConnection(Db.GetWebDbConnStr());
                var result = conn.ExecuteScalar<int>(query, new
                {
                    docNum = docNum
                });

                if (result > 0)
                {
                    return result;
                }

                return -1;
            }
            catch (Exception e)
            {
                Error = $"{e.Message}\n{e.Message}";
                return -1;
            }
        }


        /// <summary>
        /// Use when user craete dlb with web 
        /// App dlb no found 
        /// </summary>
        /// <param name="fromDLbEntry"></param>
        public void RecreteFTAPP_DLB_By_FTAPPDLB(long fromDLbEntry, DbInfo db, string commStr)
        {
            try
            {
                // check the app DLB exist 
                var sp_checkAppDlbExist = $@"Select * from {db.WEBDB}..FTAPP_DLB Where DlbEntry = @DlbEntry;";
                using var conn = new SqlConnection(commStr);
                var foundAppAlb = conn.Query<FTAPP_DLB>(sp_checkAppDlbExist, new { DlbEntry = fromDLbEntry }).FirstOrDefault();
                if (foundAppAlb == null)
                {
                    var sp_CreateAppDlb = @$"exec sp_SelectInsert_FTAPP_DLB_DLB1 @webDb, @targetDlbEntry ";
                    var res = conn.ExecuteScalar<Guid>(sp_CreateAppDlb, new
                    {
                        webDb = db.WEBDB,
                        targetDlbEntry = fromDLbEntry
                    });
                }
            }
            catch (Exception e)
            {
                Error = $"{e.Message}\n{e.Message}";
                return;
            }
        }
    }
}
