using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : BaseWindow {
        private readonly string type;
        private readonly string startId;

        public EditWindow(string type, string startId, string name, string description, string location, bool available) {
            statusLogConfig = StatusLogConfig.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += EditWindow_KeyDown;
            InitializeComponent();
            this.type = type;
            this.startId = startId;
            IdTextBox.Tag = startId;
            IdTextBox.Text = startId;
            NameTextBox.Tag = name;
            NameTextBox.Text = name;
            DescriptionTextBox.Tag = description;
            DescriptionTextBox.Text = description;
            LocationTextBox.Tag = location;
            LocationTextBox.Text = location;
            AvailableCheckBox.Tag = available;
            AvailableCheckBox.IsChecked = available;
        }

        private void EditWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                SaveButton.Focus();
                var peer = new ButtonAutomationPeer(SaveButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
        }

        private void SaveButton_Clicked(object sender, RoutedEventArgs e) {
            if (!AllTextBoxesHaveData() || !DataHasChanged()) {
                statusLogConfig.dgBLU.SelectedIndex = -1;
                statusLogConfig.dgSLT.SelectedIndex = -1;
                DialogResult = false;
                return;
            }
            var id = IdTextBox.Text;
            var name = NameTextBox.Text;
            var description = DescriptionTextBox.Text;
            var location = LocationTextBox.Text;
            bool available = AvailableCheckBox.IsChecked != null && (bool)AvailableCheckBox.IsChecked;
            bool result = false;
            if (type == "BLU") {
                result = DatabaseHandler.UpdateBlu(startId, id, name, description, location, available);
                statusLogConfig.dgBLU.SelectedIndex = -1;
            }
            else if (type == "SLT") {
                result = DatabaseHandler.UpdateSlt(startId, id, name, description, location, available);
                statusLogConfig.dgSLT.SelectedIndex = -1;
            }
            DialogResult = result;
        }

        private void EditWindow_Closed(object sender, EventArgs e) {
            statusLogConfig.dgBLU.SelectedIndex = -1;
            statusLogConfig.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
        }

        private bool AllTextBoxesHaveData() {
            return TextBoxHasData(IdTextBox)
                && TextBoxHasData(NameTextBox)
                && TextBoxHasData(DescriptionTextBox)
                && TextBoxHasData(LocationTextBox);
        }

        private bool TextBoxHasData(TextBox textBox) {
            return !string.IsNullOrEmpty(textBox.Text);
        }

        private bool DataHasChanged() {
            return TextBoxHasNewData(IdTextBox)
                || TextBoxHasNewData(NameTextBox)
                || TextBoxHasNewData(DescriptionTextBox)
                || TextBoxHasNewData(LocationTextBox)
                || (AvailableCheckBox.IsChecked != (bool) AvailableCheckBox.Tag);
        }

        private bool TextBoxHasNewData(TextBox textBox) {
            return textBox.Text != textBox.Tag.ToString();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null) {
                return;
            }
            if (TextBoxHasNewData(textBox)) {
                textBox.Foreground = Brushes.MediumVioletRed;
            } else {
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void Checkbox_Changed(object sender, RoutedEventArgs e) {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox == null || checkBox.Tag == null) {
                return;
            }
            if (AvailableCheckBox.IsChecked != (bool) AvailableCheckBox.Tag) {
                checkBox.Foreground = Brushes.MediumVioletRed;
            } else {
                checkBox.Foreground = Brushes.Gray;
            }
        }
    }
}
