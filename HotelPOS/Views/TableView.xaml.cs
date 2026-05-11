using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class TableView : UserControl
    {
        private readonly ITableService _tableService;
        private Table? _editingTable;

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public TableView(ITableService tableService)
        {
            InitializeComponent();
            _tableService = tableService;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var tables = await _tableService.GetTablesAsync();
                for (int i = 0; i < tables.Count; i++) tables[i].SNo = i + 1;
                TableGrid.ItemsSource = tables;
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
        }

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
                if (_editingTable == null)
                {
                    await _tableService.AddTableAsync(dto);
                    ShowStatus($"✅ Table '{name}' added.", true);
                }
                else
                {
                    await _tableService.UpdateTableAsync(_editingTable.Id, dto);
                    ShowStatus($"✅ Table '{name}' updated.", true);
                    ExitEditMode();
                }
                ResetForm();
                await LoadDataAsync();
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
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

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                if (MessageBox.Show("Delete this table?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _tableService.DeleteTableAsync(id);
                    await LoadDataAsync();
                    ShowStatus("🗑 Table deleted.", true);
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            ResetForm();
        }

        private void ResetForm()
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

        private void ShowStatus(string message, bool success)
        {
            StatusText.Text = message;
            StatusBorder.Background = success ? SuccessBg : ErrorBg;
            StatusText.Foreground = success ? SuccessFg : ErrorFg;
            StatusBorder.Visibility = Visibility.Visible;
        }
    }
}
