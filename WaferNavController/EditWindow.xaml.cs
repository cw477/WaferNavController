using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : BaseWindow {
        private Config configPage;

        public EditWindow(Config configPage, string id, string name, string description, string location, bool available) {
            this.configPage = configPage;
            this.KeyDown += Esc_KeyDown;
            InitializeComponent();
            BarcodeTextBox.Text = id;
            NameTextBox.Text = name;
            DescriptionTextBox.Text = description;
            LocationTextBox.Text = location;
            AvailableCheckBox.IsChecked = available;
        }

        private void SaveButton_Clicked(object sender, RoutedEventArgs e) {
            configPage.dgBLU.SelectedIndex = -1;
            configPage.dgSLT.SelectedIndex = -1;
            DialogResult = false;
        }

        private void EditWindow_Closed(object sender, EventArgs e) {
            configPage.dgBLU.SelectedIndex = -1;
            configPage.dgSLT.SelectedIndex = -1;
        }
    }
}
