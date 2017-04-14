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
using System.Windows.Input;

namespace WaferNavController {
    public partial class MainWindow : Window {
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

            //TODO: move mqtt logic to app.xaml.cs hopefully possibly. The idea being this main window won't always be active.
            mqttClient = new MqttClient(BROKER_URL);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            mqttClient.Connect(CLIENT_ID);
            mqttClient.Subscribe(new[] {SUB_TOPIC}, new[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
            AppendLine("Subscribed to " + SUB_TOPIC);
        }

        /// <summary>
        /// This method directs incoming mqtt messages to be further processed.
        /// </summary>
        /// <param name="directive">Enum-like string that directs what to do with messages.</param>
        /// <param name="messages">Contents of message.</param>
        private Dictionary<string, string> incomingMessageProcessor(Dictionary<String, object> messages)
        {
            Dictionary<string, string> returnMessage = null;
            switch ((string)messages["directive"])
            {
                case "GET_NEW_BLU":
                    returnMessage = NavigationHandler.getNewBlu(messages);
                    break;
                case "ACCEPT_NEW_BLU":
                    returnMessage = NavigationHandler.acceptNewBlu(messages);
                    break;
                case "COMPLETE_NEW_BLU":
                    returnMessage = NavigationHandler.completeNewBlu(messages);
                    break;
                case "GET_NEW_SLT":
                    returnMessage = NavigationHandler.getNewSlt(messages);
                    break;
                case "ACCEPT_NEW_SLT":
                    returnMessage = NavigationHandler.acceptNewSlt(messages);
                    break;
                case "COMPLETE_NEW_SLT":
                    returnMessage = NavigationHandler.completeNewSlt(messages);
                    break;
                case "GET_DONE_BLU":
                    returnMessage = NavigationHandler.getDoneBlu(messages);
                    break;
                case "ACCEPT_DONE_BLU":
                    returnMessage = NavigationHandler.acceptDoneBlu(messages);
                    break;
                case "COMPLETE_DONE_BLU":
                    returnMessage = NavigationHandler.completeDoneBlu(messages);
                    break;
                default:
                    Console.Error.WriteLine("incomingMessageProcessor: Directive unrecognized.");
                    break;
            }

            return returnMessage;
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            var incommingJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(e.Message, 0, e.Message.Length));

            // Print received message to window
            Dispatcher.Invoke(() => {
                textBlock.Text += DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "  Message: '"
                    + DatabaseHandler.jsonToStr(incommingJson) + "'" + "\n";
                scrollViewer.ScrollToVerticalOffset(double.MaxValue);
            });

            var returnJson = incomingMessageProcessor(incommingJson);

            // Print outgoing message to window
            Dispatcher.Invoke(() => {
                textBlock.Text += DateTime.Now + "  Message outgoing:" + DatabaseHandler.jsonToStr(returnJson) + "'" + "\n";
                scrollViewer.ScrollToVerticalOffset(double.MaxValue);
            });

            // Publish return message
            mqttClient.Publish(PUB_TOPIC, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(returnJson)));
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                AppendCurrentDatabaseData();
            }
        }

        private void ConnectToDatabase() {
            AppendText("Connecting to database . . .", true);

            killMakeDotsThread = false;
            var makeDotsThread = new Thread(MakeDots);
            makeDotsThread.Start();

            try {
                DatabaseHandler.ConnectToDatabase();
                DatabaseHandler.ResetDatabase();

                killMakeDotsThread = true;
                AppendLine(" success!", true);
            }

            catch (Exception exception) {
                killMakeDotsThread = true;
                AppendLine(" failed.", true);
                AppendLine("Exception message:\n " + exception, true);
            }
        }

        private void AppendCurrentDatabaseData() {
            var data = DatabaseHandler.GetAllBlus();
            AppendDatabaseDataToTextBox("\nAll BLUs:", data);

            data = DatabaseHandler.GetAllActiveBibs();
            AppendDatabaseDataToTextBox("Active BIBs: ", data);

            data = DatabaseHandler.GetAllHistoricBibs();
            AppendDatabaseDataToTextBox("Historic BIBs: ", data);
        }

        private void AppendDatabaseDataToTextBox(string header, List<Dictionary<string, string>> data) {
            AppendLine(header, true);
            AppendDatabaseDataToTextBox(data);
        }

        private void AppendDatabaseDataToTextBox(List<Dictionary<string, string>> data) {
            if (data.Count == 0) {
                AppendLine("    (empty)", true);
                return;
            }
            foreach (var row in data) {
                var outputStr = "";
                foreach (var col in row) {
                    outputStr += "    " + col.Key + ":" + col.Value + ", ";
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
