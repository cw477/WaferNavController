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
        static public string oldPlaceholderLogic(string message)
        {
            // Process mqtt message to get desired ID
            var resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);
            var bibId = resultMap["id"];

            // Get first available BLU id
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null)
            {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            // Add BIB to active_bib
            DatabaseHandler.AddNewActiveBib(bibId);

            // Mark BLU as unavailable
            DatabaseHandler.SetBluToUnavailable(bluId);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var bluInfo = DatabaseHandler.GetBlu(bluId);

            // Create JSON string to send back
            return JsonConvert.SerializeObject(bluInfo);
        }
    }
}
