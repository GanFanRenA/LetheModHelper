using System.Windows;

namespace LethelModHelper
{
    public partial class CreateBuffDialog : Window
    {
        public string BuffId => IdTextBox.Text.Trim();
        public string BuffName => NameTextBox.Text.Trim();
        public string BuffDesc => DescTextBox.Text.Trim();

        public CreateBuffDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(BuffId))
            {
                MessageBox.Show("Buff ID 不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}