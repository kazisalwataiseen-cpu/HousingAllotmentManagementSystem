using System.Security.Claims;
using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    // =========================================================
    // CLIENT ALLOTMENT CONTROLLER
    // =========================================================
    //
    // Clients can:
    // 1. View their own allotments
    // 2. View details of their own allotment
    //
    // Clients cannot:
    // - View other users' allotments
    // - Create allotments
    // - Edit allotments
    // - Delete allotments
    //
    // Admin uses:
    // /Allotments
    //
    // Client uses:
    // /ClientAllotment
    //
    // =========================================================

    [Authorize]
    public class ClientAllotmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientAllotmentController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // MY ALLOTMENTS
        // GET: /ClientAllotment
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var allotments = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                .Where(a =>
                    a.Application.UserId == userId.Value)
                .OrderByDescending(a => a.CreatedDate)
                .AsNoTracking()
                .ToListAsync();

            return View(allotments);
        }


        // =========================================================
        // ALLOTMENT DETAILS
        // GET: /ClientAllotment/Details/5
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

            // -----------------------------------------------------
            // SECURITY:
            // The allotment must belong to the logged-in client.
            // -----------------------------------------------------

            var allotment = await _context.Allotments
                .Include(a => a.Application)
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a =>
                    a.AllotmentId == id.Value &&
                    a.Application.UserId == userId.Value);

            if (allotment == null)
            {
                return NotFound();
            }

            return View(allotment);
        }


        // =========================================================
        // GET LOGGED-IN USER ID
        // =========================================================

        private int? GetLoggedInUserId()
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return null;
            }

            if (!int.TryParse(
                userIdClaim,
                out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}