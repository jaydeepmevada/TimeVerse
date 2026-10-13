using TimeVerse.ViewModels.Orders;

namespace TimeVerse.ViewModels.Sales
{
    public class SalesDashboardViewModel
    {
        public int PendingOrdersCount { get; set; } = 0;
        public int ConfirmedOrdersCount { get; set; } = 0;
        public int ProcessingOrdersCount { get; set; } = 0;
        public int ShippedOrdersCount { get; set; } = 0;
        public int DeliveredOrdersCount { get; set; } = 0;

        public int TodayOrdersCount { get; set; } = 0;
        public decimal TodaySales { get; set; } = 0.00m;
        public decimal WeekSales { get; set; } = 0.00m;
        public int NewInquiriesCount { get; set; } = 0;

        public List<OrderDetailsViewModel> ActiveOrdersQueue { get; set; } = new();
    }
}
