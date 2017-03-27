using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace WaferNavController {

    class DatabaseHandler {

        private static SqlConnection connection;

        public static void ConnectToDatabase() {
            connection = new SqlConnection(
                "user id=appuser;" +
                "password=appuser;" +
                "server=localhost;" +
                "database=wafer_nav;" +
                "connection timeout=3");
            connection.Open();
        }

        public static List<Dictionary<string, string>> GetAllBlus() {
            return GetData("SELECT * FROM [wn].[BLU];");
        }

        public static Dictionary<string, string> GetBlu(string bluId) {
            return GetData($"SELECT * FROM [wn].[BLU] WHERE id = {bluId};")[0];
        }

        public static List<Dictionary<string, string>> GetAllActiveBibs() {
            return GetData("SELECT * FROM [wn].[active_bib];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricBibs() {
            return GetData("SELECT * FROM [wn].[historic_bib];");
        }

        private static List<Dictionary<string, string>> GetData(string query) {
            var sqlCommand = new SqlCommand(query, connection);
            var reader = sqlCommand.ExecuteReader();

            var data = new List<Dictionary<string, string>>();

            // Iterate through rows
            while (reader.Read()) {
                var row = new Dictionary<string, string>();

                // Iterate through columns
                for (var i = 0; i < reader.FieldCount; i++) {
                    var colName = reader.GetName(i);
                    row.Add(colName, reader[colName].ToString());
                }
                data.Add(row);
            }
            reader.Close();
            return data;
        }

        public static string GetFirstAvailableBluId() {
            var query = "SELECT * FROM [wn].[BLU] WHERE available = 1;";
            var sqlCommand = new SqlCommand(query, connection);
            var reader = sqlCommand.ExecuteReader();

            string bluId = null;
            while (reader.Read()) {
                bluId = reader["id"].ToString();
                break;
            }
            reader.Close();
            return bluId;
        }

        public static void SetAllBluToAvailable() {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 1;";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        public static void SetAllBluToUnavailable() {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 0;";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        public static void SetBluToAvailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 1" +
                        $"WHERE id = {bluId};";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        public static void SetBluToUnavailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                        "SET available = 0" +
                        $"WHERE id = {bluId};";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        public static void AddNewActiveBib(string bibId) {
            try {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ({bibId});", connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception exception) {
                //TODO - Do something with exception instead of just swallowing it
            }
        }

        public static void MoveActiveBibToHistoricBib(string bibId) {
            var query = $"DELETE FROM[wn].[active_bib] where id = {bibId};";
            var command = new SqlCommand(query, connection);
            command.ExecuteNonQuery();

            query = $"INSERT INTO [wn].[historic_bib] (id) Values ({bibId});";
            command = new SqlCommand(query, connection);
            command.ExecuteNonQuery();
        }

        public static void RemoveAllActiveBibs() {
            var query = "DELETE FROM [wn].[active_bib];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllHistoricBibs() {
            var query = "DELETE FROM [wn].[historic_bib];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void CloseConnection() {
            connection.Close();
        }
    }
}
