using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace WaferNavController {

    class DatabaseHandler {

        private static SqlConnection connection;

        public static void ConnectToDatabase() {

            connection = new SqlConnection(
                "Data Source=localhost;" +
                "Initial Catalog=wafer_nav;" +
                "Persist Security Info=True;" +
                "User ID=appuser;" +
                "Password=appuser;" +
                "connection timeout=10");
            connection.Open();
        }   

        public static void ResetDatabase() {
            RemoveAllBlus();
            RemoveAllActiveBibs();
            RemoveAllHistoricBibs();
            PopulateBluTable();
        }

        public static List<Dictionary<string, string>> GetAllBlus() {
            return GetData("SELECT * FROM [wn].[BLU];");
        }

        internal static void AddNewActiveWaferType(string waferType)
        {
            try
            {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_wafer_type] (id, description) Values ('{waferType}', 'unknown');", connection); //TODO: remove literal
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                //TODO: Do something with exception instead of just swallowing it
                Console.Error.WriteLine(e.Message);
            }
        }

        internal static string jsonToStr(Dictionary<string, string> jsonMessage)
        {
            var msg = "";
            msg += "\nJson:";
            var keys = jsonMessage.Keys;
            foreach (string key in keys)
            {
                msg += "\n" + key + ": " + jsonMessage[key];
            }
            Console.WriteLine(":End Json");
            return msg;
        }

        internal static string jsonToStr(Dictionary<string, object> jsonMessage)
        {
            var msg = "";
            msg += "\nJson:";
            var keys = jsonMessage.Keys;
            foreach (string key in keys)
            {
                if (key != "bibIds")
                {
                    msg += "\n" + key + ": " + (string)jsonMessage[key];
                }
                else
                {
                    msg += "\n" + key + ": [";
                    foreach (string s in (string[])jsonMessage[key])
                    {
                        msg += "\n\t" + s;
                    }
                    msg += "\n]";
                }
            }
            Console.WriteLine(":End Json");
            return msg;
        }

        internal static void AddBluAssignmentLoad(string v, string bluId)
        {
            throw new NotImplementedException();
        }

        internal static bool confirmNewBlu(string v)
        {
            throw new NotImplementedException();
        }

        internal static void removeBluAssignmentLoad(string v)
        {
            //needs to transfer to historic
            throw new NotImplementedException();
        }

        public static Dictionary<string, string> GetBlu(string bluId) {
            //TODO: handle the case where there are no available BLUs
            return GetData($"SELECT [id] AS [bluId], [location] AS [bluInfo] FROM [wn].[BLU] WHERE id = '{bluId}';")[0];
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

        internal static Dictionary<string, string> GetSlt(object sltId)
        {
            throw new NotImplementedException();
        }

        internal static void AddNewActiveBibs(string[] v)
        {
            throw new NotImplementedException();
        }

        internal static string GetFirstAvailableSltId()
        {
            throw new NotImplementedException();
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

        internal static void AddSltAssignmentLoad(string[] v1, string v2)
        {
            throw new NotImplementedException();
        }

        internal static void SetSLTToUnavailable(string v)
        {
            throw new NotImplementedException();
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

        internal static bool confirmNewSlt(string v)
        {
            throw new NotImplementedException();
        }

        public static void SetBluToAvailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU] " +
                        "SET available = 1 " +
                        $"WHERE id = '{bluId}';";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        public static void SetBluToUnavailable(string bluId) {
            var query = "UPDATE[wafer_nav].[wn].[BLU] " +
                        "SET available = 0 " +
                        $"WHERE id = '{bluId}';";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
        }

        internal static void SetSltToAvailable(string v)
        {
            throw new NotImplementedException();
        }

        internal static void removeSltAssignments(string v)
        {
            //DONT MOVE BIBS ONLY THE ASSIGNMENTS
            throw new NotImplementedException();
        }

        public static void AddNewActiveBib(string bibId) {
            try {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ({bibId});", connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e) {
                //TODO - Do something with exception instead of just swallowing it
                Console.Error.WriteLine(e.StackTrace);
            }
        }

        public static void MoveActiveBibToHistoricBib(string bibId) {
            var query = $"DELETE FROM[wn].[active_bib] where id = '{bibId}';";
            var command = new SqlCommand(query, connection);
            command.ExecuteNonQuery();

            query = $"INSERT INTO [wn].[historic_bib] (id) Values ('{bibId}');";
            command = new SqlCommand(query, connection);
            command.ExecuteNonQuery();
        }

        internal static void AddBluAssignmentUnload(string[] v1, string v2)
        {
            throw new NotImplementedException();
        }

        private static void RemoveAllBlus() {
            var query = "DELETE FROM [wn].[BLU];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        private static void PopulateBluTable() {
            var query = "INSERT INTO[wn].[BLU] (id, location, available) VALUES " +
                "('75ae9068-c245-4af4-9cab-7ff6c520de8c', 'United States', 1)," +
                "('15e29295-9d53-4f52-bf3d-3fe69cae7a8d', 'New Guinea', 1)," +
                "('dcdb8b88-d6ac-43a7-a7a2-0ad31bf82023', 'China', 1)," +
                "('d62e4d59-1196-45c4-9243-e6fbf8ffe8b9', 'Japan', 1)," +
                "('4e0c2ba6-7430-41e3-97e6-bf43953cfd20', 'Ireland', 1)," +
                "('feb76ebf-0d7a-4bba-a178-8f17bd032ac8', 'Brazil', 1)," +
                "('6f9e8b82-5452-493a-b317-f2fb47552c62', 'Norway', 1)," +
                "('a13ffc81-43b7-423d-8b25-2f536ad5b0b2', 'Sweden', 1)," +
                "('1f828955-f791-403c-8b03-d4e9e9eff8a1', 'Congo', 1)," +
                "('05f97d5c-7899-43de-a77a-c389af36a88e', 'Peru', 1);";
            var insertCommand = new SqlCommand(query, connection);
            insertCommand.ExecuteNonQuery();
        }

        internal static bool confirmDoneBlu(string v)
        {
            //move bibs that are at this blu to historic
            throw new NotImplementedException();
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
