using System;
using System.Windows;
using Dapper;
using System.Data.SQLite;
using ServiceCenterApp.Models;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;

namespace ServiceCenterApp.Views
{
    public partial class AddService : Window
    {
        private ServiceEntry _editEntry;

        public AddService()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        public AddService(ServiceEntry entry) : this()
        {
            _editEntry = entry;

            if (_editEntry != null)
            {
                cmbCustomer.Text = _editEntry.CustomerName;
                cmbUnit.Text = _editEntry.Item;
                txtSerialNumber.Text = _editEntry.SerialNumber;
                cmbWarranty.Text = _editEntry.WarrantyStatus;
                txtProblem.Text = _editEntry.Problem;
                cmbStatus.Text = _editEntry.Status;
                cmbLocation.Text = _editEntry.ServiceLocation;
                txtAccessories.Text = _editEntry.Accessories;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();

                // Load customer names
                var customers = conn.Query<string>("SELECT DISTINCT CustomerName FROM Services WHERE CustomerName IS NOT NULL")
                                    .ToList();
                cmbCustomer.ItemsSource = customers;

                // Load items
                var units = conn.Query<string>("SELECT DISTINCT Item FROM Services WHERE Item IS NOT NULL")
                                .ToList();
                cmbUnit.ItemsSource = units;

                // Load service locations
                var locations = conn.Query<string>("SELECT DISTINCT ServiceLocation FROM Services WHERE ServiceLocation IS NOT NULL")
                                    .ToList();
                cmbLocation.ItemsSource = locations;

                // Load warranty statuses (with default values)
                var warrantyStatuses = new List<string> { "Ya", "Tidak" };
                var dbWarrantyStatuses = conn.Query<string>("SELECT DISTINCT WarrantyStatus FROM Services WHERE WarrantyStatus IS NOT NULL")
                                            .ToList();
                warrantyStatuses.AddRange(dbWarrantyStatuses.Except(warrantyStatuses));
                cmbWarranty.ItemsSource = warrantyStatuses;

                // Load service statuses (with default values)
                var statuses = new List<string> { "Masuk", "Service", "Keluar" };
                var dbStatuses = conn.Query<string>("SELECT DISTINCT Status FROM Services WHERE Status IS NOT NULL")
                                    .ToList();
                statuses.AddRange(dbStatuses.Except(statuses));
                cmbStatus.ItemsSource = statuses;

                // Set default values if not in edit mode
                if (_editEntry == null)
                {
                    cmbStatus.SelectedIndex = 0;
                    cmbWarranty.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(cmbCustomer.Text))
                {
                    MessageBox.Show("Nama Customer harus diisi!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(cmbUnit.Text))
                {
                    MessageBox.Show("Unit harus dipilih!");
                    return;
                }

                if (cmbStatus.SelectedItem == null)
                {
                    MessageBox.Show("Status Servis harus dipilih!");
                    return;
                }

                // Create service entry
                var entry = new ServiceEntry
                {
                    CustomerName = cmbCustomer.Text,
                    Item = cmbUnit.Text,
                    SerialNumber = txtSerialNumber.Text,
                    WarrantyStatus = cmbWarranty.Text,
                    Problem = txtProblem.Text,
                    Status = cmbStatus.Text,
                    DateIn = cmbStatus.Text == "Masuk" ? DateTime.Now : (DateTime?)null,
                    ServiceDate = cmbStatus.Text == "Service" ? DateTime.Now : (DateTime?)null,
                    DateOut = cmbStatus.Text == "Keluar" ? DateTime.Now : (DateTime?)null,
                    ServiceLocation = cmbLocation.Text,
                    Accessories = txtAccessories.Text,
                    LastUpdated = DateTime.Now
                };

                using var conn = DbHelper.GetConnection();
                conn.Open();

                if (_editEntry != null)
                {
                    // EDIT mode
                    entry.Id = _editEntry.Id;

                    conn.Execute(@"
                        UPDATE Services SET
                            CustomerName = @CustomerName,
                            Item = @Item,
                            SerialNumber = @SerialNumber,
                            WarrantyStatus = @WarrantyStatus,
                            Problem = @Problem,
                            Status = @Status,
                            DateIn = CASE WHEN @DateIn IS NULL THEN DateIn ELSE @DateIn END,
                            ServiceDate = CASE WHEN @ServiceDate IS NULL THEN ServiceDate ELSE @ServiceDate END,
                            DateOut = CASE WHEN @DateOut IS NULL THEN DateOut ELSE @DateOut END,
                            ServiceLocation = @ServiceLocation,
                            Accessories = @Accessories,
                            LastUpdated = @LastUpdated
                        WHERE Id = @Id", entry);

                    MessageBox.Show("Data servis berhasil diperbarui!");
                }
                else
                {
                    // ADD mode
                    conn.Execute(@"
                        INSERT INTO Services 
                            (CustomerName, Item, SerialNumber, WarrantyStatus, Problem, Status, 
                             DateIn, ServiceDate, DateOut, ServiceLocation, Accessories, LastUpdated) 
                        VALUES 
                            (@CustomerName, @Item, @SerialNumber, @WarrantyStatus, @Problem, @Status, 
                             @DateIn, @ServiceDate, @DateOut, @ServiceLocation, @Accessories, @LastUpdated)", entry);

                    MessageBox.Show("Data servis berhasil ditambahkan!");
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}");
            }
        }
    }
}