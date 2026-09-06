using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // ADMIN ONLY
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class EmiplansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmiplansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var emiPlans = await _context.Emiplans

                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Application)
                            .ThenInclude(app => app.User)

                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Property)
                            .ThenInclude(p => p.Scheme)

                .Include(e => e.Installments)

                .OrderByDescending(e => e.EmiplanId)
                .AsNoTracking()
                .ToListAsync();

            return View(emiPlans);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? emiplanid)
        {
            if (emiplanid == null)
            {
                return NotFound();
            }

            var emiPlan = await _context.Emiplans
                .Include(e => e.Loan)
                .Include(e => e.Installments
                    .OrderBy(i => i.InstallmentNumber))
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EmiplanId == emiplanid.Value);

            if (emiPlan == null)
            {
                return NotFound();
            }

            return View(emiPlan);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadLoansAsync();

            var emiPlan = new Emiplan
            {
                EmistartDate = DateOnly.FromDateTime(DateTime.Today),
                PlanStatus = "Active",
                PaidEmis = 0,
                RemainingEmis = 0,
                MonthlyEmi = 0,
                OutstandingBalance = 0,
                CreatedDate = DateTime.Now
            };

            return View(emiPlan);
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Emiplan emiplan)
        {
            RemoveNavigationValidation();

            // -----------------------------------------------------
            // Validate Loan
            // -----------------------------------------------------

            if (emiplan.LoanId <= 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.LoanId),
                    "Please select a loan.");
            }

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l =>
                    l.LoanId == emiplan.LoanId);

            if (loan == null && emiplan.LoanId > 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.LoanId),
                    "Selected loan does not exist.");
            }

            // -----------------------------------------------------
            // Prevent duplicate EMI Plan
            // -----------------------------------------------------

            if (loan != null)
            {
                var existingPlan = await _context.Emiplans
                    .AnyAsync(e =>
                        e.LoanId == emiplan.LoanId);

                if (existingPlan)
                {
                    ModelState.AddModelError(
                        nameof(Emiplan.LoanId),
                        "An EMI Plan already exists for this loan.");
                }
            }

            // -----------------------------------------------------
            // Validate Total EMIs
            // -----------------------------------------------------

            if (emiplan.TotalEmis <= 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.TotalEmis),
                    "Total EMIs must be greater than 0.");
            }

            // -----------------------------------------------------
            // Validate Start Date
            // -----------------------------------------------------

            if (emiplan.EmistartDate == default)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.EmistartDate),
                    "Please select EMI start date.");
            }

            // -----------------------------------------------------
            // Validate Loan Tenure
            // -----------------------------------------------------

            if (loan != null &&
                loan.LoanTenure > 0 &&
                emiplan.TotalEmis > loan.LoanTenure)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.TotalEmis),
                    $"Total EMIs cannot be greater than the loan tenure of {loan.LoanTenure} months.");
            }

            // -----------------------------------------------------
            // Validate EMI amount if manually supplied
            // -----------------------------------------------------

            if (emiplan.MonthlyEmi < 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.MonthlyEmi),
                    "Monthly EMI cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLoansAsync(emiplan.LoanId);
                return View(emiplan);
            }

            try
            {
                // =================================================
                // PRINCIPAL
                // =================================================

                decimal principalAmount =
                    loan!.LoanAmount - loan.DownPayment;

                if (principalAmount < 0)
                {
                    principalAmount = 0;
                }

                // =================================================
                // MONTHLY EMI
                // =================================================

                decimal monthlyEmi =
                    CalculateEmi(
                        principalAmount,
                        loan.InterestRate,
                        emiplan.TotalEmis);

                // Allow administrator to manually enter EMI
                if (emiplan.MonthlyEmi > 0)
                {
                    monthlyEmi = emiplan.MonthlyEmi;
                }

                // =================================================
                // SET PLAN VALUES
                // =================================================

                emiplan.MonthlyEmi =
                    Math.Round(monthlyEmi, 2);

                emiplan.PaidEmis = 0;

                emiplan.RemainingEmis =
                    emiplan.TotalEmis;

                emiplan.OutstandingBalance =
                    Math.Round(principalAmount, 2);

                emiplan.NextDueDate =
                    emiplan.EmistartDate;

                emiplan.EmiendDate =
                    emiplan.EmistartDate.AddMonths(
                        emiplan.TotalEmis - 1);

                emiplan.PlanStatus =
                    string.IsNullOrWhiteSpace(
                        emiplan.PlanStatus)
                        ? "Active"
                        : emiplan.PlanStatus;

                emiplan.CreatedDate =
                    DateTime.Now;

                // =================================================
                // SAVE EMI PLAN
                // =================================================

                _context.Emiplans.Add(emiplan);

                await _context.SaveChangesAsync();

                // =================================================
                // GENERATE INSTALLMENTS
                // =================================================

                GenerateInstallments(
                    emiplan,
                    loan,
                    principalAmount);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"EMI Plan created successfully. {emiplan.TotalEmis} installments generated.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error while creating EMI Plan: " +
                    (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while creating EMI Plan: " +
                    ex.Message);
            }

            await LoadLoansAsync(emiplan.LoanId);

            return View(emiplan);
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? emiplanid)
        {
            if (emiplanid == null)
            {
                return NotFound();
            }

            var emiPlan = await _context.Emiplans
                .Include(e => e.Loan)
                .FirstOrDefaultAsync(e =>
                    e.EmiplanId == emiplanid.Value);

            if (emiPlan == null)
            {
                return NotFound();
            }

            await LoadLoansAsync(emiPlan.LoanId);

            return View(emiPlan);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int emiplanid,
            Emiplan emiplan)
        {
            if (emiplanid != emiplan.EmiplanId)
            {
                return NotFound();
            }

            RemoveNavigationValidation();

            if (emiplan.LoanId <= 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.LoanId),
                    "Please select a loan.");
            }

            if (emiplan.TotalEmis <= 0)
            {
                ModelState.AddModelError(
                    nameof(Emiplan.TotalEmis),
                    "Total EMIs must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLoansAsync(emiplan.LoanId);
                return View(emiplan);
            }

            try
            {
                // -------------------------------------------------
                // Load existing EMI plan
                // -------------------------------------------------

                var existingPlan = await _context.Emiplans
                    .FirstOrDefaultAsync(e =>
                        e.EmiplanId == emiplanid);

                if (existingPlan == null)
                {
                    return NotFound();
                }

                // -------------------------------------------------
                // Load loan
                // -------------------------------------------------

                var loan = await _context.Loans
                    .FirstOrDefaultAsync(l =>
                        l.LoanId == emiplan.LoanId);

                if (loan == null)
                {
                    ModelState.AddModelError(
                        nameof(Emiplan.LoanId),
                        "Selected loan does not exist.");

                    await LoadLoansAsync(emiplan.LoanId);

                    return View(emiplan);
                }

                // -------------------------------------------------
                // Validate tenure
                // -------------------------------------------------

                if (loan.LoanTenure > 0 &&
                    emiplan.TotalEmis > loan.LoanTenure)
                {
                    ModelState.AddModelError(
                        nameof(Emiplan.TotalEmis),
                        $"Total EMIs cannot be greater than the loan tenure of {loan.LoanTenure} months.");

                    await LoadLoansAsync(emiplan.LoanId);

                    return View(emiplan);
                }

                // -------------------------------------------------
                // Load existing installments
                // -------------------------------------------------

                var installments = await _context.Installments
                    .Where(i =>
                        i.EmiplanId == existingPlan.EmiplanId)
                    .ToListAsync();

                // -------------------------------------------------
                // Check whether any payment exists
                // -------------------------------------------------

                bool hasPayments = installments.Any(i =>
                    i.PaidAmount > 0 ||
                    i.PaymentDate.HasValue ||
                    i.PaymentStatus == "Paid");

                if (hasPayments)
                {
                    ModelState.AddModelError(
                        "",
                        "This EMI Plan cannot be regenerated because payments already exist. Only the plan status should be changed.");

                    await LoadLoansAsync(emiplan.LoanId);

                    return View(emiplan);
                }

                // -------------------------------------------------
                // Delete old installments
                // -------------------------------------------------

                if (installments.Any())
                {
                    _context.Installments.RemoveRange(
                        installments);
                }

                // -------------------------------------------------
                // Calculate principal
                // -------------------------------------------------

                decimal principalAmount =
                    loan.LoanAmount - loan.DownPayment;

                if (principalAmount < 0)
                {
                    principalAmount = 0;
                }

                // -------------------------------------------------
                // Calculate EMI
                // -------------------------------------------------

                decimal monthlyEmi =
                    CalculateEmi(
                        principalAmount,
                        loan.InterestRate,
                        emiplan.TotalEmis);

                if (emiplan.MonthlyEmi > 0)
                {
                    monthlyEmi =
                        emiplan.MonthlyEmi;
                }

                // -------------------------------------------------
                // Preserve created date
                // -------------------------------------------------

                DateTime originalCreatedDate =
                    existingPlan.CreatedDate;

                // -------------------------------------------------
                // Update EMI plan
                // -------------------------------------------------

                existingPlan.LoanId =
                    emiplan.LoanId;

                existingPlan.EmistartDate =
                    emiplan.EmistartDate;

                existingPlan.TotalEmis =
                    emiplan.TotalEmis;

                existingPlan.PaidEmis =
                    0;

                existingPlan.RemainingEmis =
                    emiplan.TotalEmis;

                existingPlan.MonthlyEmi =
                    Math.Round(monthlyEmi, 2);

                existingPlan.OutstandingBalance =
                    Math.Round(principalAmount, 2);

                existingPlan.EmiendDate =
                    emiplan.EmistartDate.AddMonths(
                        emiplan.TotalEmis - 1);

                existingPlan.NextDueDate =
                    emiplan.EmistartDate;

                existingPlan.PlanStatus =
                    string.IsNullOrWhiteSpace(
                        emiplan.PlanStatus)
                        ? "Active"
                        : emiplan.PlanStatus;

                existingPlan.CreatedDate =
                    originalCreatedDate;

                // -------------------------------------------------
                // Generate new installments
                // -------------------------------------------------

                GenerateInstallments(
                    existingPlan,
                    loan,
                    principalAmount);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "EMI Plan and installments updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EmiplanExistsAsync(
                        emiplan.EmiplanId))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error while updating EMI Plan: " +
                    (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while updating EMI Plan: " +
                    ex.Message);
            }

            await LoadLoansAsync(emiplan.LoanId);

            return View(emiplan);
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? emiplanid)
        {
            if (emiplanid == null)
            {
                return NotFound();
            }

            var emiPlan = await _context.Emiplans
                .Include(e => e.Loan)
                .Include(e => e.Installments)
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EmiplanId == emiplanid.Value);

            if (emiPlan == null)
            {
                return NotFound();
            }

            return View(emiPlan);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int emiplanid)
        {
            var emiPlan = await _context.Emiplans
                .FirstOrDefaultAsync(e =>
                    e.EmiplanId == emiplanid);

            if (emiPlan == null)
            {
                return NotFound();
            }

            try
            {
                // -------------------------------------------------
                // Get installments
                // -------------------------------------------------

                var installments = await _context.Installments
                    .Where(i =>
                        i.EmiplanId == emiplanid)
                    .ToListAsync();

                // -------------------------------------------------
                // Do not delete plan if payments exist
                // -------------------------------------------------

                bool hasPayments = installments.Any(i =>
                    i.PaidAmount > 0 ||
                    i.PaymentDate.HasValue ||
                    i.PaymentStatus == "Paid");

                if (hasPayments)
                {
                    TempData["ErrorMessage"] =
                        "This EMI Plan cannot be deleted because payment records already exist.";

                    return RedirectToAction(nameof(Index));
                }

                // -------------------------------------------------
                // Delete installments
                // -------------------------------------------------

                if (installments.Any())
                {
                    _context.Installments.RemoveRange(
                        installments);
                }

                // -------------------------------------------------
                // Delete EMI plan
                // -------------------------------------------------

                _context.Emiplans.Remove(emiPlan);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "EMI Plan and its installments deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete the EMI Plan. " +
                    (ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Error while deleting EMI Plan: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EMI CALCULATION
        // =========================================================

        private decimal CalculateEmi(
            decimal principal,
            decimal annualInterestRate,
            int totalEmis)
        {
            if (totalEmis <= 0)
            {
                return 0;
            }

            if (principal <= 0)
            {
                return 0;
            }

            decimal monthlyRate =
                annualInterestRate / 12m / 100m;

            if (monthlyRate <= 0)
            {
                return principal / totalEmis;
            }

            decimal power =
                (decimal)Math.Pow(
                    (double)(1 + monthlyRate),
                    totalEmis);

            decimal emi =
                principal *
                monthlyRate *
                power /
                (power - 1);

            return Math.Round(emi, 2);
        }

        // =========================================================
        // GENERATE INSTALLMENTS
        // =========================================================

        private void GenerateInstallments(
            Emiplan emiPlan,
            Loan loan,
            decimal principalAmount)
        {
            decimal outstanding =
                principalAmount;

            decimal monthlyRate =
                loan.InterestRate / 12m / 100m;

            for (int i = 1;
                 i <= emiPlan.TotalEmis;
                 i++)
            {
                decimal interestAmount =
                    Math.Round(
                        outstanding * monthlyRate,
                        2);

                decimal installmentAmount =
                    emiPlan.MonthlyEmi;

                decimal principalPayment =
                    installmentAmount -
                    interestAmount;

                // -------------------------------------------------
                // Last installment adjustment
                // -------------------------------------------------

                if (i == emiPlan.TotalEmis)
                {
                    principalPayment =
                        outstanding;

                    installmentAmount =
                        principalPayment +
                        interestAmount;
                }

                if (principalPayment < 0)
                {
                    principalPayment = 0;
                }

                if (principalPayment > outstanding)
                {
                    principalPayment = outstanding;
                }

                outstanding -=
                    principalPayment;

                if (outstanding < 0)
                {
                    outstanding = 0;
                }

                var installment =
                    new Installment
                    {
                        EmiplanId =
                            emiPlan.EmiplanId,

                        InstallmentNumber =
                            i,

                        DueDate =
                            emiPlan.EmistartDate
                                .AddMonths(i - 1),

                        InstallmentAmount =
                            Math.Round(
                                installmentAmount,
                                2),

                        PrincipalAmount =
                            Math.Round(
                                principalPayment,
                                2),

                        InterestAmount =
                            Math.Round(
                                interestAmount,
                                2),

                        LateFee = 0,

                        PaidAmount = 0,

                        PaymentDate = null,

                        PaymentMethod = null,

                        TransactionReference = null,

                        PaymentStatus = "Pending",

                        Remarks = null,

                        CreatedDate =
                            DateTime.Now
                    };

                _context.Installments.Add(
                    installment);
            }
        }

        // =========================================================
        // LOAD LOAN DROPDOWN
        // =========================================================

        private async Task LoadLoansAsync(
            int? selectedLoanId = null)
        {
            var loans = await _context.Loans
                .AsNoTracking()
                .OrderByDescending(l => l.LoanId)
                .ToListAsync();

            var loanList = loans.Select(l => new
            {
                LoanId = l.LoanId,

                DisplayText =
                    l.LoanNumber +
                    " | ₹" +
                    l.LoanAmount.ToString("N2") +
                    " | " +
                    l.LoanStatus
            });

            ViewBag.LoanId =
                new SelectList(
                    loanList,
                    "LoanId",
                    "DisplayText",
                    selectedLoanId);
        }

        // =========================================================
        // REMOVE NAVIGATION VALIDATION
        // =========================================================

        private void RemoveNavigationValidation()
        {
            ModelState.Remove(nameof(Emiplan.Loan));
            ModelState.Remove(nameof(Emiplan.Installments));
        }

        // =========================================================
        // CHECK EMI PLAN EXISTS
        // =========================================================

        private async Task<bool> EmiplanExistsAsync(
            int emiplanid)
        {
            return await _context.Emiplans
                .AnyAsync(e =>
                    e.EmiplanId == emiplanid);
        }
    }
}