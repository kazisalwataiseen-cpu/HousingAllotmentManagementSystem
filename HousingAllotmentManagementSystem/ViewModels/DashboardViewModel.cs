namespace HousingAllotmentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalProperties { get; set; }

        public int TotalApplications { get; set; }

        public int TotalPayments { get; set; }

        public decimal TotalPaymentAmount { get; set; }
    }
}