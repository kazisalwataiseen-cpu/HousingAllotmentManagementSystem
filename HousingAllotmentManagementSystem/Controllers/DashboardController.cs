
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HousingAllotmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // =====================================================
            // BASIC DASHBOARD COUNTS
            // =====================================================

            int totalUsers = await _context.Users.CountAsync();

            int totalProperties = await _context.Properties.CountAsync();

            int totalApplications = await _context.Applications.CountAsync();

            int totalPayments = await _context.Payments.CountAsync();

            decimal totalPaymentAmount =
                await _context.Payments
                    .Select(p => (decimal?)p.Amount)
                    .SumAsync() ?? 0;


            // =====================================================
            // MONTHLY PAYMENT COLLECTION
            // =====================================================

            var currentYear = DateTime.Now.Year;

            var monthlyPaymentData = await _context.Payments
                .Where(p => p.PaymentDate.Year == currentYear)
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();


            // =====================================================
            // CREATE MONTHLY GRAPH DATA
            // =====================================================

            var monthlyPayments = new List<MonthlyPaymentViewModel>();

            for (int month = 1; month <= 12; month++)
            {
                var payment = monthlyPaymentData
                    .FirstOrDefault(x => x.Month == month);

                monthlyPayments.Add(new MonthlyPaymentViewModel
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat
                        .GetAbbreviatedMonthName(month),

                    Amount = payment?.Amount ?? 0
                });
            }


            // =====================================================
            // DASHBOARD MODEL
            // =====================================================

            DashboardViewModel model = new DashboardViewModel
            {
                TotalUsers = totalUsers,

                TotalProperties = totalProperties,

                TotalApplications = totalApplications,

                TotalPayments = totalPayments,

                TotalPaymentAmount = totalPaymentAmount,

                MonthlyPayments = monthlyPayments
            };


            return View(model);
        }
    }
}

