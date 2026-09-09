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

                .Include(x => x.EMIPlanOption)

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
                        .ThenInclude(a => a.Property)
                            .ThenInclude(p => p.Scheme)

                    .Include(x => x.EMIPlanOption)

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
            // -----------------------------------------------------
            // Get Loan Application
            // -----------------------------------------------------

            var application =
                await _context.LoanApplications

                    .Include(x => x.EMIPlanOption)

                    .Include(x => x.Allotment)
                        .ThenInclude(a => a.Property)

                    .FirstOrDefaultAsync(x =>
                        x.LoanApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Only Pending Applications
            // -----------------------------------------------------

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending loan applications can be approved.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Validate EMI Plan
            // -----------------------------------------------------

            if (application.EMIPlanOptionId == null ||
                application.EMIPlanOption == null)
            {
                TempData["ErrorMessage"] =
                    "No EMI plan has been selected for this loan application.";

                return RedirectToAction(nameof(Index));
            }


            var emiPlanOption =
                application.EMIPlanOption;


            // -----------------------------------------------------
            // Validate EMI Plan belongs to Property Scheme
            // -----------------------------------------------------

            if (application.Allotment?.Property == null)
            {
                TempData["ErrorMessage"] =
                    "Property information could not be found.";

                return RedirectToAction(nameof(Index));
            }


            if (emiPlanOption.SchemeId !=
                application.Allotment.Property.SchemeId)
            {
                TempData["ErrorMessage"] =
                    "The selected EMI plan does not belong to this property's housing scheme.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Calculate Actual Loan Amount
            // -----------------------------------------------------
            //
            // Requested Amount = 10,00,000
            // Down Payment     = 2,00,000
            // Actual Loan      = 8,00,000
            //
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
            // Get Interest Rate and Tenure from Selected EMI Plan
            // -----------------------------------------------------

            decimal interestRate =
                emiPlanOption.InterestRate;

            int loanTenure =
                emiPlanOption.TenureMonths;


            if (interestRate < 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid interest rate in the selected EMI plan.";

                return RedirectToAction(nameof(Index));
            }


            if (loanTenure <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid tenure in the selected EMI plan.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Calculate Monthly EMI
            // -----------------------------------------------------

            decimal monthlyRate =
                interestRate /
                12m /
                100m;

            decimal emi;


            if (monthlyRate == 0)
            {
                emi =
                    principal /
                    loanTenure;
            }
            else
            {
                double p =
                    (double)principal;

                double r =
                    (double)monthlyRate;

                double n =
                    loanTenure;

                double result =
                    p *
                    r *
                    Math.Pow(1 + r, n)
                    /
                    (Math.Pow(1 + r, n) - 1);

                emi =
                    (decimal)result;
            }


            emi =
                Math.Round(
                    emi,
                    2);


            // -----------------------------------------------------
            // Generate Unique Loan Number
            // -----------------------------------------------------

            string loanNumber;

            do
            {
                loanNumber =
                    "LN" +
                    DateTime.Now.ToString(
                        "yyyyMMddHHmmssfff");
            }
            while (
                await _context.Loans
                    .AnyAsync(l =>
                        l.LoanNumber == loanNumber)
            );


            // -----------------------------------------------------
            // Check Existing Loan
            // -----------------------------------------------------

            var existingLoan =
                await _context.Loans
                    .FirstOrDefaultAsync(l =>
                        l.AllotmentId ==
                        application.AllotmentId);


            if (existingLoan != null)
            {
                TempData["ErrorMessage"] =
                    "A loan already exists for this allotment.";

                return RedirectToAction(nameof(Index));
            }


            // =====================================================
            // DATABASE TRANSACTION
            // =====================================================

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // =================================================
                // 1. CREATE LOAN
                // =================================================

                var loan = new Loan
                {
                    AllotmentId =
                        application.AllotmentId,

                    LoanNumber =
                        loanNumber,

                    // IMPORTANT:
                    // Store the actual financed amount
                    // after down payment.
                    LoanAmount =
                        principal,

                    DownPayment =
                        application.DownPayment,

                    InterestRate =
                        interestRate,

                    LoanTenure =
                        loanTenure,

                    Emiamount =
                        emi,

                    SanctionDate =
                        DateOnly.FromDateTime(
                            DateTime.Today),

                    LoanStatus =
                        "Active",

                    CreatedDate =
                        DateTime.Now
                };


                _context.Loans.Add(loan);

                await _context.SaveChangesAsync();


                // =================================================
                // 2. CREATE EMI PLAN
                // =================================================

                // First EMI will be due next month.
                DateOnly emiStartDate =
                    DateOnly.FromDateTime(
                        DateTime.Today.AddMonths(1));


                DateOnly emiEndDate =
                    emiStartDate.AddMonths(
                        loanTenure - 1);


                var emiplan = new Emiplan
                {
                    LoanId =
                        loan.LoanId,

                    EmistartDate =
                        emiStartDate,

                    EmiendDate =
                        emiEndDate,

                    TotalEmis =
                        loanTenure,

                    PaidEmis =
                        0,

                    RemainingEmis =
                        loanTenure,

                    MonthlyEmi =
                        emi,

                    OutstandingBalance =
                        principal,

                    NextDueDate =
                        emiStartDate,

                    PlanStatus =
                        "Active",

                    CreatedDate =
                        DateTime.Now
                };


                _context.Emiplans.Add(emiplan);

                await _context.SaveChangesAsync();


                // =================================================
                // 3. GENERATE INSTALLMENTS
                // =================================================

                decimal outstanding =
                    principal;


                for (int i = 1;
                     i <= loanTenure;
                     i++)
                {
                    decimal interestAmount =
                        Math.Round(
                            outstanding *
                            monthlyRate,
                            2);


                    decimal principalAmount;


                    decimal installmentAmount;


                    // -------------------------------------------------
                    // Last installment
                    // -------------------------------------------------

                    if (i == loanTenure)
                    {
                        principalAmount =
                            outstanding;

                        installmentAmount =
                            Math.Round(
                                principalAmount +
                                interestAmount,
                                2);
                    }
                    else
                    {
                        installmentAmount =
                            emi;

                        principalAmount =
                            Math.Round(
                                installmentAmount -
                                interestAmount,
                                2);

                        // Safety check
                        if (principalAmount < 0)
                        {
                            principalAmount = 0;
                        }
                    }


                    // -------------------------------------------------
                    // Prevent outstanding from becoming negative
                    // -------------------------------------------------

                    if (principalAmount >
                        outstanding)
                    {
                        principalAmount =
                            outstanding;

                        installmentAmount =
                            Math.Round(
                                principalAmount +
                                interestAmount,
                                2);
                    }


                    var installment =
                        new Installment
                        {
                            EmiplanId =
                                emiplan.EmiplanId,

                            InstallmentNumber =
                                i,

                            DueDate =
                                emiStartDate.AddMonths(
                                    i - 1),

                            InstallmentAmount =
                                installmentAmount,

                            PrincipalAmount =
                                principalAmount,

                            InterestAmount =
                                interestAmount,

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


                    outstanding =
                        Math.Round(
                            outstanding -
                            principalAmount,
                            2);


                    if (outstanding < 0)
                    {
                        outstanding = 0;
                    }
                }


                await _context.SaveChangesAsync();


                // =================================================
                // 4. UPDATE LOAN APPLICATION
                // =================================================

                application.Status =
                    "Approved";

                application.ReviewedDate =
                    DateTime.Now;

                application.Remarks =
                    "Loan application approved. Loan, EMI plan and installments created successfully.";


                await _context.SaveChangesAsync();


                // =================================================
                // COMMIT TRANSACTION
                // =================================================

                await transaction.CommitAsync();


                TempData["SuccessMessage"] =
                    "Loan approved successfully. Loan, EMI plan and installments have been created.";

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