using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace WaferNavController {

    public class DatabaseHandler {

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
            RemoveAllRelations();
            RemoveAllBlus();
            RemoveAllSlts();
            RemoveAllActiveBibs();
            RemoveAllHistoricBibs();
            RemoveAllActiveWafers();
            RemoveAllHistoricWafers();
            PopulateBluTable();
            PopulateSltTable();
        }

        public static List<Dictionary<string, string>> GetAllBlus()
        {
            return GetData($"SELECT * FROM [wn].[BLU];");
        }

        public static List<Dictionary<string, string>> GetAllSlts()
        {
            return GetData($"SELECT * FROM [wn].[SLT];");
        }

        public static List<Dictionary<string, string>> GetAllActiveWafers()
        {
            return GetData($"SELECT * FROM [wn].[active_wafer_type];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricWafers()
        {
            return GetData($"SELECT * FROM [wn].[historic_wafer_type];");
        }

        public static List<Dictionary<string, string>> GetAllBluLoadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[blu_assignment_load];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricBluLoadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[historic_blu_assignment_load];");
        }

        public static List<Dictionary<string, string>> GetAllBluUnloadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[blu_assignment_unload];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricBluUnloadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[historic_blu_assignment_unload];");
        }

        public static List<Dictionary<string, string>> GetAllSltAssignments()
        {
            return GetData($"SELECT * FROM [wn].[slt_assignment];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricSltAssignments()
        {
            return GetData($"SELECT * FROM [wn].[historic_slt_assignment];");
        }

        public static void AddNewActiveWaferType(string waferType)
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

        public static string jsonToStr(Dictionary<string, string> jsonMessage)
        {
            var msg = "";
            msg += "\nJson:";
            var keys = jsonMessage.Keys;
            foreach (string key in keys)
            {
                msg += "\n" + key + ": " + jsonMessage[key];
            }
            msg += ":End Json";
            return msg;
        }

        public static string jsonToStr(Dictionary<string, object> jsonMessage)
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
            msg += ":End Json";
            return msg;
        }

        public static void AddBluAssignmentLoad(string lotId, string bluId)
        {
            try
            {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{lotId}');", connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                //TODO: Do something with exception instead of just swallowing it
                Console.Error.WriteLine(e.Message);
            }
        }

        public static bool confirmNewBlu(string v)
        {
            //TODO: Add some checking logic?
            return true;
        }

        public static void finishBluLoad(string bluId)
        {
            SqlCommand cmd;
            //get waftertype associated
            var wafertype = GetData($"SELECT [wafer_type_id] FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';")[0];
            
            //create historic wafertype
            try
            {
                cmd = new SqlCommand($"INSERT INTO [wn].[historic_wafer_type] (id) Values ('{wafertype["wafer_type_id"]}');", connection);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            //remove relation
            try
            {
                cmd = new SqlCommand($"DELETE FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", connection);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            //remove wafertype
            try
            {
                cmd = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{wafertype["wafer_type_id"]}';", connection);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            //add entry to historic
            try
            { 
            cmd = new SqlCommand($"INSERT INTO [wn].[historic_blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{wafertype["wafer_type_id"]}');", connection);
            cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            //free blu
            SetBluToAvailable(bluId);
        }

        public static Dictionary<string, string> GetBlu(string bluId) {
            //TODO: handle the case where there are no available BLUs
            return GetData($"SELECT [id] AS [bluId], [location] AS [bluInfo] FROM [wn].[BLU] WHERE id = '{bluId}';")[0];
        }

        public static List<Dictionary<string, string>> GetAllActiveBibs() {
            return GetData($"SELECT * FROM [wn].[active_bib];");
        }

        public static List<Dictionary<string, string>> GetAllHistoricBibs() {
            return GetData($"SELECT * FROM [wn].[historic_bib];");
        }

        private static List<Dictionary<string, string>> GetData(string query) {
            SqlDataReader reader = null;
            try
            {
                var sqlCommand = new SqlCommand(query, connection);
                reader = sqlCommand.ExecuteReader();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e);
            }
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

        public static Dictionary<string, string> GetSlt(object sltId)
        {
            //TODO: handle the case where there are no available SLTs
            return GetData($"SELECT [id] AS [sltId], [location] AS [sltInfo] FROM [wn].[SLT] WHERE id = '{sltId}';")[0];
        }

        public static void AddNewActiveBibs(JArray bibIds)
        {
            var sqlText = $"INSERT INTO [wn].[active_bib] (id) Values ";
            foreach (string s in bibIds)
            {
                sqlText += $"('" + s + "'),";
            }

            sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
            sqlText += ";";

            try
            {
                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e);
            }
        }


        public static void AddNewHistoricBibs(List<string> bibIds)
        {
            var sqlText = $"INSERT INTO [wn].[historic_bib] (id) Values ";
            foreach (string s in bibIds)
            {
                sqlText += $"('" + s + "'),";
            }

            sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
            sqlText += ";";

            try
            {
                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static string GetFirstAvailableSltId()
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

        public static void finishBluUnload(string bluId)
        {
            //TODO: Create method to reset the blu and add bibs to historics if they arent there etc.
            Console.Error.WriteLine("**********************FINISH BLU UNLOAD NOT IMPLEMENTED YET*********************");
        }

        public static void AddSltAssignmentLoad(JArray bibIds, string sltId)
        {
            var sqlText = $"INSERT INTO [wn].[slt_assignment] (slt_id, bib_id) Values ";
            foreach (string s in bibIds)
            {
                sqlText += $"('{sltId}', '{s}'),";
            }

            sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
            sqlText += ";";

            try
            {
                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static void SetSLTToUnavailable(string sltId)
        {
            var query = "UPDATE[wafer_nav].[wn].[SLT] " +
            "SET available = 0 " +
            $"WHERE id = '{sltId}';";
            try
            {
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.WriteLine(e);
            }

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

        public static bool confirmNewSlt(string sltId)
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

        public static void finishSlt(string sltId)
        {
            SqlCommand cmd;

            //get bibs assigned to the slt
            List<Dictionary<string, string>> oldBibIds = GetData($"SELECT [bib_id] FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';");

            List<string> oldBibList = new List<string>();
            foreach (Dictionary<string,string> oldBibId in oldBibIds)
            {
                oldBibList.Add(oldBibId["bib_id"]);
            }
            //create historic bibs
            AddNewHistoricBibs(oldBibList);

            //remove relation
            try
            {
                cmd = new SqlCommand($"DELETE FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", connection);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            //remove bibs from active
  
            foreach (string oldBibId in oldBibList)
            {
                try
                { 
                    cmd = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{oldBibId}';", connection);
                    cmd.ExecuteNonQuery();
                }
                catch(SqlException e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            //add entries to historic
            foreach (string oldBibId in oldBibList)
            { 
                try
                {
                    cmd = new SqlCommand($"INSERT INTO [wn].[historic_slt_assignment] (slt_id, bib_id) Values ('{sltId}','{oldBibId}');", connection);
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }

            //make slt available
            try
            {
                cmd = new SqlCommand($"UPDATE[wafer_nav].[wn].[BLU] SET available = 1 WHERE id = '{sltId}';", connection);
                cmd.ExecuteNonQuery();
            }
            catch(SqlException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static void AddNewActiveBib(string bibId) {
            try {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ('{bibId}');", connection);
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

        public static void AddBluAssignmentUnload(JArray bibIds, string bluId)
        {
            foreach (string bibId in bibIds)
            { 
                try
                {
                    var insertCommand = new SqlCommand($"INSERT INTO [wn].[blu_assignment_unload] (blu_id, wafer_type_id) Values ('{bluId}','{bibId}');", connection);
                    insertCommand.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    //TODO: Do something with exception instead of just swallowing it
                    Console.Error.WriteLine(e.Message);
                }
            }
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
                "('123456', 'BLU#1,Handler 1,East', 1)," +
                "('234567', 'BLU#2,Handler 2,West', 1);";
            var insertCommand = new SqlCommand(query, connection);
            insertCommand.ExecuteNonQuery();
        }

        private static void PopulateSltTable()
        {
            var query = "INSERT INTO[wn].[SLT] (id, location, available) VALUES " +
                "('890123', 'SLT#1,Test chamber1, North', 1)," +
                "('901234', 'SLT#2,Test chamber2, South', 1)," +
                "('012345', 'SLT#3,Test chamber3, East', 1)," +
                "('123456', 'SLT#4,Test chamber4, West', 1);";
            var insertCommand = new SqlCommand(query, connection);
            insertCommand.ExecuteNonQuery();
        }

        public static bool confirmDoneBlu(string v)
        {
            return true;
        }

        public static void RemoveAllActiveBibs() {
            var query = "DELETE FROM [wn].[active_bib];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllActiveWafers()
        {
            var query = "DELETE FROM [wn].[active_wafer_type];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllHistoricBibs() {
            var query = "DELETE FROM [wn].[historic_bib];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllHistoricWafers()
        {
            var query = "DELETE FROM [wn].[historic_wafer_type];";
            var deleteCommand = new SqlCommand(query, connection);
            deleteCommand.ExecuteNonQuery();
        }

        public static void RemoveAllRelations()
        {
            List<string> queries = new List<string>();
            queries.Add("DELETE FROM [wn].[blu_assignment_load];");
            queries.Add("DELETE FROM [wn].[blu_assignment_unload];");
            queries.Add("DELETE FROM [wn].[historic_blu_assignment_load];");
            queries.Add("DELETE FROM [wn].[historic_blu_assignment_unload];");
            queries.Add("DELETE FROM [wn].[slt_assignment];");
            queries.Add("DELETE FROM [wn].[historic_slt_assignment];");

            SqlCommand cmd = null;
            foreach (string query in queries)
            {
                try
                {
                    cmd = new SqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        public static void CloseConnection() {
            connection.Close();
        }
    }
}
