using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class HousingSchemesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HousingSchemesController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX - CLIENT + ADMIN
        // URL: /HousingSchemes
        // =========================================================
        //
        // Clients can view housing schemes.
        // Admin can also view housing schemes.
        //
        // DO NOT PUT [Authorize(Roles = "Admin")] HERE.
        //
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var schemes = await _context.HousingSchemes
                .AsNoTracking()
                .OrderByDescending(x => x.SchemeId)
                .ToListAsync();

            return View(
                "~/Views/HousingSchemes/Index.cshtml",
                schemes);
        }


        // =========================================================
        // DETAILS - CLIENT + ADMIN
        // URL: /HousingSchemes/Details/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme =
                await _context.HousingSchemes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SchemeId == id);

            if (housingScheme == null)
            {
                return NotFound();
            }

            return View(housingScheme);
        }


        // =========================================================
        // CREATE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // CREATE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "SchemeId,SchemeName,Description,City,State," +
                "Location,LaunchDate,LastApplicationDate," +
                "TotalUnits,Brochure,BannerImage,Status,CreatedDate")]
            HousingScheme housingScheme)
        {
            if (ModelState.IsValid)
            {
                housingScheme.CreatedDate =
                    DateTime.Now;

                _context.HousingSchemes.Add(
                    housingScheme);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Index));
            }

            return View(housingScheme);
        }


        // =========================================================
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme =
                await _context.HousingSchemes
                    .FindAsync(id);

            if (housingScheme == null)
            {
                return NotFound();
            }

            return View(housingScheme);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "SchemeId,SchemeName,Description,City,State," +
                "Location,LaunchDate,LastApplicationDate," +
                "TotalUnits,Brochure,BannerImage,Status,CreatedDate")]
            HousingScheme housingScheme)
        {
            if (id != housingScheme.SchemeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(
                        housingScheme);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HousingSchemeExists(
                            housingScheme.SchemeId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(
                    nameof(Index));
            }

            return View(housingScheme);
        }


        // =========================================================
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme =
                await _context.HousingSchemes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SchemeId == id);

            if (housingScheme == null)
            {
                return NotFound();
            }

            return View(housingScheme);
        }


        // =========================================================
        // DELETE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme =
                await _context.HousingSchemes
                    .FindAsync(id);

            if (housingScheme != null)
            {
                _context.HousingSchemes.Remove(
                    housingScheme);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // CHECK EXISTENCE
        // =========================================================

        private bool HousingSchemeExists(int id)
        {
            return _context.HousingSchemes
                .Any(x => x.SchemeId == id);
        }
    }
}