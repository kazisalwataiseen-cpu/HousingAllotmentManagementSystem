using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
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

        public async Task<IActionResult> Index()
        {
            var emiPlans = await _context.Emiplans
                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Application)
                            .ThenInclude(ap => ap.User)
                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Property)
                .Include(e => e.Installments)
                .AsNoTracking()
                .ToListAsync();

            return View(emiPlans);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emiplan = await _context.Emiplans
                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Application)
                            .ThenInclude(ap => ap.User)
                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Property)
                .Include(e => e.Installments)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmiplanId == id);

            if (emiplan == null)
            {
                return NotFound();
            }

            return View(emiplan);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        public async Task<IActionResult> Create()
        {
            var emiplan = new Emiplan
            {
                EmistartDate =
                    DateOnly.FromDateTime(
                        DateTime.Today.AddMonths(1)
                    ),

                PlanStatus = "Active",

                CreatedDate = DateTime.Now
            };

            await LoadLoansAsync(emiplan.LoanId);

            return View(emiplan);
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

            var loan = await _context.Loans
                .FirstOrDefaultAsync(
                    l => l.LoanId == emiplan.LoanId
                );

            if (loan == null)
            {
                ModelState.AddModelError(
                    "LoanId",
                    "Please select a valid loan."
                );
            }

            if (loan != null)
            {
                // -------------------------------------------------
                // Check duplicate EMI plan
                // -------------------------------------------------

                bool existingPlan =
                    await _context.Emiplans
                        .AnyAsync(
                            e => e.LoanId == emiplan.LoanId
                        );

                if (existingPlan)
                {
                    ModelState.AddModelError(
                        "LoanId",
                        "An EMI plan already exists for this loan."
                    );
                }

                // -------------------------------------------------
                // Validate total EMIs
                // -------------------------------------------------

                if (emiplan.TotalEmis <= 0)
                {
                    ModelState.AddModelError(
                        "TotalEmis",
                        "Total EMIs must be greater than zero."
                    );
                }

                // -------------------------------------------------
                // Validate start date
                // -------------------------------------------------

                if (emiplan.EmistartDate <
                    DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError(
                        "EmistartDate",
                        "EMI start date cannot be in the past."
                    );
                }

                // -------------------------------------------------
                // IMPORTANT
                //
                // LoanAmount is already the actual financed amount.
                //
                // Example:
                //
                // Requested Amount = ₹10,00,000
                // Down Payment     = ₹2,00,000
                // LoanAmount       = ₹8,00,000
                //
                // EMI is calculated on ₹8,00,000.
                // -------------------------------------------------

                decimal principalAmount =
                    loan.LoanAmount;

                if (principalAmount <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Loan amount must be greater than zero."
                    );
                }

                // -------------------------------------------------
                // Calculate EMI automatically
                // -------------------------------------------------

                if (emiplan.TotalEmis > 0 &&
                    principalAmount > 0)
                {
                    decimal calculatedEmi =
                        CalculateEmi(
                            principalAmount,
                            loan.InterestRate,
                            emiplan.TotalEmis
                        );

                    if (emiplan.MonthlyEmi <= 0)
                    {
                        emiplan.MonthlyEmi =
                            calculatedEmi;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadLoansAsync(emiplan.LoanId);

                return View(emiplan);
            }

            // =====================================================
            // TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                loan = await _context.Loans
                    .FirstOrDefaultAsync(
                        l => l.LoanId == emiplan.LoanId
                    );

                if (loan == null)
                {
                    ModelState.AddModelError(
                        "LoanId",
                        "Loan not found."
                    );

                    await LoadLoansAsync(
                        emiplan.LoanId
                    );

                    return View(emiplan);
                }

                // -------------------------------------------------
                // LoanAmount is already financed amount
                // -------------------------------------------------

                decimal principalAmount =
                    loan.LoanAmount;

                // -------------------------------------------------
                // Set EMI Plan values
                // -------------------------------------------------

                emiplan.PaidEmis = 0;

                emiplan.RemainingEmis =
                    emiplan.TotalEmis;

                emiplan.OutstandingBalance =
                    principalAmount;

                emiplan.NextDueDate =
                    emiplan.EmistartDate;

                emiplan.EmiendDate =
                    emiplan.EmistartDate.AddMonths(
                        emiplan.TotalEmis - 1
                    );

                emiplan.PlanStatus = "Active";

                emiplan.CreatedDate =
                    DateTime.Now;

                // -------------------------------------------------
                // Add EMI Plan
                // -------------------------------------------------

                _context.Emiplans.Add(emiplan);

                await _context.SaveChangesAsync();

                // -------------------------------------------------
                // Generate installments
                // -------------------------------------------------

                await GenerateInstallments(
                    emiplan,
                    loan,
                    principalAmount
                );

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "EMI plan and installments created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Unable to create EMI plan. " +
                    ex.Message
                );

                await LoadLoansAsync(
                    emiplan.LoanId
                );

                return View(emiplan);
            }
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emiplan = await _context.Emiplans
                .Include(e => e.Loan)
                .FirstOrDefaultAsync(
                    e => e.EmiplanId == id
                );

            if (emiplan == null)
            {
                return NotFound();
            }

            await LoadLoansAsync(
                emiplan.LoanId
            );

            return View(emiplan);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Emiplan emiplan)
        {
            if (id != emiplan.EmiplanId)
            {
                return NotFound();
            }

            RemoveNavigationValidation();

            var existingPlan =
                await _context.Emiplans
                    .Include(e => e.Loan)
                    .FirstOrDefaultAsync(
                        e => e.EmiplanId == id
                    );

            if (existingPlan == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .FirstOrDefaultAsync(
                    l => l.LoanId == emiplan.LoanId
                );

            if (loan == null)
            {
                ModelState.AddModelError(
                    "LoanId",
                    "Please select a valid loan."
                );
            }

            if (emiplan.TotalEmis <= 0)
            {
                ModelState.AddModelError(
                    "TotalEmis",
                    "Total EMIs must be greater than zero."
                );
            }

            if (loan != null)
            {
                // -------------------------------------------------
                // LoanAmount is already financed amount
                // -------------------------------------------------

                decimal principalAmount =
                    loan.LoanAmount;

                if (principalAmount <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Loan amount must be greater than zero."
                    );
                }

                decimal calculatedEmi =
                    CalculateEmi(
                        principalAmount,
                        loan.InterestRate,
                        emiplan.TotalEmis
                    );

                if (emiplan.MonthlyEmi <= 0)
                {
                    emiplan.MonthlyEmi =
                        calculatedEmi;
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadLoansAsync(
                    emiplan.LoanId
                );

                return View(emiplan);
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // -------------------------------------------------
                // Update EMI Plan
                // -------------------------------------------------

                existingPlan.EmistartDate =
                    emiplan.EmistartDate;

                existingPlan.TotalEmis =
                    emiplan.TotalEmis;

                existingPlan.EmiendDate =
                    emiplan.EmistartDate.AddMonths(
                        emiplan.TotalEmis - 1
                    );

                existingPlan.MonthlyEmi =
                    emiplan.MonthlyEmi;

                existingPlan.PlanStatus =
                    emiplan.PlanStatus;

                // -------------------------------------------------
                // LoanAmount is financed amount
                // -------------------------------------------------

                decimal principalAmount =
                    loan!.LoanAmount;

                existingPlan.PaidEmis = 0;

                existingPlan.RemainingEmis =
                    existingPlan.TotalEmis;

                existingPlan.OutstandingBalance =
                    principalAmount;

                existingPlan.NextDueDate =
                    existingPlan.EmistartDate;

                // -------------------------------------------------
                // Delete old installments
                // -------------------------------------------------

                var oldInstallments =
                    await _context.Installments
                        .Where(
                            i => i.EmiplanId ==
                                 existingPlan.EmiplanId
                        )
                        .ToListAsync();

                if (oldInstallments.Any())
                {
                    _context.Installments
                        .RemoveRange(
                            oldInstallments
                        );
                }

                // -------------------------------------------------
                // Generate new installments
                // -------------------------------------------------

                await GenerateInstallments(
                    existingPlan,
                    loan,
                    principalAmount
                );

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "EMI plan updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Unable to update EMI plan. " +
                    ex.Message
                );

                await LoadLoansAsync(
                    emiplan.LoanId
                );

                return View(emiplan);
            }
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emiplan = await _context.Emiplans
                .Include(e => e.Loan)
                .Include(e => e.Installments)
                    .ThenInclude(i => i.Payments)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.EmiplanId == id
                );

            if (emiplan == null)
            {
                return NotFound();
            }

            return View(emiplan);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var emiplan =
                await _context.Emiplans
                    .Include(e => e.Installments)
                        .ThenInclude(i => i.Payments)
                    .FirstOrDefaultAsync(
                        e => e.EmiplanId == id
                    );

            if (emiplan == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Do not delete if payment exists
            // -----------------------------------------------------

            bool hasPayments =
                emiplan.Installments
                    .Any(i => i.Payments.Any());

            if (hasPayments)
            {
                TempData["ErrorMessage"] =
                    "This EMI plan cannot be deleted because payments already exist.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                // -------------------------------------------------
                // Delete installments
                // -------------------------------------------------

                if (emiplan.Installments.Any())
                {
                    _context.Installments
                        .RemoveRange(
                            emiplan.Installments
                        );
                }

                // -------------------------------------------------
                // Delete EMI plan
                // -------------------------------------------------

                _context.Emiplans.Remove(emiplan);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "EMI plan deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete EMI plan. " +
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
            int tenureMonths)
        {
            if (principal <= 0 ||
                tenureMonths <= 0)
            {
                return 0;
            }

            // -----------------------------------------------------
            // Zero interest
            // -----------------------------------------------------

            if (annualInterestRate <= 0)
            {
                return Math.Round(
                    principal / tenureMonths,
                    2
                );
            }

            // -----------------------------------------------------
            // Monthly interest rate
            // -----------------------------------------------------

            decimal monthlyRate =
                annualInterestRate / 12 / 100;

            double p =
                (double)principal;

            double r =
                (double)monthlyRate;

            double n =
                tenureMonths;

            double emi =
                p *
                r *
                Math.Pow(1 + r, n) /
                (
                    Math.Pow(1 + r, n) - 1
                );

            return Math.Round(
                (decimal)emi,
                2
            );
        }

        // =========================================================
        // GENERATE INSTALLMENTS
        // =========================================================

        private async Task GenerateInstallments(
            Emiplan emiplan,
            Loan loan,
            decimal principalAmount)
        {
            decimal outstandingPrincipal =
                principalAmount;

            decimal monthlyRate =
                loan.InterestRate / 12 / 100;

            decimal monthlyEmi =
                emiplan.MonthlyEmi;

            for (
                int month = 1;
                month <= emiplan.TotalEmis;
                month++
            )
            {
                // -------------------------------------------------
                // Interest
                // -------------------------------------------------

                decimal interestAmount =
                    Math.Round(
                        outstandingPrincipal *
                        monthlyRate,
                        2
                    );

                // -------------------------------------------------
                // Principal
                // -------------------------------------------------

                decimal principalComponent =
                    monthlyEmi -
                    interestAmount;

                // -------------------------------------------------
                // Last installment adjustment
                // -------------------------------------------------

                if (
                    month ==
                    emiplan.TotalEmis
                )
                {
                    principalComponent =
                        outstandingPrincipal;
                }

                if (principalComponent < 0)
                {
                    principalComponent = 0;
                }

                if (
                    principalComponent >
                    outstandingPrincipal
                )
                {
                    principalComponent =
                        outstandingPrincipal;
                }

                // -------------------------------------------------
                // Installment amount
                // -------------------------------------------------

                decimal installmentAmount =
                    principalComponent +
                    interestAmount;

                if (
                    month ==
                    emiplan.TotalEmis
                )
                {
                    installmentAmount =
                        outstandingPrincipal +
                        interestAmount;
                }

                // -------------------------------------------------
                // Due date
                // -------------------------------------------------

                DateOnly dueDate =
                    emiplan.EmistartDate
                        .AddMonths(month - 1);

                // -------------------------------------------------
                // Create installment
                // -------------------------------------------------

                var installment =
                    new Installment
                    {
                        EmiplanId =
                            emiplan.EmiplanId,

                        InstallmentNumber =
                            month,

                        DueDate =
                            dueDate,

                        InstallmentAmount =
                            Math.Round(
                                installmentAmount,
                                2
                            ),

                        PrincipalAmount =
                            Math.Round(
                                principalComponent,
                                2
                            ),

                        InterestAmount =
                            Math.Round(
                                interestAmount,
                                2
                            ),

                        LateFee = 0,

                        PaidAmount = 0,

                        PaymentDate = null,

                        PaymentMethod = null,

                        TransactionReference = null,

                        PaymentStatus =
                            "Pending",

                        Remarks = null,

                        CreatedDate =
                            DateTime.Now
                    };

                _context.Installments.Add(
                    installment
                );

                // -------------------------------------------------
                // Reduce outstanding principal
                // -------------------------------------------------

                outstandingPrincipal -=
                    principalComponent;

                if (outstandingPrincipal < 0)
                {
                    outstandingPrincipal = 0;
                }
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // LOAD LOANS
        // =========================================================

        private async Task LoadLoansAsync(
            int selectedLoanId = 0)
        {
            var loans =
                await _context.Loans
                    .OrderByDescending(
                        l => l.LoanId
                    )
                    .AsNoTracking()
                    .ToListAsync();

            ViewBag.Loans =
                loans.Select(
                    l => new SelectListItem
                    {
                        Value =
                            l.LoanId.ToString(),

                        Text =
                            $"{l.LoanNumber} | ₹{l.LoanAmount:N2} | {l.LoanStatus}",

                        Selected =
                            l.LoanId ==
                            selectedLoanId
                    }
                ).ToList();
        }

        // =========================================================
        // REMOVE NAVIGATION VALIDATION
        // =========================================================

        private void RemoveNavigationValidation()
        {
            ModelState.Remove("Loan");

            ModelState.Remove("Installments");
        }
    }
}