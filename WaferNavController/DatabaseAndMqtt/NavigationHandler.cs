using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace WaferNavController
{
    public static class NavigationHandler {

        public static readonly int constraintCheckLevel;
        private static readonly int finishBluUnloadDelay = 7500;

        static NavigationHandler() {
            try {
                var jsonStr = File.Exists("config.json")
                    ? File.ReadAllText("config.json")
                    : System.Text.Encoding.UTF8.GetString(Properties.Resources.config);
                JObject jsonObject = JObject.Parse(jsonStr);
                constraintCheckLevel = (int) jsonObject["constraint_check_level"];
            }
            catch (Exception e) {
                MainWindow.Get().AppendLine("Malformed JSON file! " + e.Message, true);
                constraintCheckLevel = 1; // default to no constraints
            }
        }

        /// <summary>
        /// Process GET_NEW_BLU message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// lotId
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// bluId
        /// bluSiteName
        /// bluSiteDescription
        /// bluSiteLocation
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> GetNewBlu(Dictionary<string, object> messages) {
            // get available blu
            var bluId = DatabaseHandler.GetRandomAvailableBluId();
            if (constraintCheckLevel == 3) {
                //add new lot id + add assignment + set blu to unavailable
                DatabaseHandler.setForLoad((string) messages["lotId"], bluId);
            }
            else if (constraintCheckLevel == 2) {
                DatabaseHandler.setForLoadSimple(bluId);
            }

            // Get BLU info that we were successful
            var returnJson = DatabaseHandler.GetBlu(bluId);

            returnJson.Add("directive", "GET_NEW_BLU_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);
            return returnJson;
        }

        /// <summary>
        /// Process COMPLETE_NEW_BLU message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// bluId
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// confirm
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> CompleteNewBlu(Dictionary<string, object> messages) {
            var returnJson = new Dictionary<string, string>();
            returnJson.Add("directive", "COMPLETE_NEW_BLU_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);
            if (DatabaseHandler.confirmNewBlu((string) messages["bluId"])) {
                returnJson.Add("confirm", "true");
            } else {
                returnJson.Add("confirm", "false");
            }
            return returnJson;
        }

        /// <summary>
        /// process GET_NEW_SLT message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// bluId
        /// bibIds (JArray)
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// sltId
        /// sltInfo
        /// sltSiteName
        /// sltSiteDescription
        /// sltSiteLocation
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> GetNewSlt(Dictionary<string, object> messages) {
            // Get first available SLT id
            var sltId = DatabaseHandler.GetRandomAvailableSltId();

            if (constraintCheckLevel == 3) {
                //remove previous data+assignments, add new assignments+data
                DatabaseHandler.setForTest((string) messages["bluId"], (JArray) messages["bibIds"], sltId);
            }
            else if (constraintCheckLevel == 2) {
                DatabaseHandler.setForTestSimple((string)messages["bluId"], (JArray)messages["bibIds"], sltId);
            }

            // Get slt info since we were successful
            var returnJson = DatabaseHandler.GetSlt(sltId);

            returnJson.Add("directive", "GET_NEW_SLT_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);
            return returnJson;
        }

        /// <summary>
        /// process COMPLETE_NEW_SLT message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// sltId
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// confirm
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> CompleteNewSlt(Dictionary<string, object> messages) {
            var returnJson = new Dictionary<string, string>();
            returnJson.Add("directive", "COMPLETE_NEW_SLT_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);
            if (DatabaseHandler.confirmNewSlt((string) messages["sltId"])) {
                returnJson.Add("confirm", "true");
            } else {
                returnJson.Add("confirm", "false");
            }
            return returnJson;
        }

        /// <summary>
        /// Process GET_DONE_BLU message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// sltId
        /// bibIds (JArray)
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// bluId
        /// bluSiteName
        /// bluSiteDescription
        /// bluSiteLocation
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> GetDoneBlu(Dictionary<string, object> messages) {
            //get available blu
            var bluId = DatabaseHandler.GetRandomAvailableBluId();

            if (constraintCheckLevel == 3) {
                //remove previous data+assignments, add new assignments+data
                DatabaseHandler.setForUnload((string) messages["sltId"], (JArray) messages["bibIds"], bluId);
            }
            else if (constraintCheckLevel == 2) {
                DatabaseHandler.setForUnloadSimple((string)messages["sltId"], (JArray)messages["bibIds"], bluId);
            }

            //get blu info
            var returnJson = DatabaseHandler.GetBlu(bluId);
            returnJson.Add("directive", "GET_DONE_BLU_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);
            return returnJson;
        }

        /// <summary>
        /// Process COMPLETE_DONE_BLU message.
        /// </summary>
        /// <param name="messages">
        /// Incoming Json:
        /// bluId
        /// clientId
        /// </param>
        /// <returns>
        /// Outgoing Json:
        /// directive
        /// clientId
        /// confirm
        /// </returns>
        /// <exception cref="Exception">Exception thrown to "bubble up" to send via MQTT</exception>
        public static Dictionary<string, string> CompleteDoneBlu(Dictionary<string, object> messages) {
            var returnJson = new Dictionary<string, string>();
            returnJson.Add("directive", "COMPLETE_DONE_BLU_RETURN");
            returnJson.Add("clientId", (string) messages["clientId"]);

            if (DatabaseHandler.confirmDoneBlu((string) messages["bluId"])) {
                returnJson.Add("confirm", "true");
            } else {
                returnJson.Add("confirm", "false");
            }

            Task.Run(() => {
                System.Threading.Thread.Sleep(finishBluUnloadDelay);
                if (constraintCheckLevel == 3) {
                    DatabaseHandler.finishBluUnload((string) messages["bluId"]);
                }
                else if (constraintCheckLevel == 2) {
                    DatabaseHandler.finishBluUnloadSimple((string) messages["bluId"]);
                }
                Application.Current.Dispatcher.Invoke(() => {
                    MainWindow.Get().RefreshDataGrids();
                });
            });
            return returnJson;
        }
    }
}
