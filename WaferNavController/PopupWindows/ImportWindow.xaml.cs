using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow : BaseWindow {
        private string filenameToImport;

        public ImportWindow(string filenameToImport) {
            this.filenameToImport = filenameToImport;
            statusLogConfig = StatusLogConfig.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += ImportWindow_KeyDown;
            InitializeComponent();
        }

        private void ImportWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var peer = new ButtonAutomationPeer(OkButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            if (!MainWindow.IsAdmin()) { // only admin can import from file
                DialogResult = false;
                return;
            }
            var result = statusLogConfig.ImportFile(filenameToImport);
            if (result) {
                MainWindow.Get().AppendLine("Successfully reset database with data from imported config file!", true);
                statusLogConfig.SetStatusLabelTextWithClearTimer("Successfully imported database from file.");
            } else {
                MainWindow.Get().AppendLine("Failed to reset database with config file data!", true);
                statusLogConfig.SetStatusLabelTextWithClearTimer("Failed to import database from file!");
            }
            DialogResult = result;
        }

        private void ImportWindow_Closed(object sender, EventArgs e) {
            statusLogConfig.dgBLU.SelectedIndex = -1;
            statusLogConfig.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
        }
    }
}
