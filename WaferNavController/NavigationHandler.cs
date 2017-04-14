using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaferNavController
{
    class NavigationHandler
    {
        internal static string getNewBlu(Dictionary<string, object> messages)
        {
            //GET_NEW_BLU: Message will contain LOT ID, and return a message with a BLU Identifier and its information.

            //“lotId”
            //“bluId”
            //“bluInfo”
            //----------------------------------------------------------
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
            var bluInfo = DatabaseHandler.GetBlu(bluId);

            // Create JSON string to send back
            return JsonConvert.SerializeObject(bluInfo);
        }

        internal static string acceptNewBlu(Dictionary<string, object> messages)
        {
            //ACCEPT_NEW_BLU: Message will be minimal. Return message will confirm acceptance

            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------


            //TODO: implement and use below stuff as a part of it
            // Add wafer and blu to blu assigment load table
            //DatabaseHandler.AddBluAssignmentLoad(messages[0], bluId);

            // Mark BLU as unavailable
            //DatabaseHandler.SetBluToUnavailable(bluId);

            throw new NotImplementedException();
        }

        internal static string completeNewBlu(Dictionary<string, object> messages)
        {
            //COMPLETE_NEW_BLU: Message will be contain scanned blu id, and return confirm

            //“bluId”
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
            throw new NotImplementedException();
        }

        internal static string getNewSlt(Dictionary<string, object> messages)
        {
            //GET_NEW_SLT: Message will contain previous BLU ID plus all BIB ID’s, and return a message with a SLT identifier and its information.

            //“bluId”
            //“bibIds” THIS IS AN ARRAY/ LIST
            //“sltId”
            //----------------------------------------------------------
            throw new NotImplementedException();
        }

        internal static string acceptNewSlt(Dictionary<string, object> messages)
        {
            //ACCEPT_NEW_SLT: Message will contain BIB ID’s again for confirmation purposes. Return message will confirm acceptance(minimal message).
        
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
                    throw new NotImplementedException();
        }

        internal static string completeNewSlt(Dictionary<string, object> messages)
        {
            //COMPLETE_NEW_BLU: Message will contain SLT ID and return confirm

            //“sltId” 
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------


            throw new NotImplementedException();
        }

        internal static string getDoneBlu(Dictionary<string, object> messages)
        {
            //GET_DONE_BLU: Message will contain SLT ID, and return a message with a BLU identifier and its information.

            //“sltId”
            //“bluId”
            //----------------------------------------------------------
            throw new NotImplementedException();
        }

        internal static string acceptDoneBlu(Dictionary<string, object> messages)
        {
            //ACCEPT_DONE_BLU: Message will contain LOT ID again for confirmation purposes. Return message will confirm acceptance(minimal message).
        
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
            throw new NotImplementedException();
        }

        internal static string completeDoneBlu(Dictionary<string, object> messages)
        {
            //COMPLETE_DONE_BLU: Message will contain BLU ID, and return a confirm

            //“bluId”
            //“confirm” boolean, let it be a string though, “true / false” lowercase
            //----------------------------------------------------------
            throw new NotImplementedException();
        }
    }
}
