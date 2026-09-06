
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
        private readonly IWebHostEnvironment _environment;

        public HousingSchemesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================================================
        // INDEX - CLIENT + ADMIN
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
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme = await _context.HousingSchemes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SchemeId == id.Value);

            if (housingScheme == null)
            {
                return NotFound();
            }

            var emiPlans = await _context.EMIPlanOptions
                .AsNoTracking()
                .Where(x => x.SchemeId == housingScheme.SchemeId)
                .OrderByDescending(x => x.Status == "Active")
                .ThenBy(x => x.TenureMonths)
                .ThenBy(x => x.PlanName)
                .ToListAsync();

            ViewBag.EMIPlanOptions = emiPlans;

            return View(
                "~/Views/HousingSchemes/Details.cshtml",
                housingScheme);
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
            HousingScheme housingScheme,
            IFormFile? BannerImageFile,
            IFormFile? BrochureFile)
        {
            if (ModelState.IsValid)
            {
                // =================================================
                // CREATE IMAGE FOLDER
                // =================================================

                string imageFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "schemes");

                if (!Directory.Exists(imageFolder))
                {
                    Directory.CreateDirectory(imageFolder);
                }


                // =================================================
                // SAVE BANNER IMAGE
                // =================================================

                if (BannerImageFile != null &&
                    BannerImageFile.Length > 0)
                {
                    string extension =
                        Path.GetExtension(BannerImageFile.FileName)
                        .ToLowerInvariant();

                    string[] allowedExtensions =
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            "BannerImageFile",
                            "Only JPG, JPEG, PNG and WEBP images are allowed.");

                        return View(housingScheme);
                    }

                    string fileName =
                        Guid.NewGuid().ToString("N") + extension;

                    string filePath =
                        Path.Combine(imageFolder, fileName);

                    using (var stream =
                           new FileStream(filePath, FileMode.Create))
                    {
                        await BannerImageFile.CopyToAsync(stream);
                    }

                    housingScheme.BannerImage =
                        "/images/schemes/" + fileName;
                }


                // =================================================
                // SAVE BROCHURE
                // =================================================

                if (BrochureFile != null &&
                    BrochureFile.Length > 0)
                {
                    string brochureFolder = Path.Combine(
                        _environment.WebRootPath,
                        "brochures");

                    if (!Directory.Exists(brochureFolder))
                    {
                        Directory.CreateDirectory(brochureFolder);
                    }

                    string extension =
                        Path.GetExtension(BrochureFile.FileName)
                        .ToLowerInvariant();

                    if (extension != ".pdf")
                    {
                        ModelState.AddModelError(
                            "BrochureFile",
                            "Only PDF brochures are allowed.");

                        return View(housingScheme);
                    }

                    string fileName =
                        Guid.NewGuid().ToString("N") + extension;

                    string filePath =
                        Path.Combine(brochureFolder, fileName);

                    using (var stream =
                           new FileStream(filePath, FileMode.Create))
                    {
                        await BrochureFile.CopyToAsync(stream);
                    }

                    housingScheme.Brochure =
                        "/brochures/" + fileName;
                }


                // =================================================
                // SAVE DATABASE RECORD
                // =================================================

                housingScheme.CreatedDate = DateTime.Now;

                _context.HousingSchemes.Add(housingScheme);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
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
                await _context.HousingSchemes.FindAsync(id.Value);

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
            HousingScheme housingScheme,
            IFormFile? BannerImageFile,
            IFormFile? BrochureFile)
        {
            if (id != housingScheme.SchemeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // =================================================
                    // IMAGE FOLDER
                    // =================================================

                    string imageFolder = Path.Combine(
                        _environment.WebRootPath,
                        "images",
                        "schemes");

                    if (!Directory.Exists(imageFolder))
                    {
                        Directory.CreateDirectory(imageFolder);
                    }


                    // =================================================
                    // REPLACE BANNER IMAGE IF NEW IMAGE SELECTED
                    // =================================================

                    if (BannerImageFile != null &&
                        BannerImageFile.Length > 0)
                    {
                        string extension =
                            Path.GetExtension(
                                BannerImageFile.FileName)
                            .ToLowerInvariant();

                        string[] allowedExtensions =
                        {
                            ".jpg",
                            ".jpeg",
                            ".png",
                            ".webp"
                        };

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError(
                                "BannerImageFile",
                                "Only JPG, JPEG, PNG and WEBP images are allowed.");

                            return View(housingScheme);
                        }

                        // Delete old image
                        if (!string.IsNullOrWhiteSpace(
                                housingScheme.BannerImage))
                        {
                            string oldFileName =
                                Path.GetFileName(
                                    housingScheme.BannerImage);

                            string oldFilePath =
                                Path.Combine(
                                    imageFolder,
                                    oldFileName);

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }


                        // Save new image
                        string fileName =
                            Guid.NewGuid().ToString("N")
                            + extension;

                        string filePath =
                            Path.Combine(
                                imageFolder,
                                fileName);

                        using (var stream =
                               new FileStream(
                                   filePath,
                                   FileMode.Create))
                        {
                            await BannerImageFile.CopyToAsync(stream);
                        }

                        housingScheme.BannerImage =
                            "/images/schemes/" + fileName;
                    }


                    // =================================================
                    // REPLACE BROCHURE IF NEW PDF SELECTED
                    // =================================================

                    if (BrochureFile != null &&
                        BrochureFile.Length > 0)
                    {
                        string brochureFolder = Path.Combine(
                            _environment.WebRootPath,
                            "brochures");

                        if (!Directory.Exists(brochureFolder))
                        {
                            Directory.CreateDirectory(
                                brochureFolder);
                        }

                        string extension =
                            Path.GetExtension(
                                BrochureFile.FileName)
                            .ToLowerInvariant();

                        if (extension != ".pdf")
                        {
                            ModelState.AddModelError(
                                "BrochureFile",
                                "Only PDF brochures are allowed.");

                            return View(housingScheme);
                        }

                        // Delete old brochure
                        if (!string.IsNullOrWhiteSpace(
                                housingScheme.Brochure))
                        {
                            string oldFileName =
                                Path.GetFileName(
                                    housingScheme.Brochure);

                            string oldFilePath =
                                Path.Combine(
                                    brochureFolder,
                                    oldFileName);

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(
                                    oldFilePath);
                            }
                        }


                        // Save new brochure
                        string fileName =
                            Guid.NewGuid().ToString("N")
                            + extension;

                        string filePath =
                            Path.Combine(
                                brochureFolder,
                                fileName);

                        using (var stream =
                               new FileStream(
                                   filePath,
                                   FileMode.Create))
                        {
                            await BrochureFile.CopyToAsync(stream);
                        }

                        housingScheme.Brochure =
                            "/brochures/" + fileName;
                    }


                    // =================================================
                    // UPDATE DATABASE
                    // =================================================

                    _context.Update(housingScheme);

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

                return RedirectToAction(nameof(Index));
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
                    .FirstOrDefaultAsync(
                        x => x.SchemeId == id.Value);

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
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housingScheme =
                await _context.HousingSchemes
                    .FindAsync(id.Value);

            if (housingScheme != null)
            {
                // =================================================
                // DELETE IMAGE FILE
                // =================================================

                if (!string.IsNullOrWhiteSpace(
                        housingScheme.BannerImage))
                {
                    string imageFolder = Path.Combine(
                        _environment.WebRootPath,
                        "images",
                        "schemes");

                    string fileName =
                        Path.GetFileName(
                            housingScheme.BannerImage);

                    string filePath =
                        Path.Combine(
                            imageFolder,
                            fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }


                // =================================================
                // DELETE BROCHURE FILE
                // =================================================

                if (!string.IsNullOrWhiteSpace(
                        housingScheme.Brochure))
                {
                    string brochureFolder =
                        Path.Combine(
                            _environment.WebRootPath,
                            "brochures");

                    string fileName =
                        Path.GetFileName(
                            housingScheme.Brochure);

                    string filePath =
                        Path.Combine(
                            brochureFolder,
                            fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }


                // =================================================
                // DELETE DATABASE RECORD
                // =================================================

                _context.HousingSchemes.Remove(
                    housingScheme);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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

