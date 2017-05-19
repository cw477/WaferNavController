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
                cmdString = $"SELECT [id], [site_name], [site_description], [site_location], [available] FROM [wn].[{tableName}]";
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

        public static void setForLoad(string waferType, string bluId)
        {
            SqlTransaction tran = null;
            bool transStarted = false;
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                tran = connection.BeginTransaction();
                transStarted = true;

                //insert new lotid/wafertype
                var query = new SqlCommand($"INSERT INTO [wn].[active_wafer_type] (id, description) Values ('{waferType}', 'unknown');", connection, tran); //TODO: remove literal
                query.ExecuteNonQuery();

                //assign
                query = new SqlCommand($"INSERT INTO [wn].[blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{waferType}');", connection, tran);
                query.ExecuteNonQuery();

                //set unavailable
                query = new SqlCommand("UPDATE[wafer_nav].[wn].[BLU] " +
                    "SET available = 0 " +
                    $"WHERE id = '{bluId}';", connection, tran);
                query.ExecuteNonQuery();

                //commit all
                tran.Commit(); 
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                if (transStarted)
                {
                    tran.Rollback();
                }
                throw e;
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
            msg += "\n:End Json\n";
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

        public static void setForTest(string bluId, JArray bibIds, string sltId)
        {
            SqlTransaction tran = null;
            bool transStarted = false;
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                tran = connection.BeginTransaction();
                transStarted = true;

                //get waftertype associated
                var wafertype = GetData($"SELECT [wafer_type_id] FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", tran)[0];
                
                //create historic wafertype
                var cmd = new SqlCommand($"INSERT INTO [wn].[historic_wafer_type] (id) Values ('{wafertype["wafer_type_id"]}');", connection, tran);
                cmd.ExecuteNonQuery();

                //remove relation
                cmd = new SqlCommand($"DELETE FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", connection, tran);
                cmd.ExecuteNonQuery();

                //remove wafertype
                cmd = new SqlCommand($"DELETE FROM [wn].[active_wafer_type] WHERE [id] = '{wafertype["wafer_type_id"]}';", connection, tran);
                cmd.ExecuteNonQuery();

                //add entry to historic
                cmd = new SqlCommand($"INSERT INTO [wn].[historic_blu_assignment_load] (blu_id, wafer_type_id) Values ('{bluId}','{wafertype["wafer_type_id"]}');", connection, tran);
                cmd.ExecuteNonQuery();

                //free blu
                SetBluToAvailable(bluId, tran);

                //add bibs
                var sqlText = $"INSERT INTO [wn].[active_bib] (id) Values ";
                foreach (string s in bibIds)
                {
                    sqlText += $"('" + s + "'),";
                }

                sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
                sqlText += ";";

                cmd = new SqlCommand(sqlText, connection, tran);
                cmd.ExecuteNonQuery();

                //assign bibs
                sqlText = $"INSERT INTO [wn].[slt_assignment] (slt_id, bib_id) Values ";
                foreach (string s in bibIds)
                {
                    sqlText += $"('{sltId}', '{s}'),";
                }

                sqlText = sqlText.Remove(sqlText.LastIndexOf(","), 1);
                sqlText += ";";

                cmd = new SqlCommand(sqlText, connection, tran);
                cmd.ExecuteNonQuery();

                //set to unavailable
                sqlText = "UPDATE[wafer_nav].[wn].[SLT] " +
                    "SET available = 0 " +
                    $"WHERE id = '{sltId}';";

                cmd = new SqlCommand(sqlText, connection, tran);
                cmd.ExecuteNonQuery();

                //commit all if it worked
                tran.Commit();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                if (transStarted)
                {
                    tran.Rollback();
                }
                throw e;
            }
        }

        public static Dictionary<string, string> GetBlu(string bluId) {
            //TODO: handle the case where there are no available BLUs
            try
            {
                return GetData("SELECT [id] AS [bluId], " +
                    "[site_name] AS [bluSiteName], [site_description] as [bluSiteDescription], [site_location] as [bluSiteLocation] " +
                    $"FROM [wn].[BLU] WHERE id = '{bluId}';")[0];

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

        private static List<Dictionary<string, string>> GetData(string query)
        {
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

        private static List<Dictionary<string, string>> GetData(string query, SqlTransaction tran)
        {
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                SqlDataReader reader = null;
                var sqlCommand = new SqlCommand(query, connection, tran);
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
                return GetData("SELECT [id] AS [sltId], " +
                    "[site_name] AS [sltSiteName], [site_description] as [sltSiteDescription], [site_location] as [sltSiteLocation] " +
                    $"FROM [wn].[BLU] WHERE id = '{sltId}';")[0];

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static void batchAddHistoricBibs(List<string> bibIds, DateTime nowDateTime, SqlTransaction tran)
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

                var insertCommand = new SqlCommand(sqlText, connection, tran);
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
                if (!(reader.HasRows))
                {
                    throw new Exception("No available SLT.");
                }
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
                if (!(reader.HasRows))
                {
                    throw new Exception("No available BLU.");
                }
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
            /*TODO: Model this method like others, so that it can pass any
             * exception to be passed back to the app, but in this case
             * it may not be the app that does this part...
            */
            SqlTransaction tran = null;
            bool transStarted = false;
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                connectionOpenedHere = true;
            }
            tran = connection.BeginTransaction();
            transStarted = true;
            //throw exception if the assignment doesn't exist
            var query = new SqlCommand($"SELECT * FROM [wn].[blu_assignment_unload] WHERE [blu_id] = '{bluId}';", connection, tran);
            if (query.ExecuteNonQuery() == 0)
            {
                throw new Exception($"There is nothing to pick up at the blu specified: {bluId}");
            }
            //assignment data
            string cmd = "SELECT * " +
                "FROM [wafer_nav].[wn].[blu_assignment_unload] " +
                $"WHERE blu_id = '{bluId}';";
            var assignmentData = GetData(cmd,tran);

            //historic data
            cmd = "SELECT *" +
                "FROM [wafer_nav].[wn].[historic_blu_assignment_unload] " +
                $"WHERE blu_id = '{bluId}';";
            var historicData = GetData(cmd,tran);

            foreach (Dictionary<string,string> entry in assignmentData)
            {
                try
                {
                    var theBib = entry["bib_id"];

                    //add historic bib
                    DateTime insertedAt = addBibToHistoric(entry["bib_id"], tran);

                    //add historic row
                    cmd = "INSERT INTO [wn].[historic_blu_assignment_unload] " +
                        "(blu_id, bib_id, bib_id_inserted_at) VALUES " +
                        $"('{bluId}','{theBib}','{insertedAt}');";
                    new SqlCommand(cmd, connection, tran).ExecuteNonQuery();

                    //remove active row
                    cmd = "DELETE FROM [wn].[blu_assignment_unload] " +
                        $"WHERE [bib_id] = '{theBib}'";
                    if ((new SqlCommand(cmd, connection, tran)).ExecuteNonQuery() == 0)
                    {
                        throw new Exception("ERROR: NO ROWS WERE DELETED DURING REMOVAL OF ACTIVE ASSIGNMENT ROW");
                    }

                    //remove active bib
                    cmd = $"DELETE FROM [wn].[active_bib] WHERE [id] = {theBib};";
                    var sqlCommand = new SqlCommand(cmd, connection, tran);
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    if (transStarted)
                    {
                        tran.Rollback();
                    }
                    throw e;
                }
            }

            //commit all
            tran.Commit();

            SetBluToAvailable(bluId);
            if (connectionOpenedHere)
            {
                connection.Close();
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

        public static void SetBluToAvailable(string bluId)
        {
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

        public static void SetBluToAvailable(string bluId, SqlTransaction tran)
        {
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
                var updateCommand = new SqlCommand(query, connection, tran);
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

        public static void setForUnload(string sltId, JArray bibIds, string bluId)
        {
            SqlTransaction tran = null;
            bool transStarted = false;
            try
            {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                tran = connection.BeginTransaction();
                transStarted = true;
                var nowDateTime = DateTime.Now;

                //TODO: HIGH PRIORITY FIX-This is wrong, using the bibs that were put in, because some fail and are taken out
                //get bibs assigned to the slt
                List<Dictionary<string, string>> oldBibIds = GetData($"SELECT [bib_id] FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", tran);

                List<string> oldBibList = new List<string>();
                foreach (Dictionary<string, string> oldBibId in oldBibIds)
                {
                    oldBibList.Add(oldBibId["bib_id"]);
                }

                //create historic bibs
                batchAddHistoricBibs(oldBibList, nowDateTime, tran);

                //remove relation
                var cmd = new SqlCommand($"DELETE FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", connection, tran);
                cmd.ExecuteNonQuery();

                //add entries to historic
                foreach (string oldBibId in oldBibList)
                {

                    cmd = new SqlCommand($"INSERT INTO [wn].[historic_slt_assignment] (slt_id, bib_id, bib_id_inserted_at) Values ('{sltId}','{oldBibId}','{nowDateTime}');", connection, tran);
                    cmd.ExecuteNonQuery();

                }
                //make the slt available
                cmd = new SqlCommand($"UPDATE[wafer_nav].[wn].[SLT] SET available = 1 WHERE id = '{sltId}';", connection, tran);
                cmd.ExecuteNonQuery();

                //add assignment - this works without inserting bibs into active because they are still "live" at this point.
                foreach (string bibId in bibIds)
                {
                    cmd = new SqlCommand($"INSERT INTO [wn].[blu_assignment_unload] (blu_id, bib_id) Values ('{bluId}','{bibId}');", connection, tran);
                    cmd.ExecuteNonQuery();
                }

                //set the blu to unavailable
                cmd = new SqlCommand("UPDATE[wafer_nav].[wn].[BLU] " +
                    "SET available = 0 " +
                    $"WHERE id = '{bluId}';", connection, tran);
                cmd.ExecuteNonQuery();

                //commit all
                tran.Commit();
                if (connectionOpenedHere)
                {
                    connection.Close();
                }

            }
            catch (Exception e)
            {
                if (transStarted)
                {
                    tran.Rollback();
                }
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

        public static DateTime addBibToHistoric(string bibId, SqlTransaction tran) {
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
                var command = new SqlCommand(query, connection, tran);
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
                var query = "INSERT INTO[wn].[BLU] (id, available, site_name, site_description, site_location) VALUES " +
                            "('123456', 1,'BLU#1', 'Handler 1', 'East')," +
                            "('234567', 1,'BLU#2', 'Handler 2', 'West')," +
                            "('111111', 1,'BLU#3', 'Handler 3', 'South')," +
                            "('222222', 1,'BLU#4', 'Handler 4', 'West')," +
                            "('333333', 1,'BLU#5', 'Handler 5', 'North')," +
                            "('444444', 1,'BLU#6', 'Handler 6', 'West')," +
                            "('555555', 1,'BLU#7', 'Handler 7', 'East');";
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
                var query = "INSERT INTO[wn].[SLT] (id, available, site_name, site_description, site_location) VALUES " +
                            "('123456', 1,'BLU#1', 'Test Chamber 1', 'East')," +
                            "('234567', 1,'BLU#2', 'Test Chamber 2', 'West')," +
                            "('111111', 1,'BLU#3', 'Test Chamber 3', 'South')," +
                            "('222222', 1,'BLU#4', 'Test Chamber 4', 'West')," +
                            "('333333', 1,'BLU#5', 'Test Chamber 5', 'North')," +
                            "('444444', 1,'BLU#6', 'Test Chamber 6', 'West')," +
                            "('555555', 1,'BLU#7', 'Test Chamber 7', 'East');";
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

        public static bool AddBlu(string bluId, string name, string description, string location) {
            bool success = true;
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                var query = "INSERT INTO[wn].[BLU] (id, location, available) VALUES " +
                            $"('{bluId}', '{name},{description},{location}', 1);";
                var insertCommand = new SqlCommand(query, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                success = false;
            }
            if (connectionOpenedHere) {
                connection.Close();
            }
            return success;
        }

        public static bool AddSlt(string sltId, string name, string description, string location) {
            bool success = true;
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                var query = "INSERT INTO[wn].[SLT] (id, location, available) VALUES " +
                            $"('{sltId}', '{name},{description},{location}', 1);";
                var insertCommand = new SqlCommand(query, connection);
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                success = false;
            }
            if (connectionOpenedHere) {
                connection.Close();
            }
            return success;
        }
    }
}
