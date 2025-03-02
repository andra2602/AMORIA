using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using proiect.Data;
using proiect.Models;
using System.Security.Claims;

namespace proiect.Controllers
{
    [Authorize]
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public ProfilesController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            db = context;
            _env = env;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // ==========================================
        // 1. Creare profil (GET și POST)
        // ==========================================
        [HttpGet]
        public IActionResult New()
        {
            // Returnăm un view gol pentru profilul nou
            return View(new Profile());
        }

        [HttpPost]
        public async Task<IActionResult> New(Profile profile, IFormFile ImagePath)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                ModelState.AddModelError(string.Empty, "Trebuie să fii autentificat pentru a crea un profil.");
                return View(profile);
            }

            profile.UserId = currentUserId;


            // Validăm imaginea
            if (ImagePath != null && ImagePath.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(ImagePath.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImagePath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                    return View(profile);
                }

                // Salvăm fișierul
                var storagePath = Path.Combine(_env.WebRootPath, "images", ImagePath.FileName);
                var databaseFileName = "/images/" + ImagePath.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ImagePath.CopyToAsync(fileStream);
                }

                profile.ImagePath = databaseFileName;
            }
            else
            {
                ModelState.AddModelError("ImagePath", "Imaginea este obligatorie.");
            }

            // Eliminăm validarea pentru ImagePath
            ModelState.Remove(nameof(profile.ImagePath));
            ModelState.Remove(nameof(profile.UserId));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    // Salvăm profilul
                    db.Profiles.Add(profile);
                    await db.SaveChangesAsync();

                    TempData["message"] = "Profilul a fost creat cu succes!";
                    return RedirectToAction("YourProfile", "Profiles");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Eroare la salvarea profilului: {ex.Message}");
                }
            }

            return View(profile);
        }

        // ==========================================
        // 2. Editare profil (GET și POST)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await db.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.User.Id != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            return View(profile);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> Edit(Profile profile, IFormFile? ProfileImage)
        {
            // Eliminăm câmpurile User din validare
            ModelState.Remove("User");
            ModelState.Remove(nameof(profile.UserId));
            ModelState.Remove(nameof(profile.ImagePath));

            if (ModelState.IsValid)
            {
                var existingProfile = await db.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == profile.Id);

                if (existingProfile == null || existingProfile.User.Id != _userManager.GetUserId(User))
                {
                    return Unauthorized();
                }

                // Actualizăm câmpurile textuale
                existingProfile.ProfileName = profile.ProfileName;
                existingProfile.Description = profile.Description;
                existingProfile.Website = profile.Website;
                existingProfile.Location = profile.Location;
                existingProfile.Visibility = profile.Visibility;

                // Gestionăm imaginea de profil doar dacă este încărcată o nouă imagine
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ProfileImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ImagePath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                        return View(profile);
                    }

                    // Salvăm fișierul
                    var fileName = Guid.NewGuid().ToString() + fileExtension; // Generăm un nume unic
                    var filePath = Path.Combine(_env.WebRootPath, "images", fileName);
                    Console.WriteLine("Saving file to: " + filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    // Actualizăm calea imaginii în profil
                    existingProfile.ImagePath = "/images/" + fileName;
                }

                db.Profiles.Update(existingProfile);
                await db.SaveChangesAsync();

                TempData["message"] = "Profilul a fost actualizat.";
                return RedirectToAction("YourProfile", "Profiles");
            }

            return View(profile);
        }

        // ==========================================
        // 3. Ștergere profil
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await db.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.User.Id != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            db.Profiles.Remove(profile);
            await db.SaveChangesAsync();

            TempData["message"] = "Profilul a fost șters.";
            return RedirectToAction("Index", "Users");
        }

        // ==========================================
        // 4. Setare vizibilitate profil
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var profile = await db.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.User.Id != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            profile.Visibility = !profile.Visibility;
            db.Profiles.Update(profile);
            await db.SaveChangesAsync();

            TempData["message"] = profile.Visibility
                ? "Profilul este acum public."
                : "Profilul este acum privat.";

            return RedirectToAction("Show", "Profiles", new { id = profile.Id });
        }

        // ==========================================
        // 5. Afișare profil
        // ==========================================


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Show(string id)
        {
            var profile = await db.Profiles
                .Include(p => p.User)
                .ThenInclude(u => u.Posts.OrderByDescending(post => post.PostDate))
                .FirstOrDefaultAsync(p => p.User.Id == id);

            if (profile == null)
            {
                return NotFound("Profilul nu a fost găsit.");
            }

            return View(profile);
        }



        // ==========================================
        // 6. Afișare profil propriu
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> YourProfile()
        {
            // Obține ID-ul utilizatorului autenticat
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Caută utilizatorul și include datele asociate
            var currentUser = await db.Users
                .Include(u => u.Profile) // Include profilul utilizatorului
                .Include(u => u.Posts.OrderByDescending(post => post.PostDate))   // Include postările utilizatorului
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Verificăm dacă utilizatorul sau profilul acestuia există
            if (currentUser == null || currentUser.Profile == null)
            {
                // Adăugăm un ViewBag pentru mesajul de eroare
                ViewBag.ErrorMessage = "Profilul utilizatorului nu a fost găsit.";
                return View(); // Returnăm același view (YourProfile) cu mesaj de eroare
            }

            return View(currentUser); // Returnăm datele utilizatorului în view
        }



    }
}