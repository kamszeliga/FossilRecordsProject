using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FossilRecordsProject.Data;
using FossilRecordsProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace FossilRecordsProject.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        public CategoriesController(ApplicationDbContext context, UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
        }

        // EmailCategory

        public async Task<IActionResult> EmailCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();

            }

            string? userId = _userManager.GetUserId(User);

            // filter the data from the database based upon the given ID to determine your categories

            Category? category = await _context.Categories
                                                    .Include(c => c.Contacts)
                                                    .FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == userId);

            if (category == null)
            {
                return NotFound();
            }

            List<string> emails = category!.Contacts.Select(c => c.Email).ToList()!;

            EmailData emailData = new EmailData()
            {
                GroupName = category.Name,
                EmailAddress = string.Join(";", emails),
                EmailSubject = $"Group Message: {category.Name}"
            };

            return View(emailData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailCategory(EmailData emailData)
        {
            if (ModelState.IsValid) 
            {
                string? swalMessage = string.Empty;

                try
                {
                    await _emailSender.SendEmailAsync(
                                                emailData!.EmailAddress!,
                                                emailData.EmailSubject!,
                                                emailData.EmailBody!);

                    swalMessage = "Your email has been sent.";

                    return RedirectToAction(nameof(Index), new { swalMessage });

                }
                catch (Exception)
                {
                    swalMessage = "Error: Email failed to send.";

                    return RedirectToAction(nameof(Index), new { swalMessage });

                    throw;
                }
            }

            return View(emailData);
        }


        // GET: Categories
        public async Task<IActionResult> Index(string swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string userId = _userManager.GetUserId(User)!;

            IEnumerable<Category> model = await _context.Categories
                                 .Where(c => c.AppUserID == userId)
                                 .Include(c => c.Contacts)
                                 .ToListAsync();

            return View(model);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            //ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Id");

            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,AppUserID")] Category category)
        {
            ModelState.Remove("AppUserID");

            if (ModelState.IsValid)
            {
                category.AppUserID = _userManager.GetUserId(User);

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            //ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Id", category.AppUserID);
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AppUserID")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            //ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Id", category.AppUserID);
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.AppUser)
                .Include(c => c.Contacts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return (_context.Categories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
