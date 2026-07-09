using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        private bool _isInitializing;

        /// <summary>
        /// Loads items, categories, and settings into the view model, initializes filtered item list and cart state, and verifies that a cash shift is open.
        /// </summary>
        /// <remarks>
        /// Populates the internal item cache and the Categories collection (including an "All" category), sets composition-scheme state, applies the active filter, and updates cart totals and UI state. If no cash session is open a user-facing warning is shown; failures during initialization are reported via the notification service.
        /// </remarks>
        public async Task InitializeAsync()
        {
            if (_isInitializing) return;

            using (var scope = App.CreateDbScope())
            {
                var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();

                try
                {
                    _isInitializing = true;
                    _allItems = await itemService.GetItemsAsync();
                    var cats = await categoryService.GetCategoriesAsync();
                    var orderedCats = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();

                    Categories.Clear();
                    Categories.Add(new Category { Id = 0, Name = "All", DisplayOrder = -1 });
                    foreach (var cat in orderedCats) Categories.Add(cat);

                    var settings = await settingService.GetSettingsAsync();
                    IsCompositionScheme = settings.IsCompositionScheme;

                    ApplyFilter();
                    UpdateCart();

                    var currentSession = await cashService.GetCurrentSessionAsync();
                    if (currentSession == null)
                    {
                        _notificationService.ShowWarning("Please open a shift in the 'Shift' tab before starting billing.");
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load data: {ex.Message}");
                }
                finally
                {
                    _isInitializing = false;
                }
            }
        }

        [RelayCommand]
        private void ApplyFilter()
        {
            if (_allItems == null) return;
            var filtered = _allItems.AsEnumerable();
            if (SelectedCategoryId > 0)
                filtered = filtered.Where(i => i.CategoryId == SelectedCategoryId);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim();
                filtered = filtered.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                              (!string.IsNullOrEmpty(i.Barcode) && i.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)))
                                   .OrderBy(i => i.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                                   .ThenBy(i => i.Name);
            }
            else
            {
                filtered = filtered.OrderBy(i => i.Name);
            }

            Items.Clear();
            foreach (var item in filtered) Items.Add(item);
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void FilterByCategory(int categoryId)
        {
            SelectedCategoryId = categoryId;
            ApplyFilter();
        }

        partial void OnTableNumberChanged(int value) => UpdateCart();
    }
}
