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
    // /Roles
    // /Roles/Details
    // /Roles/Create
    // /Roles/Edit
    // /Roles/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesController(
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
            var roles = await _context.Roles
                .AsNoTracking()
                .ToListAsync();

            return View(roles);
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

            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.RoleId == id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
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
            Role role)
        {
            if (!ModelState.IsValid)
            {
                return View(role);
            }

            role.CreatedDate =
                DateTime.Now;

            _context.Roles.Add(role);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
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

            var role =
                await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Role role)
        {
            if (id != role.RoleId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(role);
            }

            try
            {
                _context.Update(role);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(role.RoleId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(
                nameof(Index));
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

            var role =
                await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.RoleId == id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
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
            var role =
                await _context.Roles.FindAsync(id);

            if (role != null)
            {
                _context.Roles.Remove(role);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // CHECK ROLE EXISTS
        // =========================================================

        private bool RoleExists(int id)
        {
            return _context.Roles
                .Any(r =>
                    r.RoleId == id);
        }
    }
}