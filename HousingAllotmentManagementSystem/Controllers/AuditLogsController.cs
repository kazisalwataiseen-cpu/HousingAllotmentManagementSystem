using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;


    public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var auditLogs = await _context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .OrderByDescending(a => a.ActionDate)
                .ToListAsync();

            return View(auditLogs);
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

            var auditLog = await _context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AuditLogId == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================
        public async Task<IActionResult> Create()
        {
            await LoadUsersAsync();

            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AuditLogId,UserId,Action,TableName,RecordId,Description,Ipaddress,BrowserInfo")]
        AuditLog auditLog)
        {
            if (ModelState.IsValid)
            {
                // Set automatically when the audit record is created.
                auditLog.ActionDate = DateTime.Now;

                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Audit log created successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadUsersAsync(auditLog.UserId);

            return View(auditLog);
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

            var auditLog = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AuditLogId == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            await LoadUsersAsync(auditLog.UserId);

            return View(auditLog);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("AuditLogId,UserId,Action,TableName,RecordId,Description,Ipaddress,BrowserInfo,ActionDate")]
        AuditLog auditLog)
        {
            if (id != auditLog.AuditLogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.AuditLogs.Update(auditLog);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Audit log updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuditLogExists(auditLog.AuditLogId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            await LoadUsersAsync(auditLog.UserId);

            return View(auditLog);
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

            var auditLog = await _context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AuditLogId == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var auditLog = await _context.AuditLogs
                .FindAsync(id);

            if (auditLog == null)
            {
                return NotFound();
            }

            _context.AuditLogs.Remove(auditLog);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Audit log deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD USERS DROPDOWN
        // =========================================================
        private async Task LoadUsersAsync(int? selectedUserId = null)
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewData["UserId"] = new SelectList(
                users,
                "UserId",
                "FullName",
                selectedUserId);
        }

        // =========================================================
        // CHECK EXISTS
        // =========================================================
        private bool AuditLogExists(int id)
        {
            return _context.AuditLogs
                .Any(a => a.AuditLogId == id);
        }
    }


}
