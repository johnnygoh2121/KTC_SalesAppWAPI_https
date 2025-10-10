using Dapper;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using System;
using System.Data;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Helpers
{
    public class Bread_CNDocHelper
    {
        public string LastErrorMessage { get; set; }

        public long UpdateDraft_CN(Bread_CN_Ext head, DbInfo db, string bread_dbConnStr)
        {
            try
            {
                var conn = new SqlConnection(bread_dbConnStr);
                DeleteHeadAndLine_CN(db, conn, head);              // clear the line
                return CreateBread_CN(head, db, bread_dbConnStr);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        long DeleteHeadAndLine_CN(DbInfo db, SqlConnection conn, Bread_CN_Ext head)
        {
            try
            {
                var deleteSql = $@"Delete from {db.WEBDB}..CN Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY });

                deleteSql = $@"Delete from {db.WEBDB}..CN1 Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY });

                deleteSql = $@"Delete from {db.WEBDB}..CN3 Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY });

                deleteSql = $@"Delete from {db.WEBDB}..TrcnLineDetails Where DocEntry = @DocEntry and Source = 'TRCN'";
                return conn.Execute(deleteSql, new { docentry = head.DOCENTRY });
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        // when create new invoice
        public long CreateBread_CN(Bread_CN_Ext head, DbInfo db, string bread_dbConnStr)
        {
            try
            {
                var conn = new SqlConnection(db.GetWebDbConnStr());
                if (head.DOCNUM == "0")
                {
                    var docNum = GetNextDocNum(head.COMPANYID, "2", head.DOCDATE, conn); // invoice doc number
                    if (string.IsNullOrWhiteSpace(docNum))
                    {
                        return -1;
                    }
                    head.DOCNUM = docNum;
                }

                if (head.DOCENTRY == 0)
                {
                    var docEntry = GetDocEntry(db, "CN", conn);
                    head.DOCENTRY = docEntry;
                }

                for (int x = 0; x < head.Lines.Count; x++)
                {
                    head.Lines[x].DOCENTRY = head.DOCENTRY;
                }

                if (head.Batches != null)
                {
                    for (int x = 0; x < head.Batches.Count; x++)
                    {
                        head.Batches[x].DocEntry = head.DOCENTRY;
                    }
                }

                if (head.LineDetails != null)
                {
                    for (int x = 0; x < head.LineDetails.Count; x++)
                    {
                        head.LineDetails[x].DocEntry = head.DOCENTRY;
                        head.LineDetails[x].Source = "TRCN";
                    }
                }

                return InsertCN_HeadAndDetails(db, conn, head);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        long InsertCN_HeadAndDetails(DbInfo db, SqlConnection conn, Bread_CN_Ext head)
        {
            try
            {
                #region sql insert head 
                // prepare to insert 
                var insertSql = @$"  INSERT INTO {db.WEBDB}..CN (
                                          DOCENTRY
                                        , DOCSTATUS
                                        , BASEDOCNUM
                                        , DOCNUM
                                        , COMPANYID
                                        , CARDCODE
                                        , CARDNAME
                                        , DOCDATE
                                        , BASEDOCDATE
                                        , CURRENCY
                                        , DOCRATE
                                        , CUSTREF
                                        , BILLADD1
                                        , BILLADD2
                                        , BILLADD3
                                        , BILLADD4
                                        , BILLADD5
                                        , TEL
                                        , FAX
                                        , CONTACT
                                        , TOTALBD
                                        , TAXSUM
                                        , ROUNDING
                                        , DOWNPAYMENT
                                        , DOCTOTAL
                                        , PRICEID
                                        , REMARKS
                                        , REASON
                                        , UCREATED
                                        , DCREATED
                                        , UMODIFIED
                                        , DMODIFIED
                                        , PAIDTODATE
                                        , INVENTRY
                                        , CNENTRY
                                        , SAPINV , FILES, ReceiverCode
                                        ) values ( 
                                          @DOCENTRY
                                         ,@DOCSTATUS
                                         ,@BASEDOCNUM
                                         ,@DOCNUM
                                         ,@COMPANYID
                                         ,@CARDCODE
                                         ,@CARDNAME
                                         ,GETDATE()
                                         ,GETDATE()
                                         ,@CURRENCY
                                         ,@DOCRATE
                                         ,@CUSTREF
                                         ,@BILLADD1
                                         ,@BILLADD2
                                         ,@BILLADD3
                                         ,@BILLADD4
                                         ,@BILLADD5
                                         ,@TEL
                                         ,@FAX
                                         ,@CONTACT
                                         ,@TOTALBD
                                         ,@TAXSUM
                                         ,@ROUNDING
                                         ,@DOWNPAYMENT
                                         ,@DOCTOTAL
                                         ,@PRICEID
                                         ,@REMARKS
                                         ,@REASON
                                         ,@UCREATED
                                         ,GETDATE()
                                         ,@UMODIFIED
                                         ,GETDATE()
                                         ,@PAIDTODATE
                                         ,@INVENTRY
                                         ,@CNENTRY
                                         ,@SAPINV , @FILES , @ReceiverCode)";

                var res = conn.Execute(insertSql, head);
                if (res < 0)
                {
                    LastErrorMessage = "Insert head cn fail";
                    return -1;
                }
                #endregion 

                #region insert lines
                res = 0;
                var sqInsertLinehead = $@"INSERT INTO {db.WEBDB}..CN1 (
                                          DOCENTRY
                                        , LINENUM
                                        , BASEENTRY
                                        , BASELINE
                                        , ITEMCODE
                                        , ITEMNAME
                                        , QUANTITY
                                        , PRICE
                                        , TAXCODE
                                        , TAXPERC
                                        , TAXSUM
                                        , LINETOTAL
                                        , LINETYPE ";

                var sqlInsertLineTail = @"@DOCENTRY
                                            ,@LINENUM
                                            ,@BASEENTRY
                                            ,@BASELINE
                                            ,@ITEMCODE
                                            ,@ITEMNAME
                                            ,@QUANTITY
                                            ,@PRICE
                                            ,@TAXCODE
                                            ,@TAXPERC
                                            ,@TAXSUM
                                            ,@LINETOTAL
                                            ,@LINETYPE ";

                var combineInsert = $"{sqInsertLinehead} ) values ({sqlInsertLineTail})";
                res = conn.Execute(combineInsert, head.Lines);

                if (res < 0)
                {
                    LastErrorMessage = "Insert CN lines fail";
                    return -1;
                }
                #endregion

                // insert the batch is any 
                #region insert bacth
                if (head.Batches == null) return head.DOCENTRY;
                var sqlInsertBatch = $@"INSERT INTO {db.WEBDB}..CN3 (
                                        DOCENTRY
                                       ,LINENUM
                                       ,LINENUM2
                                       ,BATCHNO
                                       ,QUANTITY 
                                    )  VALUES ( 
                                          @DOCENTRY
                                         ,@LINENUM
                                         ,@LINENUM2
                                         ,@BATCHNO
                                         ,@QUANTITY 
                                    )";

                res = conn.Execute(sqlInsertBatch, head.Batches);
                if (res < 0)
                {
                    LastErrorMessage = "Insert batch lines fail";
                    return -1;
                }
                #endregion

                #region insert line details if any 
                // insert for line details 
                if (head.LineDetails == null) return head.DOCENTRY;
                for (int dl = 0; dl < head.LineDetails.Count; dl++)
                {
                    var line = head.LineDetails[dl];
                    if (line == null) continue;

                    var sqlInsertLineDetails_head = $@"INSERT INTO {db.WEBDB}..TrcnLineDetails (
                                                          DocEntry
                                                        , LineNum                                               
                                                        , Remark
                                                        , LotNo
                                                        , Module
                                                        , ReasonCode
                                                        , WhsCode, Source ";

                    var sqlInsertLineDetails_tail = @"@DocEntry
                                                    , @LineNum                                              
                                                    , @Remark
                                                    , @LotNo
                                                    , @Module 
                                                    , @ReasonCode
                                                    , @WhsCode, @Source ";

                    if (line.ExpDate != default)
                    {
                        sqlInsertLineDetails_head += ",ExpDate ";
                        sqlInsertLineDetails_tail += ",@ExpDate ";
                    }

                    if (line.MfrDt != default)
                    {
                        sqlInsertLineDetails_head += ",MfrDt ";
                        sqlInsertLineDetails_tail += ",@MfrDt ";
                    }

                    var cmbineSql = $"{sqlInsertLineDetails_head}) values ({sqlInsertLineDetails_tail})";
                    res = conn.Execute(cmbineSql, line);
                }

                if (res < 0)
                {
                    LastErrorMessage = "Insert trcn line details fail";
                    return -1;
                }
                #endregion

                return head.DOCENTRY;
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        int GetDocEntry(DbInfo db, string tableName, SqlConnection conn)
        {
            try
            {
                var sql = @$"Select Max(docentry)+1  
                            from {db.WEBDB}..{tableName}";

                return conn.ExecuteScalar<int>(sql);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        // code reference from pit boon
        /// <summary>
        /// docid : 1 = Invoice, 2 = Credit Notes
        /// this is function to generate docnum
        /// </summary>
        /// <param name="companyid">ref to user code of seller / distributor</param>
        /// <param name="docid"></param>
        /// <param name="docDate"></param>
        /// <param name="conn"></param>
        /// <param name="errmsg"></param>
        /// <returns></returns>
        string GetNextDocNum(string companyid, string docid, DateTime docDate,
             SqlConnection conn, bool isTesting = false)
        {
            string docnum = "";
            string seriesno = "";
            string prefix = "", surfix = "", surfixDate = "", reset = "";
            int startNo = 1, noDigit = 6;
            int currNo = 0;
            int year = 0, month = 0;
            try
            {
                using (SqlDataAdapter dat = new SqlDataAdapter("", conn))
                {
                    dat.SelectCommand.CommandText = "SELECT * FROM DOCNUMBERING WHERE COMPANYID = @1 AND DOCID = @2";
                    dat.SelectCommand.Parameters.Clear();
                    dat.SelectCommand.Parameters.AddWithValue("@1", companyid);
                    dat.SelectCommand.Parameters.AddWithValue("@2", docid);
                    DataTable dt = new DataTable();
                    dat.Fill(dt);
                    if (dt.Rows.Count <= 0)
                    {
                        throw new Exception("Document numbering does not define!");
                    }
                    if (dt.Rows.Count > 0)
                    {
                        seriesno = dt.Rows[0]["SERIESID"].ToString();
                        prefix = dt.Rows[0]["PREFIX"].ToString();
                        surfix = dt.Rows[0]["SURFIX"].ToString();
                        surfixDate = dt.Rows[0]["SURFIXDATE"].ToString().Replace("m", "M").Replace("Y", "y").Replace("D", "d");
                        reset = dt.Rows[0]["RESET"].ToString();
                        if (!int.TryParse(dt.Rows[0]["STARTNO"].ToString(), out startNo)) startNo = 1;
                        if (!int.TryParse(dt.Rows[0]["NODIGIT"].ToString(), out noDigit)) noDigit = 6;
                    }
                    if (reset == "M" || reset == "Y")
                    {
                        year = docDate.Year;
                    }
                    if (reset == "M")
                    {
                        month = docDate.Month;
                    }

                    dat.SelectCommand.CommandText = "SELECT * FROM DOCNUM " +
                                                    "WHERE SERIESID = @1 AND SMONTH = @2 AND SYEAR = @3";
                    dat.SelectCommand.Parameters.Clear();
                    dat.SelectCommand.Parameters.AddWithValue("@1", seriesno);
                    dat.SelectCommand.Parameters.AddWithValue("@2", month);
                    dat.SelectCommand.Parameters.AddWithValue("@3", year);
                    dt.Reset();
                    dat.Fill(dt);
                    string updcmd = "";

                    if (isTesting)
                    {
                        goto ByPass;
                    }

                    if (dt.Rows.Count > 0)
                    {
                        if (!int.TryParse(dt.Rows[0]["CURRNO"].ToString(), out currNo)) currNo = 0;
                        currNo++;
                        updcmd = "UPDATE DOCNUM SET CURRNO = @CURRNO WHERE SERIESID = @SERIESID " +
                                    "AND SMONTH = @SMONTH AND SYEAR = @SYEAR";
                    }
                    else
                    {
                        currNo = startNo;
                        updcmd = "INSERT INTO DOCNUM (SERIESID, SMONTH, SYEAR, CURRNO) " +
                                    "VALUES (@SERIESID, @SMONTH, @SYEAR, @CURRNO) ";
                    }

                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand comm = conn.CreateCommand())
                    {
                        comm.CommandText = updcmd;
                        comm.Parameters.Clear();
                        comm.Parameters.AddWithValue("@SERIESID", seriesno);
                        comm.Parameters.AddWithValue("@SMONTH", month);
                        comm.Parameters.AddWithValue("@SYEAR", year);
                        comm.Parameters.AddWithValue("@CURRNO", currNo);
                        comm.ExecuteNonQuery();
                    }

                ByPass:
                    docnum = prefix + currNo.ToString().PadLeft(noDigit, '0') + surfix;
                    if (surfixDate != "")
                    {
                        docnum += docDate.ToString(surfixDate);
                    }
                }

                return docnum;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"{ex.Message}\n{ex.StackTrace}";
                return "";
            }
        }
    }
}

