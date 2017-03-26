using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WaferNavController {

    class DatabaseHandler {

        private static SqlConnection myConnection;

        public static void ConnectToDatabase() {
            myConnection = new SqlConnection(
                "user id=appuser;" +
                "password=appuser;" +
                "server=localhost;" +
                "database=wafer_nav;" +
                "connection timeout=3");
            myConnection.Open();
        }

        public static List<List<String>> GetData(String query) {
            SqlDataReader reader = null;
            SqlCommand sqlCommand = new SqlCommand(query, myConnection);
            reader = sqlCommand.ExecuteReader();

            List<List<String>> data = new List<List<string>>();

            // Iterate through rows
            while (reader.Read()) {
                var row = new List<string>();

                // Iterate through columns
                for (int i = 0; i < reader.FieldCount; i++) {
                    string colName = reader.GetName(i);
                    row.Add(reader[colName].ToString());
                }
                data.Add(row);
            }
            reader.Close();
            return data;
        }

        public static String GetFirstAvailableBluId() {
            String query = "SELECT * FROM [wn].[BLU] WHERE available = 1;";
            SqlDataReader reader = null;
            SqlCommand sqlCommand = new SqlCommand(query, myConnection);
            reader = sqlCommand.ExecuteReader();
            String bluId = null;
            while (reader.Read()) {
                bluId = reader["id"].ToString();
                break;
            }
            reader.Close();
            return bluId;
        }

        public static void AddNewActiveBib(String bibId) {
            try {
                SqlCommand insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ({bibId})", myConnection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception exception) {
                //TODO - Do something with exception instead of just swallowing it
            }
        }

        public static void CloseConnection() {
            myConnection.Close();
        }
    }
}
