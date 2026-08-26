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
    // /Installments
    // /Installments/Details
    // /Installments/Create
    // /Installments/Edit
    // /Installments/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class InstallmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstallmentsController(
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
            var installments = await _context.Installments
                .Include(i => i.Emiplan)
                .AsNoTracking()
                .ToListAsync();

            return View(installments);
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

            var installment =
                await _context.Installments
                    .Include(i => i.Emiplan)
                    .Include(i => i.Payments)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i =>
                        i.InstallmentId == id);

            if (installment == null)
            {
                return NotFound();
            }

            return View(installment);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadEmiPlans();

            return View();
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "InstallmentId,EmiplanId,InstallmentNumber," +
                "DueDate,InstallmentAmount,PrincipalAmount," +
                "InterestAmount,LateFee,PaidAmount,PaymentDate," +
                "PaymentMethod,TransactionReference,PaymentStatus," +
                "Remarks")]
            Installment installment)
        {
            if (ModelState.IsValid)
            {
                installment.CreatedDate =
                    DateTime.Now;

                _context.Installments.Add(
                    installment);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Index));
            }

            LoadEmiPlans(
                installment.EmiplanId);

            return View(installment);
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

            var installment =
                await _context.Installments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i =>
                        i.InstallmentId == id);

            if (installment == null)
            {
                return NotFound();
            }

            LoadEmiPlans(
                installment.EmiplanId);

            return View(installment);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "InstallmentId,EmiplanId,InstallmentNumber," +
                "DueDate,InstallmentAmount,PrincipalAmount," +
                "InterestAmount,LateFee,PaidAmount,PaymentDate," +
                "PaymentMethod,TransactionReference,PaymentStatus," +
                "Remarks,CreatedDate")]
            Installment installment)
        {
            if (id != installment.InstallmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(
                        installment);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstallmentExists(
                            installment.InstallmentId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(
                    nameof(Index));
            }

            LoadEmiPlans(
                installment.EmiplanId);

            return View(installment);
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

            var installment =
                await _context.Installments
                    .Include(i => i.Emiplan)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i =>
                        i.InstallmentId == id);

            if (installment == null)
            {
                return NotFound();
            }

            return View(installment);
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
            var installment =
                await _context.Installments
                    .FirstOrDefaultAsync(i =>
                        i.InstallmentId == id);

            if (installment != null)
            {
                _context.Installments.Remove(
                    installment);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // CHECK INSTALLMENT EXISTS
        // =========================================================

        private bool InstallmentExists(int id)
        {
            return _context.Installments
                .Any(e =>
                    e.InstallmentId == id);
        }


        // =========================================================
        // LOAD EMI PLAN DROPDOWN
        // =========================================================

        private void LoadEmiPlans(
            int? selectedEmiplanId = null)
        {
            var emiPlans =
                _context.Emiplans
                    .Include(e => e.Loan)
                    .AsNoTracking()
                    .OrderBy(e =>
                        e.EmiplanId)
                    .ToList();

            ViewData["EmiplanId"] =
                new SelectList(
                    emiPlans,
                    "EmiplanId",
                    "EmiplanId",
                    selectedEmiplanId);
        }
    }
}