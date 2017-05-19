using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaferNavController
{
    public static class NavigationHandler
    {
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
        public static Dictionary<string, string> getNewBlu(Dictionary<string, object> messages)
        {
            try
            {
                // get available blu
                var bluId = DatabaseHandler.GetFirstAvailableBluId();

                //add new lot id + add assignment + set blu to unavailable
                DatabaseHandler.setForLoad((string)messages["lotId"], bluId);

                // Get BLU info that we were successful
                var returnJson = DatabaseHandler.GetBlu(bluId);

                returnJson.Add("directive", "GET_NEW_BLU_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
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
        public static Dictionary<string, string> completeNewBlu(Dictionary<string, object> messages)
        {
            try
            {
                var returnJson = new Dictionary<string, string>();
                returnJson.Add("directive", "COMPLETE_NEW_BLU_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                if (DatabaseHandler.confirmNewBlu((string)messages["bluId"]))
                {
                    returnJson.Add("confirm", "true");
                }
                else
                {
                    returnJson.Add("confirm", "false");
                }
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
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
        public static Dictionary<string, string> getNewSlt(Dictionary<string, object> messages)
        {
            try
            {
                // Get first available SLT id
                var sltId = DatabaseHandler.GetFirstAvailableSltId();

                //remove previous data+assignments, add new assignments+data
                DatabaseHandler.setForTest((string)messages["bluId"], (JArray)messages["bibIds"], sltId);

                // Get slt info since we were successful
                var returnJson = DatabaseHandler.GetSlt(sltId);

                returnJson.Add("directive", "GET_NEW_SLT_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
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
        public static Dictionary<string, string> completeNewSlt(Dictionary<string, object> messages)
        {
            try
            {
                var returnJson = new Dictionary<string, string>();
                returnJson.Add("directive", "COMPLETE_NEW_SLT_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                if (DatabaseHandler.confirmNewSlt((string)messages["sltId"]))
                {
                    returnJson.Add("confirm", "true");
                }
                else
                {
                    returnJson.Add("confirm", "false");
                }
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
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
        public static Dictionary<string, string> getDoneBlu(Dictionary<string, object> messages)
        {
            try
            {
                //get available blu
                var bluId = DatabaseHandler.GetFirstAvailableBluId();

                //remove previous data+assignments, add new assignments+data
                DatabaseHandler.setForUnload((string)messages["sltId"], (JArray)messages["bibIds"], bluId);

                //get blu info
                var returnJson = DatabaseHandler.GetBlu(bluId);
                returnJson.Add("directive", "GET_DONE_BLU_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
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
        public static Dictionary<string, string> completeDoneBlu(Dictionary<string, object> messages)
        {
            try
            {
                var returnJson = new Dictionary<string, string>();
                returnJson.Add("directive", "COMPLETE_DONE_BLU_RETURN");
                returnJson.Add("clientId", (string)messages["clientId"]);
                if (DatabaseHandler.confirmDoneBlu((string)messages["bluId"]))
                {
                    returnJson.Add("confirm", "true");
                }
                else
                {
                    returnJson.Add("confirm", "false");
                }

                Task finish = Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(10000);
                    DatabaseHandler.finishBluUnload((string)messages["bluId"]);
                });
                return returnJson;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
    }
}
