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

        public FindWindow(Config configPage) {
            this.configPage = configPage;
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
            // Iterate through BLU datagrid
            foreach (DataRowView row in configPage.dgBLU.ItemsSource) {
                var barcodeId = row.Row[0].ToString();
                if (enteredBarcodeId == barcodeId) {
                    configPage.dgBLU.SelectedItem = row;
                    configPage.dgBLU.ScrollIntoView(configPage.dgBLU.SelectedItem);
                    DialogResult = true;
                    return;
                }
            }
            // Iterate through SLT datagrid
            foreach (DataRowView row in configPage.dgSLT.ItemsSource) {
                var barcodeId = row.Row[0].ToString();
                if (enteredBarcodeId == barcodeId) {
                    configPage.dgSLT.SelectedItem = row;
                    configPage.dgSLT.ScrollIntoView(configPage.dgSLT.SelectedItem);
                    DialogResult = true;
                    return;
                }
            }
        }
    }
}
