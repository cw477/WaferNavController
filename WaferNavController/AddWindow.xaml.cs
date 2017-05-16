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
using System.Windows.Shapes;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : Window {
        private Config configPage;

        public AddWindow(Config configPage) {
            this.configPage = configPage;
            InitializeComponent();
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

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

    }
}
