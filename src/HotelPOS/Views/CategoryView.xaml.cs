#nullable enable

using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class CategoryView : UserControl
    {
        private Category? _editingCategory;

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public CategoryView()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadDataAsync();
        }

        /// <summary>
        /// Load categories from the database, order them by DisplayOrder then Name, assign sequential SNo values, and populate the CategoryGrid.
        /// </summary>
        /// <returns>Completes when categories have been loaded into the grid; if an error occurs, an error message is shown in the status display.</returns>
        private async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                try
                {
                    var categories = await categoryService.GetCategoriesAsync();
                    var orderedCategories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
                    for (int i = 0; i < orderedCategories.Count; i++) orderedCategories[i].SNo = i + 1;
                    CategoryGrid.ItemsSource = orderedCategories;
                }
                catch (Exception ex) { ShowStatus(ex.Message, false); }
            }
        }

        /// <summary>
        /// Handle the submit button click to add a new category or update the currently edited category, then refresh the grid.
        /// </summary>
        /// <remarks>
        /// Validates that the category name is not empty (shows a failure status if it is), parses the display order (defaults to 0 on parse failure), and performs the add or update via an ICategoryService resolved from a DI scope. On success shows a status message, clears the input fields, exits edit mode when updating, and reloads the category list. Any exceptions are displayed via ShowStatus.
        /// </remarks>
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowStatus("Please enter a category name.", false); return; }

            int.TryParse(DisplayOrderBox.Text, out var displayOrder);

            using (var scope = App.CreateDbScope())
            {
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                try
                {
                    if (_editingCategory == null)
                    {
                        await categoryService.AddCategoryAsync(name, displayOrder);
                        ShowStatus($"✅ Category '{name}' added.", true);
                    }
                    else
                    {
                        await categoryService.UpdateCategoryAsync(_editingCategory.Id, name, displayOrder);
                        ShowStatus($"✅ Category '{name}' updated.", true);
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
            if (sender is Button b && b.Tag is Category category)
            {
                _editingCategory = category;
                NameBox.Text = category.Name;
                DisplayOrderBox.Text = category.DisplayOrder.ToString();
                FormTitle.Text = "✏ Edit Category";
                SubmitButton.Content = "💾 Save Changes";
                CancelEditButton.Visibility = Visibility.Visible;
                NameBox.Focus();
            }
        }

        /// <summary>
        /// Handle the delete button click: confirm deletion, remove the category, show status, and refresh the list.
        /// </summary>
        /// <remarks>
        /// Shows a confirmation dialog; if confirmed, resolves an ICategoryService from a scoped provider, deletes the category identified by the button's Tag, displays a success or error status, and reloads the category grid.
        /// </remarks>
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id
                && await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync("Delete this category? Items linked to it will lose their category.", "Confirm Delete",
                    HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning) == HotelPOS.Application.Interfaces.DialogResult.Yes)
            {
                using (var scope = App.CreateDbScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    try
                    {
                        await categoryService.DeleteCategoryAsync(id);
                        ShowStatus("🗑 Category deleted.", true);
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
            _editingCategory = null;
            FormTitle.Text = "Add New Category";
            SubmitButton.Content = "＋ Add Category";
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
