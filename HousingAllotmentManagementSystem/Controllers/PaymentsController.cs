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
// /Payments
// /Payments/Details
// /Payments/Create
// /Payments/Edit
// /Payments/Delete
//
// Clients should use their own client-side payment controller.
//
// =========================================================

[Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX - ADMIN ONLY
        // =========================================================
        //
        // Loads:
        //
        // Payment
        //   -> User
        //
        // Payment
        //   -> Installment
        //       -> EMI Plan
        //           -> Loan
        //               -> Allotment
        //                   -> Application
        //                       -> User
        //                   -> Property
        //
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments

                // Direct payment user
                .Include(p => p.User)

                // Installment -> EMI -> Loan -> Allotment -> Application -> User
                .Include(p => p.Installment)
                    .ThenInclude(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Application)
                                    .ThenInclude(app => app.User)

                // Installment -> EMI -> Loan -> Allotment -> Property -> Scheme
                .Include(p => p.Installment)
                    .ThenInclude(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Property)
                                    .ThenInclude(property => property.Scheme)

                .OrderByDescending(p => p.PaymentId)

                .AsNoTracking()

                .ToListAsync();

            return View(payments);
        }


        // =========================================================
        // DETAILS - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments

                .Include(p => p.User)

                .Include(p => p.Installment)
                    .ThenInclude(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Application)
                                    .ThenInclude(app => app.User)

                .Include(p => p.Installment)
                    .ThenInclude(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Property)
                                    .ThenInclude(property => property.Scheme)

                .AsNoTracking()

                .FirstOrDefaultAsync(p =>
                    p.PaymentId == id.Value);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropDowns();

            var payment = new Payment
            {
                PaymentDate =
                    DateTime.Now,

                PaymentStatus =
                    "Pending",

                CreatedDate =
                    DateTime.Now
            };

            return View(payment);
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Payment payment)
        {
            // Navigation properties are not submitted
            ModelState.Remove(nameof(Payment.User));
            ModelState.Remove(nameof(Payment.Installment));


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (payment.UserId <= 0)
            {
                ModelState.AddModelError(
                    nameof(payment.UserId),
                    "Please select a client.");
            }
            else
            {
                bool userExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.UserId ==
                            payment.UserId);

                if (!userExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.UserId),
                        "Selected client does not exist.");
                }
            }


            // -----------------------------------------------------
            // INSTALLMENT VALIDATION
            // -----------------------------------------------------

            if (payment.InstallmentId.HasValue &&
                payment.InstallmentId.Value > 0)
            {
                var installment =
                    await _context.Installments

                        .Include(i => i.Emiplan)
                            .ThenInclude(e => e.Loan)
                                .ThenInclude(l => l.Allotment)
                                    .ThenInclude(a => a.Application)

                        .FirstOrDefaultAsync(i =>
                            i.InstallmentId ==
                            payment.InstallmentId.Value);

                if (installment == null)
                {
                    ModelState.AddModelError(
                        nameof(payment.InstallmentId),
                        "Selected installment does not exist.");
                }
                else
                {
                    // -------------------------------------------------
                    // VERIFY CLIENT MATCHES INSTALLMENT'S LOAN CLIENT
                    // -------------------------------------------------

                    var installmentUserId =
                        installment.Emiplan?
                            .Loan?
                            .Allotment?
                            .Application?
                            .UserId;

                    if (installmentUserId.HasValue &&
                        installmentUserId.Value != payment.UserId)
                    {
                        ModelState.AddModelError(
                            nameof(payment.UserId),
                            "Selected client does not belong to the selected installment.");
                    }
                }
            }


            // -----------------------------------------------------
            // TRANSACTION ID DUPLICATE CHECK
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                payment.TransactionId))
            {
                bool transactionExists =
                    await _context.Payments
                        .AnyAsync(p =>
                            p.TransactionId ==
                            payment.TransactionId);

                if (transactionExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.TransactionId),
                        "This Transaction ID already exists.");
                }
            }


            // -----------------------------------------------------
            // RECEIPT NUMBER DUPLICATE CHECK
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                payment.ReceiptNumber))
            {
                bool receiptExists =
                    await _context.Payments
                        .AnyAsync(p =>
                            p.ReceiptNumber ==
                            payment.ReceiptNumber);

                if (receiptExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.ReceiptNumber),
                        "This Receipt Number already exists.");
                }
            }


            // -----------------------------------------------------
            // PAYMENT AMOUNT
            // -----------------------------------------------------

            if (payment.Amount <= 0)
            {
                ModelState.AddModelError(
                    nameof(payment.Amount),
                    "Payment amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // PAYMENT TYPE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                payment.PaymentType))
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentType),
                    "Please select payment type.");
            }


            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                payment.PaymentMethod))
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentMethod),
                    "Please select payment method.");
            }


            // -----------------------------------------------------
            // PAYMENT STATUS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                payment.PaymentStatus))
            {
                payment.PaymentStatus = "Pending";
            }


            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    if (payment.PaymentDate ==
                        default)
                    {
                        payment.PaymentDate =
                            DateTime.Now;
                    }

                    payment.CreatedDate =
                        DateTime.Now;

                    _context.Payments.Add(
                        payment);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Payment created successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Database error while saving payment: " +
                        (ex.InnerException?.Message ??
                         ex.Message));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Error while saving payment: " +
                        ex.Message);
                }
            }


            // -----------------------------------------------------
            // RELOAD DROPDOWNS
            // -----------------------------------------------------

            LoadDropDowns(
                payment.UserId,
                payment.InstallmentId);

            return View(payment);
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment =
                await _context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.PaymentId ==
                        id.Value);

            if (payment == null)
            {
                return NotFound();
            }

            LoadDropDowns(
                payment.UserId,
                payment.InstallmentId);

            return View(payment);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Payment.User));
            ModelState.Remove(nameof(Payment.Installment));


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (payment.UserId <= 0)
            {
                ModelState.AddModelError(
                    nameof(payment.UserId),
                    "Please select a client.");
            }
            else
            {
                bool userExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.UserId ==
                            payment.UserId);

                if (!userExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.UserId),
                        "Selected client does not exist.");
                }
            }


            // -----------------------------------------------------
            // INSTALLMENT VALIDATION
            // -----------------------------------------------------

            if (payment.InstallmentId.HasValue &&
                payment.InstallmentId.Value > 0)
            {
                var installment =
                    await _context.Installments

                        .Include(i => i.Emiplan)
                            .ThenInclude(e => e.Loan)
                                .ThenInclude(l => l.Allotment)
                                    .ThenInclude(a => a.Application)

                        .FirstOrDefaultAsync(i =>
                            i.InstallmentId ==
                            payment.InstallmentId.Value);

                if (installment == null)
                {
                    ModelState.AddModelError(
                        nameof(payment.InstallmentId),
                        "Selected installment does not exist.");
                }
                else
                {
                    var installmentUserId =
                        installment.Emiplan?
                            .Loan?
                            .Allotment?
                            .Application?
                            .UserId;

                    if (installmentUserId.HasValue &&
                        installmentUserId.Value != payment.UserId)
                    {
                        ModelState.AddModelError(
                            nameof(payment.UserId),
                            "Selected client does not belong to the selected installment.");
                    }
                }
            }


            // -----------------------------------------------------
            // TRANSACTION ID DUPLICATE CHECK
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                payment.TransactionId))
            {
                bool transactionExists =
                    await _context.Payments
                        .AnyAsync(p =>
                            p.TransactionId ==
                            payment.TransactionId &&
                            p.PaymentId !=
                            payment.PaymentId);

                if (transactionExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.TransactionId),
                        "This Transaction ID already exists.");
                }
            }


            // -----------------------------------------------------
            // RECEIPT NUMBER DUPLICATE CHECK
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                payment.ReceiptNumber))
            {
                bool receiptExists =
                    await _context.Payments
                        .AnyAsync(p =>
                            p.ReceiptNumber ==
                            payment.ReceiptNumber &&
                            p.PaymentId !=
                            payment.PaymentId);

                if (receiptExists)
                {
                    ModelState.AddModelError(
                        nameof(payment.ReceiptNumber),
                        "This Receipt Number already exists.");
                }
            }


            // -----------------------------------------------------
            // AMOUNT
            // -----------------------------------------------------

            if (payment.Amount <= 0)
            {
                ModelState.AddModelError(
                    nameof(payment.Amount),
                    "Payment amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // PAYMENT TYPE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                payment.PaymentType))
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentType),
                    "Please select payment type.");
            }


            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                payment.PaymentMethod))
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentMethod),
                    "Please select payment method.");
            }


            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadDropDowns(
                    payment.UserId,
                    payment.InstallmentId);

                return View(payment);
            }


            // -----------------------------------------------------
            // UPDATE
            // -----------------------------------------------------

            try
            {
                var existingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.PaymentId ==
                            id);

                if (existingPayment == null)
                {
                    return NotFound();
                }


                existingPayment.UserId =
                    payment.UserId;

                existingPayment.InstallmentId =
                    payment.InstallmentId;

                existingPayment.PaymentType =
                    payment.PaymentType;

                existingPayment.PaymentDate =
                    payment.PaymentDate;

                existingPayment.Amount =
                    payment.Amount;

                existingPayment.PaymentMethod =
                    payment.PaymentMethod;

                existingPayment.TransactionId =
                    payment.TransactionId;

                existingPayment.ReceiptNumber =
                    payment.ReceiptNumber;

                existingPayment.PaymentStatus =
                    payment.PaymentStatus;

                existingPayment.Remarks =
                    payment.Remarks;


                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "Payment updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error while updating payment: " +
                    (ex.InnerException?.Message ??
                     ex.Message));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while updating payment: " +
                    ex.Message);
            }


            LoadDropDowns(
                payment.UserId,
                payment.InstallmentId);

            return View(payment);
        }


        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment =
                await _context.Payments

                    .Include(p => p.User)

                    .Include(p => p.Installment)
                        .ThenInclude(i => i.Emiplan)
                            .ThenInclude(e => e.Loan)
                                .ThenInclude(l => l.Allotment)
                                    .ThenInclude(a => a.Application)
                                        .ThenInclude(app => app.User)

                    .Include(p => p.Installment)
                        .ThenInclude(i => i.Emiplan)
                            .ThenInclude(e => e.Loan)
                                .ThenInclude(l => l.Allotment)
                                    .ThenInclude(a => a.Property)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(p =>
                        p.PaymentId ==
                        id.Value);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var payment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.PaymentId ==
                        id);

            if (payment == null)
            {
                return NotFound();
            }


            try
            {
                _context.Payments.Remove(
                    payment);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Payment deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] =
                    "This payment cannot be deleted because it is linked with another record. " +
                    (ex.InnerException?.Message ??
                     "");

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        id
                    });
            }
        }


        // =========================================================
        // LOAD DROPDOWNS
        // =========================================================

        private void LoadDropDowns(
            int? selectedUserId = null,
            int? selectedInstallmentId = null)
        {
            // =====================================================
            // CLIENT DROPDOWN
            // =====================================================

            var users =
                _context.Users
                    .AsNoTracking()
                    .OrderBy(u => u.FullName)
                    .Select(u => new
                    {
                        u.UserId,
                        u.FullName
                    })
                    .ToList();

            var userList =
                users.Select(u =>
                    new
                    {
                        u.UserId,

                        DisplayText =
                            "#" +
                            u.UserId +
                            " | " +
                            u.FullName
                    });


            ViewBag.UserId =
                new SelectList(
                    userList,
                    "UserId",
                    "DisplayText",
                    selectedUserId);


            // =====================================================
            // INSTALLMENT DROPDOWN
            // =====================================================
            //
            // Shows:
            //
            // EMI #1 | Client | Property | Amount | Due Date
            //
            // =====================================================

            var installments =
                _context.Installments

                    .Include(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Application)
                                    .ThenInclude(app => app.User)

                    .Include(i => i.Emiplan)
                        .ThenInclude(e => e.Loan)
                            .ThenInclude(l => l.Allotment)
                                .ThenInclude(a => a.Property)

                    .AsNoTracking()

                    .OrderBy(i =>
                        i.DueDate)

                    .ToList();


            var installmentList =
                installments.Select(i =>
                {
                    var clientName =
                        i.Emiplan?
                            .Loan?
                            .Allotment?
                            .Application?
                            .User?
                            .FullName
                        ?? "Unknown Client";


                    var clientId =
                        i.Emiplan?
                            .Loan?
                            .Allotment?
                            .Application?
                            .UserId;


                    var propertyId =
                        i.Emiplan?
                            .Loan?
                            .Allotment?
                            .PropertyId;


                    var unitNumber =
                        i.Emiplan?
                            .Loan?
                            .Allotment?
                            .Property?
                            .UnitNumber;


                    return new
                    {
                        InstallmentId =
                            i.InstallmentId,

                        DisplayText =
                            "EMI #" +
                            i.InstallmentNumber +
                            " | Client #" +
                            (clientId?.ToString() ?? "-") +
                            " " +
                            clientName +
                            " | Property " +
                            (unitNumber ?? "#" +
                                (propertyId?.ToString() ??
                                 "-")) +
                            " | ₹" +
                            i.InstallmentAmount
                                .ToString("N2") +
                            " | Due: " +
                            i.DueDate
                                .ToString("dd-MM-yyyy")
                    };

                }).ToList();


            ViewBag.InstallmentId =
                new SelectList(
                    installmentList,
                    "InstallmentId",
                    "DisplayText",
                    selectedInstallmentId);
        }
    }


}
