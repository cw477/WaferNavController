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
        internal static string getNewBlu(List<string> messages)
        {
            // Message will contain LOT ID, and return a message with a 
            //BLU Identifier and its information.
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
            DatabaseHandler.AddNewActiveWaferType(messages[0]);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var bluInfo = DatabaseHandler.GetBlu(bluId);

            // Create JSON string to send back
            return JsonConvert.SerializeObject(bluInfo);
        }

        internal static string acceptNewBlu(List<string> messages)
        {
            // Message will contain LOT ID again for confirmation purposes. 
            //Return message will confirm acceptance (minimal message). 
            //TODO: implement and use below stuff as a part of it
            // Add wafer and blu to blu assigment load table
            //DatabaseHandler.AddBluAssignmentLoad(messages[0], bluId);

            // Mark BLU as unavailable
            //DatabaseHandler.SetBluToUnavailable(bluId);

            throw new NotImplementedException();
        }

        internal static string completeNewBlu(List<string> messages)
        {
            //Message will be minimal, and no return message will be necessary
            throw new NotImplementedException();
        }

        internal static string getNewSlt(List<string> messages)
        {
            //Message will contain previous BLU ID plus all BIB ID’s,
            //and return a message with a SLT identifier and its information.
            throw new NotImplementedException();
        }

        internal static string acceptNewSlt(List<string> messages)
        {
            //Message will contain BIB ID’s again for confirmation purposes.
            //Return message will confirm acceptance (minimal message).
            throw new NotImplementedException();
        }

        internal static string completeNewSlt(List<string> messages)
        {
            //Message will contain SLT ID and no return message will be necessary
            throw new NotImplementedException();
        }

        internal static string getDoneBlu(List<string> messages)
        {
            //Message will contain SLT + BIB IDS,
            //and return a message with a BLU identifier and its information.
            throw new NotImplementedException();
        }

        internal static string acceptDoneBlu(List<string> messages)
        {
            //Message will contain LOT ID again for confirmation purposes.
            //Return message will confirm acceptance (minimal message).
            throw new NotImplementedException();
        }

        internal static string completeDoneBlu(List<string> messages)
        {
            //Message will contain BLU ID, and no return message will be necessary.
            throw new NotImplementedException();
        }
    }
}
