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
using System.Windows.Controls;
using System.Windows.Data;
using Image = System.Windows.Controls.Image;
using Size = System.Drawing.Size;

namespace WaferNavController {
    public partial class MainWindow : Window {
        private bool killMakeDotsThread = false;
        private readonly MqttConnectionHandler mqttConnectionHandler;
        private readonly Config configPage;
        private readonly BitmapImage editIconBitmapImage;
        private readonly BitmapImage deleteIconBitmapImage;
        public static MainWindow self;

        public MainWindow() {
            InitializeComponent();
            self = this;
            configPage = new Config(this);
            this.Content = configPage;
                
            var bmp = Properties.Resources.nielsen_ninjas_LogoTranspBack;
            var hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;

            Bitmap editIconBitmap = ResizeBitmap(Properties.Resources.edit_icon, new Size(25, 25));
            Bitmap deleteIconBitmap = ResizeBitmap(Properties.Resources.delete_icon, new Size(20, 20));
            editIconBitmapImage = BitmapToBitmapImage(editIconBitmap);
            deleteIconBitmapImage = BitmapToBitmapImage(deleteIconBitmap);

            //TODO: move mqtt logic to app.xaml.cs hopefully possibly. The idea being this main window won't always be active.
            mqttConnectionHandler = new MqttConnectionHandler(this);
        }

        public static MainWindow GetMainWindow() {
            return self;
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            string currentTabName = ((TabItem)configPage.TabControl.SelectedItem).Name;
            if (e.Key == Key.A)
            {
                if (currentTabName == "LogTabItem") {
                    AppendCurrentDatabaseData();
                }
            }
            if (e.Key == Key.S)
            {
                if (currentTabName == "LogTabItem" || currentTabName == "StatusTabItem") {
                    AppendLine(DateTime.Now + ": Resetting DB...", true);
                    DatabaseHandler.ResetDatabase();
                    AppendLine(DateTime.Now + ": Resetting DB Finished.", true);
                }
            }
            if (e.Key == Key.D)
            {
                if (currentTabName == "StatusTabItem") {
                    RefreshDataGrids();
                }
            }

            if (e.Key == Key.F12) {
                AppendLine("F12 pressed!", true);
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
            FillDataGrids(ref configPage.dgBLU, "BLU");
            FillDataGrids(ref configPage.dgSLT, "SLT");
        }

        private void FillDataGrids(ref DataGrid dataGrid, string tableName) {
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();

            DatabaseHandler.fillItems(ref dataGrid, tableName);
            AddIconColumnToDataGrid(ref dataGrid, editIconBitmapImage, "");
            AddIconColumnToDataGrid(ref dataGrid, deleteIconBitmapImage, "");

            dataGrid.Columns[0].Header = "ID";
            dataGrid.Columns[1].Header = "Name";
            dataGrid.Columns[2].Header = "Description";
            dataGrid.Columns[3].Header = "Location";
            dataGrid.Columns[4].Header = "Available";

            configPage.lastRefreshedLabel.Content = " Last refreshed: " + DateTime.Now;
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
            configPage.logBox.Text += text;
            configPage.scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        private void AppendLine(string text) {
            configPage.logBox.Text += text + "\n";
            configPage.scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            mqttConnectionHandler.Disconnect();
            Application.Current.Shutdown();
        }
    }
}
