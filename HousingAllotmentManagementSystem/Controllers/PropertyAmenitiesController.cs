using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class PropertyAmenitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertyAmenitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var propertyAmenities = await _context.PropertyAmenities
                .Include(pa => pa.Property)
                .Include(pa => pa.Amenity)
                .AsNoTracking()
                .ToListAsync();

            return View(propertyAmenities);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propertyAmenity = await _context.PropertyAmenities
                .Include(pa => pa.Property)
                .Include(pa => pa.Amenity)
                .AsNoTracking()
                .FirstOrDefaultAsync(pa =>
                    pa.PropertyAmenityId == id);

            if (propertyAmenity == null)
            {
                return NotFound();
            }

            return View(propertyAmenity);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PropertyAmenityId,PropertyId,AmenityId")]
            PropertyAmenity propertyAmenity)
        {
            // Property is required for assignment
            if (!propertyAmenity.PropertyId.HasValue)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }

            // Amenity is required for assignment
            if (!propertyAmenity.AmenityId.HasValue)
            {
                ModelState.AddModelError(
                    "AmenityId",
                    "Please select an amenity.");
            }

            // Check duplicate assignment
            if (propertyAmenity.PropertyId.HasValue &&
                propertyAmenity.AmenityId.HasValue)
            {
                bool alreadyExists = await _context.PropertyAmenities
                    .AnyAsync(pa =>
                        pa.PropertyId == propertyAmenity.PropertyId &&
                        pa.AmenityId == propertyAmenity.AmenityId);

                if (alreadyExists)
                {
                    ModelState.AddModelError(
                        "",
                        "This amenity is already assigned to the selected property.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.PropertyAmenities.Add(propertyAmenity);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Amenity assigned to property successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(
                propertyAmenity.PropertyId,
                propertyAmenity.AmenityId);

            return View(propertyAmenity);
        }

        // =========================================================
        // EDIT - GET
        // =========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propertyAmenity = await _context.PropertyAmenities
                .FindAsync(id);

            if (propertyAmenity == null)
            {
                return NotFound();
            }

            await LoadDropdownsAsync(
                propertyAmenity.PropertyId,
                propertyAmenity.AmenityId);

            return View(propertyAmenity);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PropertyAmenityId,PropertyId,AmenityId")]
            PropertyAmenity propertyAmenity)
        {
            if (id != propertyAmenity.PropertyAmenityId)
            {
                return NotFound();
            }

            // Property is required
            if (!propertyAmenity.PropertyId.HasValue)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }

            // Amenity is required
            if (!propertyAmenity.AmenityId.HasValue)
            {
                ModelState.AddModelError(
                    "AmenityId",
                    "Please select an amenity.");
            }

            // Check duplicate assignment
            if (propertyAmenity.PropertyId.HasValue &&
                propertyAmenity.AmenityId.HasValue)
            {
                bool alreadyExists = await _context.PropertyAmenities
                    .AnyAsync(pa =>
                        pa.PropertyAmenityId != propertyAmenity.PropertyAmenityId &&
                        pa.PropertyId == propertyAmenity.PropertyId &&
                        pa.AmenityId == propertyAmenity.AmenityId);

                if (alreadyExists)
                {
                    ModelState.AddModelError(
                        "",
                        "This amenity is already assigned to the selected property.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.PropertyAmenities.Update(propertyAmenity);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Property amenity updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyAmenityExists(
                        propertyAmenity.PropertyAmenityId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            await LoadDropdownsAsync(
                propertyAmenity.PropertyId,
                propertyAmenity.AmenityId);

            return View(propertyAmenity);
        }

        // =========================================================
        // DELETE - GET
        // =========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propertyAmenity = await _context.PropertyAmenities
                .Include(pa => pa.Property)
                .Include(pa => pa.Amenity)
                .AsNoTracking()
                .FirstOrDefaultAsync(pa =>
                    pa.PropertyAmenityId == id);

            if (propertyAmenity == null)
            {
                return NotFound();
            }

            return View(propertyAmenity);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var propertyAmenity = await _context.PropertyAmenities
                .FindAsync(id);

            if (propertyAmenity == null)
            {
                return NotFound();
            }

            _context.PropertyAmenities.Remove(propertyAmenity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Amenity removed from property successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD DROPDOWNS
        // =========================================================
        private async Task LoadDropdownsAsync(
            int? selectedPropertyId = null,
            int? selectedAmenityId = null)
        {
            var properties = await _context.Properties
                .AsNoTracking()
                .OrderBy(p => p.PropertyId)
                .ToListAsync();

            var amenities = await _context.Amenities
                .Where(a => a.Status)
                .OrderBy(a => a.AmenityName)
                .AsNoTracking()
                .ToListAsync();

            ViewData["PropertyId"] = new SelectList(
                properties,
                "PropertyId",
                "UnitNumber",
                selectedPropertyId);

            ViewData["AmenityId"] = new SelectList(
                amenities,
                "AmenityId",
                "AmenityName",
                selectedAmenityId);
        }

        // =========================================================
        // CHECK EXISTS
        // =========================================================
        private bool PropertyAmenityExists(int id)
        {
            return _context.PropertyAmenities
                .Any(pa => pa.PropertyAmenityId == id);
        }
    }
}