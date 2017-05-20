using System;
using System.Collections;
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
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : BaseWindow {

        public AddWindow() {
            this.configPage = Config.Get();
            this.KeyDown += Esc_KeyDown;
            this.KeyDown += AddWindow_KeyDown;
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
            var id = BarcodeTextBox.Text;
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
            return TextBoxHasData(BarcodeTextBox)
                && TextBoxHasData(NameTextBox)
                && TextBoxHasData(DescriptionTextBox)
                && TextBoxHasData(LocationTextBox);
        }

        private bool TextBoxHasData(TextBox textBox) {
            return !string.IsNullOrEmpty(textBox.Text) && textBox.Text != textBox.Tag.ToString();
        }

        private void AddWindow_Closed(object sender, EventArgs e) {
            configPage.dgBLU.SelectedIndex = -1;
            configPage.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
            if (DialogResult != null && (bool) DialogResult) {

                DataGrid dataGrid;
                if (bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked) {
                    dataGrid = configPage.dgBLU;
                } else {
                    dataGrid = configPage.dgSLT;
                }

                // Iterate through datagrid
                foreach (DataRowView row in dataGrid.ItemsSource) {
                    var barcodeId = row.Row[0].ToString();
                    if (BarcodeTextBox.Text == barcodeId) {
                        dataGrid.SelectedItem = row;
                        dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        return;
                    }
                }
            }
        }
    }
}
