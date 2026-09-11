
namespace HousingAllotmentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalProperties { get; set; }

        public int TotalApplications { get; set; }

        public int TotalPayments { get; set; }

        public decimal TotalPaymentAmount { get; set; }

        // Monthly payment collection for dashboard graph
        public List<MonthlyPaymentViewModel> MonthlyPayments { get; set; }
            = new List<MonthlyPaymentViewModel>();
    }

    public class MonthlyPaymentViewModel
    {
        public string Month { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
