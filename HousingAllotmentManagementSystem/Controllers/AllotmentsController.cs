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
// Clients use:
//
// /ClientAllotment
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
        //
        // Loads:
        // Allotment
        //   -> Application
        //       -> User
        //   -> Property
        //
        // This allows the view to display:
        // Client ID
        // Client Name
        // Property ID
        // Allotment Number
        //
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allotments = await _context.Allotments
                .Include(a => a.Application)
                    .ThenInclude(app => app.User)

                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)

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
                    .ThenInclude(app => app.User)

                .Include(a => a.Property)

                .Include(a => a.Loans)

                .AsNoTracking()

                .FirstOrDefaultAsync(a =>
                    a.AllotmentId == id.Value);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
        }


        // =========================================================
        // CREATE - GET
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
                    0,

                CreatedDate =
                    DateTime.Now
            };

            return View(allotment);
        }


        // =========================================================
        // CREATE - POST
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
            // -----------------------------------------------------
            // Remove navigation validation
            // -----------------------------------------------------

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

            if (string.IsNullOrWhiteSpace(
                allotment.AllotmentNumber))
            {
                ModelState.AddModelError(
                    "AllotmentNumber",
                    "Please enter an allotment number.");
            }
            else
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
            // BOOKING AMOUNT
            // -----------------------------------------------------

            if (allotment.BookingAmount < 0)
            {
                ModelState.AddModelError(
                    "BookingAmount",
                    "Booking amount cannot be negative.");
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

            var allotment =
                await _context.Allotments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId ==
                        id.Value);

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
        // EDIT - POST
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
            // BOOKING AMOUNT
            // -----------------------------------------------------

            if (model.BookingAmount < 0)
            {
                ModelState.AddModelError(
                    "BookingAmount",
                    "Booking amount cannot be negative.");
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

            var allotment =
                await _context.Allotments

                    .Include(a => a.Application)
                        .ThenInclude(app => app.User)

                    .Include(a => a.Property)

                    .Include(a => a.Loans)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId ==
                        id.Value);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
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
            var allotment =
                await _context.Allotments
                    .FirstOrDefaultAsync(a =>
                        a.AllotmentId ==
                        id);

            if (allotment == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Check linked loans
            // -----------------------------------------------------

            bool hasLoans =
                await _context.Loans
                    .AnyAsync(l =>
                        l.AllotmentId ==
                        id);

            if (hasLoans)
            {
                TempData["ErrorMessage"] =
                    "This allotment cannot be deleted because it is linked with a loan.";

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        id
                    });
            }


            // -----------------------------------------------------
            // DELETE
            // -----------------------------------------------------

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
                    "This allotment cannot be deleted because it is linked with another record.";

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
            //
            // Show:
            // Application ID + Client Name
            //
            // -----------------------------------------------------

            var applications =
                _context.Applications
                    .Include(a => a.User)
                    .AsNoTracking()
                    .OrderByDescending(a =>
                        a.ApplicationId)
                    .ToList();

            var applicationList =
                applications.Select(a =>
                    new
                    {
                        ApplicationId =
                            a.ApplicationId,

                        DisplayText =
                            a.ApplicationId +
                            " | " +
                            (a.User?.FullName ??
                             "Unknown Client")
                    });


            ViewData["ApplicationId"] =
                new SelectList(
                    applicationList,
                    "ApplicationId",
                    "DisplayText",
                    selectedApplicationId);


            // -----------------------------------------------------
            // PROPERTY DROPDOWN
            // -----------------------------------------------------
            //
            // Currently using Property ID because the exact
            // property-name field has not yet been provided.
            //
            // -----------------------------------------------------

            var properties = _context.Properties
        .Include(p => p.Scheme)
        .AsNoTracking()
        .OrderBy(p => p.Scheme.SchemeName)
        .ThenBy(p => p.UnitNumber)
        .ToList();


            var propertyList = properties.Select(p => new
            {
                PropertyId = p.PropertyId,

                DisplayText =
                    (p.Scheme?.SchemeName ?? "Unknown Scheme") +
                    " | " +
                    (
                        !string.IsNullOrWhiteSpace(p.UnitNumber)
                            ? "Unit " + p.UnitNumber
                            : "No Unit"
                    ) +
                    (
                        !string.IsNullOrWhiteSpace(p.PlotNumber)
                            ? " | Plot " + p.PlotNumber
                            : ""
                    ) +
                    (
                        !string.IsNullOrWhiteSpace(p.PropertyType)
                            ? " | " + p.PropertyType
                            : ""
                    ) +
                    " | Property #" + p.PropertyId
            });


            ViewData["PropertyId"] = new SelectList(
                propertyList,
                "PropertyId",
                "DisplayText",
                selectedPropertyId);
        }


        // =========================================================
        // CHECK EXISTENCE
        // =========================================================

        private bool AllotmentExists(
            int id)
        {
            return _context.Allotments
                .Any(a =>
                    a.AllotmentId ==
                    id);
        }
    }


}
