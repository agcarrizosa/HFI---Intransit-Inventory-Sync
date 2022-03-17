using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intransit_Inventory_Sync
{
    class SQLConnectionManager
    {
        private String Server = "";
        private String Database = "";
        private String SQLUser = "";
        private String SQLPassword = "";
        private String ConnectionTimeOut = "";
        private SqlConnection MainConnection;

        public SQLConnectionManager(String Server, String Database, String SQLUser, String SQLPassword, String ConnectionTimeOut)
        {
            this.Server = Server;
            this.Database = Database;
            this.SQLUser = SQLUser;
            this.SQLPassword = SQLPassword;
            this.ConnectionTimeOut = ConnectionTimeOut;
        }

        public SqlConnection OpenConnection(ref String ErrorMessage)
        {
            try
            {
                MainConnection = new SqlConnection("user id=" + this.SQLUser + ";" +
                                                    "password=" + this.SQLPassword + ";" +
                                                    "server=" + this.Server + ";" +
                                                    "database=" + this.Database + ";" +
                                                     //"Trusted_Connection=yes;" +
                                                     "connection timeout=" + this.ConnectionTimeOut);
                MainConnection.Open();
            }
            catch (InvalidCastException e)
            {
                ErrorMessage = e.Message;
                EventLog.WriteEntry("Internal Transfer Epicor/Macola", e.Message, EventLogEntryType.Error);
                //return 0;
            }
            ErrorMessage = "";
            return MainConnection;
        }

        public void CloseConnection()
        {
            MainConnection.Close();
        }


        public DataSet ExecuteStoredProcedureWithReturn(String SP, Hashtable Parameters)
        {
            SqlParameter SQLParam;
            SqlDataAdapter SQLAdapter = new SqlDataAdapter();
            DataSet SQLDSet = new DataSet();
            SqlCommand SQLCommandoSP = new SqlCommand(SP, MainConnection);
            SQLCommandoSP.CommandType = CommandType.StoredProcedure;
            if (Parameters.Count != 0)
            {
                foreach (DictionaryEntry Lnum in Parameters)
                {
                    SQLParam = new SqlParameter();
                    SQLParam.ParameterName = ((SQLParametersManager)Lnum.Value).ParameterName;
                    switch (((SQLParametersManager)Lnum.Value).ParameterType)
                    {
                        case SQLParametersManager.PType.Bit:
                            SQLParam.DbType = DbType.Binary;
                            break;
                        case SQLParametersManager.PType.Entero:
                            SQLParam.DbType = DbType.Int32;
                            break;
                        case SQLParametersManager.PType.Float:
                            SQLParam.DbType = DbType.Double;
                            break;
                        case SQLParametersManager.PType.Float_Decimal:
                            SQLParam.DbType = DbType.Decimal;
                            break;
                        case SQLParametersManager.PType.Money:
                            SQLParam.DbType = DbType.Currency;
                            break;
                        case SQLParametersManager.PType.Varchar:
                            SQLParam.DbType = DbType.String;
                            break;
                    }
                    SQLParam.Value = ((SQLParametersManager)Lnum.Value).ParameterValue;
                    SQLCommandoSP.Parameters.Add(SQLParam);
                }
            }
            SQLAdapter.SelectCommand = SQLCommandoSP;
            SQLAdapter.Fill(SQLDSet);
            SQLAdapter.Dispose();
            return SQLDSet;
        }

        public void ExecuteStoredProcedure(String SP, Hashtable Parameters)
        {
            SqlParameter SQLParam;
            DataSet SQLDSet = new DataSet();
            SqlCommand SQLCommandoSP = new SqlCommand(SP, MainConnection);
            SQLCommandoSP.CommandType = CommandType.StoredProcedure;
            if (Parameters.Count != 0)
            {
                foreach (DictionaryEntry Lnum in Parameters)
                {
                    SQLParam = new SqlParameter();
                    SQLParam.ParameterName = ((SQLParametersManager)Lnum.Value).ParameterName;
                    switch (((SQLParametersManager)Lnum.Value).ParameterType)
                    {
                        case SQLParametersManager.PType.Bit:
                            SQLParam.DbType = DbType.Binary;
                            break;
                        case SQLParametersManager.PType.Entero:
                            SQLParam.DbType = DbType.Int32;
                            break;
                        case SQLParametersManager.PType.Float:
                            SQLParam.DbType = DbType.Double;
                            break;
                        case SQLParametersManager.PType.Float_Decimal:
                            SQLParam.DbType = DbType.Decimal;
                            break;
                        case SQLParametersManager.PType.Money:
                            SQLParam.DbType = DbType.Currency;
                            break;
                        case SQLParametersManager.PType.Varchar:
                            SQLParam.DbType = DbType.String;
                            break;
                    }
                    SQLParam.Value = ((SQLParametersManager)Lnum.Value).ParameterValue;
                    SQLCommandoSP.Parameters.Add(SQLParam);
                }
            }
            SQLCommandoSP.ExecuteNonQuery();
            SQLCommandoSP.Dispose();
        }


        public DataSet ExecuteQuery(string queryString, List<SqlParameter> parameters)
        {


            DataSet SQLDSet = new DataSet();
            SqlDataAdapter sqlData = new SqlDataAdapter();

            SqlCommand command = new SqlCommand(queryString, MainConnection);
            command.Parameters.AddRange(parameters.ToArray());
            try
            {

                sqlData.SelectCommand = command;
                sqlData.Fill(SQLDSet);

            }
            finally
            {
                // Always call Close when done reading.
                sqlData.Dispose();
            }

            return SQLDSet;

        }
    }
}
