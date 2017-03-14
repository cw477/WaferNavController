using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace WaferNavController {
    public partial class MainWindow : Window {
        private readonly string BROKER_URL = "iot.eclipse.org"; // Defaults to port 1883
        private readonly string CLIENT_ID = Guid.NewGuid().ToString();
        private readonly Dictionary<string, string> mockDatabase;
        private readonly string PUB_TOPIC = "wafernav/location_data";
        private readonly string SUB_TOPIC = "wafernav/location_requests";
        private readonly MqttClient mqttClient;

        public MainWindow() {
            //InitializeComponent();

            mockDatabase = new Dictionary<string, string>();
            mockDatabase.Add("123", "abc");
            mockDatabase.Add("456", "xyz");
            mockDatabase.Add("12345", "somewhere");

            Console.WriteLine(CLIENT_ID);
            mqttClient = new MqttClient(BROKER_URL);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            mqttClient.Connect(CLIENT_ID);
            mqttClient.Subscribe(new[] {SUB_TOPIC}, new[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
            Console.WriteLine("Subscribed to " + SUB_TOPIC);
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            // Print received message to console
            var receivedJsonStr = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            Console.WriteLine(DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "  Message: '" + receivedJsonStr + "'");

            // Process mqtt message to get desired ID
            var resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(receivedJsonStr);
            var idString = resultMap["id"];

            // Get location data to return from "database"
            var loc = mockDatabase.ContainsKey(idString) ? mockDatabase[idString] : "null";

            // Create JSON string to send back, e.g. {"id":123, "loc":"abc"}
            var returnMap = new Dictionary<string, string>();
            returnMap["id"] = idString;
            returnMap["loc"] = loc;
            var json = JsonConvert.SerializeObject(returnMap);

            // Publish location info
            mqttClient.Publish(PUB_TOPIC, Encoding.UTF8.GetBytes(json));
        }
    }
}
