using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : BaseWindow {
        private readonly string type;
        private readonly string startId;

        public EditWindow(string type, string startId, string name, string description, string location, bool available) {
            configPage = Config.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += EditWindow_KeyDown;
            InitializeComponent();
            this.type = type;
            this.startId = startId;
            BarcodeTextBox.Text = startId;
            NameTextBox.Text = name;
            DescriptionTextBox.Text = description;
            LocationTextBox.Text = location;
            AvailableCheckBox.IsChecked = available;
        }

        private void EditWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                SaveButton.Focus();
                ButtonAutomationPeer peer = new ButtonAutomationPeer(SaveButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void SaveButton_Clicked(object sender, RoutedEventArgs e) {
            if (!AllTextBoxesHaveData()) {
                configPage.dgBLU.SelectedIndex = -1;
                configPage.dgSLT.SelectedIndex = -1;
                DialogResult = false;
                return;
            }
            var id = BarcodeTextBox.Text;
            var name = NameTextBox.Text;
            var description = DescriptionTextBox.Text;
            var location = LocationTextBox.Text;
            bool available = AvailableCheckBox.IsChecked != null && (bool)AvailableCheckBox.IsChecked;
            bool result = false;
            if (type == "BLU") {
                result = DatabaseHandler.UpdateBlu(startId, id, name, description, location, available);
                configPage.dgBLU.SelectedIndex = -1;
            }
            else if (type == "SLT") {
                result = DatabaseHandler.UpdateSlt(startId, id, name, description, location, available);
                configPage.dgSLT.SelectedIndex = -1;
            }
            DialogResult = result;
        }

        private void EditWindow_Closed(object sender, EventArgs e) {
            configPage.dgBLU.SelectedIndex = -1;
            configPage.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
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
