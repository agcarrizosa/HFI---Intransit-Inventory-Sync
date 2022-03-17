using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Intransit_Inventory_Sync
{
    public class qtyAdjustmentSettings
    {
        public String MacolaServer = "";
        public String MacolaDatabase = "";
        public String MacolaSQLUser = "";
        public String MacolaSQLPassword = "";
        public String MacolaConnectionTimeOut = "";
        public String ClockTimerMM = "";
        public String SupportEmail = "";
        public String EmailServer = "";
        public String EpicorServer = "";
        public String EpicorDatabase = "";
        public String EpicorConfigFile = "";
        public String EpicorSQLUser = "";
        public String EpicorSQLPassword = "";
        public string ExtraRecipient = "";




        public int ReadSettings(ref String ErrorMessage)
        {
            int res = 0;
            try
            {
                if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\ITSettings.xml") == true)
                {
                    String myString = AppDomain.CurrentDomain.BaseDirectory + "\\ITSettings.xml";
                    XmlDocument XmlReader = new XmlDocument();
                    XmlReader.Load(myString);
                    this.MacolaServer = XmlReader.GetElementsByTagName("MacolaServer")[0].InnerText;
                    this.MacolaDatabase = XmlReader.GetElementsByTagName("MacolaDatabase")[0].InnerText;
                    this.MacolaSQLUser = XmlReader.GetElementsByTagName("MacolaSQLUser")[0].InnerText;
                    this.MacolaSQLPassword = XmlReader.GetElementsByTagName("MacolaSQLPassword")[0].InnerText;
                    this.MacolaConnectionTimeOut = XmlReader.GetElementsByTagName("MacolaConnectionTimeOut")[0].InnerText;
                    this.ClockTimerMM = XmlReader.GetElementsByTagName("ClockTimerMM")[0].InnerText;
                    this.SupportEmail = XmlReader.GetElementsByTagName("SupportEmail")[0].InnerText;
                    this.EmailServer = XmlReader.GetElementsByTagName("EmailServer")[0].InnerText;


                    this.EpicorServer = XmlReader.GetElementsByTagName("EpicorServer")[0].InnerText;
                    this.EpicorDatabase = XmlReader.GetElementsByTagName("EpicorDatabase")[0].InnerText;
                    this.EpicorConfigFile = XmlReader.GetElementsByTagName("EpicorConfigFile")[0].InnerText;
                    this.EpicorSQLUser = XmlReader.GetElementsByTagName("EpicorSQLUser")[0].InnerText;
                    this.EpicorSQLPassword = XmlReader.GetElementsByTagName("EpicorSQLPassword")[0].InnerText;
                    this.ExtraRecipient = XmlReader.GetElementsByTagName("ExtraRecipient")[0].InnerText;


                    ErrorMessage = "";
                    res = 1;
                }
                else
                {
                    ErrorMessage = "ITSettings.xml file not found, the file should be in the same folder as the application";
                    res = 0;
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Internal Transfer Epicor/Macola", ex.Message, EventLogEntryType.Error);
            }
            return res;
        }
    }
}
