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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Installment)
                .OrderByDescending(p => p.PaymentId)
                .AsNoTracking()
                .ToListAsync();

            return View(payments);
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

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Installment)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            var users = _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.UserId =
                new SelectList(
                    users,
                    "UserId",
                    "FullName");


            var installments = _context.Installments
                .AsNoTracking()
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            var installmentList =
                installments.Select(i => new
                {
                    InstallmentId =
                        i.InstallmentId,

                    DisplayText =
                        "Installment " +
                        i.InstallmentNumber +
                        " - ₹" +
                        i.InstallmentAmount
                            .ToString("N2") +
                        " - Due: " +
                        i.DueDate
                            .ToString("dd-MM-yyyy")
                }).ToList();

            ViewBag.InstallmentId =
                new SelectList(
                    installmentList,
                    "InstallmentId",
                    "DisplayText");


            var payment = new Payment
            {
                PaymentDate =
                    DateTime.Now,

                PaymentStatus =
                    "Paid",

                CreatedDate =
                    DateTime.Now
            };

            return View(payment);
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Payment payment)
        {
            // Navigation properties are not submitted by form.
            ModelState.Remove("User");
            ModelState.Remove("Installment");


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (payment.UserId <= 0)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Please select a user.");
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
                        "UserId",
                        "Selected user does not exist.");
                }
            }


            // -----------------------------------------------------
            // INSTALLMENT VALIDATION
            // -----------------------------------------------------

            if (payment.InstallmentId.HasValue &&
                payment.InstallmentId.Value > 0)
            {
                bool installmentExists =
                    await _context.Installments
                        .AnyAsync(i =>
                            i.InstallmentId ==
                            payment.InstallmentId.Value);

                if (!installmentExists)
                {
                    ModelState.AddModelError(
                        "InstallmentId",
                        "Selected installment does not exist.");
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
                        "TransactionId",
                        "This Transaction ID already exists. " +
                        "Please enter a different Transaction ID.");
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
                        "ReceiptNumber",
                        "This Receipt Number already exists. " +
                        "Please enter a different Receipt Number.");
                }
            }


            // -----------------------------------------------------
            // PAYMENT AMOUNT
            // -----------------------------------------------------

            if (payment.Amount <= 0)
            {
                ModelState.AddModelError(
                    "Amount",
                    "Payment amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // PAYMENT TYPE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    payment.PaymentType))
            {
                ModelState.AddModelError(
                    "PaymentType",
                    "Please select payment type.");
            }


            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    payment.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Please select payment method.");
            }


            // -----------------------------------------------------
            // PAYMENT STATUS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    payment.PaymentStatus))
            {
                payment.PaymentStatus =
                    "Pending";
            }


            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    payment.CreatedDate =
                        DateTime.Now;

                    if (payment.PaymentDate ==
                        default)
                    {
                        payment.PaymentDate =
                            DateTime.Now;
                    }

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
                    string errorMessage =
                        ex.InnerException?.Message ??
                        ex.Message;

                    ModelState.AddModelError(
                        "",
                        "Database error while saving payment: " +
                        errorMessage);
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
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment =
                await _context.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.PaymentId == id);

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
        // EDIT - POST - ADMIN ONLY
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

            ModelState.Remove("User");
            ModelState.Remove("Installment");


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (payment.UserId <= 0)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Please select a user.");
            }


            // -----------------------------------------------------
            // INSTALLMENT VALIDATION
            // -----------------------------------------------------

            if (payment.InstallmentId.HasValue &&
                payment.InstallmentId.Value > 0)
            {
                bool installmentExists =
                    await _context.Installments
                        .AnyAsync(i =>
                            i.InstallmentId ==
                            payment.InstallmentId.Value);

                if (!installmentExists)
                {
                    ModelState.AddModelError(
                        "InstallmentId",
                        "Selected installment does not exist.");
                }
            }


            // -----------------------------------------------------
            // AMOUNT VALIDATION
            // -----------------------------------------------------

            if (payment.Amount <= 0)
            {
                ModelState.AddModelError(
                    "Amount",
                    "Payment amount must be greater than zero.");
            }


            // -----------------------------------------------------
            // PAYMENT TYPE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    payment.PaymentType))
            {
                ModelState.AddModelError(
                    "PaymentType",
                    "Please select payment type.");
            }


            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    payment.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
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
                            p.PaymentId == id);

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
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment =
                await _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Installment)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
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
            var payment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.PaymentId == id);

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
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This payment cannot be deleted because it is linked with another record.";

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
            // USERS
            // =====================================================

            var users =
                _context.Users
                    .AsNoTracking()
                    .OrderBy(u => u.FullName)
                    .ToList();

            ViewBag.UserId =
                new SelectList(
                    users,
                    "UserId",
                    "FullName",
                    selectedUserId);


            // =====================================================
            // INSTALLMENTS
            // =====================================================

            var installments =
                _context.Installments
                    .AsNoTracking()
                    .OrderBy(i =>
                        i.InstallmentNumber)
                    .ToList();

            var installmentList =
                installments.Select(i => new
                {
                    InstallmentId =
                        i.InstallmentId,

                    DisplayText =
                        "Installment " +
                        i.InstallmentNumber +
                        " - ₹" +
                        i.InstallmentAmount
                            .ToString("N2") +
                        " - Due: " +
                        i.DueDate
                            .ToString("dd-MM-yyyy")
                }).ToList();

            ViewBag.InstallmentId =
                new SelectList(
                    installmentList,
                    "InstallmentId",
                    "DisplayText",
                    selectedInstallmentId);
        }


        // =========================================================
        // CHECK EXISTENCE
        // =========================================================

        private bool PaymentExists(int id)
        {
            return _context.Payments
                .Any(p =>
                    p.PaymentId == id);
        }
    }
}