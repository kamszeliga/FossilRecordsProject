using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FossilRecordsProject.Data;
using FossilRecordsProject.Models;
using FossilRecordsProject.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using FossilRecordsProject.Services.Interfaces;
using FossilRecordsProject.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace FossilRecordsProject.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IFossilRecordService _recordService;
        private readonly IEmailSender _emailSender;

        public ContactsController(ApplicationDbContext context,
                                  UserManager<AppUser> userManager,
                                  IImageService imageService,
                                  IFossilRecordService recordService, 
                                  IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _recordService = recordService;
            _emailSender = emailSender;
        }

        // GET: Contacts

        public async Task<IActionResult> Index(string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string? userId = _userManager.GetUserId(User)!;

            IEnumerable<Contact> model = await _context.Contacts
                                                   .Where(c => c.AppUserID == userId)
                                                   .Include(c => c.Categories)
                                                   .ToListAsync();


            return View(model);
        }

        // GET: Contacts/Details/5

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create

        public async Task<IActionResult> Create()
        {
            // Query and present list of categories for the logged in user
            string? userId = _userManager.GetUserId(User);

            IEnumerable<Category> categoriesList = await _context.Categories
                                                                  .Where(c => c.AppUserID == userId)
                                                                  .ToListAsync();

            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name");

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("Id,AppUserID,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            ModelState.Remove("AppUserID");

            if (ModelState.IsValid)
            {
                contact.AppUserID = _userManager.GetUserId(User);
                contact.Created = DateTime.UtcNow;

                if (contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);

                    contact.ImageType = contact.ImageFile.ContentType;
                }


                if (contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                //TODO ADD SERVICE CALL

                await _recordService.AddContactToCategoriesAsync(selected, contact.Id);

                return RedirectToAction(nameof(Index));
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // EmailContact page
        public async Task<IActionResult> EmailContact(int? id)
        {

            if (id == null )
            {
                return NotFound();
            }

            string? appUserId = _userManager.GetUserId(User)!;

            Contact? contact = await _context.Contacts
                                        .Where(c => c.AppUserID == appUserId)
                                        .FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null) 
            {
                return NotFound();
            }

            // Instantiate EmailData
            EmailData emailData = new EmailData()
            {
                EmailAddress = contact!.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
            };

            // Instantiate View Model
            EmailContactViewModel viewModel = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                string? swalMessage = string.Empty;


                try
                {
                    await _emailSender.SendEmailAsync(
                                            viewModel.EmailData!.EmailAddress!,
                                            viewModel.EmailData.EmailSubject!,
                                            viewModel.EmailData.EmailBody!);

                    swalMessage = "Your email has been sent.";

                    return RedirectToAction(nameof(Index), new{ swalMessage });
                }
                catch (Exception)
                {
                    swalMessage = "Error: Email failed to send.";
                    
                    return RedirectToAction(nameof(Index), new{ swalMessage });
                    
                    throw;
                }
            }

            return View(viewModel);
        }


        // GET: Contacts/Edit/5

        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                                        .Include(c => c.Categories)
                                        .FirstOrDefaultAsync(c => c.Id == id);

            string? userId = _userManager.GetUserId(User);

            IEnumerable<Category> categoriesList = await _context.Categories.Where(c => c.AppUserID == userId).ToListAsync();

            IEnumerable<int> currentCategories = contact!.Categories.Select(c => c.Id);

            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name", currentCategories);

            if (contact == null)
            {
                return NotFound();
            }

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserID,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageData,ImageType,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                try
                {   // Reformat Created Date
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);

                    //Check if new image was added
                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);

                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    // Reformat Birth DAte
                    if (contact.BirthDate != null)
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    if (selected != null)
                    {
                        // 1. Remove contact's categories
                        await _recordService.RemoveAllContactCategoriesAsync(contact.Id);

                        // 2. Add categories based upon selection
                        await _recordService.AddContactToCategoriesAsync(selected, contact.Id);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
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
            ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserID);
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
