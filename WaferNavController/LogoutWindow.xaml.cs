using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for LogoutWindow.xaml
    /// </summary>
    public partial class LogoutWindow : BaseWindow {

        public LogoutWindow() {
            configPage = Config.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += LogoutWindow_KeyDown;
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
