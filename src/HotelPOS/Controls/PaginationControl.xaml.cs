using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Controls
{
    /// <summary>
    /// Reusable pagination control. Bind via <c>SetSource(myList)</c> and subscribe to
    /// <c>PageChanged</c> to receive the current page slice.
    /// </summary>
    public partial class PaginationControl : UserControl
    {
        public event Action<IList>? PageChanged;
        public event Func<int, int, Task>? ExternalPageRequested;

        private IList _fullSource = new List<object>();
        private bool _isExternal = false;
        private int _totalCount = 0;
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public PaginationControl()
        {
            InitializeComponent();
            PageSizeCombo.SelectedIndex = 0; // Default 10
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Set full data source for client-side paging.</summary>
        public void SetSource(IList items)
        {
            _isExternal = false;
            _fullSource = items ?? new List<object>();
            _totalCount = _fullSource.Count;
            _currentPage = 1;
            Render();
        }

        /// <summary>Set total count for server-side paging. Subscriber must handle ExternalPageRequested.</summary>
        public void SetExternalSource(int totalCount)
        {
            _isExternal = true;
            _totalCount = totalCount;
            Render();
        }

        public void ResetToFirstPage() => _currentPage = 1;

        // ── Page Navigation ───────────────────────────────────────────────────

        private void First_Click(object sender, RoutedEventArgs e) { _currentPage = 1; Render(); }
        private void Last_Click(object sender, RoutedEventArgs e) { _currentPage = _totalPages; Render(); }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; Render(); }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; Render(); }
        }

        private void PageSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var tag = (PageSizeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            _pageSize = int.TryParse(tag, out var ps) ? ps : 10;
            _currentPage = 1;
            Render();
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private async void Render()
        {
            if (_isExternal)
            {
                await RenderExternal();
            }
            else
            {
                RenderInternal();
            }

            FirstBtn.IsEnabled = _currentPage > 1;
            PrevBtn.IsEnabled = _currentPage > 1;
            NextBtn.IsEnabled = _currentPage < _totalPages;
            LastBtn.IsEnabled = _currentPage < _totalPages;
        }

        private void RenderInternal()
        {
            int total = _totalCount;

            if (_pageSize <= 0) // "All"
            {
                _totalPages = 1;
                _currentPage = 1;
                EmitPage(_fullSource);
                InfoText.Text = $"Showing all {total} records";
                PageText.Text = "Page 1 of 1";
            }
            else
            {
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)total / _pageSize));
                _currentPage = Math.Clamp(_currentPage, 1, _totalPages);

                int start = (_currentPage - 1) * _pageSize;
                int end = Math.Min(start + _pageSize, total);

                var page = new List<object>();
                for (int i = start; i < end; i++)
                    page.Add(_fullSource[i]!);

                EmitPage(page);
                InfoText.Text = total == 0
                    ? "No records found"
                    : $"Showing {start + 1}–{end} of {total} records";
                PageText.Text = $"Page {_currentPage} of {_totalPages}";
            }
        }

        private async Task RenderExternal()
        {
            int total = _totalCount;
            _totalPages = _pageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)total / _pageSize));
            _currentPage = Math.Clamp(_currentPage, 1, _totalPages);

            if (ExternalPageRequested != null)
            {
                await ExternalPageRequested.Invoke(_currentPage, _pageSize);
            }

            int start = (_currentPage - 1) * _pageSize;
            int end = _pageSize <= 0 ? total : Math.Min(start + _pageSize, total);

            InfoText.Text = total == 0
                ? "No records found"
                : $"Showing {start + 1}–{end} of {total} records";
            PageText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private void EmitPage(IList page) => PageChanged?.Invoke(page);
    }
}
