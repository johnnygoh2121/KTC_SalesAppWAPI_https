using Dapper;
using KTC_SalesAppWAPI.Models.BreadPay;
using KTC_SalesAppWAPI.Models.CommonDb;
using System;
using System.Data;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Helpers
{
    public class Bread_PAYDocHelper
    {
        public string LastErrorMessage { get; set; }
        public long UpdateDraft_Pay(Bread_Pay_Ext head, DbInfo db, string bread_dbConnStr)
        {
            try
            {
                var conn = new SqlConnection(bread_dbConnStr);
                DeleteHeadAndLine_Pay(db, conn, head);              // clear the line
                return CreateBread_Pay(head, db, bread_dbConnStr);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        long DeleteHeadAndLine_Pay(DbInfo db, SqlConnection conn, Bread_Pay_Ext head)
        {
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var deleteSql = $@"Delete from {db.WEBDB}..PAY Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);

                deleteSql = $@"Delete from {db.WEBDB}..PAY1 Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);

                deleteSql = $@"Delete from {db.WEBDB}..PAY3 Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);
                trans.Commit();
                return 1;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        // when create new invoice
        public long CreateBread_Pay(Bread_Pay_Ext head, DbInfo db, string bread_dbConnStr)
        {
            try
            {
                var conn = new SqlConnection(db.GetWebDbConnStr());
                if (head.DOCNUM == "0")
                {
                    var docNum = GetNextDocNum(head.COMPANYID, "3", head.DOCDATE, conn); // invoice doc number
                    if (string.IsNullOrWhiteSpace(docNum))
                    {
                        return -1;
                    }
                    head.DOCNUM = docNum;
                }

                if (head.DOCENTRY == 0)
                {
                    var docEntry = GetDocEntry(db, "PAY", conn);
                    head.DOCENTRY = docEntry;
                }

                for (int x = 0; x < head.Documents.Count; x++)
                {
                    head.Documents[x].DOCENTRY = head.DOCENTRY;
                    head.Documents[x].LINENUM = x + 1; // 1 base
                }

                for (int x = 0; x < head.Payments.Count; x++)
                {
                    head.Payments[x].DOCENTRY = head.DOCENTRY;
                    head.Payments[x].LINENUM = x + 1;
                }

                return InsertPay_HeadAndDetails(db, conn, head);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        long InsertPay_HeadAndDetails(DbInfo db, SqlConnection conn, Bread_Pay_Ext head)
        {

            #region sql insert head 
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // prepare to insert 
                var insertSql_head = @$" INSERT INTO {db.WEBDB}..PAY (
                                                 DOCENTRY
                                               , COMPANYID
                                               , DOCNUM
                                               , DOCSTATUS
                                               , CARDCODE
                                               , CARDNAME                                               
                                               , REFNO
                                               , REMARKS
                                               , DOCTOTAL
                                               , PAIDTOTAL
                                               , UCREATED
                                               , DCREATED
                                               , UMODIFIED
                                               , PAIDTODATE
                                               , DOCTYPE
                                               , POSTEDUSER
                                               , POSTENTRY
                                               , POSTED
                                               , FILES ";

                var insertSql_Values = @"@DOCENTRY
                                               ,@COMPANYID
                                               ,@DOCNUM
                                               ,@DOCSTATUS
                                               ,@CARDCODE
                                               ,@CARDNAME                                               
                                               ,@REFNO
                                               ,@REMARKS
                                               ,@DOCTOTAL
                                               ,@PAIDTOTAL
                                               ,@UCREATED
                                               ,@DCREATED
                                               ,@UMODIFIED
                                               ,@PAIDTODATE
                                               ,@DOCTYPE
                                               ,@POSTEDUSER
                                               ,@POSTENTRY
                                               ,@POSTED
                                               ,@FILES ";

                if (head.DOCDATE != default)
                {
                    insertSql_head += ",DOCDATE";
                    insertSql_Values += ",@DOCDATE";
                }
                if (head.DMODIFIED != default)
                {
                    insertSql_head += ",DMODIFIED";
                    insertSql_Values += ",@DMODIFIED";
                }
                if (head.POSTEDDATE != default)
                {
                    insertSql_head += ",POSTEDDATE";
                    insertSql_Values += ",@POSTEDDATE";
                }

                var combined_headVals = $"{insertSql_head} ) values ( {insertSql_Values} ) ";
                var res = conn.Execute(combined_headVals, head, trans);                
                #endregion 

                #region insert documents
                res = 0;
                foreach (var doc in head.Documents)
                {
                    var sqInsertLinehead = $@"INSERT INTO {db.WEBDB}..PAY1 (
                                             DOCENTRY
                                           , LINENUM
                                           , BASEENTRY
                                           , BASETYPE
                                           , BASEDOCNUM                                         
                                           , BASEREFNO
                                           , BASETOTAL
                                           , DOCAMOUNT
                                           , TRANSID
                                           , TRANSLINE
                                           , OBJECTCODE
                                           , SEL
                                           , BANKAMT ";

                    var sqlInsertLineTail = @" @DOCENTRY
                                              ,@LINENUM
                                              ,@BASEENTRY
                                              ,@BASETYPE
                                              ,@BASEDOCNUM 
                                              ,@BASEREFNO
                                              ,@BASETOTAL
                                              ,@DOCAMOUNT
                                              ,@TRANSID
                                              ,@TRANSLINE
                                              ,@OBJECTCODE
                                              ,@SEL
                                              ,@BANKAMT ";

                    if (doc.BASEDOCDATE != default)
                    {
                        sqInsertLinehead += ",BASEDOCDATE";
                        sqlInsertLineTail += ",@BASEDOCDATE";
                    }

                    var combineLineInsert = $"{sqInsertLinehead} ) values ({sqlInsertLineTail})";
                    res += conn.Execute(combineLineInsert, doc, trans);
                }

                # endregion

                #region insert payment 
                res = 0;
                foreach (var pay in head.Payments)
                {
                    var insertPayments_Head = @$"INSERT INTO {db.WEBDB}..PAY2 (
                                                 DOCENTRY
                                               , LINENUM
                                               , LINETYPE
                                               , LINEREF                                               
                                               , BANK
                                               , TOTAL
                                               , REMARKS
                                               , BANK2                                               
                                               , BANKUSER                                               
                                               , CANCEL
                                               , CONFIRM
                                               , FILES ";

                    var insertPayments_Tail = $@"  @DOCENTRY
                                               ,@LINENUM
                                               ,@LINETYPE
                                               ,@LINEREF                                               
                                               ,@BANK
                                               ,@TOTAL
                                               ,@REMARKS
                                               ,@BANK2                                               
                                               ,@BANKUSER                                               
                                               ,@CANCEL
                                               ,@CONFIRM 
                                               ,@FILES ";

                    if (pay.LINEDATE != default)
                    {
                        insertPayments_Head += ",LINEDATE";
                        insertPayments_Tail += ",@LINEDATE";
                    }

                    if (pay.BANKDATE != default)
                    {
                        insertPayments_Head += ",BANKDATE";
                        insertPayments_Tail += ",@BANKDATE";
                    }

                    if (pay.UPDDATE != default)
                    {
                        insertPayments_Head += ",UPDDATE";
                        insertPayments_Tail += ",@UPDDATE";
                    }

                    var combinePay2 = $"{insertPayments_Head} ) values ({insertPayments_Tail})";
                    res += conn.Execute(combinePay2, pay, trans);
                }

                #endregion end insert payments 
                trans.Commit();
                return head.DOCENTRY;
            }
            catch (Exception e)
            {
                trans.Rollback();
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
