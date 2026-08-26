using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            DashboardViewModel model = new DashboardViewModel
            {
                TotalUsers = 0,
                TotalProperties = await _context.Properties.CountAsync(),
                TotalApplications = await _context.Applications.CountAsync(),
                TotalPayments = 0,
                TotalPaymentAmount = 0
            };

            return View(model);
        }
    }
}