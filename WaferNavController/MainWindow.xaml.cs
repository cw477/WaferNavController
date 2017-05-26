using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using Image = System.Windows.Controls.Image;
using Size = System.Drawing.Size;

namespace WaferNavController {
    public partial class MainWindow : Window {
        private bool killMakeDotsThread = false;
        private readonly MqttConnectionHandler mqttConnectionHandler;
        private readonly StatusLogConfig statusLogConfig;
        private readonly BitmapImage editIconBitmapImage;
        private readonly BitmapImage deleteIconBitmapImage;
        private static MainWindow self;
        private bool isAdmin;

        public MainWindow() {
            InitializeComponent();
            self = this;
            statusLogConfig = new StatusLogConfig();
            this.Content = statusLogConfig;
                
            Bitmap editIconBitmap = Properties.Resources.edit_icon_black_small;
            Bitmap deleteIconBitmap = Properties.Resources.delete_icon_black_small;
            editIconBitmapImage = BitmapToBitmapImage(editIconBitmap);
            deleteIconBitmapImage = BitmapToBitmapImage(deleteIconBitmap);

            //TODO: move mqtt logic to app.xaml.cs hopefully possibly. The idea being this main window won't always be active.
            mqttConnectionHandler = new MqttConnectionHandler(this);
        }

        public static MainWindow Get() {
            return self;
        }

        public void SetUsername(string username) {
            isAdmin = username.ToLower().StartsWith("admin");
            if (!isAdmin) {
                statusLogConfig.AddButton.Visibility = Visibility.Hidden;  // only admin can add new entries
                statusLogConfig.ResetDatabaseButton.Visibility = Visibility.Hidden;  // only admin can reset database
                Title += "   <NON-ADMIN>";
            } else {
                Title += "   <ADMIN>";
            }
        }

