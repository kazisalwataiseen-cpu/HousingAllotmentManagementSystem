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
    // /Allotments
    // /Allotments/Details
    // /Allotments/Create
    // /Allotments/Edit
    // /Allotments/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class AllotmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AllotmentsController(
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
            var allotments = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                .OrderByDescending(a => a.AllotmentId)
                .AsNoTracking()
                .ToListAsync();

            return View(allotments);
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

            var allotment = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AllotmentId == id);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
        }

        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();

            var allotment = new Allotment
            {
                AllotmentDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                AllotmentStatus =
                    "Pending",

                BookingAmount =
                    0
            };

            return View(allotment);
        }

        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "ApplicationId,PropertyId,AllotmentNumber," +
                "AllotmentDate,BookingAmount,AllotmentStatus," +
                "Remarks")]
            Allotment allotment)
        {
            ModelState.Remove("Application");
            ModelState.Remove("Property");
            ModelState.Remove("Loans");

            // -----------------------------------------------------
            // APPLICATION VALIDATION
            // -----------------------------------------------------

            if (allotment.ApplicationId <= 0)
            {
                ModelState.AddModelError(
                    "ApplicationId",
                    "Please select an application.");
            }
            else
            {
                bool applicationExists =
                    await _context.Applications
                        .AnyAsync(a =>
                            a.ApplicationId ==
                            allotment.ApplicationId);

                if (!applicationExists)
                {
                    ModelState.AddModelError(
                        "ApplicationId",
                        "Selected application does not exist.");
                }
            }

            // -----------------------------------------------------
            // PROPERTY VALIDATION
            // -----------------------------------------------------

            if (allotment.PropertyId <= 0)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }
            else
            {
                bool propertyExists =
                    await _context.Properties
                        .AnyAsync(p =>
                            p.PropertyId ==
                            allotment.PropertyId);

                if (!propertyExists)
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "Selected property does not exist.");
                }
            }

            // -----------------------------------------------------
            // ALLOTMENT NUMBER VALIDATION
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    allotment.AllotmentNumber))
            {
                bool allotmentNumberExists =
                    await _context.Allotments
                        .AnyAsync(a =>
                            a.AllotmentNumber ==
                            allotment.AllotmentNumber);

                if (allotmentNumberExists)
                {
                    ModelState.AddModelError(
                        "AllotmentNumber",
                        "This allotment number already exists.");
                }
            }

            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadDropdowns(
                    allotment.ApplicationId,
                    allotment.PropertyId);

                return View(allotment);
            }

            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            try
            {
                allotment.CreatedDate =
                    DateTime.Now;

                _context.Allotments.Add(
                    allotment);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Allotment created successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to save allotment. " +
                    (ex.InnerException?.Message ??
                     ex.Message));

                LoadDropdowns(
                    allotment.ApplicationId,
                    allotment.PropertyId);

                return View(allotment);
            }
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

            var allotment =
                await _context.Allotments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId == id);

            if (allotment == null)
            {
                return NotFound();
            }

            LoadDropdowns(
                allotment.ApplicationId,
                allotment.PropertyId);

            return View(allotment);
        }

        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "AllotmentId,ApplicationId,PropertyId," +
                "AllotmentNumber,AllotmentDate,BookingAmount," +
                "AllotmentStatus,Remarks")]
            Allotment model)
        {
            if (id != model.AllotmentId)
            {
                return NotFound();
            }

            ModelState.Remove("Application");
            ModelState.Remove("Property");
            ModelState.Remove("Loans");

            // -----------------------------------------------------
            // APPLICATION VALIDATION
            // -----------------------------------------------------

            if (model.ApplicationId <= 0)
            {
                ModelState.AddModelError(
                    "ApplicationId",
                    "Please select an application.");
            }
            else
            {
                bool applicationExists =
                    await _context.Applications
                        .AnyAsync(a =>
                            a.ApplicationId ==
                            model.ApplicationId);

                if (!applicationExists)
                {
                    ModelState.AddModelError(
                        "ApplicationId",
                        "Selected application does not exist.");
                }
            }

            // -----------------------------------------------------
            // PROPERTY VALIDATION
            // -----------------------------------------------------

            if (model.PropertyId <= 0)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }
            else
            {
                bool propertyExists =
                    await _context.Properties
                        .AnyAsync(p =>
                            p.PropertyId ==
                            model.PropertyId);

                if (!propertyExists)
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "Selected property does not exist.");
                }
            }

            // -----------------------------------------------------
            // ALLOTMENT NUMBER DUPLICATE CHECK
            // -----------------------------------------------------

            bool duplicateNumber =
                await _context.Allotments
                    .AnyAsync(a =>
                        a.AllotmentNumber ==
                        model.AllotmentNumber &&
                        a.AllotmentId !=
                        model.AllotmentId);

            if (duplicateNumber)
            {
                ModelState.AddModelError(
                    "AllotmentNumber",
                    "This allotment number already exists.");
            }

            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadDropdowns(
                    model.ApplicationId,
                    model.PropertyId);

                return View(model);
            }

            // -----------------------------------------------------
            // UPDATE
            // -----------------------------------------------------

            try
            {
                var existing =
                    await _context.Allotments
                        .FirstOrDefaultAsync(a =>
                            a.AllotmentId ==
                            model.AllotmentId);

                if (existing == null)
                {
                    return NotFound();
                }

                existing.ApplicationId =
                    model.ApplicationId;

                existing.PropertyId =
                    model.PropertyId;

                existing.AllotmentNumber =
                    model.AllotmentNumber;

                existing.AllotmentDate =
                    model.AllotmentDate;

                existing.BookingAmount =
                    model.BookingAmount;

                existing.AllotmentStatus =
                    model.AllotmentStatus;

                existing.Remarks =
                    model.Remarks;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Allotment updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update allotment. " +
                    (ex.InnerException?.Message ??
                     ex.Message));

                LoadDropdowns(
                    model.ApplicationId,
                    model.PropertyId);

                return View(model);
            }
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

            var allotment =
                await _context.Allotments
                    .Include(a => a.Application)
                    .Include(a => a.Property)
                    .Include(a => a.Loans)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId == id);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
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
            var allotment =
                await _context.Allotments
                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId == id);

            if (allotment == null)
            {
                return NotFound();
            }

            try
            {
                _context.Allotments.Remove(
                    allotment);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Allotment deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This allotment cannot be deleted because it is linked with another record such as a Loan.";

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

        private void LoadDropdowns(
            int? selectedApplicationId = null,
            int? selectedPropertyId = null)
        {
            // -----------------------------------------------------
            // APPLICATION DROPDOWN
            // -----------------------------------------------------

            var applications =
                _context.Applications
                    .AsNoTracking()
                    .OrderByDescending(a =>
                        a.ApplicationId)
                    .Select(a => new
                    {
                        a.ApplicationId
                    })
                    .ToList();

            ViewData["ApplicationId"] =
                new SelectList(
                    applications,
                    "ApplicationId",
                    "ApplicationId",
                    selectedApplicationId);

            // -----------------------------------------------------
            // PROPERTY DROPDOWN
            // -----------------------------------------------------

            var properties =
                _context.Properties
                    .AsNoTracking()
                    .OrderBy(p =>
                        p.PropertyId)
                    .Select(p => new
                    {
                        p.PropertyId
                    })
                    .ToList();

            ViewData["PropertyId"] =
                new SelectList(
                    properties,
                    "PropertyId",
                    "PropertyId",
                    selectedPropertyId);
        }

        // =========================================================
        // CHECK EXISTENCE
        // =========================================================

        private bool AllotmentExists(int id)
        {
            return _context.Allotments
                .Any(a =>
                    a.AllotmentId == id);
        }
    }
}