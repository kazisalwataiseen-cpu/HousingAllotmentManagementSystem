using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // ADMIN ONLY CONTROLLER
    // =========================================================
    //
    // Clients cannot access:
    //
    // /Loans
    // /Loans/Details
    // /Loans/Create
    // /Loans/Edit
    // /Loans/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoansController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Allotment)
                .OrderByDescending(l => l.LoanId)
                .AsNoTracking()
                .ToListAsync();

            return View(loans);
        }


        // =========================================================
        // DETAILS - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Allotment)
                .AsNoTracking()
                .FirstOrDefaultAsync(l =>
                    l.LoanId == id);

            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadAllotments();

            var loan = new Loan
            {
                SanctionDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                CreatedDate =
                    DateTime.Now,

                LoanStatus =
                    "Pending"
            };

            return View(loan);
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Loan loan)
        {
            // Navigation property is not submitted.
            ModelState.Remove("Allotment");


            // -----------------------------------------------------
            // ALLOTMENT VALIDATION
            // -----------------------------------------------------

            if (loan.AllotmentId <= 0)
            {
                ModelState.AddModelError(
                    "AllotmentId",
                    "Please select an allotment.");
            }
            else
            {
                bool allotmentExists =
                    await _context.Allotments
                        .AnyAsync(a =>
                            a.AllotmentId ==
                            loan.AllotmentId);

                if (!allotmentExists)
                {
                    ModelState.AddModelError(
                        "AllotmentId",
                        "Selected allotment does not exist.");
                }
            }


            // -----------------------------------------------------
            // LOAN NUMBER VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    loan.LoanNumber))
            {
                ModelState.AddModelError(
                    "LoanNumber",
                    "Loan number is required.");
            }
            else
            {
                bool loanNumberExists =
                    await _context.Loans
                        .AnyAsync(l =>
                            l.LoanNumber ==
                            loan.LoanNumber);

                if (loanNumberExists)
                {
                    ModelState.AddModelError(
                        "LoanNumber",
                        "This loan number already exists. Please enter a different loan number.");
                }
            }


            // -----------------------------------------------------
            // LOAN AMOUNT
            // -----------------------------------------------------

            if (loan.LoanAmount <= 0)
            {
                ModelState.AddModelError(
                    "LoanAmount",
                    "Loan amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // DOWN PAYMENT
            // -----------------------------------------------------

            if (loan.DownPayment < 0)
            {
                ModelState.AddModelError(
                    "DownPayment",
                    "Down payment cannot be negative.");
            }


            // -----------------------------------------------------
            // INTEREST RATE
            // -----------------------------------------------------

            if (loan.InterestRate < 0)
            {
                ModelState.AddModelError(
                    "InterestRate",
                    "Interest rate cannot be negative.");
            }


            // -----------------------------------------------------
            // LOAN TENURE
            // -----------------------------------------------------

            if (loan.LoanTenure <= 0)
            {
                ModelState.AddModelError(
                    "LoanTenure",
                    "Loan tenure must be greater than zero.");
            }


            // -----------------------------------------------------
            // EMI
            // -----------------------------------------------------

            if (loan.Emiamount <= 0)
            {
                ModelState.AddModelError(
                    "Emiamount",
                    "EMI amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // STATUS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    loan.LoanStatus))
            {
                loan.LoanStatus =
                    "Pending";
            }


            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    loan.CreatedDate =
                        DateTime.Now;

                    _context.Loans.Add(
                        loan);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Loan created successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    string errorMessage =
                        ex.InnerException?.Message ??
                        ex.Message;

                    ModelState.AddModelError(
                        "",
                        "Database error while saving loan: " +
                        errorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Error while saving loan: " +
                        ex.Message);
                }
            }


            // Reload dropdown when validation/save fails.
            LoadAllotments(
                loan.AllotmentId);

            return View(loan);
        }


        // =========================================================
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan =
                await _context.Loans
                    .FindAsync(id);

            if (loan == null)
            {
                return NotFound();
            }

            LoadAllotments(
                loan.AllotmentId);

            return View(loan);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Loan loan)
        {
            if (id != loan.LoanId)
            {
                return NotFound();
            }

            ModelState.Remove("Allotment");


            // -----------------------------------------------------
            // ALLOTMENT VALIDATION
            // -----------------------------------------------------

            if (loan.AllotmentId <= 0)
            {
                ModelState.AddModelError(
                    "AllotmentId",
                    "Please select an allotment.");
            }


            // -----------------------------------------------------
            // LOAN NUMBER VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    loan.LoanNumber))
            {
                ModelState.AddModelError(
                    "LoanNumber",
                    "Loan number is required.");
            }


            // -----------------------------------------------------
            // LOAN AMOUNT
            // -----------------------------------------------------

            if (loan.LoanAmount <= 0)
            {
                ModelState.AddModelError(
                    "LoanAmount",
                    "Loan amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // LOAN TENURE
            // -----------------------------------------------------

            if (loan.LoanTenure <= 0)
            {
                ModelState.AddModelError(
                    "LoanTenure",
                    "Loan tenure must be greater than zero.");
            }


            // -----------------------------------------------------
            // EMI
            // -----------------------------------------------------

            if (loan.Emiamount <= 0)
            {
                ModelState.AddModelError(
                    "Emiamount",
                    "EMI amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // STATUS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    loan.LoanStatus))
            {
                loan.LoanStatus =
                    "Pending";
            }


            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadAllotments(
                    loan.AllotmentId);

                return View(loan);
            }


            // -----------------------------------------------------
            // UPDATE
            // -----------------------------------------------------

            try
            {
                var existingLoan =
                    await _context.Loans
                        .FirstOrDefaultAsync(l =>
                            l.LoanId == id);

                if (existingLoan == null)
                {
                    return NotFound();
                }

                existingLoan.AllotmentId =
                    loan.AllotmentId;

                existingLoan.LoanNumber =
                    loan.LoanNumber;

                existingLoan.LoanAmount =
                    loan.LoanAmount;

                existingLoan.DownPayment =
                    loan.DownPayment;

                existingLoan.InterestRate =
                    loan.InterestRate;

                existingLoan.LoanTenure =
                    loan.LoanTenure;

                existingLoan.Emiamount =
                    loan.Emiamount;

                existingLoan.SanctionDate =
                    loan.SanctionDate;

                existingLoan.LoanStatus =
                    loan.LoanStatus;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Loan updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error while updating loan: " +
                    (ex.InnerException?.Message ??
                     ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while updating loan: " +
                    ex.Message);
            }


            LoadAllotments(
                loan.AllotmentId);

            return View(loan);
        }


        // =========================================================
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan =
                await _context.Loans
                    .Include(l => l.Allotment)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l =>
                        l.LoanId == id);

            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }


        // =========================================================
        // DELETE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var loan =
                await _context.Loans
                    .FirstOrDefaultAsync(l =>
                        l.LoanId == id);

            if (loan == null)
            {
                return NotFound();
            }

            try
            {
                _context.Loans.Remove(
                    loan);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Loan deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This loan cannot be deleted because it is linked with another record.";

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        id
                    });
            }
        }


        // =========================================================
        // LOAD ALLOTMENTS
        // =========================================================

        private void LoadAllotments(
            int? selectedAllotmentId = null)
        {
            var allotments =
                _context.Allotments
                    .AsNoTracking()
                    .OrderByDescending(
                        a => a.AllotmentId)
                    .ToList();

            ViewBag.AllotmentId =
                new SelectList(
                    allotments,
                    "AllotmentId",
                    "AllotmentId",
                    selectedAllotmentId);
        }


        // =========================================================
        // CHECK LOAN
        // =========================================================

        private bool LoanExists(int id)
        {
            return _context.Loans
                .Any(l =>
                    l.LoanId == id);
        }
    }
}