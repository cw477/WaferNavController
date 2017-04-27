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
            // Get first available BLU id
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null)
            {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            // Add wafertype to active_wafer_type
            DatabaseHandler.AddNewActiveWaferType((string)messages["lotId"]);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var returnJson = DatabaseHandler.GetBlu(bluId);
            returnJson.Add("directive", "GET_NEW_BLU_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);

            // Create JSON string to send back
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
            //COMPLETE_NEW_BLU: Message will be contain scanned blu id, and return confirm

            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------

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
            //GET_NEW_SLT: Message will contain previous BLU ID plus all BIB ID’s, and return a message with a SLT identifier and its information.

            //“bluId”
            //“bibIds” THIS IS AN ARRAY/ LIST
            //“sltId”
            //----------------------------------------------------------
            // remove wafer and blu from assignment load table, transfer data to historic tables

            //DatabaseHandler.removeBluAssignmentLoad((string)messages["bluId"]); //TODO uncomment this

            // Mark original BLU as available
            //DatabaseHandler.SetBluToAvailable((string)messages["bluId"]); // TODO uncomment this

            // Get first available SLT id
            var sltId = DatabaseHandler.GetFirstAvailableSltId();

            //HACK: Reset database and try again if no available SLTs
            if (sltId == null)
            {
                DatabaseHandler.ResetDatabase();
                sltId = DatabaseHandler.GetFirstAvailableSltId();
            }

            // Add bibs to active
            //DatabaseHandler.AddNewActiveBibs((JArray)messages["bibIds"]); // TODO uncomment this

            // Get slt info
            var returnJson = DatabaseHandler.GetSlt(sltId);
            returnJson.Add("directive", "GET_NEW_SLT_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);

            // Create JSON string to send back
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
            //COMPLETE_NEW_SLT: Message will contain SLT ID and return confirm

            //“sltId” 
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
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
            //GET_DONE_BLU: Message will contain SLT ID, and return a message with a BLU identifier and its information.

            //“sltId”
            //“bluId”
            //----------------------------------------------------------
            // remove bibs and slt from assignment table, transfer data to historic tables
            //DatabaseHandler.removeSltAssignments((string)messages["sltId"]); // TODO uncomment this

            // Mark original BLU as available
            //DatabaseHandler.SetBluToAvailable((string)messages["bluId"]); // TODO uncomment this

            //get available blu
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null)
            {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var returnJson = DatabaseHandler.GetBlu(bluId);
            returnJson.Add("directive", "GET_DONE_BLU_RETURN");
            returnJson.Add("clientId", (string)messages["clientId"]);

            // Create JSON string to send back
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
            //COMPLETE_DONE_BLU: Message will contain BLU ID, and return a confirm

            //“bluId”
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
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
            return returnJson;
        }
    }
}
