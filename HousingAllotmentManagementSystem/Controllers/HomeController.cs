using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using HousingAllotmentManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // HOME PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // -----------------------------------------------------
            // PUBLIC USER
            // -----------------------------------------------------

            if (User.Identity?.IsAuthenticated != true)
            {
                return View();
            }

            // -----------------------------------------------------
            // GET LOGGED-IN USER ID
            // -----------------------------------------------------

            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return View();
            }

            // -----------------------------------------------------
            // GET CURRENT USER'S APPLICATIONS
            // -----------------------------------------------------
            //
            // User
            //   ↓
            // Application
            //   ↓
            // Allotment
            //   ↓
            // Loan
            //
            // -----------------------------------------------------

            var applications = await _context.Applications
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Include(a => a.Property)
                .Include(a => a.Allotments)
                    .ThenInclude(al => al.Loans)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            // -----------------------------------------------------
            // SEND APPLICATIONS TO VIEW
            // -----------------------------------------------------

            ViewBag.UserApplications = applications;

            // -----------------------------------------------------
            // GET ALL LOANS BELONGING TO CURRENT USER
            // -----------------------------------------------------

            var loans = applications
                .SelectMany(a => a.Allotments)
                .SelectMany(a => a.Loans)
                .ToList();

            ViewBag.UserLoans = loans;

            return View();
        }

        // =========================================================
        // PUBLIC HOUSING SCHEMES PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> HousingSchemes()
        {
            var schemes = await _context.HousingSchemes
                .AsNoTracking()
                .OrderByDescending(x => x.SchemeId)
                .ToListAsync();

            return View(
                "~/Views/Home/HousingSchemes.cshtml",
                schemes);
        }

        // =========================================================
        // PUBLIC SCHEME DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SchemeDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheme = await _context.HousingSchemes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.SchemeId == id);

            if (scheme == null)
            {
                return NotFound();
            }

            return View(scheme);
        }

        // =========================================================
        // TEST EMAIL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "YOUR_TEST_EMAIL@gmail.com",
                "Housing Allotment System - Test Email",
                "<h2>Email Test Successful!</h2>" +
                "<p>Your Housing Allotment Management System is successfully connected to Gmail SMTP.</p>"
            );

            return Content(
                "Test email sent successfully!");
        }
    }
}