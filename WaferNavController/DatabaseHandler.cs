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
            while (reader.Read()) {
                var row = new List<string>();
                row.Add(reader["id"].ToString());
                row.Add(reader["location"].ToString());
                row.Add(reader["available"].ToString());
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

        public static void CloseConnection() {
            myConnection.Close();
        }
    }
}
