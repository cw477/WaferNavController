using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.IO;
using System.Windows;

namespace WaferNavController {
    class MqttConnectionHandler {
        private readonly string BROKER_URL = "iot.eclipse.org"; // Defaults to port 1883
        public string ClientId { get; } = Guid.NewGuid().ToString();
        public string PubTopic { get; } = "wafernav/location_data";
        public string SubTopic { get; } = "wafernav/location_requests";
        private readonly MqttClient mqttClient;
        private readonly MainWindow mainWindow;

        public MqttConnectionHandler(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
            mqttClient = new MqttClient(BROKER_URL);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            mqttClient.Connect(ClientId);
            mqttClient.Subscribe(new[] { SubTopic }, new[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            mainWindow.AppendLine("CLIENT ID: " + ClientId, true);
            mainWindow.AppendLine("Subscribed to " + SubTopic, true);
        }


        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            var incommingJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(e.Message, 0, e.Message.Length));

            // Print received message to window
            mainWindow.AppendLine(DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "  Message: "
                    + DatabaseHandler.jsonToStr(incommingJson) + "\n", true);

            var returnJson = incomingMessageProcessor(incommingJson);
            returnJson["computerName"] = Environment.MachineName;

            // Print outgoing message to window
            mainWindow.AppendLine(DateTime.Now + "  Message outgoing:" + DatabaseHandler.jsonToStr(returnJson) + "\n", true);

            // Publish return message
            mqttClient.Publish(PubTopic, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(returnJson)));

            // Refresh data grids
            Application.Current.Dispatcher.Invoke(() => mainWindow.RefreshDataGrids());
        }

        /// <summary>
        /// This method directs incoming mqtt messages to be further processed.
        /// </summary>
        /// <param name="directive">Enum-like string that directs what to do with messages.</param>
        /// <param name="messages">Contents of message.</param>
        private Dictionary<string, string> incomingMessageProcessor(Dictionary<String, object> messages) {
            Dictionary<string, string> returnMessage = null;
            try {
                switch ((string)messages["directive"]) {
                    case "GET_NEW_BLU":
                        returnMessage = NavigationHandler.GetNewBlu(messages);
                        break;
                    case "COMPLETE_NEW_BLU":
                        returnMessage = NavigationHandler.CompleteNewBlu(messages);
                        break;
                    case "GET_NEW_SLT":
                        returnMessage = NavigationHandler.GetNewSlt(messages);
                        break;
                    case "COMPLETE_NEW_SLT":
                        returnMessage = NavigationHandler.CompleteNewSlt(messages);
                        break;
                    case "GET_DONE_BLU":
                        returnMessage = NavigationHandler.GetDoneBlu(messages);
                        break;
                    case "COMPLETE_DONE_BLU":
                        returnMessage = NavigationHandler.CompleteDoneBlu(messages);
                        break;
                    default:
                        Console.Error.WriteLine("incomingMessageProcessor: Directive unrecognized.");
                        break;
                }
            }
            catch (Exception e) {
                returnMessage = new Dictionary<string, string> { ["error"] = e.Message };
                returnMessage.Add("directive", "ERROR");
                returnMessage.Add("clientId", (string)messages["clientId"]);
                using (StreamWriter writer = new StreamWriter(@"Error.txt", true))
                {
                    writer.WriteLine("Message :" + e.Message + "<br/>" + Environment.NewLine + "StackTrace :" + e.StackTrace +
                       "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                    writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                }
            }
            return returnMessage;
        }

        public void Disconnect() {
            mqttClient.Disconnect();
        }
    }
}
