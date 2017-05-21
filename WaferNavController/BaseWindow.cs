using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WaferNavController {

    public abstract class BaseWindow : Window {

        protected StatusLogConfig statusLogConfig;

        protected void Esc_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                DialogResult = false;
                statusLogConfig.dgBLU.SelectedIndex = -1;
                statusLogConfig.dgSLT.SelectedIndex = -1;
            }
        }

        protected void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox) sender;
            textBox.Text = string.Empty;
            textBox.Foreground = Brushes.Black;
            textBox.GotFocus -= TextBox_GotFocus;
        }

        protected void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Trim().Equals(string.Empty)) {
                textBox.Text = (string) textBox.Tag;
                textBox.Foreground = Brushes.LightGray;
                textBox.GotFocus += TextBox_GotFocus;
            }
        }
    }
}
