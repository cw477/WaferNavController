using System;
using System.Collections.Generic;
using System.Data.SqlClient;

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

        public static List<List<string>> GetData(string query) {
            var sqlCommand = new SqlCommand(query, myConnection);
            var reader = sqlCommand.ExecuteReader();

            var data = new List<List<string>>();

            // Iterate through rows
            while (reader.Read()) {
                var row = new List<string>();

                // Iterate through columns
                for (var i = 0; i < reader.FieldCount; i++) {
                    var colName = reader.GetName(i);
                    row.Add(reader[colName].ToString());
                }
                data.Add(row);
            }
            reader.Close();
            return data;
        }

        public static string GetFirstAvailableBluId() {
            var query = "SELECT * FROM [wn].[BLU] WHERE available = 1;";
            var sqlCommand = new SqlCommand(query, myConnection);
            var reader = sqlCommand.ExecuteReader();

            string bluId = null;
            while (reader.Read()) {
                bluId = reader["id"].ToString();
                break;
            }
            reader.Close();
            return bluId;
        }

        public static void AddNewActiveBib(string bibId) {
            try {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ({bibId})", myConnection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception exception) {
                //TODO - Do something with exception instead of just swallowing it
            }
        }

        public static void RemoveAllActiveBibs() {
            var query = "DELETE FROM [wn].[active_bib];";
            var deleteCommand = new SqlCommand(query, myConnection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void CloseConnection() {
            myConnection.Close();
        }
    }
}
