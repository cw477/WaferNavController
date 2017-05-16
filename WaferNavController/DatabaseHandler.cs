using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Data;

namespace WaferNavController
{
    public class DatabaseHandler {

        private static SqlConnection connection;

        public static void TestConnectToDatabase()
        {
            string connectionString = generateConnectionString();
            connection = new SqlConnection(connectionString);
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                if (connectionOpenedHere)
                {
                    connection.Close();
                }

            }
            catch (Exception e)
            {

                Console.Error.WriteLine("\n**Exception was thrown in method ");
                Console.Error.Write(MethodBase.GetCurrentMethod().Name + "**");
                Console.Error.WriteLine(e.Message);
            }            
        }

        private static string generateConnectionString()
        {
            byte[] jsonByteArr = Properties.Resources.aws;
            string jsonStr = System.Text.Encoding.UTF8.GetString(jsonByteArr);
            JObject jsonObject = JObject.Parse(jsonStr);
            string connectionString = "";
            foreach (var kp in jsonObject)
            {
                connectionString += $"{kp.Key}={kp.Value};";
            }
            return connectionString;
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

        public static void fillItems(ref DataGrid dg, string tableName)
        {
            try
            {
                string cmdString = string.Empty;
                cmdString = $"SELECT [id], [location], [available] FROM [wn].[{tableName}]";
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable("BLU");
                sda.Fill(dt);
                dg.ItemsSource = dt.DefaultView;
            }
            catch (InvalidOperationException e)
            {

                Console.Error.WriteLine(e.Message);
            }
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
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_wafer_type] (id, description) Values ('{waferType}', 'unknown');", connection); //TODO: remove literal
                insertCommand.ExecuteNonQuery();

                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static string jsonToStr(Dictionary<string, string> jsonMessage)
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            var msg = "";
            msg += "\nJson:";
            var keys = jsonMessage.Keys;
            foreach (string key in keys)
            {
                msg += "\n" + key + ": " + jsonMessage[key];
            }
            msg += ":End Json";
            if (connectionOpenedHere)
            {
                connection.Close();
            }
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
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{lotId}');", connection);
                insertCommand.ExecuteNonQuery();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static bool confirmNewBlu(string bluId)
        {
            try
            {
                //TODO: Add some checking logic?
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void finishBluLoad(string bluId)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                SqlCommand cmd;

                //get waftertype associated
                var wafertype = GetData($"SELECT [wafer_type_id] FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';")[0];
                //create historic wafertype
                cmd = new SqlCommand($"INSERT INTO [wn].[historic_wafer_type] (id) Values ('{wafertype["wafer_type_id"]}');", connection);
                cmd.ExecuteNonQuery();
                //remove relation
                cmd = new SqlCommand($"DELETE FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", connection);
                cmd.ExecuteNonQuery();
                //remove wafertype
                cmd = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{wafertype["wafer_type_id"]}';", connection);
                cmd.ExecuteNonQuery();
                //add entry to historic
                cmd = new SqlCommand($"INSERT INTO [wn].[historic_blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{wafertype["wafer_type_id"]}');", connection);
                cmd.ExecuteNonQuery();
                //free blu
                SetBluToAvailable(bluId);
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static Dictionary<string, string> GetBlu(string bluId) {
            //TODO: handle the case where there are no available BLUs
            try
            {
                return GetData($"SELECT [id] AS [bluId], [location] AS [bluInfo] FROM [wn].[BLU] WHERE id = '{bluId}';")[0];

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static List<Dictionary<string, string>> GetAllActiveBibs() {
            try
            {
                return GetData($"SELECT * FROM [wn].[active_bib];");
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static List<Dictionary<string, string>> GetAllHistoricBibs() {
            try
            {
                return GetData($"SELECT * FROM [wn].[historic_bib];");
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        private static List<Dictionary<string, string>> GetData(string query) {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                SqlDataReader reader = null;
                var sqlCommand = new SqlCommand(query, connection);
                reader = sqlCommand.ExecuteReader();
                var data = new List<Dictionary<string, string>>();
                // Iterate through rows
                while (reader.Read())
                {
                    var row = new Dictionary<string, string>();

                    // Iterate through columns
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var colName = reader.GetName(i);
                        row.Add(colName, reader[colName].ToString());
                    }
                    data.Add(row);
                }
                reader.Close();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
                return data;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static Dictionary<string, string> GetSlt(object sltId)
        {
            try
            {
                //TODO: handle the case where there are no available SLTs
                return GetData($"SELECT [id] AS [sltId], [location] AS [sltInfo] FROM [wn].[SLT] WHERE id = '{sltId}';")[0];

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void AddNewActiveBibs(JArray bibIds)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var sqlText = $"INSERT INTO [wn].[active_bib] (id) Values ";
                foreach (string s in bibIds)
                {
                    sqlText += $"('" + s + "'),";
                }

                sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
                sqlText += ";";

                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void AddNewHistoricBibs(List<string> bibIds, DateTime nowDateTime)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var sqlText = $"INSERT INTO [wn].[historic_bib] (id, inserted_at) Values ";
                foreach (string s in bibIds)
                {
                    sqlText += $"('" + s + "','" + nowDateTime + "'),";
                }

                sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
                sqlText += ";";

                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();

                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static string GetFirstAvailableSltId()
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
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
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
                return sltId;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static string GetFirstAvailableBluId() {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var query = "SELECT * FROM [wn].[BLU] WHERE available = 1;";
                var sqlCommand = new SqlCommand(query, connection);
                var reader = sqlCommand.ExecuteReader();

                string bluId = null;
                while (reader.Read())
                {
                    bluId = reader["id"].ToString();
                    break;
                }
                reader.Close();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
                return bluId;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static void finishBluUnload(string bluId)
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            //throw exception if the assignment doesn't exist
            var query = new SqlCommand($"SELECT * FROM [wn].[blu_assignment_unload] WHERE [blu_id] = '{bluId}';", connection);
            if (query.ExecuteNonQuery() == 0)
            {
                throw new Exception($"There is nothing to pick up at the blu specified: {bluId}");
            }
            //assignment data
            string cmd = "SELECT * " +
                "FROM [wafer_nav].[wn].[blu_assignment_unload] " +
                $"WHERE blu_id = '{bluId}';";
            var assignmentData = GetData(cmd);

            //historic data
            cmd = "SELECT *" +
                "FROM [wafer_nav].[wn].[historic_blu_assignment_unload] " +
                $"WHERE blu_id = '{bluId}';";
            var historicData = GetData(cmd);

            foreach (Dictionary<string,string> entry in assignmentData)
            {
                try
                {
                    var theBib = entry["bib_id"];

                    //add historic bib
                    DateTime insertedAt = addBibToHistoric(entry["bib_id"]);

                    //add historic row
                    cmd = "INSERT INTO [wn].[historic_blu_assignment_unload] " +
                        "(blu_id, bib_id, bib_id_inserted_at) VALUES " +
                        $"('{bluId}','{theBib}','{insertedAt}');";
                    new SqlCommand(cmd, connection).ExecuteNonQuery();

                    //remove active row
                    cmd = "DELETE FROM [wn].[blu_assignment_unload] " +
                        $"WHERE [bib_id] = '{theBib}'";
                    if ((new SqlCommand(cmd, connection)).ExecuteNonQuery() == 0)
                    {
                        throw new Exception("ERROR: NO ROWS WERE DELETED DURING REMOVAL OF ACTIVE ASSIGNMENT ROW");
                    }

                    //remove active bib
                    cmd = $"DELETE FROM [wn].[active_bib] WHERE [id] = {theBib};";
                    var sqlCommand = new SqlCommand(cmd, connection);
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {

                    throw e;
                }
            }

            SetBluToAvailable(bluId);
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void AddSltAssignmentLoad(JArray bibIds, string sltId)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var sqlText = $"INSERT INTO [wn].[slt_assignment] (slt_id, bib_id) Values ";
                foreach (string s in bibIds)
                {
                    sqlText += $"('{sltId}', '{s}'),";
                }

                sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
                sqlText += ";";

                var insertCommand = new SqlCommand(sqlText, connection);
                insertCommand.ExecuteNonQuery();

                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void SetSLTToUnavailable(string sltId)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var query = "UPDATE[wafer_nav].[wn].[SLT] " +
                "SET available = 0 " +
                $"WHERE id = '{sltId}';";

                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();

                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void SetAllBluToAvailable() {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                    "SET available = 1;";
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void SetAllBluToUnavailable() {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "UPDATE[wafer_nav].[wn].[BLU]" +
                    "SET available = 0;";
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static bool confirmNewSlt(string sltId)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void SetBluToAvailable(string bluId) {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "UPDATE[wafer_nav].[wn].[BLU] " +
                    "SET available = 1 " +
                    $"WHERE id = '{bluId}';";
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void SetBluToUnavailable(string bluId) {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                var query = "UPDATE[wafer_nav].[wn].[BLU] " +
                    "SET available = 0 " +
                    $"WHERE id = '{bluId}';";
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void finishSlt(string sltId)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                SqlCommand cmd;

                var nowDateTime = DateTime.Now;

                //get bibs assigned to the slt
                List<Dictionary<string, string>> oldBibIds = GetData($"SELECT [bib_id] FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';");

                List<string> oldBibList = new List<string>();
                foreach (Dictionary<string, string> oldBibId in oldBibIds)
                {
                    oldBibList.Add(oldBibId["bib_id"]);
                }

                //create historic bibs
                AddNewHistoricBibs(oldBibList, nowDateTime);

                //remove relation
                cmd = new SqlCommand($"DELETE FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", connection);
                cmd.ExecuteNonQuery();


                //TODO: Figure out how to finish recycling bibIds, in conjunction with finishing bluUnload
                ////remove bibs from active
                //foreach (string oldBibId in oldBibList)
                //{
                //    try
                //    { 
                //        cmd = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{oldBibId}';", connection);
                //        cmd.ExecuteNonQuery();
                //    }
                //    catch(SqlException e)
                //    {
                //        Console.Error.WriteLine(e.Message);
                //    }
                //}

                //add entries to historic
                foreach (string oldBibId in oldBibList)
                {

                    cmd = new SqlCommand($"INSERT INTO [wn].[historic_slt_assignment] (slt_id, bib_id, bib_id_inserted_at) Values ('{sltId}','{oldBibId}','{nowDateTime}');", connection);
                    cmd.ExecuteNonQuery();

                }
                //make slt available
                cmd = new SqlCommand($"UPDATE[wafer_nav].[wn].[SLT] SET available = 1 WHERE id = '{sltId}';", connection);
                cmd.ExecuteNonQuery();


                if (connectionOpenedHere)
                {
                    connection.Close();
                }

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void AddNewActiveBib(string bibId) {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var insertCommand = new SqlCommand($"INSERT INTO [wn].[active_bib] (id) Values ('{bibId}');", connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e) {
                //TODO - Do something with exception instead of just swallowing it
                Console.Error.WriteLine(e.StackTrace);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static DateTime addBibToHistoric(string bibId) {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            var nowDateTime = DateTime.Now;
            try
            {
                var query = $"INSERT INTO [wn].[historic_bib] (id, inserted_at) Values ('{bibId}','{nowDateTime}');";
                var command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
            return nowDateTime;
        }

        public static void AddBluAssignmentUnload(JArray bibIds, string bluId)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                foreach (string bibId in bibIds)
                {
                    var insertCommand = new SqlCommand($"INSERT INTO [wn].[blu_assignment_unload] (blu_id, bib_id) Values ('{bluId}','{bibId}');", connection);
                    insertCommand.ExecuteNonQuery();
                }
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        private static void RemoveAllBlus()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[BLU];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        private static void RemoveAllSlts()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[SLT];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        private static void PopulateBluTable()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                var query = "INSERT INTO[wn].[BLU] (id, location, available) VALUES " +
                            "('123456', 'BLU#1,Handler 1,East', 1)," +
                            "('234567', 'BLU#2,Handler 2,West', 1)," +
                            "('111111', 'BLU#111,Handler 111,West', 1)," +
                            "('222222', 'BLU#222,Handler 222,West', 1)," +
                            "('333333', 'BLU#333,Handler 333,West', 1)," +
                            "('444444', 'BLU#444,Handler 444,West', 1)," +
                            "('555555', 'BLU#555,Handler 555,West', 1);";
                var insertCommand = new SqlCommand(query, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        private static void PopulateSltTable()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                var query = "INSERT INTO[wn].[SLT] (id, location, available) VALUES " +
                            "('890123', 'SLT#1,Test chamber1, North', 1)," +
                            "('901234', 'SLT#2,Test chamber2, South', 1)," +
                            "('012345', 'SLT#3,Test chamber3, East', 1)," +
                            "('123456', 'SLT#4,Test chamber4, West', 1)," +
                            "('111111', 'SLT#111,Test chamber111, West', 1)," +
                            "('222222', 'SLT#222,Test chamber222, West', 1)," +
                            "('333333', 'SLT#333,Test chamber333, West', 1)," +
                            "('444444', 'SLT#444,Test chamber444, West', 1)," +
                            "('555555', 'SLT#555,Test chamber555, West', 1);";
                var insertCommand = new SqlCommand(query, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static bool confirmDoneBlu(string bluId)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void RemoveAllActiveBibs() {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[active_bib];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception s)
            {
                Console.Error.WriteLine(s.Message);
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void RemoveAllActiveWafers()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[active_wafer_type];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                throw;
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void RemoveAllHistoricBibs() {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[historic_bib];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                throw;
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
        }

        public static void RemoveAllHistoricWafers()
        {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            try
            {
                var query = "DELETE FROM [wn].[historic_wafer_type];";
                var deleteCommand = new SqlCommand(query, connection);
                deleteCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                throw;
            }
            if (connectionOpenedHere)
            {
                connection.Close();
            }
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
                    bool connectionOpenedHere = false;
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                        connectionOpenedHere = true;
                    }
                    cmd = new SqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    if (connectionOpenedHere)
                    {
                        connection.Close();
                    }
                }
                catch (SqlException e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
        }
    }
}
