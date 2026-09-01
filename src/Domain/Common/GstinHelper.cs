namespace HotelPOS.Domain.Common
{
    /// <summary>Shared GSTIN parsing used both when billing an order (to decide CGST+SGST vs IGST)
    /// and when building the GSTR-1 filing report (Place of Supply column). Kept in Domain so both
    /// the Application-layer billing/report code and the WPF-only GSTR-1 view can share it without
    /// a layering dependency in either direction.</summary>
    public static class GstinHelper
    {
        /// <summary>The first two digits of a GSTIN are the issuing state's GST state code.</summary>
        public static string DeriveStateCode(string? gstin) =>
            !string.IsNullOrWhiteSpace(gstin) && gstin.Length >= 2 ? gstin[..2] : string.Empty;

        /// <summary>True when both GSTINs carry a known state code and those codes differ - i.e. the
        /// sale is interstate and should be taxed as IGST rather than CGST+SGST. Returns false (the
        /// safe intrastate default) whenever either party's state can't be determined, e.g. a B2C
        /// sale with no customer GSTIN on file.</summary>
        public static bool IsInterstate(string? hotelGstin, string? customerGstin)
        {
            var hotelState = DeriveStateCode(hotelGstin);
            var customerState = DeriveStateCode(customerGstin);
            return hotelState.Length > 0 && customerState.Length > 0 && hotelState != customerState;
        }
    }
}
