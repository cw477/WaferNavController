using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using Path = System.IO.Path;

namespace WaferNavController
{
    /// <summary>
    /// Interaction logic for StatusLogConfig.xaml
    /// </summary>
    public partial class StatusLogConfig : Page {
        private readonly MainWindow mainWindow;
        private static StatusLogConfig self;

        public StatusLogConfig() {
            self = this;
            mainWindow = MainWindow.Get();
            InitializeComponent();
        }

        public static StatusLogConfig Get() {
            return self;
        }

        //Save File Dialog Method Log Tab 
        private void saveFD(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.AddExtension = true;
            sfd.Filter = "Text Files (*.txt)|*.txt";
            sfd.FileName = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + "-WaferNavController-Log";

            if (sfd.ShowDialog() == true)
            {
                string filename = sfd.FileName;
                File.WriteAllText(filename, logBox.Text.Replace("\n", "\r\n"));
            }  

        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            textBox.Text = string.Empty;
            textBox.Foreground = Brushes.Black;
            textBox.GotFocus -= TextBox_GotFocus;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Trim().Equals(string.Empty)) {
                textBox.Text = (string)textBox.Tag;
                textBox.Foreground = Brushes.LightGray;
                textBox.GotFocus += TextBox_GotFocus;
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) {
            FindWindow findWindow = new FindWindow();
            findWindow.Owner = mainWindow;
            findWindow.ShowDialog();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            AddWindow addWindow = new AddWindow();
            addWindow.Owner = mainWindow;
            addWindow.ShowDialog();
        }

        private void DataGrid_MouseUp(object sender, MouseButtonEventArgs e) {
            if (!MainWindow.IsAdmin()) { return; } // only admin can edit or delete an entry
            DataGrid dataGrid = (DataGrid) sender;
            int row = dataGrid.SelectedIndex;
            if (row == -1) { return; }  // clicked in DataGrid, but not in a cell

            int col;
            try {
                col = dataGrid.CurrentCell.Column.DisplayIndex;
            }
            catch (NullReferenceException ex) {
                mainWindow.AppendLine("NullReferenceException: " + ex.Message, true);
                return;
            }

            if (col == 5 && dataGrid.SelectedItems.Count == 1) {  // only fire if clicked on edit column, and only 1 row selected
                string type = dataGrid.Tag.ToString();
                string id = (string) ((DataRowView) dataGrid.SelectedItem).Row[0];
                string name = (string)((DataRowView)dataGrid.SelectedItem).Row[1];
                string description = (string)((DataRowView)dataGrid.SelectedItem).Row[2];
                string location = (string)((DataRowView)dataGrid.SelectedItem).Row[3];
                bool available = (bool) ((DataRowView)dataGrid.SelectedItem).Row[4];
                EditWindow editWindow = new EditWindow(type, id, name, description, location, available);
                editWindow.Owner = mainWindow;
                editWindow.ShowDialog();
            }
            else if (col == 6 && dataGrid.SelectedItems.Count == 1) {  // only fire if clicked on delete column, and only 1 row selected
                string type = dataGrid.Tag.ToString();
                string id = (string) ((DataRowView) dataGrid.SelectedItem).Row[0];
                DeleteWindow deleteWindow = new DeleteWindow(type, id);
                deleteWindow.Owner = mainWindow;
                deleteWindow.ShowDialog();
            }
        }

        //Open File Dialog, Select File Button
        private void SelectFileButton_Click(object sender, RoutedEventArgs e) {
            //Create OpenFileDialog
            OpenFileDialog ofd = new OpenFileDialog();

            //Set default file extension
            ofd.DefaultExt = ".csv";

            if (ofd.ShowDialog() == true) {
                string filename = ofd.FileName;
                FileNameTextBox.Text = filename;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e) {
            try {
                string filepath = FileNameTextBox.Text;
                if (filepath == "Properties.Resources.testdata") {
                    string runningPath = AppDomain.CurrentDomain.BaseDirectory;
                    filepath = string.Format("{0}Resources\\testdata.txt", Path.GetFullPath(Path.Combine(runningPath, @"..\..\")));
                }
                if (!string.IsNullOrEmpty(filepath)) {
                    DatabaseHandler.ResetDatabaseWithConfigFileData(filepath);
                }
                mainWindow.AppendLine("Successfully reset database with data from imported config file!", true);
                TabControl.SelectedIndex = 0;
                MainWindow.Get().RefreshDataGrids();
            }
            catch (Exception) {
                mainWindow.AppendLine("Failed to load reset database with config file data!", true);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) {
            mainWindow.RefreshDataGrids();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) {
            LogoutWindow logoutWindow = new LogoutWindow();
            logoutWindow.Owner = mainWindow;
            logoutWindow.ShowDialog();
        }
    }
}
