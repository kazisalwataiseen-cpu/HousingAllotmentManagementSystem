using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoanApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanApplicationsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ALL LOAN APPLICATIONS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.LoanApplications
                .Include(x => x.User)

                .Include(x => x.Allotment)
                    .ThenInclude(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                .OrderByDescending(x => x.LoanApplicationId)
                .AsNoTracking()
                .ToListAsync();

            return View(applications);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application =
                await _context.LoanApplications
                    .Include(x => x.User)
                    .Include(x => x.Allotment)
                    .FirstOrDefaultAsync(x =>
                        x.LoanApplicationId == id.Value);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id)
        {
            var application =
                await _context.LoanApplications
                    .FirstOrDefaultAsync(x =>
                        x.LoanApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending loan applications can be approved.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check existing loan
            // -----------------------------------------------------

            var existingLoan =
                await _context.Loans
                    .FirstOrDefaultAsync(l =>
                        l.AllotmentId ==
                        application.AllotmentId);

            if (existingLoan != null)
            {
                application.Status = "Approved";
                application.ReviewedDate = DateTime.Now;
                application.Remarks =
                    "Loan already exists for this allotment.";

                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] =
                    "A loan already exists for this allotment.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Calculate principal
            // -----------------------------------------------------

            decimal principal =
                application.RequestedLoanAmount -
                application.DownPayment;

            if (principal <= 0)
            {
                TempData["ErrorMessage"] =
                    "Requested loan amount must be greater than the down payment.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Calculate EMI
            // -----------------------------------------------------

            decimal monthlyRate =
                application.InterestRate /
                12m /
                100m;

            decimal emi;

            if (monthlyRate == 0)
            {
                emi =
                    principal /
                    application.LoanTenure;
            }
            else
            {
                double p =
                    (double)principal;

                double r =
                    (double)monthlyRate;

                double n =
                    application.LoanTenure;

                double result =
                    p * r *
                    Math.Pow(1 + r, n)
                    /
                    (Math.Pow(1 + r, n) - 1);

                emi =
                    (decimal)result;
            }

            // -----------------------------------------------------
            // Generate unique loan number
            // -----------------------------------------------------

            string loanNumber;

            do
            {
                loanNumber =
                    "LN" +
                    DateTime.Now.ToString(
                        "yyyyMMddHHmmssfff");
            }
            while (await _context.Loans
                .AnyAsync(l =>
                    l.LoanNumber == loanNumber));

            // -----------------------------------------------------
            // Create actual loan
            // -----------------------------------------------------

            var loan = new Loan
            {
                AllotmentId =
                    application.AllotmentId,

                LoanNumber =
                    loanNumber,

                LoanAmount =
                    application.RequestedLoanAmount,

                DownPayment =
                    application.DownPayment,

                InterestRate =
                    application.InterestRate,

                LoanTenure =
                    application.LoanTenure,

                Emiamount =
                    Math.Round(
                        emi,
                        2),

                SanctionDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                LoanStatus =
                    "Active",

                CreatedDate =
                    DateTime.Now
            };

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                _context.Loans.Add(loan);

                application.Status =
                    "Approved";

                application.ReviewedDate =
                    DateTime.Now;

                application.Remarks =
                    "Loan application approved.";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Loan application approved and loan created successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Unable to approve loan application: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Index));
            }
        }

        // =========================================================
        // REJECT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? remarks)
        {
            var application =
                await _context.LoanApplications
                    .FirstOrDefaultAsync(x =>
                        x.LoanApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending loan applications can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            application.Status =
                "Rejected";

            application.ReviewedDate =
                DateTime.Now;

            application.Remarks =
                string.IsNullOrWhiteSpace(remarks)
                    ? "Loan application rejected."
                    : remarks;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Loan application rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}