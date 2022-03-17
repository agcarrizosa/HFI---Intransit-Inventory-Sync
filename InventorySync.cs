using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Wisys.AllSystem;
using Wisys.ImTrx;

namespace Intransit_Inventory_Sync
{
    public partial class InventorySync : ServiceBase
    {
        public static Ice.Core.Session epicorSession = null;
        public qtyAdjustmentSettings mainSettings { get; set; }
        List<Error> errorList { get; set; }
        EmailManager MailManager = new EmailManager("smtp.hfinet.com");
        private string EventTitle = "Intransit Inventory Sync Epicor/Macola";
        private Timer MainTimer = new Timer();
        SqlConnection con1 = new SqlConnection("Data Source=HFIOCSQLTEST01;Initial Catalog=002;Integrated Security=True");
        SqlConnection con2 = new SqlConnection("Data Source=HFIAZEPCRTEST01;Initial Catalog=EpicorTest;Integrated Security=True");
        int TranNum;
        string ord, tr, b;

        public InventorySync()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            try
            {
                //Epicor connection
                epicorSession = new Ice.Core.Session("manager", "manager", Ice.Core.Session.LicenseType.Default, @"C:\\Epicor\\Client\\config\\EpicorTest.sysconfig");
                epicorSession.PlantID = "INTRAN";
                //creating and opening connection to dbs
                //con1.Open();
                //con2.Open();

                EventLog.WriteEntry(EventTitle,"Service started", EventLogEntryType.Information);
                string ErrorMessage = "";
                mainSettings = new qtyAdjustmentSettings();


                if (mainSettings.ReadSettings(ref ErrorMessage) == 1)
                {
                    EventLog.WriteEntry(EventTitle, "Settings read", EventLogEntryType.Information);


                    //EventLog.WriteEntry(EventTitle, "Manin list  count: "+ mainList.Count.ToString(), EventLogEntryType.Information);


                    EventLog.WriteEntry(EventTitle, @"Service Started" + "\n" +
                       "Macola Server: " + mainSettings.MacolaServer + "\n" +
                       "Macola Database: " + mainSettings.MacolaDatabase + "\n" +
                       "Macola SQL User: " + mainSettings.MacolaSQLUser + "\n" +
                       "Macola SQL Password: " + mainSettings.MacolaSQLPassword + "\n" +
                       "Epicor Server: " + mainSettings.EpicorServer + "\n" +
                       "Epicor Database: " + mainSettings.EpicorDatabase + "\n" +
                       "Support Email: " + mainSettings.SupportEmail + "\n" +
                       "Timer: " + mainSettings.ClockTimerMM + "\n" +
                       "Macola Connection Time out: " + mainSettings.MacolaConnectionTimeOut);

                    MainTimer.Interval = Convert.ToDouble(mainSettings.ClockTimerMM);
                    MainTimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
                    MainTimer.Enabled = true;
                    MainTimer.Start();
                    EventLog.WriteEntry(EventTitle, "Timer started", EventLogEntryType.Information);


                }
                else
                {
                    EventLog.WriteEntry(EventTitle, ErrorMessage, EventLogEntryType.Error);
                    MailManager.SendEmail(mainSettings.SupportEmail, "InternalTransfer@hfi-inc.com", "Error Reading the Settings File", ErrorMessage, ref ErrorMessage);
                }



            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                EventLog.WriteEntry(EventTitle, e.Message, EventLogEntryType.Error);
            }

            //enabling, setting interval and starting timer.

            //MainTimer.Enabled = true;
            //MainTimer.Interval = 30000; //86400000
            //MainTimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            //MainTimer.Start();

        }

