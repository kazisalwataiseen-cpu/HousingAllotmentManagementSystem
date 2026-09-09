using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    [Authorize]
    public class ClientEmiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientEmiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: ClientEmi
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var emiPlans = await _context.Emiplans
                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Application)
                            .ThenInclude(app => app.User)

                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Property)

                .Include(e => e.Installments
                    .OrderBy(i => i.InstallmentNumber))

                .Where(e =>
                    e.Loan.Allotment.Application.UserId == userId.Value)

                .OrderByDescending(e => e.EmiplanId)

                .AsNoTracking()

                .ToListAsync();

            return View(
                "~/Views/ClientEmi/Index.cshtml",
                emiPlans);
        }


        // =========================================================
        // GET: ClientEmi/Details/5
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
                return Unauthorized();
            }

            var emiPlan = await _context.Emiplans

                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Application)
                            .ThenInclude(app => app.User)

                .Include(e => e.Loan)
                    .ThenInclude(l => l.Allotment)
                        .ThenInclude(a => a.Property)
                            .ThenInclude(p => p.Scheme)

                .Include(e => e.Installments
                    .OrderBy(i => i.InstallmentNumber))

                .AsNoTracking()

                .FirstOrDefaultAsync(e =>
                    e.EmiplanId == id.Value &&
                    e.Loan.Allotment.Application.UserId == userId.Value);

            if (emiPlan == null)
            {
                return NotFound();
            }

            return View(
                "~/Views/ClientEmi/Details.cshtml",
                emiPlan);
        }


        // =========================================================
        // GET: ClientEmi/Pay/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Pay(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            // -----------------------------------------------------
            // Get installment belonging to logged-in client
            // -----------------------------------------------------

            var installment = await _context.Installments

                .Include(i => i.Emiplan)
                    .ThenInclude(e => e.Loan)
                        .ThenInclude(l => l.Allotment)
                            .ThenInclude(a => a.Application)

                .FirstOrDefaultAsync(i =>
                    i.InstallmentId == id.Value &&
                    i.Emiplan.Loan.Allotment.Application.UserId
                        == userId.Value);

            if (installment == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Already paid?
            // -----------------------------------------------------

            if (installment.PaymentStatus == "Paid" ||
                installment.PaidAmount >= installment.InstallmentAmount)
            {
                TempData["Error"] =
                    "This EMI installment has already been paid.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = installment.EmiplanId
                    });
            }

            return View(
                "~/Views/ClientEmi/Pay.cshtml",
                installment);
        }


        // =========================================================
        // POST: ClientEmi/Pay
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
            int id,
            string paymentMethod)
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            // -----------------------------------------------------
            // Validate payment method
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Error"] =
                    "Please select a payment method.";

                return RedirectToAction(
                    nameof(Pay),
                    new { id });
            }

            // -----------------------------------------------------
            // Load installment securely
            // -----------------------------------------------------

            var installment = await _context.Installments

                .Include(i => i.Emiplan)
                    .ThenInclude(e => e.Loan)
                        .ThenInclude(l => l.Allotment)
                            .ThenInclude(a => a.Application)

                .FirstOrDefaultAsync(i =>
                    i.InstallmentId == id &&
                    i.Emiplan.Loan.Allotment.Application.UserId
                        == userId.Value);

            if (installment == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Prevent duplicate payment
            // -----------------------------------------------------

            if (installment.PaymentStatus == "Paid" ||
                installment.PaidAmount >= installment.InstallmentAmount)
            {
                TempData["Error"] =
                    "This EMI installment has already been paid.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = installment.EmiplanId
                    });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // =================================================
                // PAYMENT AMOUNT
                // =================================================

                decimal amountToPay =
                    installment.InstallmentAmount +
                    installment.LateFee -
                    installment.PaidAmount;

                if (amountToPay <= 0)
                {
                    TempData["Error"] =
                        "Invalid payment amount.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = installment.EmiplanId
                        });
                }


                // =================================================
                // GENERATE PAYMENT DETAILS
                // =================================================

                string transactionId =
                    "TXN" +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff");

                string receiptNumber =
                    "REC-" +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff");


                // =================================================
                // CREATE PAYMENT
                // =================================================

                var payment = new Payment
                {
                    InstallmentId =
                        installment.InstallmentId,

                    UserId =
                        userId.Value,

                    PaymentType =
                        "EMI",

                    PaymentDate =
                        DateTime.Now,

                    Amount =
                        amountToPay,

                    PaymentMethod =
                        paymentMethod,

                    TransactionId =
                        transactionId,

                    ReceiptNumber =
                        receiptNumber,

                    PaymentStatus =
                        "Completed",

                    Remarks =
                        $"Payment for EMI #{installment.InstallmentNumber}",

                    CreatedDate =
                        DateTime.Now
                };


                _context.Payments.Add(payment);


                // =================================================
                // UPDATE INSTALLMENT
                // =================================================

                installment.PaidAmount =
                    installment.InstallmentAmount +
                    installment.LateFee;

                installment.PaymentDate =
                    DateOnly.FromDateTime(DateTime.Today);

                installment.PaymentMethod =
                    paymentMethod;

                installment.TransactionReference =
                    transactionId;

                installment.PaymentStatus =
                    "Paid";

                installment.Remarks =
                    "EMI payment completed successfully.";


                // =================================================
                // UPDATE EMI PLAN
                // =================================================

                var emiPlan =
                    installment.Emiplan;

                // Count already paid installments plus this current one
                int previouslyPaid =
                    await _context.Installments
                        .CountAsync(i =>
                            i.EmiplanId == emiPlan.EmiplanId &&
                            i.PaymentStatus == "Paid" &&
                            i.InstallmentId != installment.InstallmentId);

                emiPlan.PaidEmis = previouslyPaid + 1;

                emiPlan.RemainingEmis =
                    Math.Max(
                        0,
                        emiPlan.TotalEmis -
                        emiPlan.PaidEmis);


                // -------------------------------------------------
                // Outstanding balance: sum of remaining unpaid principals
                // -------------------------------------------------

                decimal remainingPrincipal =
                    await _context.Installments
                        .Where(i =>
                            i.EmiplanId == emiPlan.EmiplanId &&
                            i.PaymentStatus != "Paid" &&
                            i.InstallmentId != installment.InstallmentId)
                        .SumAsync(i => i.PrincipalAmount);

                emiPlan.OutstandingBalance =
                    Math.Round(Math.Max(0, remainingPrincipal), 2);


                // =================================================
                // FIND NEXT UNPAID INSTALLMENT
                // =================================================

                var nextInstallment =
                    await _context.Installments
                        .Where(i =>
                            i.EmiplanId ==
                                emiPlan.EmiplanId &&
                            i.PaymentStatus != "Paid" &&
                            i.InstallmentId !=
                                installment.InstallmentId)
                        .OrderBy(i =>
                            i.InstallmentNumber)
                        .FirstOrDefaultAsync();


                if (nextInstallment != null)
                {
                    emiPlan.NextDueDate =
                        nextInstallment.DueDate;

                    emiPlan.PlanStatus =
                        "Active";
                }
                else
                {
                    // -------------------------------------------------
                    // All installments paid
                    // -------------------------------------------------

                    emiPlan.NextDueDate =
                        emiPlan.EmiendDate;

                    emiPlan.RemainingEmis =
                        0;

                    emiPlan.OutstandingBalance =
                        0;

                    emiPlan.PlanStatus =
                        "Completed";

                    // Also mark the loan as completed
                    if (emiPlan.Loan != null)
                    {
                        emiPlan.Loan.LoanStatus =
                            "Completed";
                    }
                }


                // =================================================
                // SAVE EVERYTHING & COMMIT TRANSACTION
                // =================================================

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                TempData["Success"] =
                    $"EMI #{installment.InstallmentNumber} payment completed successfully. " +
                    $"Receipt: {receiptNumber}";


                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = emiPlan.EmiplanId
                    });
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] =
                    "Payment could not be completed: " +
                    (ex.InnerException?.Message ??
                     ex.Message);

                return RedirectToAction(
                    nameof(Pay),
                    new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "An error occurred while processing payment: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Pay),
                    new { id });
            }
        }


        // =========================================================
        // GET LOGGED-IN USER ID
        // =========================================================

        private int? GetLoggedInUserId()
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
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