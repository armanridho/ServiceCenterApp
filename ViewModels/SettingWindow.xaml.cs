using System;
using System.Windows;

namespace ServiceCenterApp.Views
{
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();

            // Load initial data with null check
            txtDbPath.Text = Properties.Settings.Default.DbPath ?? string.Empty;
            txtCloudDbUrl.Text = DbHelper.CloudDbUrl ?? string.Empty;
            txtCloudUploadUrl.Text = DbHelper.CloudUploadUrl ?? string.Empty;
        }

        private void ChangeDbPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                txtDbPath.Text = dialog.FileName;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save to app settings
                Properties.Settings.Default.DbPath = txtDbPath.Text;
                Properties.Settings.Default.Save();

                // Update runtime values
                DbHelper.CloudDbUrl = txtCloudDbUrl.Text;
                DbHelper.CloudUploadUrl = txtCloudUploadUrl.Text;

                MessageBox.Show("Pengaturan disimpan", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan pengaturan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}