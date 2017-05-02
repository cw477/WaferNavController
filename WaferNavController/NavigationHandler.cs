using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// bluInfo
        /// </returns>
        public static Dictionary<string, string> getNewBlu(Dictionary<string, object> messages)
        {
            // get available blu
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null)
            {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            //add new lot id
            DatabaseHandler.AddNewActiveWaferType((string)messages["lotId"]);

            //add assignment
            DatabaseHandler.AddBluAssignmentLoad((string)messages["lotId"], bluId);

            //set blu to unavailable
            DatabaseHandler.SetBluToUnavailable(bluId);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var returnJson = DatabaseHandler.GetBlu(bluId);

            returnJson.Add("directive", "GET_NEW_BLU_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);
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
        public static Dictionary<string, string> completeNewBlu(Dictionary<string, object> messages)
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
        /// </returns>
        public static Dictionary<string, string> getNewSlt(Dictionary<string, object> messages)
        {
            //remove assignment
            DatabaseHandler.finishBluLoad((string)messages["bluId"]);
        
            // Get first available SLT id
            var sltId = DatabaseHandler.GetFirstAvailableSltId();

            //HACK: Reset database and try again if no available SLTs
            if (sltId == null)
            {
                DatabaseHandler.ResetDatabase();
                sltId = DatabaseHandler.GetFirstAvailableSltId();
            }

            // Add bibs to active
            DatabaseHandler.AddNewActiveBibs((JArray)messages["bibIds"]);

            // Add assignment
            DatabaseHandler.AddSltAssignmentLoad((JArray)messages["bibIds"], sltId);

            // Set slt to unavailable
            DatabaseHandler.SetSLTToUnavailable(sltId);

            // Get slt info
            var returnJson = DatabaseHandler.GetSlt(sltId);

            returnJson.Add("directive", "GET_NEW_SLT_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);
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
        public static Dictionary<string, string> completeNewSlt(Dictionary<string, object> messages)
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
        /// bluInfo
        /// </returns>
        public static Dictionary<string, string> getDoneBlu(Dictionary<string, object> messages)
        {
            //remove assignment
            DatabaseHandler.finishSlt((string)messages["sltId"]);

            //get available blu
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null)
            {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            //add assignment
            DatabaseHandler.AddBluAssignmentUnload((JArray)messages["bibIds"], bluId);

            //set blu to unavailable
            DatabaseHandler.SetBluToUnavailable(bluId);
            
            //get blu info
            var returnJson = DatabaseHandler.GetBlu(bluId);
            returnJson.Add("directive", "GET_DONE_BLU_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);
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
        public static Dictionary<string, string> completeDoneBlu(Dictionary<string, object> messages)
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
            DatabaseHandler.finishBluUnload((string)messages["bluId"]);
            return returnJson;
        }
    }
}
