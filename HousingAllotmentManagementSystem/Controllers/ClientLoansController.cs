using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // CLIENT LOAN CONTROLLER
    // =========================================================

    [Authorize]
    public class ClientLoanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientLoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MY LOANS
        // GET: /ClientLoan
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // -----------------------------------------------------
            // Get actual loans belonging to logged-in client
            // -----------------------------------------------------

            var loans = await _context.Loans
                .Include(l => l.Allotment)
                    .ThenInclude(a => a.Application)
                .Include(l => l.Allotment)
                    .ThenInclude(a => a.Property)
                        .ThenInclude(p => p.Scheme)
                .Include(l => l.Emiplans)
                .Where(l =>
                    l.Allotment.Application.UserId == userId.Value &&
                    l.LoanStatus != "Pending")
                .OrderByDescending(l => l.CreatedDate)
                .AsNoTracking()
                .ToListAsync();

            // -----------------------------------------------------
            // Get all loan applications belonging to client
            // -----------------------------------------------------

            var loanApplications = await _context.LoanApplications
                .Include(x => x.Allotment)
                    .ThenInclude(a => a.Property)
                .Include(x => x.EMIPlanOption)
                .Where(x =>
                    x.UserId == userId.Value)
                .OrderByDescending(x => x.LoanApplicationId)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.LoanApplications = loanApplications;

            // -----------------------------------------------------
            // Find active allotment
            // -----------------------------------------------------

            var activeAllotment = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .Where(a =>
                    a.Application.UserId == userId.Value &&
                    a.AllotmentStatus == "Active")
                .OrderByDescending(a => a.CreatedDate)
                .FirstOrDefaultAsync();

            ViewBag.HasActiveAllotment =
                activeAllotment != null;

            // -----------------------------------------------------
            // Check pending application
            // -----------------------------------------------------

            ViewBag.HasPendingApplication =
                loanApplications.Any(x =>
                    x.Status == "Pending");

            // -----------------------------------------------------
            // Pass active allotment
            // -----------------------------------------------------

            if (activeAllotment != null)
            {
                ViewBag.ActiveAllotment =
                    activeAllotment;
            }

            return View(loans);
        }

        // =========================================================
        // LOAN DETAILS
        // GET: /ClientLoan/Details/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var loan = await _context.Loans
                .Include(l => l.Allotment)
                    .ThenInclude(a => a.Application)
                .Include(l => l.Allotment)
                    .ThenInclude(a => a.Property)
                        .ThenInclude(p => p.Scheme)
                .Include(l => l.Emiplans)
                    .ThenInclude(e => e.Installments)
                .AsNoTracking()
                .FirstOrDefaultAsync(l =>
                    l.LoanId == id.Value &&
                    l.Allotment.Application.UserId == userId.Value);

            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // =========================================================
        // APPLY FOR LOAN - GET
        // GET: /ClientLoan/Apply
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // -----------------------------------------------------
            // Find active allotment
            // -----------------------------------------------------

            var activeAllotments = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .Where(a =>
                    a.Application.UserId == userId.Value &&
                    a.AllotmentStatus == "Active")
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            if (activeAllotments.Count == 0)
            {
                TempData["Error"] =
                    "Your account does not have an active property allotment. A housing loan can be applied for after a property has been allotted to you.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Use latest active allotment
            // -----------------------------------------------------

            var allotment =
                activeAllotments.First();

            // -----------------------------------------------------
            // Check actual loan
            // -----------------------------------------------------

            var existingLoan = await _context.Loans
                .AsNoTracking()
                .AnyAsync(l =>
                    l.AllotmentId == allotment.AllotmentId);

            if (existingLoan)
            {
                TempData["Error"] =
                    "A housing loan already exists for your allotted property.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check pending application
            // -----------------------------------------------------

            var pendingApplication = await _context.LoanApplications
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId.Value &&
                    x.AllotmentId == allotment.AllotmentId &&
                    x.Status == "Pending");

            if (pendingApplication)
            {
                TempData["Error"] =
                    "You already have a pending housing loan application for this property.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Load active EMI plan options for this scheme
            // -----------------------------------------------------

            var emiPlanOptions = await _context.EMIPlanOptions
                .Where(x =>
                    x.SchemeId == allotment.Property.SchemeId &&
                    x.Status == "Active")
                .OrderBy(x => x.TenureMonths)
                .ThenBy(x => x.PlanName)
                .AsNoTracking()
                .ToListAsync();

            if (emiPlanOptions.Count == 0)
            {
                TempData["Error"] =
                    "No active EMI plans are currently available for your housing scheme. Please contact the administrator.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Load ViewBag data
            // -----------------------------------------------------

            await LoadLoanApplicationViewData(
                allotment);

            // -----------------------------------------------------
            // Create application model
            // -----------------------------------------------------

            var application = new LoanApplication
            {
                UserId =
                    userId.Value,

                AllotmentId =
                    allotment.AllotmentId,

                RequestedLoanAmount =
                    0,

                DownPayment =
                    0,

                InterestRate =
                    0,

                LoanTenure =
                    0,

                // IMPORTANT:
                // Nullable EMIPlanOptionId starts as null.
                EMIPlanOptionId =
                    null,

                ApplicationDate =
                    DateTime.Now,

                Status =
                    "Pending",

                Remarks =
                    null,

                ReviewedDate =
                    null,

                CreatedDate =
                    DateTime.Now
            };

            return View(application);
        }

        // =========================================================
        // APPLY FOR LOAN - POST
        // POST: /ClientLoan/Apply
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            LoanApplication model)
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // -----------------------------------------------------
            // ALWAYS use logged-in user's ID
            // -----------------------------------------------------

            model.UserId =
                userId.Value;

            // -----------------------------------------------------
            // Verify active allotment
            // -----------------------------------------------------

            var allotment = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .FirstOrDefaultAsync(a =>
                    a.AllotmentId == model.AllotmentId &&
                    a.Application.UserId == userId.Value &&
                    a.AllotmentStatus == "Active");

            if (allotment == null)
            {
                TempData["Error"] =
                    "The selected property allotment is invalid or inactive.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check actual loan
            // -----------------------------------------------------

            var existingLoan = await _context.Loans
                .AnyAsync(l =>
                    l.AllotmentId == model.AllotmentId);

            if (existingLoan)
            {
                TempData["Error"] =
                    "A housing loan already exists for this property.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check pending application
            // -----------------------------------------------------

            var pendingExists =
                await _context.LoanApplications
                    .AnyAsync(x =>
                        x.UserId == userId.Value &&
                        x.AllotmentId == model.AllotmentId &&
                        x.Status == "Pending");

            if (pendingExists)
            {
                TempData["Error"] =
                    "You already have a pending loan application for this property.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Remove navigation validation
            // -----------------------------------------------------

            ModelState.Remove(
                nameof(LoanApplication.User));

            ModelState.Remove(
                nameof(LoanApplication.Allotment));

            ModelState.Remove(
                nameof(LoanApplication.EMIPlanOption));

            // =====================================================
            // VALIDATE EMI PLAN
            // =====================================================

            EMIPlanOption? selectedPlan = null;

            if (!model.EMIPlanOptionId.HasValue ||
                model.EMIPlanOptionId.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.EMIPlanOptionId),
                    "Please select an EMI plan.");
            }
            else
            {
                selectedPlan =
                    await _context.EMIPlanOptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.EMIPlanOptionId ==
                                model.EMIPlanOptionId.Value &&
                            x.SchemeId ==
                                allotment.Property.SchemeId &&
                            x.Status == "Active");

                if (selectedPlan == null)
                {
                    ModelState.AddModelError(
                        nameof(model.EMIPlanOptionId),
                        "Please select a valid active EMI plan for your housing scheme.");
                }
                else
                {
                    // -------------------------------------------------
                    // Get rate and tenure directly from database plan
                    // -------------------------------------------------

                    model.InterestRate =
                        selectedPlan.InterestRate;

                    model.LoanTenure =
                        selectedPlan.TenureMonths;
                }
            }

            // =====================================================
            // VALIDATE REQUESTED LOAN AMOUNT
            // =====================================================

            if (model.RequestedLoanAmount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.RequestedLoanAmount),
                    "Please enter a valid loan amount.");
            }

            // =====================================================
            // VALIDATE DOWN PAYMENT
            // =====================================================

            if (model.DownPayment < 0)
            {
                ModelState.AddModelError(
                    nameof(model.DownPayment),
                    "Down payment cannot be negative.");
            }

            // -----------------------------------------------------
            // Down payment must be less than requested amount
            // -----------------------------------------------------

            if (model.RequestedLoanAmount > 0 &&
                model.DownPayment >=
                    model.RequestedLoanAmount)
            {
                ModelState.AddModelError(
                    nameof(model.DownPayment),
                    "Down payment must be less than the requested loan amount.");
            }

            // =====================================================
            // VALIDATE INTEREST RATE
            // =====================================================

            if (selectedPlan != null &&
                model.InterestRate <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.InterestRate),
                    "The selected EMI plan has an invalid interest rate.");
            }

            // =====================================================
            // VALIDATE TENURE
            // =====================================================

            if (selectedPlan != null &&
                model.LoanTenure <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.LoanTenure),
                    "The selected EMI plan has an invalid loan tenure.");
            }

            if (selectedPlan != null &&
                model.LoanTenure > 360)
            {
                ModelState.AddModelError(
                    nameof(model.LoanTenure),
                    "Loan tenure cannot exceed 360 months.");
            }

            // =====================================================
            // RETURN FORM IF INVALID
            // =====================================================

            if (!ModelState.IsValid)
            {
                await LoadLoanApplicationViewData(
                    allotment);

                return View(model);
            }

            // =====================================================
            // CREATE LOAN APPLICATION
            // =====================================================

            var loanApplication =
                new LoanApplication
                {
                    UserId =
                        userId.Value,

                    AllotmentId =
                        allotment.AllotmentId,

                    RequestedLoanAmount =
                        Math.Round(
                            model.RequestedLoanAmount,
                            2),

                    DownPayment =
                        Math.Round(
                            model.DownPayment,
                            2),

                    InterestRate =
                        Math.Round(
                            selectedPlan!.InterestRate,
                            2),

                    LoanTenure =
                        selectedPlan.TenureMonths,

                    // =================================================
                    // IMPORTANT FIX
                    //
                    // Save the selected EMI Plan ID in database.
                    // =================================================

                    EMIPlanOptionId =
                        selectedPlan.EMIPlanOptionId,

                    ApplicationDate =
                        DateTime.Now,

                    Status =
                        "Pending",

                    Remarks =
                        null,

                    ReviewedDate =
                        null,

                    CreatedDate =
                        DateTime.Now
                };

            // =====================================================
            // SAVE APPLICATION
            // =====================================================

            try
            {
                _context.LoanApplications.Add(
                    loanApplication);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Your housing loan application has been submitted successfully and is pending admin approval.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to submit the loan application: " +
                    (
                        ex.InnerException?.Message ??
                        ex.Message
                    ));

                await LoadLoanApplicationViewData(
                    allotment);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred: " +
                    ex.Message);

                await LoadLoanApplicationViewData(
                    allotment);

                return View(model);
            }
        }

        // =========================================================
        // CLIENT APPLICATION DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var application =
                await _context.LoanApplications
                    .Include(x => x.Allotment)
                        .ThenInclude(a => a.Property)
                            .ThenInclude(p => p.Scheme)
                    .Include(x => x.EMIPlanOption)
                    .Include(x => x.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.LoanApplicationId == id.Value &&
                        x.UserId == userId.Value);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // =========================================================
        // LOAD DATA FOR LOAN APPLICATION VIEW
        // =========================================================

        private async Task LoadLoanApplicationViewData(
            Allotment allotment)
        {
            // -----------------------------------------------------
            // Make sure Property is loaded
            // -----------------------------------------------------

            if (allotment.Property == null)
            {
                allotment.Property =
                    await _context.Properties
                        .Include(p => p.Scheme)
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                            allotment.PropertyId);
            }
            else if (allotment.Property.Scheme == null)
            {
                allotment.Property.Scheme =
                    await _context.HousingSchemes
                        .FirstOrDefaultAsync(s =>
                            s.SchemeId ==
                            allotment.Property.SchemeId);
            }

            // -----------------------------------------------------
            // Load active EMI plans for this scheme
            // -----------------------------------------------------

            var emiPlanOptions =
                await _context.EMIPlanOptions
                    .Where(x =>
                        x.SchemeId ==
                            allotment.Property.SchemeId &&
                        x.Status == "Active")
                    .OrderBy(x => x.TenureMonths)
                    .ThenBy(x => x.PlanName)
                    .AsNoTracking()
                    .ToListAsync();

            // -----------------------------------------------------
            // Send data to Apply.cshtml
            // -----------------------------------------------------

            ViewBag.ActiveAllotment =
                allotment;

            ViewBag.Property =
                allotment.Property;

            ViewBag.HousingScheme =
                allotment.Property.Scheme;

            ViewBag.EMIPlanOptions =
                emiPlanOptions;
        }

        // =========================================================
        // LOGGED-IN USER ID
        // =========================================================

        private int? GetLoggedInUserId()
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(
                userIdClaim))
            {
                return null;
            }

            if (!int.TryParse(
                userIdClaim,
                out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}