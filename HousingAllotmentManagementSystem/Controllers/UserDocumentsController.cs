
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HousingAllotmentManagementSystem.Models;
using HousingAllotmentManagementSystem.Data;

public class UserDocumentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserDocumentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: USERDOCUMENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.UserDocuments.ToListAsync());
    }

    // GET: USERDOCUMENTS/Details/5
    public async Task<IActionResult> Details(int? documentid)
    {
        if (documentid == null)
        {
            return NotFound();
        }

        var userdocument = await _context.UserDocuments
            .FirstOrDefaultAsync(m => m.DocumentId == documentid);
        if (userdocument == null)
        {
            return NotFound();
        }

        return View(userdocument);
    }

    // GET: USERDOCUMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: USERDOCUMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("DocumentId,UserId,AadhaarCard,Pancard,IncomeCertificate,SalarySlip,PassportPhoto,BankStatement,OtherDocument,VerificationStatus,Remarks,UploadedDate,VerifiedDate,User")] UserDocument userdocument)
    {
        if (ModelState.IsValid)
        {
            _context.Add(userdocument);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(userdocument);
    }

    // GET: USERDOCUMENTS/Edit/5
    public async Task<IActionResult> Edit(int? documentid)
    {
        if (documentid == null)
        {
            return NotFound();
        }

        var userdocument = await _context.UserDocuments.FindAsync(documentid);
        if (userdocument == null)
        {
            return NotFound();
        }
        return View(userdocument);
    }

    // POST: USERDOCUMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? documentid, [Bind("DocumentId,UserId,AadhaarCard,Pancard,IncomeCertificate,SalarySlip,PassportPhoto,BankStatement,OtherDocument,VerificationStatus,Remarks,UploadedDate,VerifiedDate,User")] UserDocument userdocument)
    {
        if (documentid != userdocument.DocumentId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(userdocument);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserDocumentExists(userdocument.DocumentId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(userdocument);
    }

    // GET: USERDOCUMENTS/Delete/5
    public async Task<IActionResult> Delete(int? documentid)
    {
        if (documentid == null)
        {
            return NotFound();
        }

        var userdocument = await _context.UserDocuments
            .FirstOrDefaultAsync(m => m.DocumentId == documentid);
        if (userdocument == null)
        {
            return NotFound();
        }

        return View(userdocument);
    }

    // POST: USERDOCUMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? documentid)
    {
        var userdocument = await _context.UserDocuments.FindAsync(documentid);
        if (userdocument != null)
        {
            _context.UserDocuments.Remove(userdocument);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserDocumentExists(int? documentid)
    {
        return _context.UserDocuments.Any(e => e.DocumentId == documentid);
    }
}
