using Dapper;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using System;
using System.Data;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Helpers
{
    public class Bread_INVDocHelper
    {
        public string LastErrorMessage { get; set; }

        public long UpdateDraftInvoice(Bread_OINV_Ext head, DbInfo db, string bread_dbConnStr)
        {
            try
            {
                var res = DeleteHeadAndLine(db, head);   // clear the line
                if (res == -1)
                {
                    LastErrorMessage = $"Deletion error for INV doc entry {head.DOCENTRY}";
                    return -1;
                }

                return CreateBreadInvoice(head, db, bread_dbConnStr);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }
        long DeleteHeadAndLine(DbInfo db, Bread_OINV_Ext head)
        {
            using var conn = new SqlConnection(db.GetWebDbConnStr());
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var deleteSql = $@"Delete from {db.WEBDB}..INV Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);

                deleteSql = $@"Delete from {db.WEBDB}..INV1 Where DocEntry = @DocEntry";
                conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);

                deleteSql = $@"Delete from {db.WEBDB}..INV3 Where DocEntry = @DocEntry";
                var result = conn.Execute(deleteSql, new { docentry = head.DOCENTRY }, trans);

                trans.Commit();
                return result;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastErrorMessage = $"{e.Message}, {e.StackTrace}";
                return -1;
            }
        }

        // when create new invoice
        public long CreateBreadInvoice(Bread_OINV_Ext head, DbInfo db, string crCommonDbConnStr)
        {
            try
            {
                using var conn = new SqlConnection(db.GetWebDbConnStr());
                if (head.DOCNUM == "0")
                {
                    var docNum = GetNextDocNum(head.COMPANYID, "1", head.DOCDATE, conn); // invoice doc number
                    if (string.IsNullOrWhiteSpace(docNum))
                    {
                        return -1;
                    }
                    head.DOCNUM = docNum;
                }

                if (head.DOCENTRY == 0)
                {
                    var docEntry = GetDocEntry(db, "INV", conn);
                    head.DOCENTRY = docEntry;
                }

                for (int x = 0; x < head.Lines.Count; x++)
                {
                    head.Lines[x].DOCENTRY = head.DOCENTRY;
                }

                // update the batch docentry
                if (head.Batches != null && head.Batches.Count > 0)
                {
                    for (int x = 0; x < head.Batches.Count; x++)
                    {
                        head.Batches[x].DocEntry = head.DOCENTRY;
                    }
                }

                return InsertInvoiceHeadAndDetails(db, conn, head);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        long InsertInvoiceHeadAndDetails(DbInfo db, SqlConnection conn, Bread_OINV_Ext head)
        {
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                #region sql insert head 
                // prepare to insert 
                var insertSql = @$"  INSERT INTO {db.WEBDB}..INV (
                                     DOCENTRY
                                   , DOCSTATUS
                                   , DOCNUM
                                   , COMPANYID
                                   , CARDCODE
                                   , CARDNAME
                                   , DOCDATE
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
                                   , UCREATED
                                   , DCREATED
                                   , UMODIFIED
                                   , DMODIFIED
                                   , PAIDTODATE
                                   , INVENTRY
                                   , CNENTRY
                                   , SAPINV
                                   , APPR
                                   , APPRUSER
                                 --  , APPRDATE
                                   , APPRREM
                                   , HOLREM
                                   , HOLDREM , FILES
                                        ) Values (
                                     @DOCENTRY
                                    ,@DOCSTATUS
                                    ,@DOCNUM
                                    ,@COMPANYID
                                    ,@CARDCODE
                                    ,@CARDNAME
                                    ,GETDATE() --@DOCDATE
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
                                    ,@UCREATED
                                    ,GETDATE()   ---DCREATED
                                    ,@UMODIFIED
                                    ,@DMODIFIED
                                    ,@PAIDTODATE
                                    ,@INVENTRY
                                    ,@CNENTRY
                                    ,@SAPINV
                                    ,@APPR
                                    ,@APPRUSER
                                    --,GETDATE()
                                    ,@APPRREM
                                    ,@HOLREM
                                    ,@HOLDREM , @FILES )";
                #endregion 

                var res = conn.Execute(insertSql, head, trans);
                #region insert lines
                var sqInsertLine = $@"INSERT INTO {db.WEBDB}..INV1 (
                                        DOCENTRY
                                       ,LINENUM
                                       ,ITEMCODE
                                       ,ITEMNAME
                                       ,QUANTITY
                                       ,PRICE
                                       ,TAXCODE
                                       ,TAXPERC
                                       ,TAXSUM
                                       ,LINETOTAL
                                       ,LINETYPE 
                                        ) VALUES (
                                           @DOCENTRY
                                          ,@LINENUM
                                          ,@ITEMCODE
                                          ,@ITEMNAME
                                          ,@QUANTITY
                                          ,@PRICE
                                          ,@TAXCODE
                                          ,@TAXPERC
                                          ,@TAXSUM
                                          ,@LINETOTAL
                                          ,@LINETYPE )";
                res = conn.Execute(sqInsertLine, head.Lines, trans);
                #endregion

                // insert the batch is any 
                #region insert bacth
                if (head.Batches == null)
                {
                    trans.Commit();
                    return head.DOCENTRY;
                }

                if (head.Batches.Count == 0)
                {
                    trans.Commit();
                    return head.DOCENTRY;
                }

                var sqlInsertBatch = $@"INSERT INTO {db.WEBDB}..INV3 (
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
                res = conn.Execute(sqlInsertBatch, head.Batches, trans);               
                #endregion

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
                    dat.SelectCommand.CommandText = " SELECT * FROM DOCNUMBERING " +
                                                    " WHERE COMPANYID = @1 AND DOCID = @2";
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

                    dat.SelectCommand.CommandText = "SELECT * " +
                                                    "FROM DOCNUM " +
                                                    "WHERE SERIESID = @1 " +
                                                    "AND SMONTH = @2 " +
                                                    "AND SYEAR = @3 ";

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
