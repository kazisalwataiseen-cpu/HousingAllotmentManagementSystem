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
    // /Emiplans
    // /Emiplans/Details
    // /Emiplans/Create
    // /Emiplans/Edit
    // /Emiplans/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class EmiplansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmiplansController(
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
            var emiPlans = await _context.Emiplans
                .Include(e => e.Loan)
                .Include(e => e.Installments)
                .OrderByDescending(e => e.EmiplanId)
                .AsNoTracking()
                .ToListAsync();

            return View(emiPlans);
        }


        // =========================================================
        // DETAILS - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int? emiplanid)
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
                    e.EmiplanId == emiplanid);

            if (emiPlan == null)
            {
                return NotFound();
            }

            return View(emiPlan);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadLoans();

            var emiPlan = new Emiplan
            {
                EmistartDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                PlanStatus =
                    "Active",

                PaidEmis =
                    0,

                RemainingEmis =
                    0,

                CreatedDate =
                    DateTime.Now
            };

            return View(emiPlan);
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Emiplan emiplan)
        {
            ModelState.Remove("Loan");
            ModelState.Remove("Installments");


            // -----------------------------------------------------
            // LOAN VALIDATION
            // -----------------------------------------------------

            if (emiplan.LoanId <= 0)
            {
                ModelState.AddModelError(
                    "LoanId",
                    "Please select a loan.");
            }

            var loan =
                await _context.Loans
                    .FirstOrDefaultAsync(l =>
                        l.LoanId ==
                        emiplan.LoanId);

            if (loan == null)
            {
                ModelState.AddModelError(
                    "LoanId",
                    "Selected loan does not exist.");
            }


            // -----------------------------------------------------
            // TOTAL EMI VALIDATION
            // -----------------------------------------------------

            if (emiplan.TotalEmis <= 0)
            {
                ModelState.AddModelError(
                    "TotalEmis",
                    "Total EMIs must be greater than 0.");
            }


            // -----------------------------------------------------
            // START DATE VALIDATION
            // -----------------------------------------------------

            if (emiplan.EmistartDate == default)
            {
                ModelState.AddModelError(
                    "EmistartDate",
                    "Please select EMI start date.");
            }


            // -----------------------------------------------------
            // LOAN TENURE CHECK
            // -----------------------------------------------------

            if (loan != null &&
                loan.LoanTenure > 0 &&
                emiplan.TotalEmis >
                loan.LoanTenure)
            {
                ModelState.AddModelError(
                    "TotalEmis",
                    $"Total EMIs cannot be greater than the loan tenure of {loan.LoanTenure} months.");
            }


            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadLoans(
                    emiplan.LoanId);

                return View(emiplan);
            }


            try
            {
                // =================================================
                // CALCULATE PRINCIPAL
                // =================================================

                decimal principalAmount =
                    loan!.LoanAmount -
                    loan.DownPayment;

                if (principalAmount < 0)
                {
                    principalAmount = 0;
                }


                // =================================================
                // CALCULATE MONTHLY RATE
                // =================================================

                decimal monthlyRate =
                    loan.InterestRate /
                    12m /
                    100m;


                decimal monthlyEmi;


                if (loan.InterestRate > 0 &&
                    monthlyRate > 0)
                {
                    decimal power =
                        (decimal)Math.Pow(
                            (double)(
                                1 + monthlyRate),
                            emiplan.TotalEmis);

                    monthlyEmi =
                        principalAmount *
                        monthlyRate *
                        power /
                        (power - 1);
                }
                else
                {
                    monthlyEmi =
                        principalAmount /
                        emiplan.TotalEmis;
                }


                // -------------------------------------------------
                // MANUAL EMI
                // -------------------------------------------------

                if (emiplan.MonthlyEmi > 0)
                {
                    monthlyEmi =
                        emiplan.MonthlyEmi;
                }


                // =================================================
                // SET EMI PLAN VALUES
                // =================================================

                emiplan.MonthlyEmi =
                    Math.Round(
                        monthlyEmi,
                        2);

                emiplan.PaidEmis =
                    0;

                emiplan.RemainingEmis =
                    emiplan.TotalEmis;

                emiplan.OutstandingBalance =
                    Math.Round(
                        principalAmount,
                        2);

                emiplan.NextDueDate =
                    emiplan.EmistartDate;

                emiplan.EmiendDate =
                    emiplan.EmistartDate
                        .AddMonths(
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

                _context.Emiplans.Add(
                    emiplan);

                await _context.SaveChangesAsync();


                // =================================================
                // GENERATE INSTALLMENTS
                // =================================================

                await GenerateInstallments(
                    emiplan,
                    loan,
                    principalAmount);

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    $"EMI Plan created successfully. {emiplan.TotalEmis} installments generated.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                string message =
                    ex.InnerException?.Message ??
                    ex.Message;

                ModelState.AddModelError(
                    "",
                    "Database error while saving EMI Plan: " +
                    message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while creating EMI Plan: " +
                    ex.Message);
            }


            LoadLoans(
                emiplan.LoanId);

            return View(emiplan);
        }


        // =========================================================
        // GENERATE INSTALLMENTS
        // =========================================================

        private async Task GenerateInstallments(
            Emiplan emiPlan,
            Loan loan,
            decimal principalAmount)
        {
            decimal outstanding =
                principalAmount;

            decimal monthlyRate =
                loan.InterestRate /
                12m /
                100m;


            for (
                int i = 1;
                i <= emiPlan.TotalEmis;
                i++)
            {
                decimal interestAmount =
                    Math.Round(
                        outstanding *
                        monthlyRate,
                        2);

                decimal installmentAmount =
                    emiPlan.MonthlyEmi;

                decimal principalPayment =
                    installmentAmount -
                    interestAmount;


                // -------------------------------------------------
                // LAST INSTALLMENT ADJUSTMENT
                // -------------------------------------------------

                if (i ==
                    emiPlan.TotalEmis)
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


                if (principalPayment >
                    outstanding)
                {
                    principalPayment =
                        outstanding;
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
                                .AddMonths(
                                    i - 1),

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

                        LateFee =
                            0,

                        PaidAmount =
                            0,

                        PaymentDate =
                            null,

                        PaymentMethod =
                            null,

                        TransactionReference =
                            null,

                        PaymentStatus =
                            "Pending",

                        Remarks =
                            null,

                        CreatedDate =
                            DateTime.Now
                    };

                _context.Installments.Add(
                    installment);
            }

            await Task.CompletedTask;
        }


        // =========================================================
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int? emiplanid)
        {
            if (emiplanid == null)
            {
                return NotFound();
            }

            var emiPlan =
                await _context.Emiplans
                    .Include(e => e.Loan)
                    .FirstOrDefaultAsync(e =>
                        e.EmiplanId ==
                        emiplanid);

            if (emiPlan == null)
            {
                return NotFound();
            }

            LoadLoans(
                emiPlan.LoanId);

            return View(emiPlan);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int emiplanid,
            Emiplan emiplan)
        {
            if (emiplanid !=
                emiplan.EmiplanId)
            {
                return NotFound();
            }


            ModelState.Remove("Loan");
            ModelState.Remove("Installments");


            if (emiplan.LoanId <= 0)
            {
                ModelState.AddModelError(
                    "LoanId",
                    "Please select a loan.");
            }


            if (emiplan.TotalEmis <= 0)
            {
                ModelState.AddModelError(
                    "TotalEmis",
                    "Total EMIs must be greater than 0.");
            }


            if (!ModelState.IsValid)
            {
                LoadLoans(
                    emiplan.LoanId);

                return View(emiplan);
            }


            try
            {
                var existing =
                    await _context.Emiplans
                        .FirstOrDefaultAsync(e =>
                            e.EmiplanId ==
                            emiplanid);

                if (existing == null)
                {
                    return NotFound();
                }


                var loan =
                    await _context.Loans
                        .FirstOrDefaultAsync(l =>
                            l.LoanId ==
                            emiplan.LoanId);

                if (loan == null)
                {
                    ModelState.AddModelError(
                        "LoanId",
                        "Selected loan does not exist.");

                    LoadLoans(
                        emiplan.LoanId);

                    return View(emiplan);
                }


                // -------------------------------------------------
                // CHECK PAYMENTS
                // -------------------------------------------------

                var installments =
                    await _context.Installments
                        .Where(i =>
                            i.EmiplanId ==
                            existing.EmiplanId)
                        .ToListAsync();


                bool hasPayments =
                    installments.Any(i =>
                        i.PaidAmount > 0 ||
                        i.PaymentStatus ==
                        "Paid");


                if (hasPayments)
                {
                    ModelState.AddModelError(
                        "",
                        "This EMI Plan cannot be regenerated because payments already exist. Edit only the plan status.");

                    LoadLoans(
                        emiplan.LoanId);

                    return View(emiplan);
                }


                // -------------------------------------------------
                // DELETE OLD INSTALLMENTS
                // -------------------------------------------------

                if (installments.Any())
                {
                    _context.Installments
                        .RemoveRange(
                            installments);
                }


                // -------------------------------------------------
                // PRINCIPAL
                // -------------------------------------------------

                decimal principalAmount =
                    loan.LoanAmount -
                    loan.DownPayment;

                if (principalAmount < 0)
                {
                    principalAmount = 0;
                }


                decimal monthlyRate =
                    loan.InterestRate /
                    12m /
                    100m;


                decimal monthlyEmi;


                if (loan.InterestRate > 0 &&
                    monthlyRate > 0)
                {
                    decimal power =
                        (decimal)Math.Pow(
                            (double)(
                                1 + monthlyRate),
                            emiplan.TotalEmis);

                    monthlyEmi =
                        principalAmount *
                        monthlyRate *
                        power /
                        (power - 1);
                }
                else
                {
                    monthlyEmi =
                        principalAmount /
                        emiplan.TotalEmis;
                }


                if (emiplan.MonthlyEmi > 0)
                {
                    monthlyEmi =
                        emiplan.MonthlyEmi;
                }


                // -------------------------------------------------
                // UPDATE PLAN
                // -------------------------------------------------

                existing.LoanId =
                    emiplan.LoanId;

                existing.EmistartDate =
                    emiplan.EmistartDate;

                existing.TotalEmis =
                    emiplan.TotalEmis;

                existing.PaidEmis =
                    0;

                existing.RemainingEmis =
                    emiplan.TotalEmis;

                existing.MonthlyEmi =
                    Math.Round(
                        monthlyEmi,
                        2);

                existing.OutstandingBalance =
                    Math.Round(
                        principalAmount,
                        2);

                existing.EmiendDate =
                    emiplan.EmistartDate
                        .AddMonths(
                            emiplan.TotalEmis - 1);

                existing.NextDueDate =
                    emiplan.EmistartDate;

                existing.PlanStatus =
                    string.IsNullOrWhiteSpace(
                        emiplan.PlanStatus)
                    ? "Active"
                    : emiplan.PlanStatus;


                // -------------------------------------------------
                // GENERATE NEW INSTALLMENTS
                // -------------------------------------------------

                await GenerateInstallments(
                    existing,
                    loan,
                    principalAmount);

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "EMI Plan and installments updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmiplanExists(
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
                    (ex.InnerException?.Message ??
                     ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while updating EMI Plan: " +
                    ex.Message);
            }


            LoadLoans(
                emiplan.LoanId);

            return View(emiplan);
        }


        // =========================================================
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int? emiplanid)
        {
            if (emiplanid == null)
            {
                return NotFound();
            }

            var emiPlan =
                await _context.Emiplans
                    .Include(e => e.Loan)
                    .Include(e => e.Installments)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.EmiplanId ==
                        emiplanid);

            if (emiPlan == null)
            {
                return NotFound();
            }

            return View(emiPlan);
        }


        // =========================================================
        // DELETE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int emiplanid)
        {
            var emiPlan =
                await _context.Emiplans
                    .FirstOrDefaultAsync(e =>
                        e.EmiplanId ==
                        emiplanid);

            if (emiPlan == null)
            {
                return NotFound();
            }


            try
            {
                // -------------------------------------------------
                // REMOVE INSTALLMENTS FIRST
                // -------------------------------------------------

                var installments =
                    await _context.Installments
                        .Where(i =>
                            i.EmiplanId ==
                            emiplanid)
                        .ToListAsync();


                if (installments.Any())
                {
                    _context.Installments
                        .RemoveRange(
                            installments);
                }


                // -------------------------------------------------
                // REMOVE EMI PLAN
                // -------------------------------------------------

                _context.Emiplans.Remove(
                    emiPlan);

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "EMI Plan and its installments deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This EMI Plan cannot be deleted because it is linked with another record.";
            }


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // LOAD LOAN DROPDOWN
        // =========================================================

        private void LoadLoans(
            int? selectedLoanId = null)
        {
            var loans =
                _context.Loans
                    .AsNoTracking()
                    .OrderByDescending(
                        l => l.LoanId)
                    .ToList();


            var loanList =
                loans.Select(l =>
                    new
                    {
                        LoanId =
                            l.LoanId,

                        DisplayText =
                            l.LoanNumber +
                            " | ₹" +
                            l.LoanAmount
                                .ToString("N2") +
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
        // EXISTS
        // =========================================================

        private bool EmiplanExists(
            int emiplanid)
        {
            return _context.Emiplans
                .Any(e =>
                    e.EmiplanId ==
                    emiplanid);
        }
    }
}