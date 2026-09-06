using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HousingAllotmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EMIPlanOptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EMIPlanOptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _context.EMIPlanOptions
                .Include(e => e.HousingScheme)
                .OrderByDescending(e => e.EMIPlanOptionId)
                .AsNoTracking()
                .ToListAsync();

            return View(plans);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.EMIPlanOptions
                .Include(e => e.HousingScheme)
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EMIPlanOptionId == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadHousingSchemes();

            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            EMIPlanOption emiPlanOption)
        {
            // -----------------------------------------------------
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // -----------------------------------------------------

            ModelState.Remove(nameof(EMIPlanOption.HousingScheme));

            // -----------------------------------------------------
            // VALIDATE HOUSING SCHEME
            // -----------------------------------------------------

            if (emiPlanOption.SchemeId <= 0)
            {
                ModelState.AddModelError(
                    nameof(EMIPlanOption.SchemeId),
                    "Please select a housing scheme.");
            }
            else
            {
                bool schemeExists =
                    await _context.HousingSchemes
                        .AnyAsync(h => h.SchemeId == emiPlanOption.SchemeId);

                if (!schemeExists)
                {
                    ModelState.AddModelError(
                        nameof(EMIPlanOption.SchemeId),
                        "Selected housing scheme does not exist.");
                }
            }

            // -----------------------------------------------------
            // PLAN NAME
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(emiPlanOption.PlanName))
            {
                emiPlanOption.PlanName =
                    emiPlanOption.PlanName.Trim();

                bool duplicate =
                    await _context.EMIPlanOptions.AnyAsync(e =>
                        e.SchemeId == emiPlanOption.SchemeId &&
                        e.PlanName.ToLower() ==
                        emiPlanOption.PlanName.ToLower());

                if (duplicate)
                {
                    ModelState.AddModelError(
                        nameof(EMIPlanOption.PlanName),
                        "This EMI plan already exists for the selected housing scheme.");
                }
            }

            // -----------------------------------------------------
            // DEFAULT STATUS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(emiPlanOption.Status))
            {
                emiPlanOption.Status = "Active";
            }

            // -----------------------------------------------------
            // CREATED DATE
            // -----------------------------------------------------

            emiPlanOption.CreatedDate = DateTime.Now;

            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            if (ModelState.IsValid)
            {
                _context.EMIPlanOptions.Add(emiPlanOption);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "EMI Plan created successfully.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // RELOAD HOUSING SCHEMES IF VALIDATION FAILS
            // -----------------------------------------------------

            await LoadHousingSchemes(
                emiPlanOption.SchemeId);

            return View(emiPlanOption);
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.EMIPlanOptions
                .FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            await LoadHousingSchemes(plan.SchemeId);

            return View(plan);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EMIPlanOption emiPlanOption)
        {
            if (id != emiPlanOption.EMIPlanOptionId)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // -----------------------------------------------------

            ModelState.Remove(nameof(EMIPlanOption.HousingScheme));

            // -----------------------------------------------------
            // VALIDATE HOUSING SCHEME
            // -----------------------------------------------------

            if (emiPlanOption.SchemeId <= 0)
            {
                ModelState.AddModelError(
                    nameof(EMIPlanOption.SchemeId),
                    "Please select a housing scheme.");
            }
            else
            {
                bool schemeExists =
                    await _context.HousingSchemes
                        .AnyAsync(h => h.SchemeId == emiPlanOption.SchemeId);

                if (!schemeExists)
                {
                    ModelState.AddModelError(
                        nameof(EMIPlanOption.SchemeId),
                        "Selected housing scheme does not exist.");
                }
            }

            // -----------------------------------------------------
            // PLAN NAME
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(emiPlanOption.PlanName))
            {
                emiPlanOption.PlanName =
                    emiPlanOption.PlanName.Trim();

                bool duplicate =
                    await _context.EMIPlanOptions.AnyAsync(e =>
                        e.EMIPlanOptionId !=
                        emiPlanOption.EMIPlanOptionId &&

                        e.SchemeId ==
                        emiPlanOption.SchemeId &&

                        e.PlanName.ToLower() ==
                        emiPlanOption.PlanName.ToLower());

                if (duplicate)
                {
                    ModelState.AddModelError(
                        nameof(EMIPlanOption.PlanName),
                        "This EMI plan already exists for the selected housing scheme.");
                }
            }

            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadHousingSchemes(
                    emiPlanOption.SchemeId);

                return View(emiPlanOption);
            }

            // -----------------------------------------------------
            // UPDATE
            // -----------------------------------------------------

            try
            {
                var existingPlan =
                    await _context.EMIPlanOptions
                        .FirstOrDefaultAsync(e =>
                            e.EMIPlanOptionId == id);

                if (existingPlan == null)
                {
                    return NotFound();
                }

                existingPlan.SchemeId =
                    emiPlanOption.SchemeId;

                existingPlan.PlanName =
                    emiPlanOption.PlanName;

                existingPlan.TenureMonths =
                    emiPlanOption.TenureMonths;

                existingPlan.InterestRate =
                    emiPlanOption.InterestRate;

                existingPlan.Description =
                    emiPlanOption.Description;

                existingPlan.Status =
                    emiPlanOption.Status;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "EMI Plan updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EMIPlanOptionExists(
                    emiPlanOption.EMIPlanOptionId))
                {
                    return NotFound();
                }

                throw;
            }
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.EMIPlanOptions
                .Include(e => e.HousingScheme)
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EMIPlanOptionId == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var plan =
                await _context.EMIPlanOptions
                    .FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            _context.EMIPlanOptions.Remove(plan);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "EMI Plan deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD HOUSING SCHEMES
        // =========================================================

        private async Task LoadHousingSchemes(
            int? selectedId = null)
        {
            var schemes =
                await _context.HousingSchemes
                    .OrderBy(h => h.SchemeName)
                    .ToListAsync();

            ViewBag.SchemeId = new SelectList(
                schemes,
                "SchemeId",
                "SchemeName",
                selectedId);
        }

        // =========================================================
        // EXISTS
        // =========================================================

        private bool EMIPlanOptionExists(int id)
        {
            return _context.EMIPlanOptions
                .Any(e =>
                    e.EMIPlanOptionId == id);
        }
    }
}