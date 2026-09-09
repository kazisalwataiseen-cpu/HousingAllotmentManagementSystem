using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Models;
using HousingAllotmentManagementSystem.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

    // =========================================================
    // PROPERTY STATUS
    // =========================================================

    private const string PropertyStatusAvailable = "Available";
        private const string PropertyStatusReserved = "Reserved";

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ApplicationsController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        // =========================================================
        // INDEX - ADMIN
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.Applications
                .Include(a => a.User)

                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)

                .OrderByDescending(a => a.ApplicationId)

                .AsNoTracking()

                .ToListAsync();

            return View(applications);
        }


        // =========================================================
        // DETAILS - ADMIN
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(
            int? applicationid)
        {
            if (applicationid == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.User)

                .Include(a => a.Property)
                    .ThenInclude(p => p.Scheme)

                .AsNoTracking()

                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationid.Value);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }


        // =========================================================
        // APPROVE APPLICATION - ADMIN
        // =========================================================
        //
        // Shortcut action used from Applications/Index.
        //
        // Pending
        //    ↓
        // Approve
        //    ↓
        // Approved
        //
        // The property remains Reserved.
        //
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id)
        {
            var application =
                await _context.Applications

                    .Include(a => a.User)

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // ONLY PENDING APPLICATIONS CAN BE APPROVED
            // -----------------------------------------------------

            if (!string.Equals(
                    application.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only pending applications can be approved.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // VERIFY PROPERTY
            // -----------------------------------------------------

            if (application.PropertyId <= 0)
            {
                TempData["ErrorMessage"] =
                    "This application does not have a valid property.";

                return RedirectToAction(
                    nameof(Index));
            }


            var property =
                await _context.Properties
                    .FirstOrDefaultAsync(p =>
                        p.PropertyId ==
                        application.PropertyId);

            if (property == null)
            {
                TempData["ErrorMessage"] =
                    "The property linked to this application could not be found.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // PROPERTY MUST BE RESERVED
            // -----------------------------------------------------
            //
            // Normally a submitted application reserves the
            // property.
            //
            // If property is still Available, we reserve it now.
            //
            // If it is already Reserved, approval can continue.
            //
            // -----------------------------------------------------

            if (string.Equals(
                    property.Status,
                    PropertyStatusAvailable,
                    StringComparison.OrdinalIgnoreCase))
            {
                property.Status =
                    PropertyStatusReserved;
            }
            else if (!string.Equals(
                         property.Status,
                         PropertyStatusReserved,
                         StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "The property linked to this application is not available for approval.";

                return RedirectToAction(
                    nameof(Index));
            }


            // -----------------------------------------------------
            // UPDATE APPLICATION
            // -----------------------------------------------------

            application.Status =
                "Approved";

            application.UpdatedDate =
                DateTime.Now;

            application.Remarks =
                "Application approved by administrator.";

            try
            {
                await _context.SaveChangesAsync();


                // -------------------------------------------------
                // SEND APPROVAL EMAIL
                // -------------------------------------------------

                await SendStatusEmailAsync(
                    application.ApplicationId,
                    "Approved");


                TempData["SuccessMessage"] =
                    $"Application #{application.ApplicationId} approved successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] =
                    "Database error while approving application: " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to approve application: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Index));
            }
        }


        // =========================================================
        // REJECT APPLICATION - ADMIN
        // =========================================================
        //
        // Shortcut action used from Applications/Index.
        //
        // Pending
        //    ↓
        // Reject
        //    ↓
        // Rejected
        //
        // The reserved property is released.
        //
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id)
        {
            var application =
                await _context.Applications

                    .Include(a => a.User)

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // ONLY PENDING APPLICATIONS CAN BE REJECTED
            // -----------------------------------------------------

            if (!string.Equals(
                    application.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only pending applications can be rejected.";

                return RedirectToAction(
                    nameof(Index));
            }


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // -------------------------------------------------
                // RELEASE PROPERTY
                // -------------------------------------------------

                if (application.PropertyId > 0)
                {
                    var property =
                        await _context.Properties
                            .FirstOrDefaultAsync(p =>
                                p.PropertyId ==
                                application.PropertyId);

                    if (property != null &&
                        string.Equals(
                            property.Status,
                            PropertyStatusReserved,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        property.Status =
                            PropertyStatusAvailable;
                    }
                }


                // -------------------------------------------------
                // UPDATE APPLICATION
                // -------------------------------------------------

                application.Status =
                    "Rejected";

                application.UpdatedDate =
                    DateTime.Now;

                application.Remarks =
                    "Application rejected by administrator.";


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // -------------------------------------------------
                // SEND REJECTION EMAIL
                // -------------------------------------------------

                await SendStatusEmailAsync(
                    application.ApplicationId,
                    "Rejected");


                TempData["SuccessMessage"] =
                    $"Application #{application.ApplicationId} rejected successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Database error while rejecting application: " +
                    (ex.InnerException?.Message ?? ex.Message);

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Unable to reject application: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Index));
            }
        }


        // =========================================================
        // MY APPLICATION DETAILS - LOGGED-IN USER
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyApplicationDetails(
            int? applicationId)
        {
            if (applicationId == null)
            {
                return NotFound();
            }

            int? userId =
                GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var application =
                await _context.Applications

                    .Include(a => a.User)

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .Include(a => a.Allotments)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId ==
                            applicationId.Value &&
                        a.UserId ==
                            userId.Value);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }


        // =========================================================
        // CREATE - GET - ADMIN
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            LoadDropDowns();

            var application = new Application
            {
                ApplicationDate =
                    DateTime.Now,

                CreatedDate =
                    DateTime.Now,

                Status =
                    "Pending"
            };

            return View(application);
        }


        // =========================================================
        // CREATE - POST - ADMIN
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Application application)
        {
            ModelState.Remove("User");
            ModelState.Remove("Property");

            ModelState.Remove("Status");

            application.Status =
                "Pending";


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (application.UserId <= 0)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Please select an applicant.");
            }
            else
            {
                bool userExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.UserId ==
                            application.UserId);

                if (!userExists)
                {
                    ModelState.AddModelError(
                        "UserId",
                        "Selected applicant does not exist.");
                }
            }


            // -----------------------------------------------------
            // PROPERTY VALIDATION
            // -----------------------------------------------------

            if (application.PropertyId <= 0)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }
            else
            {
                var property =
                    await _context.Properties
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                            application.PropertyId);

                if (property == null)
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "Selected property does not exist.");
                }
            }


            // -----------------------------------------------------
            // VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                LoadDropDowns(
                    application.UserId,
                    application.PropertyId);

                return View(application);
            }


            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                application.ApplicationDate =
                    DateTime.Now;

                application.CreatedDate =
                    DateTime.Now;

                application.UpdatedDate =
                    null;

                application.Status =
                    "Pending";

                application.User =
                    null!;

                application.Property =
                    null!;


                _context.Applications.Add(
                    application);

                await _context.SaveChangesAsync();


                // Reserve the property when an application is created.

                var selectedProperty =
                    await _context.Properties
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                            application.PropertyId);

                if (selectedProperty != null &&
                    string.Equals(
                        selectedProperty.Status,
                        PropertyStatusAvailable,
                        StringComparison.OrdinalIgnoreCase))
                {
                    selectedProperty.Status =
                        PropertyStatusReserved;

                    await _context.SaveChangesAsync();
                }


                await transaction.CommitAsync();


                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                await SendSubmittedEmailAsync(
                    application.ApplicationId);


                TempData["SuccessMessage"] =
                    "Application created successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Database error while saving application: " +
                    (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Error while saving application: " +
                    ex.Message);
            }


            LoadDropDowns(
                application.UserId,
                application.PropertyId);

            return View(application);
        }


        // =========================================================
        // EDIT - GET - ADMIN
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int? applicationid)
        {
            if (applicationid == null)
            {
                return NotFound();
            }

            var application =
                await _context.Applications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId ==
                        applicationid.Value);

            if (application == null)
            {
                return NotFound();
            }

            LoadDropDowns(
                application.UserId,
                application.PropertyId);

            return View(application);
        }


        // =========================================================
        // EDIT - POST - ADMIN
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int applicationid,
            Application application)
        {
            if (applicationid !=
                application.ApplicationId)
            {
                return NotFound();
            }

            ModelState.Remove("User");
            ModelState.Remove("Property");


            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

            if (application.UserId <= 0)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Please select an applicant.");
            }
            else
            {
                bool userExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.UserId ==
                            application.UserId);

                if (!userExists)
                {
                    ModelState.AddModelError(
                        "UserId",
                        "Selected applicant does not exist.");
                }
            }


            // -----------------------------------------------------
            // PROPERTY VALIDATION
            // -----------------------------------------------------

            if (application.PropertyId <= 0)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }
            else
            {
                bool propertyExists =
                    await _context.Properties
                        .AnyAsync(p =>
                            p.PropertyId ==
                            application.PropertyId);

                if (!propertyExists)
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "Selected property does not exist.");
                }
            }


            if (!ModelState.IsValid)
            {
                LoadDropDowns(
                    application.UserId,
                    application.PropertyId);

                return View(application);
            }


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var existingApplication =
                    await _context.Applications
                        .FirstOrDefaultAsync(a =>
                            a.ApplicationId ==
                            applicationid);

                if (existingApplication == null)
                {
                    return NotFound();
                }


                int oldPropertyId =
                    existingApplication.PropertyId;

                string oldStatus =
                    existingApplication.Status ??
                    "Pending";


                // -------------------------------------------------
                // UPDATE INFORMATION
                // -------------------------------------------------

                existingApplication.UserId =
                    application.UserId;

                existingApplication.PropertyId =
                    application.PropertyId;

                existingApplication.EmploymentType =
                    application.EmploymentType;

                existingApplication.AnnualIncome =
                    application.AnnualIncome;

                existingApplication.NomineeName =
                    application.NomineeName;

                existingApplication.NomineeRelation =
                    application.NomineeRelation;

                existingApplication.Remarks =
                    application.Remarks;


                // -------------------------------------------------
                // STATUS
                // -------------------------------------------------

                string newStatus =
                    string.IsNullOrWhiteSpace(
                        application.Status)
                            ? "Pending"
                            : application.Status.Trim();

                bool statusChanged =
                    !string.Equals(
                        oldStatus,
                        newStatus,
                        StringComparison.OrdinalIgnoreCase);

                existingApplication.Status =
                    newStatus;

                existingApplication.UpdatedDate =
                    DateTime.Now;


                // -------------------------------------------------
                // REJECTED
                // -------------------------------------------------

                if (newStatus.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var rejectedProperty =
                        await _context.Properties
                            .FirstOrDefaultAsync(p =>
                                p.PropertyId ==
                                existingApplication.PropertyId);

                    if (rejectedProperty != null &&
                        string.Equals(
                            rejectedProperty.Status,
                            PropertyStatusReserved,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        rejectedProperty.Status =
                            PropertyStatusAvailable;
                    }
                }


                // -------------------------------------------------
                // PROPERTY CHANGED
                // -------------------------------------------------

                if (oldPropertyId !=
                    existingApplication.PropertyId)
                {
                    var oldProperty =
                        await _context.Properties
                            .FirstOrDefaultAsync(p =>
                                p.PropertyId ==
                                oldPropertyId);

                    if (oldProperty != null &&
                        string.Equals(
                            oldProperty.Status,
                            PropertyStatusReserved,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        oldProperty.Status =
                            PropertyStatusAvailable;
                    }


                    if (!newStatus.Equals(
                        "Rejected",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var newProperty =
                            await _context.Properties
                                .FirstOrDefaultAsync(p =>
                                    p.PropertyId ==
                                    existingApplication.PropertyId);

                        if (newProperty != null)
                        {
                            newProperty.Status =
                                PropertyStatusReserved;
                        }
                    }
                }


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // -------------------------------------------------
                // SEND STATUS EMAIL
                // -------------------------------------------------

                if (statusChanged)
                {
                    await SendStatusEmailAsync(
                        existingApplication.ApplicationId,
                        newStatus);
                }


                TempData["SuccessMessage"] =
                    "Application updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Database error while updating application: " +
                    (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Error while updating application: " +
                    ex.Message);
            }


            LoadDropDowns(
                application.UserId,
                application.PropertyId);

            return View(application);
        }


        // =========================================================
        // APPLY - GET - CLIENT
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Apply(
            int? schemeId)
        {
            if (schemeId == null)
            {
                return NotFound();
            }


            int? userId =
                GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        returnUrl =
                            $"/Applications/Apply?schemeId={schemeId}"
                    });
            }


            var scheme =
                await _context.HousingSchemes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.SchemeId ==
                        schemeId.Value);

            if (scheme == null)
            {
                return NotFound();
            }


            var currentUser =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.UserId ==
                        userId.Value);

            if (currentUser == null)
            {
                return NotFound();
            }


            var properties =
                await GetAvailablePropertiesAsync(
                    schemeId.Value);


            bool alreadyAppliedForScheme =
                await _context.Applications
                    .AnyAsync(a =>
                        a.UserId ==
                            userId.Value &&
                        a.Property != null &&
                        a.Property.SchemeId ==
                            schemeId.Value);


            var application = new Application
            {
                UserId =
                    userId.Value,

                ApplicationDate =
                    DateTime.Now,

                CreatedDate =
                    DateTime.Now,

                Status =
                    "Pending"
            };


            ViewBag.Scheme =
                scheme;

            ViewBag.Properties =
                properties;

            ViewBag.CurrentUser =
                currentUser;

            ViewBag.AlreadyApplied =
                alreadyAppliedForScheme;

            return View(application);
        }


        // =========================================================
        // APPLY - POST - CLIENT
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            int schemeId,
            Application application)
        {
            ModelState.Remove("User");
            ModelState.Remove("Property");
            ModelState.Remove("Status");


            int? userId =
                GetLoggedInUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        returnUrl =
                            $"/Applications/Apply?schemeId={schemeId}"
                    });
            }


            application.UserId =
                userId.Value;

            application.Status =
                "Pending";


            var scheme =
                await _context.HousingSchemes
                    .FirstOrDefaultAsync(s =>
                        s.SchemeId ==
                        schemeId);

            if (scheme == null)
            {
                return NotFound();
            }


            var currentUser =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId ==
                        userId.Value);

            if (currentUser == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // USER INFORMATION
            // -----------------------------------------------------

            string applicantName =
                currentUser.FullName?.Trim()
                ?? string.Empty;

            string applicantEmail =
                currentUser.Email?.Trim()
                ?? string.Empty;

            string applicantMobile =
                currentUser.Mobile?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                applicantName))
            {
                ModelState.AddModelError(
                    "",
                    "Your name is missing from your user profile. Please update your profile before applying.");
            }


            if (string.IsNullOrWhiteSpace(
                applicantEmail))
            {
                ModelState.AddModelError(
                    "",
                    "Your email address is missing from your user profile. Please update your profile before applying.");
            }
            else
            {
                var emailValidator =
                    new System.ComponentModel.DataAnnotations
                        .EmailAddressAttribute();

                if (!emailValidator.IsValid(
                    applicantEmail))
                {
                    ModelState.AddModelError(
                        "",
                        "Your registered email address is not valid. Please update your profile.");
                }
            }


            if (string.IsNullOrWhiteSpace(
                applicantMobile))
            {
                ModelState.AddModelError(
                    "",
                    "Your mobile number is missing from your user profile. Please update your profile before applying.");
            }


            // -----------------------------------------------------
            // PROPERTY VALIDATION
            // -----------------------------------------------------

            if (application.PropertyId <= 0)
            {
                ModelState.AddModelError(
                    "PropertyId",
                    "Please select a property.");
            }
            else
            {
                var property =
                    await _context.Properties
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                                application.PropertyId &&
                            p.SchemeId ==
                                schemeId);

                if (property == null)
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "The selected property does not belong to this housing scheme.");
                }
                else if (
                    string.IsNullOrWhiteSpace(
                        property.Status) ||
                    !property.Status.Equals(
                        PropertyStatusAvailable,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "PropertyId",
                        "The selected property is no longer available.");
                }
            }


            // -----------------------------------------------------
            // PREVIOUS APPLICATION
            // -----------------------------------------------------

            bool alreadyAppliedForScheme =
                await _context.Applications
                    .AnyAsync(a =>
                        a.UserId ==
                            userId.Value &&
                        a.Property != null &&
                        a.Property.SchemeId ==
                            schemeId);


            if (!ModelState.IsValid)
            {
                return await ReturnApplyValidationFailedView(
                    schemeId,
                    scheme,
                    currentUser,
                    application,
                    alreadyAppliedForScheme);
            }


            application.ApplicationDate =
                DateTime.Now;

            application.CreatedDate =
                DateTime.Now;

            application.UpdatedDate =
                null;

            application.Status =
                "Pending";

            application.User =
                null!;

            application.Property =
                null!;


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var selectedProperty =
                    await _context.Properties
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                                application.PropertyId &&
                            p.SchemeId ==
                                schemeId);

                if (selectedProperty == null)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        "PropertyId",
                        "The selected property could not be found.");

                    return await ReturnApplyValidationFailedView(
                        schemeId,
                        scheme,
                        currentUser,
                        application,
                        alreadyAppliedForScheme);
                }


                if (!string.Equals(
                        selectedProperty.Status,
                        PropertyStatusAvailable,
                        StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        "PropertyId",
                        "The selected property is no longer available. Please select another property.");

                    return await ReturnApplyValidationFailedView(
                        schemeId,
                        scheme,
                        currentUser,
                        application,
                        alreadyAppliedForScheme);
                }


                _context.Applications.Add(
                    application);


                selectedProperty.Status =
                    PropertyStatusReserved;


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Database error while saving application: " +
                    (ex.InnerException?.Message ?? ex.Message));

                return await ReturnApplyValidationFailedView(
                    schemeId,
                    scheme,
                    currentUser,
                    application,
                    alreadyAppliedForScheme);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Error while saving application: " +
                    ex.Message);

                return await ReturnApplyValidationFailedView(
                    schemeId,
                    scheme,
                    currentUser,
                    application,
                    alreadyAppliedForScheme);
            }


            // -----------------------------------------------------
            // CONFIRMATION EMAIL
            // -----------------------------------------------------

            bool emailSent =
                false;

            try
            {
                var property =
                    await _context.Properties
                        .Include(p => p.Scheme)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.PropertyId ==
                            application.PropertyId);

                string schemeName =
                    property?.Scheme?.SchemeName ??
                    scheme.SchemeName ??
                    "Housing Scheme";

                string unitNumber =
                    property?.UnitNumber ??
                    "N/A";

                string propertyType =
                    property?.PropertyType ??
                    "Property";

                string price =
                    property?.Price.HasValue == true
                        ? "₹" +
                          property.Price.Value
                              .ToString("N2")
                        : "N/A";


                string subject =
                    "Housing Application Submitted Successfully";


                string previousApplicationMessage =
                    alreadyAppliedForScheme
                        ? "You have previously submitted an application for this housing scheme. This new application has also been recorded successfully."
                        : "This is your first application for this housing scheme.";


                string body =
                    BuildSubmittedEmailBody(
                        applicantName,
                        applicantEmail,
                        applicantMobile,
                        application.ApplicationId,
                        schemeName,
                        unitNumber,
                        propertyType,
                        price,
                        previousApplicationMessage);


                await _emailService.SendEmailAsync(
                    applicantEmail,
                    subject,
                    body);

                emailSent =
                    true;
            }
            catch (Exception emailEx)
            {
                Console.WriteLine(
                    "Public application email failed: " +
                    emailEx.Message);
            }


            if (alreadyAppliedForScheme)
            {
                TempData["SuccessMessage"] =
                    emailSent
                        ? "You already had an application for this scheme, but the new application was submitted successfully and a confirmation email was sent."
                        : "You already had an application for this scheme, but the new application was submitted successfully. The confirmation email could not be sent.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    emailSent
                        ? "Application submitted successfully. Confirmation email sent to your registered email address."
                        : "Application submitted successfully. However, the confirmation email could not be sent.";
            }


            TempData["ApplicationEmailSent"] =
                emailSent
                    ? "true"
                    : "false";


            return RedirectToAction(
                nameof(Success),
                new
                {
                    applicationId =
                        application.ApplicationId
                });
        }


        // =========================================================
        // MY APPLICATIONS
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyApplications()
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(
                    userIdClaim.Value,
                    out int userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var applications =
                await _context.Applications

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .Include(a => a.Allotments)

                    .Where(a =>
                        a.UserId == userId)

                    .OrderByDescending(
                        a => a.ApplicationId)

                    .AsNoTracking()

                    .ToListAsync();


            return View(
                "~/Views/Applications/MyApplications.cshtml",
                applications);
        }


        // =========================================================
        // SUCCESS PAGE
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Success(
            int? applicationId)
        {
            if (applicationId == null)
            {
                return NotFound();
            }


            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(
                    userIdClaim.Value,
                    out int userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var application =
                await _context.Applications

                    .Include(a => a.User)

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId ==
                            applicationId.Value &&
                        a.UserId ==
                            userId);

            if (application == null)
            {
                return NotFound();
            }


            ViewBag.EmailSent =
                TempData["ApplicationEmailSent"]?
                    .ToString() ==
                "true";


            return View(application);
        }


        // =========================================================
        // DELETE - GET - ADMIN
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(
            int? applicationid)
        {
            if (applicationid == null)
            {
                return NotFound();
            }


            var application =
                await _context.Applications

                    .Include(a => a.User)

                    .Include(a => a.Property)
                        .ThenInclude(p => p.Scheme)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId ==
                        applicationid.Value);

            if (application == null)
            {
                return NotFound();
            }


            return View(application);
        }


        // =========================================================
        // DELETE - POST - ADMIN
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int applicationid)
        {
            var application =
                await _context.Applications
                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId ==
                        applicationid);

            if (application == null)
            {
                return NotFound();
            }


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // -------------------------------------------------
                // RELEASE PROPERTY
                // -------------------------------------------------

                if (application.PropertyId > 0)
                {
                    var property =
                        await _context.Properties
                            .FirstOrDefaultAsync(p =>
                                p.PropertyId ==
                                application.PropertyId);

                    if (property != null &&
                        string.Equals(
                            property.Status,
                            PropertyStatusReserved,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        property.Status =
                            PropertyStatusAvailable;
                    }
                }


                _context.Applications.Remove(
                    application);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["SuccessMessage"] =
                    "Application deleted successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "This application cannot be deleted because it is linked with another record.";

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        applicationid
                    });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Error while deleting application: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Delete),
                    new
                    {
                        applicationid
                    });
            }
        }


        // =========================================================
        // LOAD DROPDOWNS - ADMIN
        // =========================================================

        private void LoadDropDowns(
            int? selectedUserId = null,
            int? selectedPropertyId = null)
        {
            // -----------------------------------------------------
            // USERS
            // -----------------------------------------------------

            var users =
                _context.Users
                    .AsNoTracking()
                    .OrderBy(u => u.FullName)
                    .ToList();


            ViewBag.UserId =
                new SelectList(
                    users,
                    "UserId",
                    "FullName",
                    selectedUserId);


            // -----------------------------------------------------
            // PROPERTIES
            // -----------------------------------------------------

            var properties =
                _context.Properties
                    .Include(p => p.Scheme)
                    .AsNoTracking()
                    .OrderBy(p =>
                        p.Scheme.SchemeName)
                    .ThenBy(p =>
                        p.UnitNumber)
                    .ToList();


            var propertyList =
                properties.Select(p =>
                    new
                    {
                        PropertyId =
                            p.PropertyId,

                        DisplayText =
                            (p.Scheme?.SchemeName ??
                             "Unknown Scheme") +

                            " | " +

                            (
                                string.IsNullOrWhiteSpace(
                                    p.UnitNumber)
                                    ? "No Unit"
                                    : "Unit " +
                                      p.UnitNumber
                            ) +

                            (
                                string.IsNullOrWhiteSpace(
                                    p.PlotNumber)
                                    ? ""
                                    : " | Plot " +
                                      p.PlotNumber
                            ) +

                            (
                                string.IsNullOrWhiteSpace(
                                    p.PropertyType)
                                    ? ""
                                    : " | " +
                                      p.PropertyType
                            ) +

                            " | Property #" +
                            p.PropertyId
                    });


            ViewBag.PropertyId =
                new SelectList(
                    propertyList,
                    "PropertyId",
                    "DisplayText",
                    selectedPropertyId);
        }


        // =========================================================
        // AVAILABLE PROPERTIES
        // =========================================================

        private async Task<List<Property>>
            GetAvailablePropertiesAsync(
                int schemeId)
        {
            return await _context.Properties

                .Where(p =>
                    p.SchemeId ==
                        schemeId &&

                    p.Status !=
                        null &&

                    p.Status.ToLower() ==
                        PropertyStatusAvailable
                            .ToLower())

                .Include(p => p.Scheme)

                .AsNoTracking()

                .OrderBy(p =>
                    p.UnitNumber)

                .ToListAsync();
        }


        // =========================================================
        // RETURN APPLY VIEW AFTER VALIDATION FAILURE
        // =========================================================

        private async Task<IActionResult>
            ReturnApplyValidationFailedView(
                int schemeId,
                HousingScheme scheme,
                User currentUser,
                Application application,
                bool alreadyAppliedForScheme)
        {
            var properties =
                await GetAvailablePropertiesAsync(
                    schemeId);


            ViewBag.Scheme =
                scheme;

            ViewBag.Properties =
                properties;

            ViewBag.CurrentUser =
                currentUser;

            ViewBag.AlreadyApplied =
                alreadyAppliedForScheme;


            return View(
                "Apply",
                application);
        }


        // =========================================================
        // GET LOGGED-IN USER ID
        // =========================================================

        private int? GetLoggedInUserId()
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return null;
            }


            if (!int.TryParse(
                    userIdClaim.Value,
                    out int userId))
            {
                return null;
            }


            return userId;
        }


        // =========================================================
        // SEND SUBMITTED EMAIL
        // =========================================================

        private async Task SendSubmittedEmailAsync(
            int applicationId)
        {
            try
            {
                var application =
                    await _context.Applications

                        .Include(a => a.User)

                        .Include(a => a.Property)
                            .ThenInclude(p => p.Scheme)

                        .AsNoTracking()

                        .FirstOrDefaultAsync(a =>
                            a.ApplicationId ==
                            applicationId);

                if (application == null ||
                    application.User == null ||
                    string.IsNullOrWhiteSpace(
                        application.User.Email))
                {
                    return;
                }


                string applicantName =
                    application.User.FullName ??
                    "Applicant";

                string applicantEmail =
                    application.User.Email;

                string applicantMobile =
                    application.User.Mobile ??
                    "N/A";

                string schemeName =
                    application.Property?
                        .Scheme?
                        .SchemeName ??
                    "Housing Scheme";

                string propertyName =
                    application.Property?
                        .UnitNumber ??
                    "N/A";

                string applicationDate =
                    application.ApplicationDate
                        .ToString("dd-MM-yyyy");


                string body =
                    BuildStatusEmailBody(
                        "Housing Application Submitted",
                        "#2c3e50",
                        applicantName,
                        application.ApplicationId,
                        schemeName,
                        propertyName,
                        applicationDate,
                        null,
                        null,
                        "Pending",
                        "#212529",
                        "Your housing application has been successfully submitted and is currently under review.",
                        null);


                await _emailService.SendEmailAsync(
                    applicantEmail,
                    "Housing Application Submitted Successfully",
                    body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Admin application email failed: " +
                    ex.Message);
            }
        }


        // =========================================================
        // SEND STATUS EMAIL
        // =========================================================

        private async Task SendStatusEmailAsync(
            int applicationId,
            string newStatus)
        {
            try
            {
                var application =
                    await _context.Applications

                        .Include(a => a.User)

                        .Include(a => a.Property)
                            .ThenInclude(p => p.Scheme)

                        .AsNoTracking()

                        .FirstOrDefaultAsync(a =>
                            a.ApplicationId ==
                            applicationId);

                if (application == null ||
                    application.User == null ||
                    string.IsNullOrWhiteSpace(
                        application.User.Email))
                {
                    return;
                }


                string applicantName =
                    application.User.FullName ??
                    "Applicant";

                string schemeName =
                    application.Property?
                        .Scheme?
                        .SchemeName ??
                    "Housing Scheme";

                string propertyName =
                    application.Property?
                        .UnitNumber ??
                    "N/A";

                string applicationDate =
                    application.ApplicationDate
                        .ToString("dd-MM-yyyy");

                string updatedDate =
                    application.UpdatedDate?
                        .ToString("dd-MM-yyyy")
                    ?? DateTime.Now
                        .ToString("dd-MM-yyyy");


                // -------------------------------------------------
                // APPROVED
                // -------------------------------------------------

                if (newStatus.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string body =
                        BuildStatusEmailBody(
                            "Housing Application Approved",
                            "#198754",
                            applicantName,
                            application.ApplicationId,
                            schemeName,
                            propertyName,
                            applicationDate,
                            "Approval Date",
                            updatedDate,
                            "Approved",
                            "#198754",
                            "Congratulations! Your housing application has been successfully approved.<br/><br/>Please contact the housing administration office for further allotment and payment instructions.",
                            null);


                    await _emailService.SendEmailAsync(
                        application.User.Email,
                        "Housing Application Approved",
                        body);
                }


                // -------------------------------------------------
                // REJECTED
                // -------------------------------------------------

                else if (newStatus.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string remarks =
                        string.IsNullOrWhiteSpace(
                            application.Remarks)
                            ? "No additional remarks were provided."
                            : application.Remarks;


                    string body =
                        BuildStatusEmailBody(
                            "Housing Application Status Update",
                            "#dc3545",
                            applicantName,
                            application.ApplicationId,
                            schemeName,
                            propertyName,
                            applicationDate,
                            "Status Update Date",
                            updatedDate,
                            "Rejected",
                            "#dc3545",
                            "Your housing application has been rejected. If you have any questions regarding this decision, please contact the housing administration office.",
                            remarks);


                    await _emailService.SendEmailAsync(
                        application.User.Email,
                        "Housing Application Rejected",
                        body);
                }
            }
            catch (Exception emailEx)
            {
                Console.WriteLine(
                    "Application status email failed: " +
                    emailEx.Message);
            }
        }


        // =========================================================
        // BUILD SUBMITTED EMAIL
        // =========================================================

        private static string BuildSubmittedEmailBody(
            string applicantName,
            string applicantEmail,
            string applicantMobile,
            int applicationId,
            string schemeName,
            string unitNumber,
            string propertyType,
            string price,
            string previousApplicationMessage)
        {
            string safeApplicantName =
                WebUtility.HtmlEncode(
                    applicantName ?? string.Empty);

            string safeApplicantEmail =
                WebUtility.HtmlEncode(
                    applicantEmail ?? string.Empty);

            string safeApplicantMobile =
                WebUtility.HtmlEncode(
                    applicantMobile ?? string.Empty);

            string safeSchemeName =
                WebUtility.HtmlEncode(
                    schemeName ?? string.Empty);

            string safeUnitNumber =
                WebUtility.HtmlEncode(
                    unitNumber ?? string.Empty);

            string safePropertyType =
                WebUtility.HtmlEncode(
                    propertyType ?? string.Empty);

            string safePrice =
                WebUtility.HtmlEncode(
                    price ?? string.Empty);

            string safePreviousMessage =
                WebUtility.HtmlEncode(
                    previousApplicationMessage ??
                    string.Empty);


            return $@"

<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<title>
Housing Application
</title>

</head>

<body style='font-family:Arial,sans-serif;
             background:#f5f7fb;
             padding:30px;'>

<div style='max-width:650px;
            margin:auto;
            background:white;
            padding:30px;
            border-radius:12px;'>

<h2 style='color:#2563eb;'>
Housing Application Submitted
</h2>

<p>
Dear <strong>{safeApplicantName}</strong>,
</p>

<p>
Your housing application has been
successfully submitted.
</p>

<table style='width:100%;
              border-collapse:collapse;'>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Application ID</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{applicationId}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Applicant Name</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeApplicantName}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Email</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeApplicantEmail}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Mobile</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeApplicantMobile}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Housing Scheme</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeSchemeName}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Property / Unit</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeUnitNumber}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Property Type</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safePropertyType}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Price</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safePrice}
</td>
</tr>

<tr>
<td style='padding:10px;border:1px solid #ddd;'>
<strong>Status</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
Pending
</td>
</tr>

</table>

<p style='margin-top:25px;'>
<strong>Application Notice:</strong><br>
{safePreviousMessage}
</p>

<p>
You will receive another notification when
your application status changes.
</p>

<p>
Thank you for using the Housing Allotment Management System.
</p>

<hr>

<p style='font-size:12px;color:#777;'>
This is an automated email.
Please do not reply to this email.
</p>

</div>

</body>

</html>";
        }

    // =========================================================
    // BUILD STATUS EMAIL
    // =========================================================

    private static string BuildStatusEmailBody(
        string headerTitle,
        string headerColor,
        string applicantName,
        int applicationId,
        string schemeName,
        string propertyName,
        string applicationDate,
        string? dateLabel,
        string? dateValue,
        string statusText,
        string statusColor,
        string messageHtml,
        string? remarks)
        {
            string safeApplicantName =
                WebUtility.HtmlEncode(
                    applicantName ?? string.Empty);

            string safeSchemeName =
                WebUtility.HtmlEncode(
                    schemeName ?? string.Empty);

            string safePropertyName =
                WebUtility.HtmlEncode(
                    propertyName ?? string.Empty);

            string safeStatusText =
                WebUtility.HtmlEncode(
                    statusText ?? string.Empty);

            string safeHeaderTitle =
                WebUtility.HtmlEncode(
                    headerTitle ?? string.Empty);


            string extraDateRow =
                string.IsNullOrWhiteSpace(
                    dateLabel)
                    ? string.Empty
                    : $@"

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
{WebUtility.HtmlEncode(dateLabel)}
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{WebUtility.HtmlEncode(dateValue ?? string.Empty)}
</td>

</tr>";


        string remarksRow =
            string.IsNullOrWhiteSpace(
                remarks)
                ? string.Empty
                : $@"


<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Remarks
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{WebUtility.HtmlEncode(remarks)}
</td>

</tr>";

        return $@"


<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<title>
{safeHeaderTitle}
</title>

</head>

<body style='font-family:Arial,sans-serif;
             background-color:#f5f6fa;
             padding:30px;'>

<div style='max-width:650px;
            margin:auto;
            background-color:white;
            padding:30px;
            border-radius:10px;'>

<h2 style='color:{headerColor};'>
{safeHeaderTitle}
</h2>

<p>
Dear <strong>{safeApplicantName}</strong>,
</p>

<p>
{messageHtml}
</p>

<table style='width:100%;
              border-collapse:collapse;'>

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Application ID
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{applicationId}
</td>

</tr>

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Housing Scheme
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safeSchemeName}
</td>

</tr>

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Property / Unit
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{safePropertyName}
</td>

</tr>

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Application Date
</strong>
</td>

<td style='padding:10px;border:1px solid #ddd;'>
{WebUtility.HtmlEncode(applicationDate)}
</td>

</tr>

{extraDateRow}

<tr>

<td style='padding:10px;border:1px solid #ddd;'>
<strong>
Status
</strong>
</td>

<td style='padding:10px;
           border:1px solid #ddd;
           color:{statusColor};'>

<strong>
{safeStatusText}
</strong>

</td>

</tr>

{remarksRow}

</table>

<p style='margin-top:25px;'>

Thank you for using the
Housing Allotment Management System.

</p>

<hr>

<p style='font-size:12px;color:#777;'>

This is an automated email.
Please do not reply to this email.

</p>

</div>

</body>

</html>";
        }
    }
}
