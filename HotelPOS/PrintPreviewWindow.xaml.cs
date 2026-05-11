using HotelPOS.Domain;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace HotelPOS
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly Order _order;
        private readonly SystemSetting _settings;
        private bool _isLoaded;

        public PrintPreviewWindow(Order order, SystemSetting settings)
        {
            InitializeComponent();
            _order = order;
            _settings = settings;

            // Set default format based on settings
            ThermalToggle.IsChecked = _settings.ReceiptFormat == "Thermal";
            A4Toggle.IsChecked = _settings.ReceiptFormat == "A4";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            GeneratePreview();
        }

        private void Format_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            GeneratePreview();
        }

        private void GeneratePreview()
        {
            if (_order == null) return;

            bool isThermal = ThermalToggle.IsChecked == true;

            // Build the Flow Document
            FlowDocument document = ReceiptGenerator.CreateReceipt(_order, isThermal, _settings);

            // Render inside DocumentViewer
            DocViewer.Document = document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (DocViewer.Document is not FlowDocument document) return;

            PrintDialog printDialog = new PrintDialog();
            bool shouldPrint = false;

            // If a default printer is set in settings, try to use it directly
            if (!string.IsNullOrEmpty(_settings.DefaultPrinter))
            {
                try
                {
                    printDialog.PrintQueue = new System.Printing.LocalPrintServer().GetPrintQueue(_settings.DefaultPrinter);
                    shouldPrint = true;
                }
                catch
                {
                    // Fallback to dialog if printer not found
                    shouldPrint = printDialog.ShowDialog() == true;
                }
            }
            else
            {
                // No default printer, show dialog
                shouldPrint = printDialog.ShowDialog() == true;
            }

            if (shouldPrint)
            {
                // To print a FlowDocument native to the printer's printable area bounds:
                IDocumentPaginatorSource idpSource = document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"Receipt for Order #{_order.Id}");
                this.Close(); // Auto-close window upon queuing into Windows Print Spooler
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Print_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }
    }
}
