using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for DeleteWindow.xaml
    /// </summary>
    public partial class DeleteWindow : BaseWindow {
        private readonly string type;
        private readonly string id;

        public DeleteWindow(string type, string id) {
            configPage = Config.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += DeleteWindow_KeyDown;
            InitializeComponent();
            this.type = type;
            this.id = id;
        }

        private void DeleteWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(OkButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            if (type == "BLU" && !string.IsNullOrEmpty(id)) {
                DatabaseHandler.RemoveBlu(id);
                DialogResult = true;
            }
            else if (type == "SLT" && !string.IsNullOrEmpty(id)) {
                DatabaseHandler.RemoveSlt(id);
                DialogResult = true;
            }
            else {
                DialogResult = false;
            }
        }

        private void DeleteWindow_Closed(object sender, EventArgs e) {
            configPage.dgBLU.SelectedIndex = -1;
            configPage.dgSLT.SelectedIndex = -1;
            MainWindow.Get().RefreshDataGrids();
        }
    }
}
