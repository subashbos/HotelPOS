using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class TableView : UserControl
    {
        private Table? _editingTable;

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public TableView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        /// <summary>
        /// Loads table data from the data service and populates the UI grid.
        /// </summary>
        /// <remarks>
        /// Fetches all tables, assigns sequential SNo values starting at 1, and sets TableGrid.ItemsSource.
        /// If an exception occurs, the exception message is displayed via ShowStatus.
        /// </remarks>
        private async Task LoadDataAsync()
        {
            try
            {
                List<Table> tables;
                using (var scope = App.CreateDbScope())
                {
                    var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                    tables = await tableService.GetTablesAsync();
                }
                for (int i = 0; i < tables.Count; i++) tables[i].SNo = i + 1;
                TableGrid.ItemsSource = tables;
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
        }

        /// <summary>
        /// Handle the Add/Save button click: validate form inputs and create or update a table record.
        /// </summary>
        /// <remarks>
        /// Validates the Name, Number, and Capacity fields; constructs a CreateTableDto; calls the table service to add a new table when not editing or to update the selected table when editing; shows a success or error status, resets the form, and reloads the table list.
        /// </remarks>
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowStatus("Please enter a table name.", false); return; }

            if (!int.TryParse(NumberBox.Text, out int number))
            {
                ShowStatus("Please enter a valid table number.", false);
                return;
            }

            if (!int.TryParse(CapacityBox.Text, out int capacity))
            {
                ShowStatus("Please enter a valid capacity.", false);
                return;
            }

            var dto = new CreateTableDto
            {
                Number = number,
                Name = name,
                Capacity = capacity,
                IsActive = IsActiveBox.IsChecked ?? true
            };

            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                    if (_editingTable == null)
                    {
                        await tableService.AddTableAsync(dto);
                        ShowStatus($"✅ Table '{name}' added.", true);
                    }
                    else
                    {
                        await tableService.UpdateTableAsync(_editingTable.Id, dto);
                        ShowStatus($"✅ Table '{name}' updated.", true);
                        ExitEditMode();
                    }
                }
                ResetForm();
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }

            await LoadDataAsync();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Table table)
            {
                _editingTable = table;
                NameBox.Text = table.Name;
                NumberBox.Text = table.Number.ToString();
                CapacityBox.Text = table.Capacity.ToString();
                IsActiveBox.IsChecked = table.IsActive;
                FormTitle.Text = "✏ Edit Table";
                SubmitButton.Content = "💾 Save Changes";
                CancelEditButton.Visibility = Visibility.Visible;
                NameBox.Focus();
            }
        }

        /// <summary>
        /// Prompts for confirmation and, if confirmed, deletes the table identified by the Button's Tag and refreshes the list.
        /// </summary>
        /// <remarks>
        /// Executes only when the event sender is a <see cref="Button"/> whose <c>Tag</c> is an <c>int</c> table id. On success shows a success status message; on error shows the exception message. Always reloads table data after attempting the deletion.
        /// </remarks>
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id
                && await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync("Delete this table?", "Confirm Delete",
                    HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning) == HotelPOS.Application.Interfaces.DialogResult.Yes)
            {
                try
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                        await tableService.DeleteTableAsync(id);
                    }
                    ShowStatus("🗑 Table deleted.", true);
                }
                catch (Exception ex) { ShowStatus(ex.Message, false); }

                await LoadDataAsync();
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            ResetForm();
        }

        private void ResetForm() // NOSONAR
        {
            NameBox.Clear();
            NumberBox.Text = "1";
            CapacityBox.Text = "4";
            IsActiveBox.IsChecked = true;
        }

        private void ExitEditMode()
        {
            _editingTable = null;
            FormTitle.Text = "Add New Table";
            SubmitButton.Content = "＋ Add Table";
            CancelEditButton.Visibility = Visibility.Collapsed;
        }

        private void ShowStatus(string message, bool success) // NOSONAR
        {
            StatusText.Text = message;
            StatusBorder.Background = success ? SuccessBg : ErrorBg;
            StatusText.Foreground = success ? SuccessFg : ErrorFg;
            StatusBorder.Visibility = Visibility.Visible;
        }
    }
}
