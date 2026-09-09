
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HousingAllotmentManagementSystem.Controllers
{
    [Authorize]
    public class ClientPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientPaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX - CLIENT PAYMENT PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();

            if (userId <= 0)
            {
                return Unauthorized();
            }

            // -----------------------------------------------------
            // PAYMENT HISTORY
            // -----------------------------------------------------

            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .Include(p => p.Installment)
                    .ThenInclude(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                .OrderByDescending(p => p.PaymentDate)
                .AsNoTracking()
                .ToListAsync();


            // -----------------------------------------------------
            // FIND ALL INSTALLMENTS BELONGING TO CURRENT CLIENT
            // -----------------------------------------------------

            var installments = await _context.Installments
                .Include(i => i.Emiplan)
                    .ThenInclude(e => e.Loan)
                        .ThenInclude(l => l.Allotment)
                            .ThenInclude(a => a.Application)
                .Where(i =>
                    i.Emiplan != null &&
                    i.Emiplan.Loan != null &&
                    i.Emiplan.Loan.Allotment != null &&
                    i.Emiplan.Loan.Allotment.Application != null &&
                    i.Emiplan.Loan.Allotment.Application.UserId == userId &&
                    i.PaymentStatus != "Paid")
                .OrderBy(i => i.DueDate)
                .AsNoTracking()
                .ToListAsync();


            // -----------------------------------------------------
            // MARK OVERDUE INSTALLMENTS FOR DISPLAY
            // -----------------------------------------------------

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var installment in installments)
            {
                if (installment.DueDate < today)
                {
                    installment.PaymentStatus = "Overdue";
                }
            }


            // -----------------------------------------------------
            // SEND PAYABLE INSTALLMENTS TO VIEW
            // -----------------------------------------------------

            ViewBag.PayableInstallments = installments;


            return View(payments);
        }


        // =========================================================
        // PAY - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Pay(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int userId = GetCurrentUserId();

            if (userId <= 0)
            {
                return Unauthorized();
            }


            // -----------------------------------------------------
            // LOAD INSTALLMENT
            // -----------------------------------------------------

            var installment = await _context.Installments
                .Include(i => i.Emiplan)
                    .ThenInclude(e => e.Loan)
                        .ThenInclude(l => l.Allotment)
                            .ThenInclude(a => a.Application)
                .FirstOrDefaultAsync(i =>
                    i.InstallmentId == id.Value &&
                    i.Emiplan.Loan.Allotment.Application.UserId == userId);


            // -----------------------------------------------------
            // INSTALLMENT NOT FOUND
            // -----------------------------------------------------

            if (installment == null)
            {
                TempData["Error"] =
                    "The selected installment could not be found.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // ALREADY PAID
            // -----------------------------------------------------

            if (installment.PaymentStatus == "Paid")
            {
                TempData["Error"] =
                    "This installment has already been paid.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // CALCULATE PAYABLE AMOUNT
            // -----------------------------------------------------

            decimal payableAmount =
                Math.Round(
                    installment.InstallmentAmount +
                    installment.LateFee,
                    2);


            // -----------------------------------------------------
            // CREATE PAYMENT MODEL
            // -----------------------------------------------------

            var payment = new Payment
            {
                InstallmentId = installment.InstallmentId,

                UserId = userId,

                PaymentType = "EMI",

                Amount = payableAmount,

                PaymentDate = DateTime.Now,

                PaymentMethod = string.Empty,

                PaymentStatus = "Completed",

                Remarks =
                    $"Payment for EMI #{installment.InstallmentNumber}",

                CreatedDate = DateTime.Now
            };


            ViewBag.Installment = installment;


            return View(payment);
        }


        // =========================================================
        // PAY - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(Payment payment)
        {
            int userId = GetCurrentUserId();

            if (userId <= 0)
            {
                return Unauthorized();
            }


            // -----------------------------------------------------
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // -----------------------------------------------------

            ModelState.Remove(nameof(Payment.User));
            ModelState.Remove(nameof(Payment.Installment));


            // -----------------------------------------------------
            // INSTALLMENT ID REQUIRED
            // -----------------------------------------------------

            if (!payment.InstallmentId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(payment.InstallmentId),
                    "Invalid installment.");
            }


            Installment? installment = null;


            // -----------------------------------------------------
            // LOAD INSTALLMENT AND VERIFY OWNERSHIP
            // -----------------------------------------------------

            if (payment.InstallmentId.HasValue)
            {
                installment = await _context.Installments
                    .Include(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Application)
                    .FirstOrDefaultAsync(i =>
                        i.InstallmentId == payment.InstallmentId.Value &&
                        i.Emiplan.Loan.Allotment.Application.UserId == userId);
            }


            // -----------------------------------------------------
            // VERIFY INSTALLMENT
            // -----------------------------------------------------

            if (installment == null)
            {
                ModelState.AddModelError(
                    "",
                    "The selected installment does not belong to your account.");
            }


            // -----------------------------------------------------
            // PREVENT DOUBLE PAYMENT
            // -----------------------------------------------------

            if (installment != null &&
                installment.PaymentStatus == "Paid")
            {
                ModelState.AddModelError(
                    "",
                    "This installment has already been paid.");
            }


            // -----------------------------------------------------
            // PAYMENT METHOD REQUIRED
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(payment.PaymentMethod))
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentMethod),
                    "Please select a payment method.");
            }


            // -----------------------------------------------------
            // TRANSACTION ID
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(payment.TransactionId))
            {
                payment.TransactionId =
                    payment.TransactionId.Trim();


                bool transactionExists =
                    await _context.Payments.AnyAsync(p =>
                        p.TransactionId == payment.TransactionId);

                if (transactionExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.TransactionId),
                        "This Transaction ID already exists.");
                }
            }


            // -----------------------------------------------------
            // ALWAYS CALCULATE AMOUNT FROM DATABASE
            // -----------------------------------------------------

            if (installment != null)
            {
                payment.Amount =
                    Math.Round(
                        installment.InstallmentAmount +
                        installment.LateFee,
                        2);
            }


            // -----------------------------------------------------
            // GENERATE RECEIPT NUMBER
            // -----------------------------------------------------

            payment.ReceiptNumber =
                GenerateReceiptNumber();


            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Installment = installment;

                return View(payment);
            }


            try
            {
                // =================================================
                // SET PAYMENT INFORMATION
                // =================================================

                payment.UserId = userId;

                payment.PaymentType = "EMI";

                payment.PaymentDate = DateTime.Now;

                payment.PaymentStatus = "Completed";

                payment.CreatedDate = DateTime.Now;


                // =================================================
                // UPDATE INSTALLMENT
                // =================================================

                installment!.PaidAmount =
                    Math.Round(payment.Amount, 2);

                installment.PaymentDate =
                    DateOnly.FromDateTime(payment.PaymentDate);

                installment.PaymentMethod =
                    payment.PaymentMethod;

                installment.TransactionReference =
                    payment.TransactionId;

                installment.PaymentStatus =
                    "Paid";

                installment.Remarks =
                    payment.Remarks;


                // =================================================
                // ADD PAYMENT
                // =================================================

                _context.Payments.Add(payment);


                // =================================================
                // LOAD EMI PLAN
                // =================================================

                var emiPlan =
                    await _context.Emiplans
                        .Include(e => e.Installments)
                        .Include(e => e.Loan)
                        .FirstOrDefaultAsync(e =>
                            e.EmiplanId == installment.EmiplanId);


                if (emiPlan != null)
                {
                    // -------------------------------------------------
                    // PAID EMI COUNT
                    // -------------------------------------------------

                    int paidEmis =
                        emiPlan.Installments.Count(i =>
                            i.PaymentStatus == "Paid");


                    emiPlan.PaidEmis =
                        paidEmis;


                    // -------------------------------------------------
                    // REMAINING EMI COUNT
                    // -------------------------------------------------

                    emiPlan.RemainingEmis =
                        Math.Max(
                            emiPlan.TotalEmis -
                            paidEmis,
                            0);


                    // -------------------------------------------------
                    // OUTSTANDING PRINCIPAL
                    // -------------------------------------------------

                    decimal outstanding =
                        emiPlan.Installments
                            .Where(i =>
                                i.PaymentStatus != "Paid")
                            .Sum(i =>
                                i.PrincipalAmount);


                    emiPlan.OutstandingBalance =
                        Math.Round(
                            Math.Max(outstanding, 0),
                            2);


                    // -------------------------------------------------
                    // NEXT DUE DATE
                    // -------------------------------------------------

                    var nextInstallment =
                        emiPlan.Installments
                            .Where(i =>
                                i.PaymentStatus != "Paid")
                            .OrderBy(i =>
                                i.InstallmentNumber)
                            .FirstOrDefault();


                    if (nextInstallment != null)
                    {
                        emiPlan.NextDueDate =
                            nextInstallment.DueDate;
                    }
                    else
                    {
                        emiPlan.NextDueDate =
                            emiPlan.EmiendDate;
                    }


                    // -------------------------------------------------
                    // EMI PLAN STATUS
                    // -------------------------------------------------

                    emiPlan.PlanStatus =
                        emiPlan.RemainingEmis <= 0
                            ? "Completed"
                            : "Active";


                    // -------------------------------------------------
                    // LOAN STATUS
                    // -------------------------------------------------

                    if (emiPlan.RemainingEmis <= 0 &&
                        emiPlan.Loan != null)
                    {
                        emiPlan.Loan.LoanStatus =
                            "Completed";
                    }
                }


                // =================================================
                // SAVE
                // =================================================

                await _context.SaveChangesAsync();


                // =================================================
                // SUCCESS MESSAGE
                // =================================================

                TempData["Success"] =
                    $"EMI #{installment.InstallmentNumber} payment completed successfully.";


                // =================================================
                // REDIRECT TO PAYMENT DETAILS
                // =================================================

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = payment.PaymentId
                    });
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error while processing payment: " +
                    (ex.InnerException?.Message ??
                     ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while processing payment: " +
                    ex.Message);
            }


            ViewBag.Installment = installment;

            return View(payment);
        }


        // =========================================================
        // PAYMENT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int userId = GetCurrentUserId();

            if (userId <= 0)
            {
                return Unauthorized();
            }


            var payment =
                await _context.Payments

                    .Include(p => p.User)

                    .Include(p => p.Installment)
                        .ThenInclude(i => i.Emiplan)
                            .ThenInclude(e => e.Loan)

                    .FirstOrDefaultAsync(p =>
                        p.PaymentId == id.Value &&
                        p.UserId == userId);


            if (payment == null)
            {
                return NotFound();
            }


            return View(payment);
        }


        // =========================================================
        // GET CURRENT LOGGED-IN USER ID
        // =========================================================

        private int GetCurrentUserId()
        {
            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                claim =
                    User.FindFirst("UserId");
            }

            if (claim == null)
            {
                return 0;
            }

            return int.TryParse(
                claim.Value,
                out int userId)
                ? userId
                : 0;
        }


        // =========================================================
        // GENERATE RECEIPT NUMBER
        // =========================================================

        private string GenerateReceiptNumber()
        {
            return
                "REC-" +
                DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }
    }
}
