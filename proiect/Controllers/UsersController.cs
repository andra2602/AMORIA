using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect.Data;
using proiect.Models;
using System.Security.Claims;

namespace proiect.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            db = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // ==========================================
        // 1. Afișarea listei de utilizatori
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await db.ApplicationUsers.Include(u => u.Profile).ToListAsync();
            return View(users);
        }

  
         // ==========================================
         // 2. Afișarea detaliilor unui utilizator
         // ==========================================
         [Authorize]
         [HttpGet]
        public async Task<IActionResult> Show(string id)
        {
            var userId = _userManager.GetUserId(User); // utilizatorul conectat


            var user = await db.ApplicationUsers
                .Include(u => u.Profile)
                .Include(u => u.Posts.OrderByDescending(post => post.PostDate)) // Include postările utilizatorului
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("Utilizatorul nu a fost găsit.");
           
            }

            // Verifică dacă utilizatorul are un profil
            if (user.Profile == null)
            {
                TempData["message"] = "Acest utilizator nu are un profil asociat.";
                return RedirectToAction("Index", "Users");
            }

            // Verifică dacă utilizatorul curent este prieten cu utilizatorul al cărui profil îl vizualizează
            var isFriend = await db.Friendships.AnyAsync(f =>
                (f.UserId1 == userId && f.UserId2 == id && f.Status == "Confirmed") ||
                (f.UserId1 == id && f.UserId2 == userId && f.Status == "Confirmed"));


            // daca nu sunt prieteni si daca utilizatorul curent nu-si vizualizeaza propriul profil atunci
            // este afisat butonul TRIMITE CERERE PRIETENIE
            ViewBag.AfisareButoane = !isFriend && userId != id;

            if (user.Profile.Visibility || isFriend)
            {

                // Afișează profilul complet dacă este public sau dacă sunt prieteni
                return View("PublicProfile", user);
            }
            else
            {
                var basicProfile = new ApplicationUser
                {
                    UserFirstName = user.UserFirstName,
                    UserLastName = user.UserLastName,
                    Profile = new Profile
                    {
                        Description = user.Profile.Description,
                        ImagePath = user.Profile.ImagePath
                    },
                    Posts = null // În profil privat, poți alege să nu arăți postările
                };
                return View("PrivateProfile", basicProfile);
            }
        }

        // ==========================================
        // 3. Creare utilizator (Admin, User)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin, User")]
        public IActionResult New()
        {
            return View(new ApplicationUser());
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> New(ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                user.RegistrationDate = DateTime.Now;
                var result = await _userManager.CreateAsync(user, "DefaultPassword123!");
                if (result.Succeeded)
                {
                    TempData["message"] = "Utilizatorul a fost creat cu succes!";
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(user);
        }

        // ==========================================
        // 4. Editare utilizator (self și Admin)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await db.ApplicationUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Edit(string id, ApplicationUser updatedUser, Profile updatedProfile)
        {
            // Găsim utilizatorul din baza de date
            var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                user.UserFirstName = updatedUser.UserFirstName;
                user.UserLastName = updatedUser.UserLastName;
                user.Email = updatedUser.Email;

                if (user.Profile != null)
                {
                    user.Profile.ProfileName = updatedProfile.ProfileName;
                    user.Profile.Description = updatedProfile.Description;
                    user.Profile.Website = updatedProfile.Website;
                    user.Profile.Location = updatedProfile.Location;
                    user.Profile.Visibility = updatedProfile.Visibility;
                }

                db.Update(user);
                await db.SaveChangesAsync();

                TempData["message"] = "Profilul a fost actualizat.";
                return RedirectToAction("Show", new { id = user.Id });
            }

            return View(updatedUser);
        }

        // ==========================================
        // 5. Ștergere utilizator (doar Admin și self)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await db.ApplicationUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> ConfirmDelete(string id)
        {
            var user = await db.ApplicationUsers
                .Include(u => u.FriendshipsAsUser1) // Include prieteniile unde este User1
                .Include(u => u.FriendshipsAsUser2) // Include prieteniile unde este User2
                .Include(u => u.Posts)             // Include postările utilizatorului
                .Include(u => u.Comments)          // Include comentariile utilizatorului
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Id != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            // Ștergem toate prieteniile în care utilizatorul este implicat
            db.Friendships.RemoveRange(user.FriendshipsAsUser1);
            db.Friendships.RemoveRange(user.FriendshipsAsUser2);

            // Ștergem toate postările asociate utilizatorului
            db.Posts.RemoveRange(user.Posts);

            // Ștergem toate comentariile utilizatorului
            db.Comments.RemoveRange(user.Comments);

            // Ștergem profilul asociat utilizatorului (dacă există)
            var profiles = db.Profiles.Where(p => p.UserId == id).ToList();
            db.Profiles.RemoveRange(profiles);

            // Salvăm modificările înainte de a șterge utilizatorul pentru a evita conflicte
            await db.SaveChangesAsync();

            // Ștergem utilizatorul
            db.ApplicationUsers.Remove(user);
            await db.SaveChangesAsync();

            TempData["message"] = "Utilizatorul și toate datele asociate au fost șterse.";
            return RedirectToAction("Index");
        }



        // ==========================================
        // 6. Căutare utilizator
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var allUsers = await db.ApplicationUsers.Include(u => u.Profile).ToListAsync();
                return View(allUsers);
            }

            var users = await db.ApplicationUsers
                .Include(u => u.Profile)
                .Where(u => EF.Functions.Like(u.UserFirstName + " " + u.UserLastName, $"%{searchTerm}%"))
                .ToListAsync();

            return View(users);
        }
    }
}