        //stoping timer and closing connection to both the databases.
        protected override void OnStop()
        {
            EventLog.WriteEntry(EventTitle, "Service Stoped", EventLogEntryType.Information);
            MainTimer.Stop();
            con1.Close();
            con2.Close();

            //Disconnect from Epicor
            epicorSession.Dispose();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Create object to use Epicor Business Objects
            var objInventoryQtyAdj = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<Erp.Proxy.BO.InventoryQtyAdjImpl>(epicorSession, Erp.Proxy.BO.InventoryQtyAdjImpl.UriPath);

            EventLog.WriteEntry(EventTitle, "Timer Elapsed", EventLogEntryType.Information);

            SqlConnection con1 = new SqlConnection("Data Source=HFIOCSQLTEST01;Initial Catalog=002;Integrated Security=True");
            using (SqlConnection conn = con1)
            {
                try
                {
                    EventLog.WriteEntry(EventTitle, "Macola -> Epicor", EventLogEntryType.Information);
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"select iminvtrx_sql.id,iminvtrx_sql.ord_no,iminvtrx_sql.item_no,iminvtrx_sql.loc,iminvtrx_sql.lev_no,iminvtrx_sql.trx_dt,iminvtrx_sql.trx_tm,iminvtrx_sql.source,iminvtrx_sql.doc_type,iminvtrx_sql.quantity, iminvtrx_sql.extra_8, iminvtrx_sql.extra_9
                                        from iminvtrx_sql with(nolock) 
                                        where source = 'I' and doc_type = 'T'
                                        and iminvtrx_sql.loc = '50'
                                        and iminvtrx_sql .trx_dt >= '02/16/2022'
										and iminvtrx_sql.quantity > '0'
                                        and iminvtrx_sql.ord_no not in (select ord_no from [HFIAZEPCRTEST01].[EpicorTest].[dbo].[HFI_Internal_Transfer] with(nolock))
                                        and iminvtrx_sql.item_no in (select iminvloc_sql.item_no
                                        from iminvloc_sql with(nolock) inner join imlocfil_sql with(nolock)
                                        on iminvloc_sql.loc = imlocfil_sql.loc
                                        inner join imitmidx_sql with(nolock)
                                        on imitmidx_sql.item_no = iminvloc_sql.item_no
                                        and mrp_fg = 'Y'
                                        and imitmidx_sql.activity_cd = 'A')
                                        order by iminvtrx_sql.trx_dt asc,iminvtrx_sql.ord_no,iminvtrx_sql.lev_no";
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable MacolaInstransitDS = new DataTable();
                            adapter.Fill(MacolaInstransitDS);

                            string ord_no, truck, bol;

                            foreach (DataRow dtRow in MacolaInstransitDS.Rows)
                            {
                                ord_no = dtRow["ord_no"].ToString().Trim();
                                string itemnum = dtRow["item_no"].ToString().Trim();
                                string loc = dtRow["loc"].ToString().Trim();
                                string fromto = dtRow["lev_no"].ToString().Trim();
                                truck = dtRow["extra_8"].ToString().Trim();
                                bol = dtRow["extra_9"].ToString().Trim();
                                string itemqty;
                                if (fromto == "0" && loc == "50")
                                {
                                    itemqty = "-" + dtRow["quantity"].ToString().Trim();
                                }
                                else
                                {
                                    itemqty = dtRow["quantity"].ToString().Trim();
                                }
                                //Get dataset for part
                                Erp.BO.InventoryQtyAdjDataSet dsInventoryQtyAdjDataSet = new Erp.BO.InventoryQtyAdjDataSet();
                                dsInventoryQtyAdjDataSet = objInventoryQtyAdj.GetInventoryQtyAdj(itemnum, "");

                                dsInventoryQtyAdjDataSet.Tables["InventoryQtyAdj"].Rows[0]["AdjustQuantity"] = itemqty;
                                dsInventoryQtyAdjDataSet.Tables["InventoryQtyAdj"].Rows[0]["ReasonCode"] = "INTT";
                                dsInventoryQtyAdjDataSet.Tables["InventoryQtyAdj"].Rows[0]["LotNum"] = "11-2021";
                                dsInventoryQtyAdjDataSet.Tables["InventoryQtyAdj"].Rows[0]["BinNum"] = "Intrans";

                                bool bInput = false;
                                string strMessage = "";

                                try
                                {
                                    objInventoryQtyAdj.PreSetInventoryQtyAdj(dsInventoryQtyAdjDataSet, out bInput);
                                    objInventoryQtyAdj.SetInventoryQtyAdj(dsInventoryQtyAdjDataSet, out strMessage);

                                    int tnum = Convert.ToInt32(strMessage.Remove(0, 15));

                                    SqlConnection conI = new SqlConnection("Data Source=HFIAZEPCRTEST01;Initial Catalog=EpicorTest;Integrated Security=True");
                                    using (SqlConnection con = conI)
                                    {
                                        con.Open();
                                        using (SqlCommand cmdI = con.CreateCommand())
                                        {
                                            cmdI.CommandText = @"INSERT INTO [HFIAZEPCRTEST01].[EpicorTest].[dbo].[HFI_Internal_Transfer] (TranNum, ord_no, truck, bol) VALUES(@TranNum, @ord_no, @truck, @bol)";

                                            cmdI.Parameters.AddWithValue("@TranNum", tnum);
                                            cmdI.Parameters.AddWithValue("@ord_no", ord_no);
                                            cmdI.Parameters.AddWithValue("@truck", truck);
                                            cmdI.Parameters.AddWithValue("@bol", bol);

                                            cmdI.ExecuteNonQuery();
                                        }
                                        con.Close();
                                    }
                                }

                                catch (Exception er)
                                {
                                    string errorMsg = er.Message;
                                    if (strMessage != null)
                                    {
                                        addToErrorList(itemnum, loc, itemqty, strMessage);
                                        string emailBody = string.Empty;
                                        emailBody = AssembleStringHTMLMSG(errorList);//, results);
                                        MailManager.SendEmail(mainSettings.SupportEmail, "InternalTransfer@hfi-inc.com", "Quantity Adjustments Results", emailBody, ref strMessage);
                                    }
                                    else
                                    {
                                        addToErrorList(itemnum, loc, itemqty, er.Message);
                                        string emailBody = string.Empty;
                                        emailBody = AssembleStringHTMLMSG(errorList);//, results);
                                        MailManager.SendEmail(mainSettings.SupportEmail, "InternalTransfer@hfi-inc.com", "Quantity Adjustments Results", emailBody, ref errorMsg);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception er)
                {
                    EventLog.WriteEntry(EventTitle, er.Message, EventLogEntryType.Information);
                }
                finally 
                {
                    conn.Close();
                }
                
                
            }

            SqlConnection con2 = new SqlConnection("Data Source=HFIAZEPCRTEST01;Initial Catalog=EpicorTest;Integrated Security=True");
            //Epicor -> Macola
            using (SqlConnection conn = con2)
            {
                try
                {
                    EventLog.WriteEntry(EventTitle, "Epicor -> Macola", EventLogEntryType.Information);
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"Select TranNum, PartNum, WareHouseCode, BinNum, WareHouse2, BinNum2, TranDate, TranQty 
                                        from Erp.PartTran with(nolock) where WareHouseCode = 'CANAL' AND BinNum = '50Intran' AND Plant = 'INTRAN' AND TranType = 'ADJ-QTY'
                                        and TranDate >= '02/16/2022' and TranNum not in (select TranNum from [EpicorTest].[dbo].[HFI_Internal_Transfer] with(nolock))";
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable EpicorInstransitDS = new DataTable();
                            adapter.Fill(EpicorInstransitDS);
                            List<string> errorsFound = new List<string>();

                            foreach (DataRow dtRow in EpicorInstransitDS.Rows)
                            {
                                String sRtnErr = "";
                                //Wisys.AllSystem.ConnectionInfo WisysConn = new Wisys.AllSystem.ConnectionInfo();
                                Wisys.AllSystem.ConnectionInfo WiSysTrans = new Wisys.AllSystem.ConnectionInfo();

                                //Update
                                try
                                {
                                    WiSysTrans.Parameters(mainSettings.MacolaServer, mainSettings.MacolaDatabase); //,mainSettings.MacolaSQLUser,mainSettings.MacolaSQLPassword,"HFI Qty Adj");
                                                                                                                   //WiSysTrans.TestConnectivity(ref sRtnErr);
                                    WiSysTrans.OpenWisysConnection(true, ref sRtnErr);

                                    //if (quantity > 0 && type == "ADJ-QTY")
                                    //{
                                    //    quantity = quantity * -1;
                                    //}
                                    Wisys.ImTrx.QtyAdj qtyAdj = new Wisys.ImTrx.QtyAdj();

                                    TranNum = Convert.ToInt32(dtRow["TranNum"]);
                                    qtyAdj.Connection(ref WiSysTrans);
                                    string itemnum = dtRow["PartNum"].ToString().Trim();
                                    //string location = dtRow["BinNum"].ToString();
                                    decimal qty = Convert.ToDecimal(dtRow["TranQty"]);
                                    qtyAdj.ItemNumber = itemnum;
                                    qtyAdj.Location = "50";
                                    qtyAdj.Quantity = decimal.ToDouble(qty);
                                    qtyAdj.Comment1 = "Transaction from Internal Transfer Service";
                                    qtyAdj.UserName = "Test";
                                    qtyAdj.DocumentDate = DateTime.Now;
                                    string comment = TranNum.ToString() + itemnum;
                                    qtyAdj.Comment2 = comment;
                                    qtyAdj.JobNo = "";

                                    qtyAdj.PostTrx(ref sRtnErr);

                                    if (sRtnErr != "")
                                    {
                                        errorsFound.Add(sRtnErr);
                                    }

                                    using (SqlConnection conS = con1)
                                    {
                                        conS.Open();

                                        using (SqlCommand cmdS = conS.CreateCommand())
                                        {
                                            cmdS.CommandText = @"SELECT iminvtrx_sql.ord_no, iminvtrx_sql.extra_8 ,iminvtrx_sql.extra_9 
                                                             FROM [HFIOCSQLTEST01].[002].[dbo].[iminvtrx_sql] with (nolock)
                                                             WHERE iminvtrx_sql.comment_2 = @comment";

                                            cmdS.Parameters.AddWithValue("@comment", comment);
                                            ;
                                            //cmdS.ExecuteNonQuery();
                                            using (SqlDataAdapter adpt = new SqlDataAdapter(cmdS))
                                            {
                                                DataTable Comment2DS = new DataTable();
                                                adpt.Fill(Comment2DS);

                                                foreach (DataRow CmtRow in Comment2DS.Rows)
                                                {
                                                    ord = CmtRow["ord_no"].ToString().Trim();
                                                    tr = CmtRow["extra_8"].ToString().Trim();
                                                    b = CmtRow["extra_9"].ToString().Trim();
                                                }
                                            }
                                        }
                                        conS.Close();
                                    }

                                    using (SqlCommand cmdI = conn.CreateCommand())
                                    {
                                        cmdI.CommandText = @"INSERT INTO [HFIAZEPCRTEST01].[EpicorTest].[dbo].[HFI_Internal_Transfer] (TranNum, ord_no, truck, bol) VALUES(@TranNum, @ord_no, @truck, @bol)";

                                        cmdI.Parameters.AddWithValue("@TranNum", TranNum);
                                        cmdI.Parameters.AddWithValue("@ord_no", ord);
                                        cmdI.Parameters.AddWithValue("@truck", tr);
                                        cmdI.Parameters.AddWithValue("@bol", b);

                                        cmdI.ExecuteNonQuery();
                                    }
                                }

                                catch
                                {
                                    addToErrorList(dtRow["PartNum"].ToString().Trim(), "50", (dtRow["TranQty"]).ToString().Trim(), sRtnErr);
                                    string emailBody = string.Empty;
                                    emailBody = AssembleStringHTMLMSG(errorList);
                                    MailManager.SendEmail(mainSettings.SupportEmail, "InternalTransfer@hfi-inc.com", "Quantity Adjustments Results", emailBody, ref sRtnErr);
                                }

                                finally
                                {
                                    //WiSysTrans.CloseWisysConnection(Wisys.AllSystem.TrxEnums.TransactionAction.Rollback, ref sRtnErr);
                                    WiSysTrans.CloseWisysConnection(TrxEnums.TransactionAction.Commit, ref sRtnErr);

                                }
                            }
                        }
                    }
                }
                catch (Exception er)
                {
                    EventLog.WriteEntry(EventTitle, er.Message, EventLogEntryType.Information);
                }
                finally 
                {
                    conn.Close();
                }
                
            }
            con1.Close();
            con2.Close();
            epicorSession.Dispose();
        }

        private string AssembleStringHTMLMSG(List<Error> errorList)//, List<FinalResult> resultList)
        {
            string TableTail = "</body></html>";

            string TableHead = "";

            //TableHeadError = "<html><head><style>#PLs { font-family: 'Trebuchet MS', Arial, Helvetica, sans-serif; border-collapse: collapse; width: 100%; } #PLs td, #PLs th { border: 1px solid #ddd; padding: 8px; } #PLs tr:nth-child(even){background-color: #f2f2f2;} #PLs tr:hover {background-color: #ddd;} #PLs th { padding-top: 12px; padding-bottom: 12px; text-align: left; background-color: #C71616; color: white; } </style></head>";

            TableHead = "<html><head><style>#PLs { font-family: 'Trebuchet MS', Arial, Helvetica, sans-serif; border-collapse: collapse; width: 100%; } #PLs td, #PLs th { border: 1px solid #ddd; padding: 8px; } #PLs tr:nth-child(even){background-color: #f2f2f2;} #PLs tr:hover {background-color: #ddd;} #PLs th { padding-top: 12px; padding-bottom: 12px; text-align: left; background-color: #164CE1; color: white; } #PLs2 { font-family: 'Trebuchet MS', Arial, Helvetica, sans-serif; border-collapse: collapse; width: 100%; } #PLs2 td, #PLs th { border: 1px solid #ddd; padding: 8px; } #PLs2 tr:nth-child(even){background-color: #f2f2f2;} #PLs2 tr:hover {background-color: #ddd;} #PLs2 th { padding-top: 12px; padding-bottom: 12px; text-align: left; background-color: #C71616; color: white; } </style></head>";

            string Body = "<h2>" + "Date: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm") + "</h2>";

            Body += "<body>";

            //if (resultList.Count > 0)
            //{
            //    Body += "<table id='PLs'>";
            //    Body += "<th><b>Item Number</b></th><th><b>Location</b></th><th><b>Packing</b></th><th><b>Quantity</b></th>";
            //    foreach (FinalResult e in resultList)
            //    {
            //        Body += "<tr><td>" + e.ItemNumber + "</td>";
            //        Body += "<td>" + e.Location + "</td>";
            //        Body += "<td>" + e.Packing + "</td>";
            //        Body += "<td>" + e.Quantity + "</td></tr>";
            //    }
            //    Body += "</table>";
            //}

            if (errorList.Count > 0)
            {
                Body += "<br><br><table id='PLs2'>";

                Body += "<th><b>Item Number</b></th><th><b>Location</b></th><th><b>Quantity</b></th><th><b>Error</b></th>";
                foreach (Error e in errorList)
                {

                    Body += "<tr><td>" + e.ItemNUmber + "</td>";
                    Body += "<td>" + e.Location + "</td>";
                    Body += "<td>" + e.Quantity + "</td>";
                    Body += "<td>" + e.ErrorMessage + "</td></tr>";
                }
                Body += "</table>";
            }
            return TableHead + Body + TableTail;
        }


        private bool addToErrorList(string itemNumber, string location, string quantity, string errorMessage)
        {
            errorList = new List<Error>();
            int count = errorList.Where(e => e.ItemNUmber == itemNumber && e.ErrorMessage == errorMessage && e.Quantity == quantity).Count();
            if (count == 0)
            {
                Error err = new Error();
                err.ErrorMessage = errorMessage;
                err.ItemNUmber = itemNumber;
                err.Location = location;
                err.Quantity = quantity;
                errorList.Add(err);
                errorList.Add(err);
            }
            //Text File Log
            using (StreamWriter tw = File.AppendText(@"C:\Users\agarcia\Documents\TransferErrors.txt"))
            {
                foreach (var e in errorList)
                {
                    tw.WriteLine(string.Format("Item: {0}\nError Message: {1}\nLocation: {2}\nQuantity: {3}\n", e.ItemNUmber, e.ErrorMessage, e.Location, e.Quantity));
                }
            }
            return count == 0;
        }
    }
}
