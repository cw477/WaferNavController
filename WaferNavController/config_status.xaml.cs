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
    }
}
