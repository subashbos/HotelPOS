using HotelPOS.Application.Interface;
using HotelPOS.Domain;
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
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var categories = await _categoryService.GetCategoriesAsync();
                for (int i = 0; i < categories.Count; i++) categories[i].SNo = i + 1;
                CategoryGrid.ItemsSource = categories;
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowStatus("Please enter a category name.", false); return; }

            try
            {
                if (_editingCategory == null)
                {
                    await _categoryService.AddCategoryAsync(name);
                    ShowStatus($"✅ Category '{name}' added.", true);
                }
                else
                {
                    await _categoryService.UpdateCategoryAsync(_editingCategory.Id, name);
                    ShowStatus($"✅ Category '{name}' updated.", true);
                    ExitEditMode();
                }
                NameBox.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Category category)
            {
                _editingCategory = category;
                NameBox.Text = category.Name;
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
                    await _categoryService.DeleteCategoryAsync(id);
                    await LoadDataAsync();
                    ShowStatus("🗑 Category deleted.", true);
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            NameBox.Clear();
        }

        private void ExitEditMode()
        {
            _editingCategory = null;
            FormTitle.Text = "Add New Category";
            SubmitButton.Content = "＋ Add Category";
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
