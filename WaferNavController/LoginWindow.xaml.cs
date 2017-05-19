using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    ///     Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : BaseWindow {
        private readonly MainWindow mainWindow;
        private bool clickedLoginButton = false;

        public LoginWindow(MainWindow mainWindow, Config configPage) {
            this.mainWindow = mainWindow;
            this.configPage = configPage;
            KeyDown += Esc_KeyDown;
            KeyDown += LoginWindow_KeyDown;
            InitializeComponent();
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(LoginButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void LoginButton_Clicked(object sender, RoutedEventArgs e) {
            mainWindow.AppendLine("LoginButton_Clicked", true);
            clickedLoginButton = true;
            DialogResult = true;
        }

        private void LoginWindow_Closing(object sender, CancelEventArgs e) {
            mainWindow.AppendLine("LoginWindow_Closing", true);
            if (!string.IsNullOrEmpty(UsernameTextBox.Text) && !string.IsNullOrEmpty(PasswordTextBox.Text) && clickedLoginButton) {
                e.Cancel = false;
            } else {
                e.Cancel = true;
            }
            clickedLoginButton = false;
        }

        private void LoginWindow_Closed(object sender, EventArgs e) {
            mainWindow.AppendLine("LoginWindow_Closed", true);
        }
    }
}
