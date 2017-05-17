using System;
using System.Collections.Generic;
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
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : Window {
        private Config configPage;

        public AddWindow(Config configPage) {
            this.configPage = configPage;
            this.KeyDown += AddWindow_KeyDown;
            InitializeComponent();
        }

        private void AddWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(AddButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if (e.Key == Key.Escape) {
                DialogResult = false;
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

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            if (!AllTextBoxesHaveData()) {
                DialogResult = false;
                return;
            }
            var barcode = BarcodeTextBox.Text;
            var name = NameTextBox.Text;
            var description = DescriptionTextBox.Text;
            var location = LocationTextBox.Text;

            bool result;
            if (bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked) {
                result = DatabaseHandler.AddBlu(barcode, name, description, location);
            } else {
                result = DatabaseHandler.AddSlt(barcode, name, description, location);
            }
            DialogResult = result;
        }

        private bool AllTextBoxesHaveData() {
            return TextBoxHasData(BarcodeTextBox)
                && TextBoxHasData(NameTextBox)
                && TextBoxHasData(DescriptionTextBox)
                && TextBoxHasData(LocationTextBox);
        }

        private bool TextBoxHasData(TextBox textBox) {
            return !string.IsNullOrEmpty(textBox.Text) && textBox.Text != textBox.Tag.ToString();
        }
    }
}
