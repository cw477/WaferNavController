using System;
using System.Collections.Generic;
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

namespace WaferNavController
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Page
    {
        public Config()
        {
            InitializeComponent();
        }

     

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
    }
}
