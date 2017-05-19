using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace WaferNavController
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Page {
        private MainWindow mainWindow;

        public Config(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
            InitializeComponent();
        }

     
        //Open FIle Dialog, Select File Button 
        private void button_selectFile_Click(object sender, RoutedEventArgs e)
        {
            //Create OpenFileDialog
            OpenFileDialog ofd = new OpenFileDialog();

            //Set default file extension
            ofd.DefaultExt = ".csv";

           if(ofd.ShowDialog()==true)
            {
                string filename = ofd.FileName;
                FileNameTxtBx.Text = filename;
                
            }
            
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
        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_2(object sender, TextChangedEventArgs e)
        {

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

        private void LogOutButton_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void CancelLogOutButton_Click(object sender, RoutedEventArgs e) {
            TabControl.SelectedIndex = 0;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) {
            FindWindow findWindow = new FindWindow(this);
            findWindow.Owner = mainWindow;
            findWindow.ShowDialog();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            AddWindow addWindow = new AddWindow(this);
            addWindow.Owner = mainWindow;
            addWindow.ShowDialog();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            mainWindow.AppendLine("SaveButton_Click called.", true);
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            mainWindow.AppendLine("DataGrid_CellEditEnding called.", true);
        }

        private int DataGrid_SelectionChanged_count = 0;
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            DataGrid_SelectionChanged_count++;
            mainWindow.AppendLine(DataGrid_SelectionChanged_count + " DataGrid_SelectionChanged called.", true);
        }

        private int DataGrid_SelectedCellsChanged_count = 0;
        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            DataGrid_SelectedCellsChanged_count++;
            mainWindow.AppendLine(DataGrid_SelectedCellsChanged_count + " DataGrid_SelectedCellsChanged called.", true);
        }

        private int DataGrid_MouseUp_count = 0;
        private void DataGrid_MouseUp(object sender, MouseButtonEventArgs e) {
            DataGrid_MouseUp_count++;
            mainWindow.AppendLine(DataGrid_MouseUp_count + " DataGrid_MouseUp called.", true);
            DataGrid dataGrid = (DataGrid) sender;
            int row = dataGrid.SelectedIndex;
            if (row == -1) { return; }  // clicked in DataGrid, but not in a cell
            int col = dataGrid.CurrentCell.Column.DisplayIndex;

            if (col == 5 && dataGrid.SelectedItems.Count == 1) {  // only fire if clicked on edit column, and only 1 row selected
                string type = dataGrid.Tag.ToString();
                string id = (string) ((DataRowView) dataGrid.SelectedItem).Row[0];
                string name = (string)((DataRowView)dataGrid.SelectedItem).Row[1];
                string description = (string)((DataRowView)dataGrid.SelectedItem).Row[2];
                string location = (string)((DataRowView)dataGrid.SelectedItem).Row[3];
                bool available = (bool) ((DataRowView)dataGrid.SelectedItem).Row[4];
                EditWindow editWindow = new EditWindow(this, type, id, name, description, location, available);
                editWindow.Owner = mainWindow;
                editWindow.ShowDialog();
            }
            else if (col == 6 && dataGrid.SelectedItems.Count == 1) {  // only fire if clicked on delete column, and only 1 row selected
                string type = dataGrid.Tag.ToString();
                string id = (string) ((DataRowView) dataGrid.SelectedItem).Row[0];
                DeleteWindow deleteWindow = new DeleteWindow(this, type, id);
                deleteWindow.Owner = mainWindow;
                deleteWindow.ShowDialog();
            }
        }

        private int DataGrid_BeginningEdit_count = 0;
        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e) {
            DataGrid_BeginningEdit_count++;
            mainWindow.AppendLine(DataGrid_BeginningEdit_count + " DataGrid_BeginningEdit called.", true);
        }
    }
}
