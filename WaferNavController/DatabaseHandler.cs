using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace WaferNavController {

    class DatabaseHandler {

        private static SqlConnection connection;

        public static void ConnectToDatabase() {
            byte[] jsonByteArr = Properties.Resources.aws;
            string jsonStr = System.Text.Encoding.UTF8.GetString(jsonByteArr);
            JObject jsonObject = JObject.Parse(jsonStr);
            string connectionString = "";
            foreach (var kp in jsonObject) {
                connectionString += $"{kp.Key}={kp.Value};";
            }
            connection = new SqlConnection(connectionString);
            connection.Open();
        }

        public static void ResetDatabase() {
            RemoveAllBlus();
            RemoveAllSlts();
            RemoveAllActiveBibs();
            RemoveAllHistoricBibs();
            PopulateBluTable();
            PopulateSltTable();
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
                    foreach (string s in (JArray)jsonMessage[key])
                    {
                        msg += "\n\t" + s;
                    }
                    msg += "\n]";
                }
            }
            Console.WriteLine(":End Json");
            return msg;
        }

        internal static void AddBluAssignmentLoad(string lotId, string bluId)
        {
            try
            {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[blu_assignment_load] (blu_id, wafer_type_id) Values ({bluId}'{lotId}');", connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                //TODO: Do something with exception instead of just swallowing it
                Console.Error.WriteLine(e.Message);
            }
        }

        internal static bool confirmNewBlu(string v)
        {
            //TODO: Add some checking logic?
            return true;
        }

        internal static void removeBluAssignmentLoad(string bluId)
        {
            //get waftertype associated
            var wafertype = GetData($"SELECT [wafer_type_id] FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';")[0];
            //create historic wafertype
            var insertCommand = new SqlCommand($"INSERT INTO [wn].[historic_wafer_type] (id) Values ({wafertype["wafer_type_id"]});", connection);
            insertCommand.ExecuteNonQuery();
            //remove relation, then wafertype
            var deleteCommand = new SqlCommand($"DELETE FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", connection);
            deleteCommand.ExecuteNonQuery();
            deleteCommand = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{wafertype["wafer_type_id"]}';", connection);
            deleteCommand.ExecuteNonQuery();
            //add entry to historic
            insertCommand = new SqlCommand($"INSERT INTO [wn].[historic_blu_assignment_load] (blu_id, wafer_type_id, completed_at) Values ({bluId},{wafertype["wafer_type_id"]}, CURRENT_TIMESTAMP);", connection);
            insertCommand.ExecuteNonQuery();
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
            //TODO: handle the case where there are no available SLTs
            return GetData($"SELECT [id] AS [sltId], [location] AS [sltInfo] FROM [wn].[SLT] WHERE id = '{sltId}';")[0];
        }

        internal static void AddNewActiveBibs(JArray v)
        {
            var sqlText = $"INSERT INTO [wn].[active_bib] (id) Values ";
            foreach (string s in v)
            {
                sqlText += $"(" + s + "),";
            }

            sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
            sqlText += ";";

            var insertCommand = new SqlCommand(sqlText, connection);
            insertCommand.ExecuteNonQuery();
        }

        internal static string GetFirstAvailableSltId()
        {
            var query = "SELECT * FROM [wn].[SLT] WHERE available = 1;";
            var sqlCommand = new SqlCommand(query, connection);
            var reader = sqlCommand.ExecuteReader();

            string sltId = null;
            while (reader.Read())
            {
                sltId = reader["id"].ToString();
                break;
            }
            reader.Close();
            return sltId;
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

        internal static void AddSltAssignmentLoad(JArray bibIds, string sltId)
        {
            var sqlText = $"INSERT INTO [wn].[slt_assignment] (slt_id, bib_id) Values ";
            foreach (string s in bibIds)
            {
                sqlText += $"({sltId}, " + s + "),";
            }

            sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
            sqlText += ";";

            var insertCommand = new SqlCommand(sqlText, connection);
            insertCommand.ExecuteNonQuery();
        }

        internal static void SetSLTToUnavailable(string sltId)
        {
            var query = "UPDATE[wafer_nav].[wn].[SLT] " +
            "SET available = 0 " +
            $"WHERE id = '{sltId}';";
            var updateCommand = new SqlCommand(query, connection);
            updateCommand.ExecuteNonQuery();
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
            return true;
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

        private static void RemoveAllBlus()
        {
            var query = "DELETE FROM [wn].[BLU];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        private static void RemoveAllSlts()
        {
            var query = "DELETE FROM [wn].[SLT];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        private static void PopulateBluTable()
        {
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

        private static void PopulateSltTable()
        {
            var query = "INSERT INTO[wn].[SLT] (id, location, available) VALUES " +
                "('dfbbdb1c-8346-44a6-ac0f-be627312460b', 'Los Angeles', 1)," +
                "('e5def8d6-e466-4706-99c8-0a49980dd548', 'New York', 1)," +
                "('e95584d7-829f-4e68-afaf-cc566444049f', 'Chicago', 1)," +
                "('5bd3d648-eb9b-49ef-9d26-d9ad6a0b695e', 'San Francisco', 1)," +
                "('d045c5b5-e544-4bb3-9690-640fdf54a0de', 'Boston', 1)," +
                "('029a0825-e991-4b34-8ae6-cf362d36ccf0', 'Miami', 1)," +
                "('32c0ef0c-7ae3-4dbd-a32b-5975bf80126c', 'Seattle', 1)," +
                "('f7c03c6b-7bbc-48d3-bcc4-a693acf852e7', 'Portland', 1)," +
                "('cdfc868c-a672-4746-907a-f8a9067230a6', 'New Orleans', 1)," +
                "('7270c8e7-10f3-4e2b-ba02-bf083157fdd0', 'Austin', 1);";
            var insertCommand = new SqlCommand(query, connection);
            insertCommand.ExecuteNonQuery();
        }

        internal static bool confirmDoneBlu(string v)
        {
            //move bibs that are at this blu to historic
            return true;
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
