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
    /// Interaction logic for LogoutWindow.xaml
    /// </summary>
    public partial class LogoutWindow : BaseWindow {

        public LogoutWindow() {
            this.configPage = Config.Get();
            this.KeyDown += Esc_KeyDown;
            this.KeyDown += LogoutWindow_KeyDown;
            InitializeComponent();
        }

        private void LogoutWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(LogoutButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void LogoutButton_Clicked(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void CancelButton_Clicked(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void LogoutWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            DialogResult = false;
        }
    }
}
