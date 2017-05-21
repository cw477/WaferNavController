using System;
using System.Data;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : BaseWindow {

        public AddWindow() {
            statusLogConfig = StatusLogConfig.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += AddWindow_KeyDown;
            InitializeComponent();
        }

        private void AddWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(AddButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            if (!AllTextBoxesHaveData()) {
                DialogResult = false;
                return;
            }
            var id = IdTextBox.Text;
            var name = NameTextBox.Text;
            var description = DescriptionTextBox.Text;
            var location = LocationTextBox.Text;

            bool result;
            if (bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked) {
                result = DatabaseHandler.AddBlu(id, name, description, location);
            } else {
                result = DatabaseHandler.AddSlt(id, name, description, location);
            }
            DialogResult = result;
        }

        private bool AllTextBoxesHaveData() {
            return TextBoxHasData(IdTextBox)
                && TextBoxHasData(NameTextBox)
                && TextBoxHasData(DescriptionTextBox)
                && TextBoxHasData(LocationTextBox);
        }

        private bool TextBoxHasData(TextBox textBox) {
            return !string.IsNullOrEmpty(textBox.Text) && textBox.Text != textBox.Tag.ToString();
        }

        private void AddWindow_Closed(object sender, EventArgs e) {
            statusLogConfig.dgBLU.SelectedIndex = -1;
            statusLogConfig.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
            if (DialogResult != null && (bool) DialogResult) {

                DataGrid dataGrid;
                if (bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked) {
                    dataGrid = statusLogConfig.dgBLU;
                } else {
                    dataGrid = statusLogConfig.dgSLT;
                }

                // Iterate through datagrid
                foreach (DataRowView row in dataGrid.ItemsSource) {
                    var id = row.Row[0].ToString();
                    if (IdTextBox.Text == id) {
                        dataGrid.SelectedItem = row;
                        dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        return;
                    }
                }
            }
        }
    }
}
