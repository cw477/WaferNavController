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
        private bool clickedLoginButton = false;

        public LoginWindow() {
            InitializeComponent();
            KeyDown += LoginWindow_KeyDown;
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                LoginButton.Focus();
                ButtonAutomationPeer peer = new ButtonAutomationPeer(LoginButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            } else if (e.Key == Key.Escape) {
                Application.Current.Shutdown();
            }
        }

        private void LoginButton_Clicked(object sender, RoutedEventArgs e) {
            clickedLoginButton = true;
            Close();
        }

        private void LoginWindow_Closing(object sender, CancelEventArgs e) {
            if (!clickedLoginButton) { // closing by clicking x or alt f4 or esc, so just exit app
                Application.Current.Shutdown();
            }
            if (!string.IsNullOrEmpty(UsernameTextBox.Text) && !string.IsNullOrEmpty(PasswordTextBox.Password) && clickedLoginButton) {
                e.Cancel = false;
            } else {
                e.Cancel = true;
            }
            clickedLoginButton = false;
        }

        private void LoginWindow_Closed(object sender, EventArgs e) {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
