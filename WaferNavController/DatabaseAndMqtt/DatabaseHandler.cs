using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WaferNavController.DatabaseAndMqtt;

namespace WaferNavController
{
    public class DatabaseHandler {

        private static SqlConnection connection;
        private static Random random = new Random();

        public static void TestConnectToDatabase()
        {
            try
            {
                string connectionString = generateConnectionString();
                connection = new SqlConnection(connectionString);

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

        private static string generateConnectionString() {
            string connectionString = null;
            try {
                var jsonStr = File.Exists("config.json")
                    ? File.ReadAllText("config.json")
                    : Properties.Resources.config;
                JObject jsonObject = JObject.Parse(jsonStr);
                JObject innerJsonObject = JObject.Parse(jsonObject["database"].ToString());

                connectionString = "";
                foreach (var kp in innerJsonObject) {
                    connectionString += $"{kp.Key}={kp.Value};";
                }
            }
            catch (Exception e) {
                MainWindow.Get().AppendLine("Malformed JSON file! " + e.Message, true);
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

        public static void ResetDatabaseWithConfigFileData(string filePath) {
            // 1. Get all file lines
            // 2. Go through line by line, getting the following data
            //     a. BLU definition
            //     b. BLU entries
            //     c. SLT definition
            //     d. SLT entries
            // 3. Build queries
            // 4. Execute queries

            List<string> clearQueries = new List<string>();
            clearQueries.Add("DELETE FROM [wn].[blu_assignment_load];");
            clearQueries.Add("DELETE FROM [wn].[blu_assignment_unload];");
            clearQueries.Add("DELETE FROM [wn].[historic_blu_assignment_load];");
            clearQueries.Add("DELETE FROM [wn].[historic_blu_assignment_unload];");
            clearQueries.Add("DELETE FROM [wn].[slt_assignment];");
            clearQueries.Add("DELETE FROM [wn].[historic_slt_assignment];");
            clearQueries.Add("DELETE FROM [wn].[BLU];");
            clearQueries.Add("DELETE FROM [wn].[SLT];");
            clearQueries.Add("DELETE FROM [wn].[active_bib];");
            clearQueries.Add("DELETE FROM [wn].[historic_bib];");
            clearQueries.Add("DELETE FROM [wn].[active_wafer_type];");
            clearQueries.Add("DELETE FROM [wn].[historic_wafer_type];");

            SqlTransaction tran = null;
            bool transStarted = false;
            string[] queries = BuildQueriesFromJsonConfigFileData(filePath);

            try {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed) {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                tran = connection.BeginTransaction();
                transStarted = true;
                SqlCommand query = null;

                foreach (var clearQuery in clearQueries) {
                    query = new SqlCommand(clearQuery, connection, tran);
                    query.ExecuteNonQuery();
                }

                if (queries.Length > 0 && queries[0] != null) {
                    query = new SqlCommand(queries[0], connection, tran);
                    query.ExecuteNonQuery();
                }

                if (queries.Length > 1 && queries[1] != null) {
                    query = new SqlCommand(queries[1], connection, tran);
                    query.ExecuteNonQuery();
                }

                // commit all
                tran.Commit();
                if (connectionOpenedHere) {
                    connection.Close();
                }

            }
            catch (Exception e) {
                if (transStarted) {
                    tran.Rollback();
                }
                throw e;
            }
        }

        public static string[] BuildQueriesFromJsonConfigFileData(string filePath) {
            string dataReadFromFile = File.ReadAllText(filePath);
            Dictionary<string, object> jsonFromFile = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataReadFromFile);

            var query1 = BuildQueryFromJsonConfigFileHelper(jsonFromFile, "BLU");
            var query2 = BuildQueryFromJsonConfigFileHelper(jsonFromFile, "SLT");

            return new[] { query1, query2 };
        }

        private static string BuildQueryFromJsonConfigFileHelper(Dictionary<string, object> jsonFromFile, string type) {
            JArray innerDictArr = (JArray)jsonFromFile[type];
            List<string> keys = null;
            string query = "";

            foreach (JToken jToken in innerDictArr) {
                var dict = jToken.ToObject<Dictionary<string, object>>();

                if (keys == null) { // assuming all keys for all BLU/SLT entries are the same
                    keys = dict.Keys.ToList();
                    query += $"INSERT INTO [wn].[{type}] (";
                    foreach (var key in keys) {
                        query += key + ", ";
                    }
                    query = query.Substring(0, query.Length - 2) + ") VALUES ";
                }

                query += "(";
                foreach (var key in keys) {
                    if (key == "available") {
                        if ((string)dict[key] == "True") {
                            query += "1, ";
                        }
                        else {
                            query += "0, ";
                        }
                    }
                    else {
                        query += $"'{dict[key]}', ";
                    }
                }
                query = query.Substring(0, query.Length - 2) + "), ";
            }
            query = query.Substring(0, query.Length - 2) + ";";
            return query;
        }

        public static List<Dictionary<string, string>> GetAllBlus()
        {
            return GetData($"SELECT * FROM [wn].[BLU];");
        }

        public static List<Dictionary<string, string>> GetAllSlts()
        {
            return GetData($"SELECT * FROM [wn].[SLT];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllActiveWafers()
        {
            return GetData($"SELECT * FROM [wn].[active_wafer_type];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllHistoricWafers()
        {
            return GetData($"SELECT * FROM [wn].[historic_wafer_type];");
        }

        public static void FillDataGridWithItems(ref DataGrid dg, string tableName) {
            string cmdString = $"SELECT [id], [site_name], [site_description], [site_location], [available] FROM [wn].[{tableName}]";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable(tableName);
            sda.Fill(dt);
            dg.ItemsSource = dt.DefaultView;
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllBluLoadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[blu_assignment_load];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllHistoricBluLoadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[historic_blu_assignment_load];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllBluUnloadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[blu_assignment_unload];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllHistoricBluUnloadAssignments()
        {
            return GetData($"SELECT * FROM [wn].[historic_blu_assignment_unload];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
        public static List<Dictionary<string, string>> GetAllSltAssignments()
        {
            return GetData($"SELECT * FROM [wn].[slt_assignment];");
        }

        // Only called by AppendCurrentDatabaseData in MainWindow
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

        public static string jsonToStr(Dictionary<string, string> jsonMessage) {
            var json = JsonConvert.SerializeObject(jsonMessage);
            var jsonFormatted = JToken.Parse(json).ToString(Formatting.Indented);
            return jsonFormatted;
        }

        public static string jsonToStr(Dictionary<string, object> jsonMessage) {
            var json = JsonConvert.SerializeObject(jsonMessage);
            var jsonFormatted = JToken.Parse(json).ToString(Formatting.Indented);
            return jsonFormatted;
        }

        public static bool confirmNewBlu(string bluId) {
            //TODO: Add some checking logic?
            return true;
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

                // Verify BLU exists
                if (GetBlu(bluId, tran) == null) {
                    throw new Exception($"BLU {bluId} does not exist!");
                }

                //get waftertype associated
                Dictionary<string, string> wafertype;
                try {
                    wafertype = GetData($"SELECT [wafer_type_id] FROM [wn].[blu_assignment_load] WHERE [blu_id] = '{bluId}';", tran)[0];
                }
                catch (ArgumentOutOfRangeException) {
                    throw new Exception($"Could not find wafer_type_id for blu_id {bluId}!");
                }

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

        public static Dictionary<string, string> GetBlu(string bluId, SqlTransaction tran = null) {
            var data = GetData("SELECT [id] AS [bluId], " +
                           "[site_name] AS [bluSiteName], [site_description] as [bluSiteDescription], [site_location] as [bluSiteLocation] " +
                           $"FROM [wn].[BLU] WHERE id = '{bluId}';", tran);
            return data.Count == 0 ? null : data[0];
        }

        public static List<Dictionary<string, string>> GetAllActiveBibs() {
            return GetData($"SELECT * FROM [wn].[active_bib];");
        }

        public static List<string> GetAllActiveBibIds(SqlTransaction tran = null) {
            var data = GetData($"SELECT * FROM [wn].[active_bib];", tran);
            return data.Select(dict => dict["id"]).ToList();
        }

        public static List<Dictionary<string, string>> GetAllHistoricBibs() {
            return GetData($"SELECT * FROM [wn].[historic_bib];");
        }

        private static List<Dictionary<string, string>> GetData(string query, SqlTransaction tran = null) {
            var connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }

            var sqlCommand = tran == null
                ? new SqlCommand(query, connection)
                : new SqlCommand(query, connection, tran);

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
            if (connectionOpenedHere) {
                connection.Close();
            }
            return data;
        }

        public static Dictionary<string, string> GetSlt(object sltId, SqlTransaction tran = null) {
            var data = GetData("SELECT [id] AS [sltId], " +
                           "[site_name] AS [sltSiteName], [site_description] as [sltSiteDescription], [site_location] as [sltSiteLocation] " +
                           $"FROM [wn].[SLT] WHERE id = '{sltId}';", tran);
            return data.Count == 0 ? null : data[0];
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

        public static string GetFirstAvailableSltId() {
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            var query = "SELECT * FROM [wn].[SLT] WHERE available = 1;";
            var sqlCommand = new SqlCommand(query, connection);
            var reader = sqlCommand.ExecuteReader();
            if (!(reader.HasRows)) {
                reader.Close();
                throw new Exception("No available SLTs!");
            }
            string sltId = null;
            while (reader.Read()) {
                sltId = reader["id"].ToString();
                break;
            }
            reader.Close();
            if (connectionOpenedHere) {
                connection.Close();
            }
            return sltId;
        }

        public static string GetFirstAvailableBluId() {
            var db = new DataContext(generateConnectionString());
            var BLUs = db.GetTable<Tables.BLU>();
            var query =
                from b in BLUs
                where b.available
                select b;
            foreach (var blu in query) {
                return blu.id;
            }
            throw new Exception("No available BLUs!");
        }

        public static string GetRandomAvailableBluId() {
            var db = new DataContext(generateConnectionString());
            var BLUs = db.GetTable<Tables.BLU>();
            var query =
                from b in BLUs
                where b.available
                select b;
            var count = query.Count(); // 1st round-trip
            var index = random.Next(count);
            var blu = query.Skip(index).FirstOrDefault(); // 2nd round-trip
            if (blu == null) {
                throw new Exception("No available BLUs!");
            }
            return blu.id;
        }

        public static string GetRandomAvailableSltId() {
            var db = new DataContext(generateConnectionString());
            var SLTs = db.GetTable<Tables.SLT>();
            var query =
                from s in SLTs
                where s.available
                select s;
            var count = query.Count(); // 1st round-trip
            var index = random.Next(count);
            var slt = query.Skip(index).FirstOrDefault(); // 2nd round-trip
            if (slt == null) {
                throw new Exception("No available SLTs!");
            }
            return slt.id;
        }

        public static void finishBluUnload(string bluId, Boolean demoMode)
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
            if (!demoMode)
            {
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
                var assignmentData = GetData(cmd, tran);

                //historic data
                cmd = "SELECT *" +
                    "FROM [wafer_nav].[wn].[historic_blu_assignment_unload] " +
                    $"WHERE blu_id = '{bluId}';";
                var historicData = GetData(cmd, tran);

                foreach (Dictionary<string, string> entry in assignmentData)
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
            }

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

        public static bool confirmNewSlt(string sltId) {
            //TODO: Add some checking logic?
            return true;
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

        public static void setForUnload(string sltId, JArray bibIds, string bluId) {
            SqlTransaction tran = null;
            bool transStarted = false;
            try {
                bool connectionOpenedHere = false;
                if (connection.State == ConnectionState.Closed) {
                    connection.Open();
                    connectionOpenedHere = true;
                }
                tran = connection.BeginTransaction();
                transStarted = true;
                var nowDateTime = DateTime.Now;


                // Verify SLT exists
                if (GetSlt(sltId, tran) == null) {
                    throw new Exception($"SLT {sltId} does not exist!");
                }

                //get bibs assigned to the slt
                var oldBibs = GetData($"SELECT [bib_id] FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", tran);


                // Verify there are BIBs associated with the SLT
                if (oldBibs.Count == 0) {
                    throw new Exception($"No BIBs associated with SLT {sltId}!");
                }


                // Verify that BIBs received are all currently active (otherwise will get a foreign key constraint error later when trying to insert into blu_assignment_unload)
                var inactiveBibs = new List<string>();
                var activeBibs = GetAllActiveBibIds(tran);
                foreach (var bibId in bibIds) {
                    if (!activeBibs.Contains(bibId.ToString())) {
                        inactiveBibs.Add(bibId.ToString());
                    }
                }
                if (inactiveBibs.Count > 0) {
                    throw new Exception($"BIB(s) {string.Join(", ", inactiveBibs.OrderBy(x => x).ToList())} not currently active!");
                }


                //TODO: HIGH PRIORITY FIX-This is wrong, using the bibs that were put in, because some fail and are taken out
                var oldBibList = new List<string>();
                foreach (Dictionary<string, string> oldBibId in oldBibs) {
                    oldBibList.Add(oldBibId["bib_id"]);
                }
                //create historic bibs
                batchAddHistoricBibs(oldBibList, nowDateTime, tran);

                //remove relation
                var cmd = new SqlCommand($"DELETE FROM [wn].[slt_assignment] WHERE [slt_id] = '{sltId}';", connection, tran);
                cmd.ExecuteNonQuery();

                //add entries to historic
                foreach (string oldBibId in oldBibList) {
                    cmd = new SqlCommand($"INSERT INTO [wn].[historic_slt_assignment] (slt_id, bib_id, bib_id_inserted_at) Values ('{sltId}','{oldBibId}','{nowDateTime}');", connection, tran);
                    cmd.ExecuteNonQuery();
                }

                //make the slt available
                cmd = new SqlCommand($"UPDATE[wafer_nav].[wn].[SLT] SET available = 1 WHERE id = '{sltId}';", connection, tran);
                cmd.ExecuteNonQuery();


                //add assignment - this works without inserting bibs into active because they are still "live" at this point.
                foreach (string bibId in bibIds) {
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
                if (connectionOpenedHere) {
                    connection.Close();
                }
            }
            catch (Exception e) {
                if (transStarted) {
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
                            "('079303', 1,'BLU #1', 'Handler 1', 'North')," +
                            "('087268', 1,'BLU #2', 'Handler 2', 'East')," +
                            "('105453', 1,'BLU #3', 'Handler 3', 'West')," +
                            "('111690', 1,'BLU #4', 'Handler 4', 'West')," +
                            "('123826', 1,'BLU #5', 'Handler 5', 'West')," +
                            "('233575', 1,'BLU #6', 'Handler 6', 'South')," +
                            "('341512', 1,'BLU #7', 'Handler 7', 'East');";
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
                            "('598514', 1,'SLT #1', 'Test Chamber 1', 'West')," +
                            "('614412', 1,'SLT #2', 'Test Chamber 2', 'East')," +
                            "('631746', 1,'SLT #3', 'Test Chamber 3', 'West')," +
                            "('762573', 1,'SLT #4', 'Test Chamber 4', 'East')," +
                            "('796568', 1,'SLT #5', 'Test Chamber 5', 'West')," +
                            "('862986', 1,'SLT #6', 'Test Chamber 6', 'North')," +
                            "('967224', 1,'SLT #7', 'Test Chamber 7', 'South');";
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

        public static bool confirmDoneBlu(string bluId) {
            //TODO: Add some checking logic?
            return true;
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

        public static bool AddBluOrSlt(string type, string id, string name, string description, string location) {
            bool success = true;
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                var query = $"INSERT INTO[wn].[{type.ToUpper()}] (id, site_name, site_description, site_location, available) VALUES " +
                            $"('{id}', '{name}', '{description}', '{location}', 1);";
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

        public static bool RemoveBluOrSlt(string type, string id) {
            bool success = true;
            bool connectionOpenedHere = false;
            SqlTransaction tran = null;
            bool tranStarted = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                tran = connection.BeginTransaction();
                tranStarted = true;

                var extra = type == "BLU" ? "_load" : "";
                var query = $"DELETE FROM [wn].[{type.ToLower()}_assignment{extra}] WHERE [{type.ToLower()}_id] = '{id}'";
                var deleteCommand = new SqlCommand(query, connection, tran);
                deleteCommand.ExecuteNonQuery();

                if (type == "BLU") {
                    query = $"DELETE FROM [wn].[{type.ToLower()}_assignment_unload] WHERE [{type.ToLower()}_id] = '{id}'";
                    deleteCommand = new SqlCommand(query, connection, tran);
                    deleteCommand.ExecuteNonQuery();
                }

                query = $"DELETE FROM [wn].[{type.ToUpper()}] WHERE [id] = '{id}';";
                deleteCommand = new SqlCommand(query, connection, tran);
                deleteCommand.ExecuteNonQuery();
                tran.Commit();
            }
            catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                success = false;
                if (tranStarted) {
                    tran.Rollback();
                }
            }
            if (connectionOpenedHere) {
                connection.Close();
            }
            return success;
        }

        public static bool UpdateBluOrSlt(string type, string startId, string newId, string name, string description, string location, bool available) {
            bool success = true;
            bool connectionOpenedHere = false;
            if (connection.State == ConnectionState.Closed) {
                connection.Open();
                connectionOpenedHere = true;
            }
            try {
                string query = $"UPDATE[wafer_nav].[wn].[{type.ToUpper()}] " +
                               $"SET id = '{newId}', site_name = '{name}', site_description = '{description}', site_location = '{location}', available = '{Convert.ToInt32(available)}' " +
                               $"WHERE id = '{startId}';";
                var updateCommand = new SqlCommand(query, connection);
                updateCommand.ExecuteNonQuery();
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
