using System;
using System.Collections.Generic;
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
                scrollViewer.ScrollToVerticalOffset(double.MaxValue);
            });

            // Process mqtt message to get desired ID
            var resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(receivedJsonStr);
            var bibId = resultMap["id"];

            // START - TEMPORARY TO RESET DATABASE
            DatabaseHandler.SetAllBluToAvailable();
            DatabaseHandler.RemoveAllActiveBibs();
            // END - TEMPORARY TO RESET DATABASE

            // Get first available BLU id
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            // Add BIB to active_bib
            DatabaseHandler.AddNewActiveBib(bibId);

            // Mark BLU as unavailable
            DatabaseHandler.SetBluToUnavailable(bluId);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var bluInfo = DatabaseHandler.GetBlu(bluId);

            // Create JSON string to send back
            var json = JsonConvert.SerializeObject(bluInfo);

            // Publish BLU info
            mqttClient.Publish(PUB_TOPIC, Encoding.UTF8.GetBytes(json));
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
        }

        private void ConnectToDatabase() {
            AppendText("Connecting to database . . .", true);

            killMakeDotsThread = false;
            var makeDotsThread = new Thread(MakeDots);
            makeDotsThread.Start();

            try {
                DatabaseHandler.ConnectToDatabase();

                killMakeDotsThread = true;
                AppendLine(" success!", true);


                var data = DatabaseHandler.GetAllBlus();
                AppendDatabaseDataToTextBox("\nAll BLUs:", data);


                var availBluId = DatabaseHandler.GetFirstAvailableBluId();
                AppendLine("\nFirst available BLU ID: " + availBluId, true);


                DatabaseHandler.RemoveAllActiveBibs();
                DatabaseHandler.RemoveAllHistoricBibs();


                DatabaseHandler.AddNewActiveBib("555");
                data = DatabaseHandler.GetAllActiveBibs();
                AppendDatabaseDataToTextBox("\nActive BIBs: ", data);

                data = DatabaseHandler.GetAllBlus();
                AppendDatabaseDataToTextBox("\nALL BLUs: ", data);

//                DatabaseHandler.SetBluToUnavailable("123");
//                DatabaseHandler.SetBluToAvailable("123");

                AppendLine("\nActive BIBs (before):", true);
                data = DatabaseHandler.GetAllActiveBibs();
                AppendDatabaseDataToTextBox(data);

                AppendLine("\nHistoric BIBs (before):", true);
                data = DatabaseHandler.GetAllHistoricBibs();
                AppendDatabaseDataToTextBox(data);

                DatabaseHandler.MoveActiveBibToHistoricBib("555");

                AppendLine("\nActive BIBs (after):", true);
                data = DatabaseHandler.GetAllActiveBibs();
                AppendDatabaseDataToTextBox(data);

                AppendLine("\nHistoric BIBs (after):", true);
                data = DatabaseHandler.GetAllHistoricBibs();
                AppendDatabaseDataToTextBox(data);
            }

            catch (Exception exception) {
                killMakeDotsThread = true;
                AppendLine(" failed.", true);
                AppendLine("Exception message:\n " + exception, true);
            }
        }

        private void AppendDatabaseDataToTextBox(string header, List<Dictionary<string, string>> data) {
            AppendLine(header, true);
            AppendDatabaseDataToTextBox(data);
        }

        private void AppendDatabaseDataToTextBox(List<Dictionary<string, string>> data) {
            foreach (var row in data) {
                var outputStr = "";
                foreach (var col in row) {
                    outputStr += col.Key + ":" + col.Value + ", ";
                }
                outputStr = outputStr.Substring(0, outputStr.Length - 2);
                AppendLine(outputStr, true);
            }
        }

        private void MakeDots() {
            while (true) {
                Thread.Sleep(1000);
                if (killMakeDotsThread) {
                    break;
                }
                AppendText(" .", true);
            }
        }

        private void AppendText(string text, bool useDispatcher) {
            if (useDispatcher) {
                Dispatcher.Invoke(() => AppendText(text));
            } else {
                AppendText(text);
            }
        }

        private void AppendLine(string text, bool useDispatcher) {
            if (useDispatcher) {
                Dispatcher.Invoke(() => AppendLine(text));
            } else {
                AppendLine(text);
            }
        }

        private void AppendText(string text) {
            textBlock.Text += text;
            scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        private void AppendLine(string text) {
            textBlock.Text += text + "\n";
            scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            mqttClient.Disconnect();
            DatabaseHandler.CloseConnection();
            Application.Current.Shutdown();
        }
    }
}
