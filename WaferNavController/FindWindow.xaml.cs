using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for FindWindow.xaml
    /// </summary>
    public partial class FindWindow : Window {
        private Config configPage;

        public FindWindow(Config configPage) {
            this.configPage = configPage;
            this.KeyDown += FindWindow_KeyDown;
            InitializeComponent();
        }

        private void FindWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(FindButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
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
            var enteredBarcodeId = BarcodeTextBox.Text;
            // Iterate through BLU datagrid
            foreach (DataRowView row in configPage.dgBLU.ItemsSource) {
                var barcodeId = row.Row[0].ToString();
                if (enteredBarcodeId == barcodeId) {
                    configPage.dgBLU.SelectedItem = row;
                    DialogResult = true;
                    return;
                }
            }
            // Iterate through SLT datagrid
            foreach (DataRowView row in configPage.dgSLT.ItemsSource) {
                var barcodeId = row.Row[0].ToString();
                if (enteredBarcodeId == barcodeId) {
                    configPage.dgSLT.SelectedItem = row;
                    DialogResult = true;
                    return;
                }
            }
        }
    }
}
