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
    // /Notifications
    // /Notifications/Details
    // /Notifications/Create
    // /Notifications/Edit
    // /Notifications/Delete
    //
    // =========================================================

    [Authorize(Roles = "Admin")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(
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
            var notifications =
                await _context.Notifications
                    .AsNoTracking()
                    .ToListAsync();

            return View(notifications);
        }


        // =========================================================
        // DETAILS - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int? notificationid)
        {
            if (notificationid == null)
            {
                return NotFound();
            }

            var notification =
                await _context.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.NotificationId ==
                             notificationid);

            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
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
            [Bind(
                "NotificationId,UserId,Title,Message," +
                "NotificationType,IsRead,SentDate," +
                "ReadDate,Status")]
            Notification notification)
        {
            // Navigation property should not be received
            // from the form.
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Notifications.Add(
                    notification);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Index));
            }

            return View(notification);
        }


        // =========================================================
        // EDIT - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int? notificationid)
        {
            if (notificationid == null)
            {
                return NotFound();
            }

            var notification =
                await _context.Notifications
                    .FindAsync(notificationid);

            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }


        // =========================================================
        // EDIT - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int? notificationid,
            [Bind(
                "NotificationId,UserId,Title,Message," +
                "NotificationType,IsRead,SentDate," +
                "ReadDate,Status")]
            Notification notification)
        {
            if (notificationid !=
                notification.NotificationId)
            {
                return NotFound();
            }

            // Navigation property is not submitted.
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Notifications.Update(
                        notification);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(
                            notification.NotificationId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(
                    nameof(Index));
            }

            return View(notification);
        }


        // =========================================================
        // DELETE - GET - ADMIN ONLY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int? notificationid)
        {
            if (notificationid == null)
            {
                return NotFound();
            }

            var notification =
                await _context.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.NotificationId ==
                             notificationid);

            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }


        // =========================================================
        // DELETE - POST - ADMIN ONLY
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int? notificationid)
        {
            var notification =
                await _context.Notifications
                    .FindAsync(notificationid);

            if (notification != null)
            {
                _context.Notifications.Remove(
                    notification);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // CHECK NOTIFICATION EXISTS
        // =========================================================

        private bool NotificationExists(
            int? notificationid)
        {
            return _context.Notifications
                .Any(e =>
                    e.NotificationId ==
                    notificationid);
        }
    }
}