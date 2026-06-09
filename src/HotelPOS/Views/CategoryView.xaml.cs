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
        private readonly ICategoryService _categoryService;
        private Category? _editingCategory;

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public CategoryView(ICategoryService categoryService)
        {
            InitializeComponent();
            _categoryService = categoryService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(categoryService);
            }

            Loaded += async (s, e) => await LoadDataAsync();
        }

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

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                if (MessageBox.Show("Delete this category? Items linked to it will lose their category.", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
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
