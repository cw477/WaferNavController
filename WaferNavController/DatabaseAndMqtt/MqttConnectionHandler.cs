extern alias M2Mqtt;
extern alias GnatMQ;

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using M2Mqtt::uPLibrary.Networking.M2Mqtt;
using M2Mqtt::uPLibrary.Networking.M2Mqtt.Messages;
using System.IO;
using System.Net;
using System.Windows;
using Newtonsoft.Json.Linq;
using RestSharp;
using MqttBroker = uPLibrary.Networking.M2Mqtt.MqttBroker;

namespace WaferNavController {
    class MqttConnectionHandler {
        private string ClientId { get; } = Guid.NewGuid().ToString();
        private string SubTopic { get; set; }
        private string PubTopic { get; set; }
        private string BrokerUrl { get; set; } // Defaults to port 1883
        private string BrokerRedirectIp { get; set; }
        private readonly MqttClient mqttClient;
        private readonly MainWindow mainWindow;
        private readonly MqttBroker mqttBroker;

        public MqttConnectionHandler(MainWindow mainWindow) {
            try {
                this.mainWindow = mainWindow;
                SetMqttConnectionInfoFromJsonFile();
                if (BrokerUrl == "localhost") {
                    mqttBroker = new MqttBroker();
                    mqttBroker.Start();
                    SendRestCallToSetBrokerIp();
                }
                mqttClient = new MqttClient(BrokerUrl);
                mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                mqttClient.Connect(ClientId);
                mqttClient.Subscribe(new[] {SubTopic}, new[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
                mainWindow.AppendLine("Connected to MQTT broker at " + BrokerUrl, true);123
                mainWindow.AppendLine("CLIENT ID: " + ClientId, true);
                mainWindow.AppendLine("Subscribed to " + SubTopic, true);
                mainWindow.AppendLine("Publishing to " + PubTopic, true);

                int constraintLvl = NavigationHandler.constraintCheckLevel;
                if (constraintLvl == 2) {
                    mainWindow.AppendLine($"Constraint check level: {constraintLvl} (medium constraints)", true);
                } else if (constraintLvl == 3) {
                    mainWindow.AppendLine($"Constraint check level: {constraintLvl} (medium constraints)", true);
                } else {
                    mainWindow.AppendLine($"Constraint check level: {constraintLvl} (no constraints)", true);
                }
            }
            catch (Exception e) {
                MainWindow.Get().AppendLine("Exception thrown in MqttConnectionHandler constructor: " + e.Message, true);
            }
        }

        private void SendRestCallToSetBrokerIp() {
            var publicIp = new WebClient().DownloadString("http://bot.whatismyipaddress.com");
            mainWindow.AppendLine($"Public IP address: {publicIp}", true);
            var brokerUrl = $"tcp://{publicIp}:1883";
            var client = new RestClient(BrokerRedirectIp);
            var request = new RestRequest("broker_url", Method.POST);
            request.AddParameter("url", brokerUrl);
            request.AddHeader("Content-Type", "application/json");
            var response = client.Execute(request);
            var content = response.Content;
            mainWindow.AppendLine($"REST call response content (raw): {content}", true);
        }

        private void SetMqttConnectionInfoFromJsonFile() {
            try {
                var jsonStr = File.Exists("config.json")
                    ? File.ReadAllText("config.json")
                    : System.Text.Encoding.UTF8.GetString(Properties.Resources.config);
                JObject jsonObject = JObject.Parse(jsonStr);
                JObject innerJsonObject = JObject.Parse(jsonObject["mqtt"].ToString());
                SubTopic = innerJsonObject["sub_topic"].ToString();
                PubTopic = innerJsonObject["pub_topic"].ToString();
                BrokerUrl = innerJsonObject["broker_url"].ToString();
                BrokerRedirectIp = innerJsonObject["broker_redirect_ip"].ToString();
            }
            catch (Exception e) {
                MainWindow.Get().AppendLine("Malformed JSON file! " + e.Message, true);
            }
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            var incomingJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(e.Message, 0, e.Message.Length));

            // Print received message to window
            mainWindow.AppendLine("\n" + DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "\nMessage: "
                    + DatabaseHandler.jsonToStr(incomingJson), true);

            var returnJson = incomingMessageProcessor(incomingJson);
            returnJson["computerName"] = Environment.MachineName;

            // Print outgoing message to window
            mainWindow.AppendLine("\n" + DateTime.Now + "  Message outgoing.  Topic: " + PubTopic + "\nMessage: "
                + DatabaseHandler.jsonToStr(returnJson), true);

            // Publish return message
            mqttClient.Publish(PubTopic, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(returnJson)));

            // Highlight changed row
            try {
                switch (returnJson["directive"]) {
                    case "GET_NEW_BLU_RETURN":
                    case "GET_DONE_BLU_RETURN":
                    case "COMPLETE_DONE_BLU_RETURN":
                        Application.Current.Dispatcher.Invoke(() => {
                            // Refresh data grids
                            mainWindow.RefreshDataGrids();
                            // Highlight changed BLU row
                            StatusLogConfig.Get().FindAndSelectRowIfExists(StatusLogConfig.Get().dgBLU, returnJson["bluId"]);
                        });
                        break;
                    case "GET_NEW_SLT_RETURN":
                        Application.Current.Dispatcher.Invoke(() => {
                            // Refresh data grids
                            mainWindow.RefreshDataGrids();
                            // Highlight changed SLT row
                            StatusLogConfig.Get().FindAndSelectRowIfExists(StatusLogConfig.Get().dgSLT, returnJson["sltId"]);
                        });
                        break;
                    case "COMPLETE_NEW_BLU_RETURN":
                    case "COMPLETE_NEW_SLT_RETURN":
                        // don't refresh data grids
                        break;
                    default:
                        Application.Current.Dispatcher.Invoke(() => {
                            // Refresh data grids
                            mainWindow.RefreshDataGrids();
                        });
                        break;
                }
            }
            catch (Exception) {
                Console.Error.WriteLine("client_MqttMsgPublishReceived: Directive unrecognized.");
            }
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
            mqttBroker.Stop();
        }
    }
}
