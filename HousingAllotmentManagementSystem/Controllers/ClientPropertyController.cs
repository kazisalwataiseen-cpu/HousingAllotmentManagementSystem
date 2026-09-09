using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // CLIENT PROPERTY CONTROLLER
    // =========================================================
    //
    // Clients can:
    // 1. View properties allotted to them
    // 2. View property details including scheme & allotment
    // 3. View housing schemes associated with their properties
    //
    // Clients CANNOT see other users' properties or schemes.
    //
    // =========================================================

    [Authorize]
    public class ClientPropertyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientPropertyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MY PROPERTIES
        // GET: /ClientProperty
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get only allotments belonging to logged-in user
            var myAllotments = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .Include(a => a.Loans)
                .Where(a => a.Application.UserId == userId.Value)
                .OrderByDescending(a => a.AllotmentDate)
                .AsNoTracking()
                .ToListAsync();

            return View(myAllotments);
        }

        // =========================================================
        // PROPERTY DETAILS
        // GET: /ClientProperty/Details/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Securely load the allotment and property belonging to current user
            var allotment = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .Include(a => a.Property)
                    .ThenInclude(p => p.PropertyAmenities)
                        .ThenInclude(pa => pa.Amenity)
                .Include(a => a.Loans)
                    .ThenInclude(l => l.Emiplans)
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    (a.PropertyId == id.Value || a.AllotmentId == id.Value) &&
                    a.Application.UserId == userId.Value);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
        }

        // =========================================================
        // MY SCHEMES
        // GET: /ClientProperty/Schemes
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Schemes()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get schemes associated with user's approved/allotted applications
            var mySchemes = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)
                .Where(a => a.Application.UserId == userId.Value && a.Property != null && a.Property.Scheme != null)
                .Select(a => a.Property.Scheme)
                .Distinct()
                .AsNoTracking()
                .ToListAsync();

            return View(mySchemes);
        }

        // =========================================================
        // GET LOGGED-IN USER ID
        // =========================================================

        private int? GetLoggedInUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                userIdClaim = User.FindFirst("UserId")?.Value;
            }

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return null;
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}
