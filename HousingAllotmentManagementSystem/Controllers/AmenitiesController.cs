using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // ADMIN ONLY CONTROLLER
    // =========================================================
    //
    // Clients cannot access:
    //
    // /Amenities
    // /Amenities/Details
    // /Amenities/Create
    // /Amenities/Edit
    // /Amenities/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class AmenitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AmenitiesController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var amenities = await _context.Amenities
                .AsNoTracking()
                .ToListAsync();

            return View(amenities);
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

            var amenity = await _context.Amenities
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AmenityId == id);

            if (amenity == null)
            {
                return NotFound();
            }

            return View(amenity);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AmenityName,Status")] Amenity amenity)
        {
            if (ModelState.IsValid)
            {
                amenity.CreatedDate =
                    DateTime.Now;

                _context.Amenities.Add(amenity);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Amenity created successfully.";

                return RedirectToAction(
                    nameof(Index));
            }

            return View(amenity);
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

            var amenity =
                await _context.Amenities.FindAsync(id);

            if (amenity == null)
            {
                return NotFound();
            }

            return View(amenity);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "AmenityId,AmenityName,Status,CreatedDate")]
            Amenity amenity)
        {
            if (id != amenity.AmenityId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Amenities.Update(amenity);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Amenity updated successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AmenityExists(
                            amenity.AmenityId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            return View(amenity);
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

            var amenity =
                await _context.Amenities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.AmenityId == id);

            if (amenity == null)
            {
                return NotFound();
            }

            return View(amenity);
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
            var amenity =
                await _context.Amenities
                    .FirstOrDefaultAsync(a =>
                        a.AmenityId == id);

            if (amenity == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // CHECK WHETHER AMENITY IS CURRENTLY USED
            // -----------------------------------------------------

            bool isUsed =
                await _context.PropertyAmenities
                    .AnyAsync(pa =>
                        pa.AmenityId == id);

            if (isUsed)
            {
                TempData["ErrorMessage"] =
                    "This amenity cannot be deleted because it is assigned to one or more properties.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // DELETE AMENITY
            // -----------------------------------------------------

            _context.Amenities.Remove(amenity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Amenity deleted successfully.";

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // CHECK AMENITY EXISTS
        // =========================================================

        private bool AmenityExists(int id)
        {
            return _context.Amenities
                .Any(a =>
                    a.AmenityId == id);
        }
    }
}