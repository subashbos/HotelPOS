using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        // --- Table Layout ---
        public ObservableCollection<TableStatus> Tables { get; } = new();

        [ObservableProperty]
        private bool _isTableLayoutOpen;

        [RelayCommand]
        private void OpenTableLayout(object? parameter)
        {
            // Table layout is irrelevant for Takeaway / Online orders
            if (IsTableless) return;

            bool open = true;
            if (parameter is bool b) open = b;
            else if (parameter is string s && bool.TryParse(s, out bool b2)) open = b2;

            if (open) RefreshTables();
            IsTableLayoutOpen = open;

            if (open && !IsTransferMode) IsTransferMode = false;
        }

        [RelayCommand]
        private void SelectTable(int tableNumber)
        {
            if (IsTransferMode)
            {
                _cartService.TransferTable(TableNumber, tableNumber);
                IsTransferMode = false;
                StatusMessage = $"Table {TableNumber} items moved to Table {tableNumber}";
                TableNumber = tableNumber;
            }
            else
            {
                TableNumber = tableNumber;
            }

            IsTableLayoutOpen = false;
            UpdateCart();
        }

        [RelayCommand]
        private void ToggleTransferMode()
        {
            // Move Items is only meaningful for DineIn
            if (IsTableless) return;
            if (Cart.Count == 0) return;
            IsTransferMode = !IsTransferMode;

            if (IsTransferMode)
            {
                StatusMessage = "MOVE MODE: Select target table from the Table menu";
                OpenTableLayout(true);
            }
            else
            {
                StatusMessage = "Ready";
                IsTableLayoutOpen = false;
            }
        }

        /// <summary>
        /// Reloads the ViewModel's Tables collection from the table service and updates each TableStatus's occupancy and current flags.
        /// </summary>
        /// <remarks>
        /// If the table service returns no tables, populates a default set of 20 tables. Any error during refresh is reported via the notification service.
        /// </remarks>
        private async void RefreshTables()
        {
            using (var scope = App.CreateDbScope())
            {
                var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                try
                {
                    var tables = await tableService.GetTablesAsync();
                    Tables.Clear();
                    var activeTables = _cartService.GetActiveTables();

                    if (tables == null || tables.Count == 0)
                    {
                        // Fallback to default 20 tables if none defined yet
                        for (int i = 1; i <= 20; i++)
                        {
                            Tables.Add(new TableStatus
                            {
                                TableNumber = i,
                                IsOccupied = activeTables.Contains(i),
                                IsCurrent = i == TableNumber
                            });
                        }
                    }
                    else
                    {
                        foreach (var t in tables.Where(x => x.IsActive).OrderBy(x => x.Number))
                        {
                            Tables.Add(new TableStatus
                            {
                                TableNumber = t.Number,
                                TableName = t.Name,
                                IsOccupied = activeTables.Contains(t.Number),
                                IsCurrent = t.Number == TableNumber
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to refresh tables: {ex.Message}");
                }
            }
        }
    }

    public class TableStatus : ObservableObject
    {
        public int TableNumber { get; set; }
        public string TableName { get; set; } = string.Empty;

        private bool _isOccupied;
        public bool IsOccupied
        {
            get => _isOccupied;
            set => SetProperty(ref _isOccupied, value);
        }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }
    }
}
