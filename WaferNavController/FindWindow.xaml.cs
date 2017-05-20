using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for FindWindow.xaml
    /// </summary>
    public partial class FindWindow : BaseWindow {

        public FindWindow() {
            this.configPage = Config.Get();
            this.KeyDown += Esc_KeyDown;
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

        private void FindButton_Click(object sender, RoutedEventArgs e) {
            // Just return if either data grid is null
            if (configPage.dgBLU.ItemsSource == null || configPage.dgSLT.ItemsSource == null) {
                DialogResult = false;
                return;
            }
            var enteredBarcodeId = BarcodeTextBox.Text;
            var dataGrids = new List<DataGrid> {configPage.dgBLU, configPage.dgSLT };
            // Iterate through both datagrids
            foreach (var dataGrid in dataGrids) {
                foreach (DataRowView row in dataGrid.ItemsSource) {
                    var barcodeId = row.Row[0].ToString();
                    if (enteredBarcodeId == barcodeId) {
                        dataGrid.SelectedItem = row;
                        dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                        DialogResult = true;
                        return;
                    }
                }
            }
        }
    }
}
