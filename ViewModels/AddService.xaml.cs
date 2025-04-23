using System;
using System.Windows;
using Dapper;
using System.Data.SQLite;
using ServiceCenterApp.Models;
using System.Linq;
using System.Windows.Controls;
using System.Net.NetworkInformation;

namespace ServiceCenterApp.Views
{
    public partial class AddService : Window
    {
        public AddService()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbStatus.SelectedItem as ComboBoxItem;
            var status = selectedItem?.Content.ToString();
            var warrantyItem = cmbWarranty.SelectedItem as ComboBoxItem;
            var warrantyStatus = warrantyItem?.Content.ToString();
            var accessories = txtAccessories.Text;
            var serialNumber = txtSerialNumber.Text;
            var entry = new ServiceEntry
            {
                CustomerName = cmbCustomer.Text,
                Item = cmbUnit.Text,
                SerialNumber = serialNumber,
                Problem = txtProblem.Text,
                Status = status,
                ServiceLocation = cmbLocation.Text,
                DateIn = status == "Masuk" ? DateTime.Now : (DateTime?)null,
                ServiceDate = status == "Service" ? DateTime.Now : (DateTime?)null,
                DateOut = status == "Keluar" ? DateTime.Now : (DateTime?)null
            };

            using var conn = DbHelper.GetConnection();

            if (_editEntry != null)
            {
                // mode EDIT
                entry.Id = _editEntry.Id;

                conn.Execute(@"
            UPDATE Services SET 
                RowNumber = @RowNumber,
                CustomerName = @CustomerName,
                Item = @Item,
                SerialNumber = @SerialNumber,   
                Problem = @Problem,
                Status = @Status,
                DateIn = COALESCE(DateIn, @DateIn),
                ServiceDate = COALESCE(ServiceDate, @ServiceDate),
                DateOut = COALESCE(DateOut, @DateOut),
                ServiceLocation = @ServiceLocation
            WHERE Id = @Id", entry);

                MessageBox.Show("Data servis berhasil diperbarui!");
            }
            else
            {
                // mode TAMBAH
                conn.Execute(@"
            INSERT INTO Services 
                (RowNumber, CustomerName, Item, SerialNumber, Problem, Status, DateIn, ServiceDate, DateOut, ServiceLocation) 
            VALUES 
                (@RowNumber, @CustomerName, @Item, @SerialNumber, @Problem, @Status, @DateIn, @ServiceDate, @DateOut, @ServiceLocation)", entry);

                MessageBox.Show("Data servis berhasil ditambahkan!");
            }

            this.Close();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using var conn = DbHelper.GetConnection();

            var customers = conn.Query<string>("SELECT DISTINCT CustomerName FROM Services").ToList();
            cmbCustomer.ItemsSource = customers;

            var units = conn.Query<string>("SELECT DISTINCT Item FROM Services").ToList();
            cmbUnit.ItemsSource = units;

            var locations = conn.Query<string>("SELECT DISTINCT ServiceLocation FROM Services WHERE ServiceLocation IS NOT NULL").ToList(); ;
            cmbLocation.ItemsSource = locations;
        }

        private ServiceEntry _editEntry;
        public AddService(ServiceEntry entry = null)
        {
            InitializeComponent();
            _editEntry = entry;

            if (_editEntry != null)
            {
                cmbCustomer.Text = _editEntry.CustomerName;
                cmbUnit.Text = _editEntry.Item;
                txtProblem.Text = _editEntry.Problem;
                cmbStatus.Text = _editEntry.Status;
                cmbLocation.Text = _editEntry.ServiceLocation;
            }
        }




    }
}
