using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace WaferNavController {
    public partial class MainWindow : Window {
        //Adding a comment to test a first commit - Cameron Watt
        private readonly string BROKER_URL = "iot.eclipse.org"; // Defaults to port 1883
        private readonly string CLIENT_ID = Guid.NewGuid().ToString();
        private readonly Dictionary<string, string> mockDatabase;
        private readonly string PUB_TOPIC = "wafernav/location_data";
        private readonly string SUB_TOPIC = "wafernav/location_requests";
        private readonly MqttClient mqttClient;
        private bool killMakeDotsThread = false;

        public MainWindow() {
            InitializeComponent();
            var bmp = Properties.Resources.nielsen_ninjas_LogoTranspBack;
            var hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;

            mockDatabase = new Dictionary<string, string>();
            mockDatabase.Add("123", "abc");
            mockDatabase.Add("456", "xyz");
            mockDatabase.Add("12345", "somewhere");

            AppendLine("ClIENT ID: " + CLIENT_ID);

            mqttClient = new MqttClient(BROKER_URL);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            mqttClient.Connect(CLIENT_ID);
            mqttClient.Subscribe(new[] {SUB_TOPIC}, new[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
            AppendLine("Subscribed to " + SUB_TOPIC);
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            // Print received message to window
            var receivedJsonStr = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            Dispatcher.Invoke(() => {
                textBlock.Text += DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "  Message: '" + receivedJsonStr + "'" + "\n";
                scrollViewer.ScrollToVerticalOffset(Double.MaxValue);
            });

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

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
        }

        private void ConnectToDatabase() {
            Dispatcher.Invoke(() => { AppendText("Connecting to database"); });

            killMakeDotsThread = false;
            var makeDotsThread = new Thread(MakeDots);
            makeDotsThread.Start();

            SqlConnection myConnection = new SqlConnection("user id=appuser;" +
                            "password=appuser;" +
                            "server=localhost;" +
                            "database=wafer_nav;" +
                            "connection timeout=10");
            try {
                myConnection.Open();
                killMakeDotsThread = true;
                Dispatcher.Invoke(() => { AppendLine(" success!"); });
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from wn.active_bib", myConnection);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read()) {
                    Dispatcher.Invoke(() => { AppendLine(myReader["id"].ToString()); });
                }
            }
            catch (Exception exception) {
                killMakeDotsThread = true;
                Dispatcher.Invoke(() => AppendLine(" failed."));
                Dispatcher.Invoke(() => AppendLine("Exception message:\n " + exception));
            }
        }

        private void MakeDots() {
            while (true) {
                Thread.Sleep(1000);
                if (killMakeDotsThread) {
                    break;
                }
                Dispatcher.Invoke(() => AppendText(" ."));
            }
        }

        private void AppendText(String text) {
            textBlock.Text += text;
        }

        private void AppendLine(String text) {
            textBlock.Text += text + "\n";
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            mqttClient.Disconnect();
            Application.Current.Shutdown();
        }
    }
}
