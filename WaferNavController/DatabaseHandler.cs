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

        public static List<List<string>> GetAllBlus() {
            return GetData("SELECT * FROM [wn].[BLU]");
        }

        public static List<List<string>> GetAllActiveBibs() {
            return GetData("SELECT * FROM [wn].[active_bib]");
        }

        public static List<List<string>> GetAllHistoricBibs() {
            return GetData("SELECT * FROM [wn].[historic_bib]");
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

        public static void SetBluToUnavailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 0" +
                        $"WHERE id = {bluId}";
            var updateCommand = new SqlCommand(query, myConnection);
            updateCommand.ExecuteNonQuery();
        }

        public static void SetBluToAvailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 1" +
                        $"WHERE id = {bluId}";
            var updateCommand = new SqlCommand(query, myConnection);
            updateCommand.ExecuteNonQuery();
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

        public static void MoveActiveBibToHistoricBib(string bibId) {
            var query = $"DELETE FROM[wn].[active_bib] where id = {bibId};";
            var command = new SqlCommand(query, myConnection);
            command.ExecuteNonQuery();

            query = $"INSERT INTO [wn].[historic_bib] (id) Values ({bibId});";
            command = new SqlCommand(query, myConnection);
            command.ExecuteNonQuery();
        }

        public static void RemoveAllActiveBibs() {
            var query = "DELETE FROM [wn].[active_bib];";
            var deleteCommand = new SqlCommand(query, myConnection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllHistoricBibs() {
            var query = "DELETE FROM [wn].[historic_bib];";
            var deleteCommand = new SqlCommand(query, myConnection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void CloseConnection() {
            myConnection.Close();
        }
    }
}