        public static bool IsAdmin() {
            return self.isAdmin;
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            var ctrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            var currentTabName = ((TabItem)statusLogConfig.TabControl.SelectedItem).Name;

            // Append database contents to log when press A on log tab
            if (e.Key == Key.A && currentTabName == "LogTabItem") {
                AppendCurrentDatabaseData();
            }

            // Click find button when Ctrl + F is pressed
            else if (e.Key == Key.F && ctrlPressed) {
                var peer = new ButtonAutomationPeer(statusLogConfig.FindButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            // Click refresh button when F5 is pressed
            else if (e.Key == Key.F5) {
                var peer = new ButtonAutomationPeer(statusLogConfig.RefreshButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            // Click add button when Ctrl + = pressed
            else if (e.Key == Key.OemPlus && ctrlPressed) {
                var peer = new ButtonAutomationPeer(statusLogConfig.AddButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            // Click reset database button when Ctrl + Delete pressed
            else if (e.Key == Key.Delete && ctrlPressed) {
                var peer = new ButtonAutomationPeer(statusLogConfig.ResetDatabaseButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            // Click logout button when Ctrl + 0 pressed
            else if (e.Key == Key.D0 && ctrlPressed) {
                var peer = new ButtonAutomationPeer(statusLogConfig.LogoutButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            // Change tab index if Ctrl + 1|2|3 pressed
            else if (e.Key == Key.D1 && ctrlPressed) {
                statusLogConfig.TabControl.SelectedIndex = 0;
            }
            else if (e.Key == Key.D2 && ctrlPressed) {
                statusLogConfig.TabControl.SelectedIndex = 1;
            }
            else if (e.Key == Key.D3 && ctrlPressed) {
                statusLogConfig.TabControl.SelectedIndex = 2;
            }

            else if (e.Key == Key.F12) {
                AppendLine("F12 pressed! - selectedIndex: " + statusLogConfig.TabControl.SelectedIndex, true);
            }
        }

        private void CreateFillDataGridsTask() {
            //Task.Run(() => {
            //    while (true) {
            //        Dispatcher.Invoke(RefreshDataGrids); // Need to use Dispatcher.Invoke() since fillDataGrids() accesses a UI element
            //        Thread.Sleep(3000);
            //    }
            //});
        }

        public void RefreshDataGrids() {
            try {
                FillDataGrids(ref statusLogConfig.dgBLU, "BLU");
                FillDataGrids(ref statusLogConfig.dgSLT, "SLT");
            }
            catch (Exception e) {
                AppendLine("Failed to refresh data grids: " + e.Message, true);
            }
        }

        private void FillDataGrids(ref DataGrid dataGrid, string tableName) {
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();

            DatabaseHandler.FillDataGridWithItems(ref dataGrid, tableName);
            if (isAdmin) { // only admin can edit or delete an entry
                AddIconColumnToDataGrid(ref dataGrid, editIconBitmapImage, "");
                AddIconColumnToDataGrid(ref dataGrid, deleteIconBitmapImage, "");
            }

            dataGrid.Columns[0].Header = "ID";
            dataGrid.Columns[1].Header = "Name";
            dataGrid.Columns[2].Header = "Description";
            dataGrid.Columns[3].Header = "Location";
            dataGrid.Columns[4].Header = "Available";

            dataGrid.Columns[0].MinWidth= 75;
            dataGrid.Columns[1].MinWidth = 100;
            dataGrid.Columns[2].MinWidth = 205;

            statusLogConfig.lastRefreshedLabel.Content = " Last refreshed: " + DateTime.Now;
        }

        public class DataObj {
            public BitmapImage Image { get; set; }
        }

        private void AddIconColumnToDataGrid(ref DataGrid dataGrid, BitmapImage bitmapImage, string headerName) {
            DataObj dataObj = new DataObj { Image = bitmapImage };
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Image));
            Binding binding = new Binding("Image") { Source = dataObj }; // "Image" must match class data member name

            factory.SetValue(Image.SourceProperty, binding);
            DataTemplate cellTemplate = new DataTemplate() { VisualTree = factory };
            DataGridTemplateColumn imgCol = new DataGridTemplateColumn() {
                Header = headerName,
                CellTemplate = cellTemplate
            };
            dataGrid.Columns.Add(imgCol);
        }

        private Bitmap ResizeBitmap(Bitmap imgToResize, Size size) {
            Bitmap newImage = new Bitmap(size.Width, size.Height);
            using (Graphics gr = Graphics.FromImage(newImage)) {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(imgToResize, new Rectangle(0, 0, size.Width, size.Height));
            }
            return newImage;
        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap) {
            var memStream = new MemoryStream();
            bitmap.Save(memStream, ImageFormat.Png);
            memStream.Position = 0;
            var bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.StreamSource = memStream;
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.EndInit();
            return bmpImg;
        }

        private void ConnectToDatabase() {
            AppendText("Testing Database Connection . . .", true);

            killMakeDotsThread = false;
            var makeDotsThread = new Thread(MakeDots);
            makeDotsThread.Start();

            try {
                DatabaseHandler.TestConnectToDatabase();

                killMakeDotsThread = true;
                AppendLine(" success!", true);
                Dispatcher.Invoke(RefreshDataGrids);
            }

            catch (Exception exception) {
                killMakeDotsThread = true;
                AppendLine(" failed.", true);
                AppendLine("Exception message:\n " + exception, true);
            }
        }

        private void AppendCurrentDatabaseData() {
            AppendLine("\n**********Current Database Information************" +
                "\n**********Current Time:" + DateTime.Now.ToString() + "********", true);
            var data = DatabaseHandler.GetAllBlus();
            AppendDatabaseDataToTextBox("\nAll BLUs:", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllSlts();
            AppendDatabaseDataToTextBox("\nAll SLTs:", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllActiveBibs();
            AppendDatabaseDataToTextBox("\nActive BIBs: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllHistoricBibs();
            AppendDatabaseDataToTextBox("\nHistoric BIBs: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllActiveWafers();
            AppendDatabaseDataToTextBox("\nActive Wafers: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllHistoricWafers();
            AppendDatabaseDataToTextBox("\nHistoric Wafers: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllBluLoadAssignments();
            AppendDatabaseDataToTextBox("\nBLU Loading Assignments: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllHistoricBluLoadAssignments();
            AppendDatabaseDataToTextBox("\nHistoric BLU Loading Assignments: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllBluUnloadAssignments();
            AppendDatabaseDataToTextBox("\nBLU Unloading Assignments: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllHistoricBluUnloadAssignments();
            AppendDatabaseDataToTextBox("\nHistoric BLU Unloading Assignments: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllSltAssignments();
            AppendDatabaseDataToTextBox("\nSlt Assignments: ", data);
            AppendLine("\n**************************************************");
            data = DatabaseHandler.GetAllHistoricSltAssignments();
            AppendDatabaseDataToTextBox("\nHistoric Slt Assignments: ", data);
            AppendLine("\n**************************************************");
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

        public void AppendLine(string text, bool useDispatcher) {
            if (useDispatcher) {
                Dispatcher.Invoke(() => AppendLine(text));
            } else {
                AppendLine(text);
            }
        }

        private void AppendText(string text) {
            statusLogConfig.logBox.Text += text;
            statusLogConfig.scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        private void AppendLine(string text) {
            statusLogConfig.logBox.Text += text + "\n";
            statusLogConfig.scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            mqttConnectionHandler.Disconnect();
            Application.Current.Shutdown();
        }
    }
}
