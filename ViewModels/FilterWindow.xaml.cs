using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ServiceCenterApp.Models;
using System.Windows.Controls;



namespace ServiceCenterApp.Views
{
    public partial class FilterWindow : Window
    {
        public List<string> SelectedCustomers = new List<string>();
        public List<string> SelectedItems = new List<string>();
        public List<string> SelectedSerialNumbers = new List<string>();
        public List<string> SelectedWarranty = new List<string>();
        public List<string> SelectedStatus = new List<string>();
        public List<string> SelectedLocations = new List<string>();

        public FilterWindow(List<ServiceEntry> services)
        {
            InitializeComponent();
            LoadCheckboxes(services);
        }

        private void LoadCheckboxes(List<ServiceEntry> services)
        {
            foreach (var customer in services.Select(s => s.CustomerName).Distinct())
                CustomerCheckboxes.Items.Add(new CheckBox { Content = customer });

            foreach (var item in services.Select(s => s.Item).Distinct())
                ItemCheckboxes.Items.Add(new CheckBox { Content = item });

            foreach (var serial in services.Select(s => s.SerialNumber).Distinct())
                SerialNumberCheckboxes.Items.Add(new CheckBox { Content = serial });

            foreach (var warranty in services.Select(s => s.WarrantyStatus).Distinct())
                WarrantyCheckboxes.Items.Add(new CheckBox { Content = warranty });

            foreach (var status in services.Select(s => s.Status).Distinct())
                StatusCheckboxes.Items.Add(new CheckBox { Content = status });

            foreach (var location in services.Select(s => s.ServiceLocation).Distinct())
                LocationCheckboxes.Items.Add(new CheckBox { Content = location });
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomers = CustomerCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            SelectedItems = ItemCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            SelectedSerialNumbers = SerialNumberCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            SelectedWarranty = WarrantyCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            SelectedStatus = StatusCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            SelectedLocations = LocationCheckboxes.Items.Cast<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();

            this.DialogResult = true;
            this.Close();
        }
    }

}
