using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class AuditView : UserControl
    {
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public AuditView(IAuditService auditService, INotificationService notificationService)
        {
            InitializeComponent();
            _auditService = auditService;
            _notificationService = notificationService;

            FromDate.SelectedDate = DateTime.Today;
            ToDate.SelectedDate = DateTime.Today;

            Loaded += async (s, e) => await RefreshLogsAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshLogsAsync();
        }

        private async Task RefreshLogsAsync()
        {
            try
            {
                var start = FromDate.SelectedDate?.Date;
                var end = ToDate.SelectedDate?.Date.AddDays(1).AddSeconds(-1);
                var logs = await _auditService.GetLogsAsync(start, end);
                var sorted = logs.OrderByDescending(l => l.Timestamp).ToList();
                for (int i = 0; i < sorted.Count; i++) sorted[i].SNo = i + 1;
                AuditGrid.ItemsSource = sorted;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading audit logs: {ex.Message}");
            }
        }
    }
}
