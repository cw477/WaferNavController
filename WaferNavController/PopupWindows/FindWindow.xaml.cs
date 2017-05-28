using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for FindWindow.xaml
    /// </summary>
    public partial class FindWindow : BaseWindow {

        public FindWindow() {
            statusLogConfig = StatusLogConfig.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += FindWindow_KeyDown;
            InitializeComponent();
        }

        private void FindWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var peer = new ButtonAutomationPeer(FindButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) {
            // Just return if either data grid is null
            if (statusLogConfig.dgBLU.ItemsSource == null || statusLogConfig.dgSLT.ItemsSource == null) {
                DialogResult = false;
                return;
            }

            IdTextBox.Text = IdTextBox.Text.Trim();
            var enteredId = IdTextBox.Text;
            if (string.IsNullOrEmpty(enteredId)) {
                ErrorLabel.Content = "Please enter an ID to search for";
                ErrorLabel.Visibility = Visibility.Visible;
                return;
            }

            var dataGrid = bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked ? statusLogConfig.dgBLU : statusLogConfig.dgSLT;
            var result = statusLogConfig.FindAndSelectRowIfExists(dataGrid, enteredId);
            if (!result) {
                ErrorLabel.Content = "ID not found; please try again";
                ErrorLabel.Visibility = Visibility.Visible;
                return;
            }

            if (dataGrid.Name == "dgBLU") {
                statusLogConfig.dgSLT.SelectedIndex = -1;
                statusLogConfig.SetStatusLabelTextWithClearTimer($"Found BLU {enteredId}.");
            } else {
                statusLogConfig.dgBLU.SelectedIndex = -1;
                statusLogConfig.SetStatusLabelTextWithClearTimer($"Found SLT {enteredId}.");
            }
            DialogResult = result;
        }
    }
}
