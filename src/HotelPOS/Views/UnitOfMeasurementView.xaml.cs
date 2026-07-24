using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class UnitOfMeasurementView : UserControl
    {
        private UnitOfMeasurement? _editingUnit;

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public UnitOfMeasurementView()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var unitService = scope.ServiceProvider.GetRequiredService<IUnitOfMeasurementService>();
                try
                {
                    var units = await unitService.GetUnitsAsync();
                    var orderedUnits = units.OrderBy(u => u.DisplayOrder).ThenBy(u => u.Name).ToList();
                    for (int i = 0; i < orderedUnits.Count; i++) orderedUnits[i].SNo = i + 1;
                    UnitGrid.ItemsSource = orderedUnits;
                }
                catch (Exception ex) { ShowStatus(ex.Message, false); }
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowStatus("Please enter a unit name.", false); return; }

            int.TryParse(DisplayOrderBox.Text, out var displayOrder);

            using (var scope = App.CreateDbScope())
            {
                var unitService = scope.ServiceProvider.GetRequiredService<IUnitOfMeasurementService>();
                try
                {
                    if (_editingUnit == null)
                    {
                        await unitService.AddUnitAsync(name, displayOrder);
                        ShowStatus($"✅ Unit '{name}' added.", true);
                    }
                    else
                    {
                        await unitService.UpdateUnitAsync(_editingUnit.Id, name, displayOrder);
                        ShowStatus($"✅ Unit '{name}' updated.", true);
                        ExitEditMode();
                    }
                    NameBox.Clear();
                    DisplayOrderBox.Text = "0";
                }
                catch (Exception ex) { ShowStatus(ex.Message, false); }
            }

            await LoadDataAsync();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is UnitOfMeasurement unit)
            {
                _editingUnit = unit;
                NameBox.Text = unit.Name;
                DisplayOrderBox.Text = unit.DisplayOrder.ToString();
                FormTitle.Text = "✏ Edit Unit";
                SubmitButton.Content = "💾 Save Changes";
                CancelEditButton.Visibility = Visibility.Visible;
                NameBox.Focus();
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id
                && await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync("Delete this unit? Items using it cannot be reassigned automatically.", "Confirm Delete",
                    HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning) == HotelPOS.Application.Interfaces.DialogResult.Yes)
            {
                using (var scope = App.CreateDbScope())
                {
                    var unitService = scope.ServiceProvider.GetRequiredService<IUnitOfMeasurementService>();
                    try
                    {
                        await unitService.DeleteUnitAsync(id);
                        ShowStatus("🗑 Unit deleted.", true);
                    }
                    catch (Exception ex) { ShowStatus(ex.Message, false); }
                }

                await LoadDataAsync();
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            NameBox.Clear();
            DisplayOrderBox.Text = "0";
        }

        private void ExitEditMode()
        {
            _editingUnit = null;
            FormTitle.Text = "Add New Unit";
            SubmitButton.Content = "＋ Add Unit";
            CancelEditButton.Visibility = Visibility.Collapsed;
            DisplayOrderBox.Text = "0";
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) // NOSONAR
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250));
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
