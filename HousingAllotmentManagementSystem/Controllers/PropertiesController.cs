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
    // /Properties
    // /Properties/Details
    // /Properties/Create
    // /Properties/Edit
    // /Properties/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertiesController(
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
            var properties = await _context.Properties
                .Include(p => p.Scheme)
                .AsNoTracking()
                .OrderByDescending(p => p.PropertyId)
                .ToListAsync();

            return View(properties);
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

            var property = await _context.Properties
                .Include(p => p.Scheme)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSchemesAsync();

            return View(new Property
            {
                Status = "Available"
            });
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Property property)
        {
            // Remove navigation-property validation.
            ModelState.Remove("Scheme");

            // -----------------------------------------------------
            // VALIDATE SCHEME
            // -----------------------------------------------------

            if (property.SchemeId <= 0)
            {
                ModelState.AddModelError(
                    "SchemeId",
                    "Please select a housing scheme.");
            }
            else
            {
                bool schemeExists =
                    await _context.HousingSchemes
                        .AnyAsync(s =>
                            s.SchemeId ==
                            property.SchemeId);

                if (!schemeExists)
                {
                    ModelState.AddModelError(
                        "SchemeId",
                        "Selected housing scheme does not exist.");
                }
            }

            // -----------------------------------------------------
            // SAVE PROPERTY
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    property.CreatedDate =
                        DateTime.Now;

                    if (string.IsNullOrWhiteSpace(
                            property.Status))
                    {
                        property.Status =
                            "Available";
                    }

                    _context.Properties.Add(
                        property);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Property created successfully.";

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
                        "Database error: " +
                        errorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Error while saving property: " +
                        ex.Message);
                }
            }

            await LoadSchemesAsync(
                property.SchemeId);

            return View(property);
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

            var property = await _context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            await LoadSchemesAsync(
                property.SchemeId);

            return View(property);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Property property)
        {
            if (id != property.PropertyId)
            {
                return NotFound();
            }

            ModelState.Remove("Scheme");

            // -----------------------------------------------------
            // VALIDATE SCHEME
            // -----------------------------------------------------

            if (property.SchemeId <= 0)
            {
                ModelState.AddModelError(
                    "SchemeId",
                    "Please select a housing scheme.");
            }
            else
            {
                bool schemeExists =
                    await _context.HousingSchemes
                        .AnyAsync(s =>
                            s.SchemeId ==
                            property.SchemeId);

                if (!schemeExists)
                {
                    ModelState.AddModelError(
                        "SchemeId",
                        "Selected housing scheme does not exist.");
                }
            }

            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadSchemesAsync(
                    property.SchemeId);

                return View(property);
            }

            // -----------------------------------------------------
            // UPDATE PROPERTY
            // -----------------------------------------------------

            try
            {
                var existingProperty =
                    await _context.Properties
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                            property.PropertyId);

                if (existingProperty == null)
                {
                    return NotFound();
                }

                existingProperty.SchemeId =
                    property.SchemeId;

                existingProperty.UnitNumber =
                    property.UnitNumber;

                existingProperty.PlotNumber =
                    property.PlotNumber;

                existingProperty.PropertyType =
                    property.PropertyType;

                existingProperty.Bedrooms =
                    property.Bedrooms;

                existingProperty.Bathrooms =
                    property.Bathrooms;

                existingProperty.CarpetArea =
                    property.CarpetArea;

                existingProperty.BuiltupArea =
                    property.BuiltupArea;

                existingProperty.FloorPlanImage =
                    property.FloorPlanImage;

                existingProperty.Facing =
                    property.Facing;

                existingProperty.Price =
                    property.Price;

                existingProperty.BookingAmount =
                    property.BookingAmount;

                existingProperty.Status =
                    property.Status;

                existingProperty.Description =
                    property.Description;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Property updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(
                        property.PropertyId))
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    "",
                    "The property was modified by another user.");

                await LoadSchemesAsync(
                    property.SchemeId);

                return View(property);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Database error: " +
                    (ex.InnerException?.Message ??
                     ex.Message));

                await LoadSchemesAsync(
                    property.SchemeId);

                return View(property);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Error while updating property: " +
                    ex.Message);

                await LoadSchemesAsync(
                    property.SchemeId);

                return View(property);
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

            var property = await _context.Properties
                .Include(p => p.Scheme)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
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
            var property =
                await _context.Properties
                    .FirstOrDefaultAsync(p =>
                        p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            try
            {
                _context.Properties.Remove(
                    property);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Property deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This property cannot be deleted because it is linked with another record.";

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        id
                    });
            }
        }


        // =========================================================
        // LOAD HOUSING SCHEMES
        // =========================================================

        private async Task LoadSchemesAsync(
            int? selectedSchemeId = null)
        {
            var schemes =
                await _context.HousingSchemes
                    .AsNoTracking()
                    .OrderBy(s => s.SchemeName)
                    .ToListAsync();

            ViewBag.Schemes =
                new SelectList(
                    schemes,
                    "SchemeId",
                    "SchemeName",
                    selectedSchemeId);
        }


        // =========================================================
        // CHECK PROPERTY EXISTS
        // =========================================================

        private bool PropertyExists(int id)
        {
            return _context.Properties
                .Any(p =>
                    p.PropertyId == id);
        }
    }
}