using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect.Data;
using proiect.Models;

namespace proiect.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager,
                                 IWebHostEnvironment env)
        {
            db = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _env = env;
        }
        // ==========================================
        // 0. Afișare postări utilizator
        // ==========================================
        public async Task<IActionResult> Index(string id)
        {
            var posts = await db.Posts
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.PostDate)
                .ToListAsync();

            return View(posts);
        }

        // ==========================================
        // 1. Afișarea unei postări detaliate
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Show(int id)
        {
            var post = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments) // Include comentariile asociate
                .ThenInclude(c => c.User) // Include autorii comentariilor
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // ==========================================
        // 2. Creare postare (GET și POST)
        // ==========================================
        [HttpGet]
        public IActionResult New()
        {
            return View(new Post());
        }

        [HttpPost]
        public async Task<IActionResult> New(Post post, IFormFile ImageVideoPath)
        {
            post.PostDate = DateTime.Now;
            post.UserId = _userManager.GetUserId(User);

            if (ImageVideoPath != null && ImageVideoPath.Length > 0)
            {
                // Verificăm extensia
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(ImageVideoPath.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageVideoPath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif) sau un video (mp4, mov).");
                    return View(post);
                }

                // Cale stocare
                var storagePath = Path.Combine(_env.WebRootPath, "images", ImageVideoPath.FileName);
                var databaseFileName = "/images/" + ImageVideoPath.FileName;

                // Salvare fișier
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ImageVideoPath.CopyToAsync(fileStream);
                }

                post.ImageVideoPath = databaseFileName;
            }

            ModelState.Remove(nameof(post.ImageVideoPath));
            ModelState.Remove(nameof(post.Group));

            if (!string.IsNullOrEmpty(post.EmbeddedVideoUrl))
            {
                // Dacă link-ul este un link standard YouTube
                if (post.EmbeddedVideoUrl.Contains("youtube.com/watch"))
                {
                    var videoId = post.EmbeddedVideoUrl.Split("v=")[1];
                    var ampersandPosition = videoId.IndexOf('&');
                    if (ampersandPosition != -1)
                    {
                        videoId = videoId.Substring(0, ampersandPosition);
                    }
                    post.EmbeddedVideoUrl = $"https://www.youtube.com/embed/{videoId}";
                }
            }




            if (TryValidateModel(post))
            {
                // Salvăm postarea
                db.Posts.Add(post);
                await db.SaveChangesAsync();

                TempData["message"] = "Postarea a fost creată cu succes!";
                return RedirectToAction("Show", new { id = post.PostId });
            }

            return View(post);
        }


        // ==========================================
        // 3. Editare postare (GET și POST)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await db.Posts.FindAsync(id);

            if (post == null || post.UserId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Post post, IFormFile ImageVideoPath)
        {
            var existingPost = await db.Posts.FindAsync(post.PostId);

            if (existingPost == null || existingPost.UserId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            // Actualizăm conținutul postării
            existingPost.PostContent = post.PostContent;

            // Verificăm dacă un fișier nou a fost încărcat
            if (ImageVideoPath != null && ImageVideoPath.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(ImageVideoPath.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageVideoPath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif) sau un video (mp4, mov).");
                    return View(post);
                }

                // Salvăm noul fișier
                var storagePath = Path.Combine(_env.WebRootPath, "images", ImageVideoPath.FileName);
                var databaseFileName = "/images/" + ImageVideoPath.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ImageVideoPath.CopyToAsync(fileStream);
                }

                existingPost.ImageVideoPath = databaseFileName;
            }

            // Actualizăm URL-ul videoclipului embedded (dacă există)
            if (!string.IsNullOrEmpty(post.EmbeddedVideoUrl) && post.EmbeddedVideoUrl.Contains("youtube.com/watch"))
            {
                var videoId = post.EmbeddedVideoUrl.Split("v=")[1];
                var ampersandPosition = videoId.IndexOf('&');
                if (ampersandPosition != -1)
                {
                    videoId = videoId.Substring(0, ampersandPosition);
                }
                existingPost.EmbeddedVideoUrl = $"https://www.youtube.com/embed/{videoId}";
            }

            ModelState.Remove(nameof(post.ImageVideoPath));
            ModelState.Remove(nameof(post.Group));

            if (TryValidateModel(existingPost))
            {
                db.Posts.Update(existingPost);
                await db.SaveChangesAsync();

                TempData["message"] = "Postarea a fost actualizată cu succes!";
                return RedirectToAction("Show", new { id = existingPost.PostId });
            }

            return View(post);
        }


        // ==========================================
        // 4. Ștergere postare
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await db.Posts
                .Include(p => p.User) // Include informațiile utilizatorului dacă este necesar
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                ViewData["ErrorMessage"] = "NU ESTE POSTAREA DUMNEAVOASTRA !!! NU O PUTETI STERGE !!!";
                return View(post); // Renderizăm pagina pentru prompt
            }

            return View(post); // Returnăm View-ul de confirmare
        }


        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await db.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                ViewData["ErrorMessage"] = "NU ESTE POSTAREA DUMNEAVOASTRĂ !!! NU O PUTETI STERGE !!!";
                return View(post); // Renderizăm pagina pentru prompt
            }

            db.Posts.Remove(post);
            await db.SaveChangesAsync();

            TempData["message"] = "Postarea a fost ștearsă cu succes!";
            return RedirectToAction("YourProfile", "Profiles");
        }





        // ==========================================
        // 5. Adăugare postare în grup (GET și POST)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AddPostToGroup(int groupId)
        {
            // Verificăm dacă grupul există
            var group = await db.Groups
                .Include(g => g.UserGroups) // Include relația many-to-many pentru membri
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
            {
                return NotFound();
            }

            // Verificăm dacă utilizatorul este membru sau moderator al grupului
            var userId = _userManager.GetUserId(User);
            var isMemberOrModerator = group.UserGroups.Any(ug => ug.UserId == userId);

            if (!isMemberOrModerator && group.ManagerGroupId != userId)
            {
                TempData["ErrorMessage"] = "Nu aveți permisiunea de a posta în acest grup.";
                return RedirectToAction("Index", "Groups");
            }

            var post = new Post
            {
                GroupId = groupId,
                Group = group
            };

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> AddPostToGroup(Post post, IFormFile ImageVideoPath)
        {
            // Setăm data postării și utilizatorul curent
            post.PostDate = DateTime.Now;
            post.UserId = _userManager.GetUserId(User);

            // Verificăm dacă grupul există
            var group = await db.Groups.FindAsync(post.GroupId);

            if (group == null)
            {
                return NotFound();
            }

            // Verificăm dacă utilizatorul este membru sau moderator al grupului
            var isMemberOrModerator = db.UserGroups
                .Any(ug => ug.GroupId == post.GroupId && ug.UserId == post.UserId);

            if (!isMemberOrModerator && group.ManagerGroupId != post.UserId)
            {
                TempData["ErrorMessage"] = "Nu aveți permisiunea de a posta în acest grup.";
                return RedirectToAction("Index", "Groups");
            }

            if (ImageVideoPath != null && ImageVideoPath.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(ImageVideoPath.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageVideoPath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif) sau un video (mp4, mov).");
                    return View(post);
                }

                // Salvare fișier
                var storagePath = Path.Combine(_env.WebRootPath, "images", ImageVideoPath.FileName);
                var databaseFileName = "/images/" + ImageVideoPath.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ImageVideoPath.CopyToAsync(fileStream);
                }

                post.ImageVideoPath = databaseFileName;
            }

            ModelState.Remove(nameof(post.ImageVideoPath));
            ModelState.Remove(nameof(post.Group));

            if (!string.IsNullOrEmpty(post.EmbeddedVideoUrl) && post.EmbeddedVideoUrl.Contains("youtube.com/watch"))
            {
                var videoId = post.EmbeddedVideoUrl.Split("v=")[1];
                var ampersandPosition = videoId.IndexOf('&');
                if (ampersandPosition != -1)
                {
                    videoId = videoId.Substring(0, ampersandPosition);
                }
                post.EmbeddedVideoUrl = $"https://www.youtube.com/embed/{videoId}";
            }

            if (TryValidateModel(post))
            {
                db.Posts.Add(post);
                await db.SaveChangesAsync();

                TempData["message"] = "Postarea a fost adăugată în grup cu succes!";
                return RedirectToAction("ViewGroup", "Groups", new { id = post.GroupId });
            }

            return View(post);
        }


        // ==========================================
        // 6. Afișarea unei postări detaliate dintr-un grup
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ShowGroupPost(int id)
        {
            var post = await db.Posts
                .Include(p => p.User) // Include utilizatorul care a creat postarea
                .Include(p => p.Comments) // Include comentariile asociate
                .ThenInclude(c => c.User) // Include autorii comentariilor
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            // Obține grupul căruia îi aparține postarea
            var group = await db.Groups.FirstOrDefaultAsync(g => g.GroupId == post.GroupId);

            if (group == null)
            {
                return NotFound();
            }

            // Adaugă GroupId la ViewBag pentru a permite redirecționarea înapoi la grup
            ViewBag.GroupId = group.GroupId;

            return View(post);
        }

        // ==========================================
        // 7. Editarea unei postări dintr-un grup
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditGroupPost(int id)
        {
            var post = await db.Posts
                 .Include(p => p.Group)
                 .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            // Verificăm dacă utilizatorul este membru al grupului
            var isMember = db.UserGroups.Any(ug => ug.UserId == _userManager.GetUserId(User) && ug.GroupId == post.GroupId);
            if (!isMember)
            {
                TempData["message"] = "Nu mai ești membru al grupului. Nu poți edita această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("ViewGroup", "Groups", new { id = post.GroupId });
            }

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> EditGroupPost(Post post, IFormFile ImageVideoPath)
        {
            var existingPost = await db.Posts
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.PostId == post.PostId);

            if (existingPost == null)
            {
                return NotFound();
            }

            if (existingPost.UserId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            // Verificăm dacă utilizatorul este membru al grupului
            var isMember = db.UserGroups.Any(ug => ug.UserId == _userManager.GetUserId(User) && ug.GroupId == existingPost.GroupId);
            if (!isMember)
            {
                TempData["message"] = "Nu mai ești membru al grupului. Nu poți edita această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("ViewGroup", "Groups", new { id = existingPost.GroupId });
            }

            // Actualizăm conținutul postării
            existingPost.PostContent = post.PostContent;

            // Verificăm dacă un fișier nou a fost încărcat
            if (ImageVideoPath != null && ImageVideoPath.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(ImageVideoPath.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageVideoPath", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif) sau un video (mp4, mov).");
                    return View(post);
                }

                // Salvăm noul fișier
                var storagePath = Path.Combine(_env.WebRootPath, "images", ImageVideoPath.FileName);
                var databaseFileName = "/images/" + ImageVideoPath.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ImageVideoPath.CopyToAsync(fileStream);
                }

                existingPost.ImageVideoPath = databaseFileName;
            }

            // Actualizăm URL-ul videoclipului embedded (dacă există)
            if (!string.IsNullOrEmpty(post.EmbeddedVideoUrl) && post.EmbeddedVideoUrl.Contains("youtube.com/watch"))
            {
                var videoId = post.EmbeddedVideoUrl.Split("v=")[1];
                var ampersandPosition = videoId.IndexOf('&');
                if (ampersandPosition != -1)
                {
                    videoId = videoId.Substring(0, ampersandPosition);
                }
                existingPost.EmbeddedVideoUrl = $"https://www.youtube.com/embed/{videoId}";
            }

            ModelState.Remove(nameof(post.ImageVideoPath));
            ModelState.Remove(nameof(post.Group));

            if (TryValidateModel(existingPost))
            {
                db.Posts.Update(existingPost);
                await db.SaveChangesAsync();

                TempData["message"] = "Postarea din grup a fost actualizată cu succes!";
                return RedirectToAction("ShowGroupPost", new { id = existingPost.PostId });
            }

            return View(post);
        }




        // ==========================================
        // 8. Stergerea unei postări dintr-un grup
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> DeleteGroupPost(int id)
        {
            var post = await db.Posts
                .Include(p => p.Group)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == id);


            if (post == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            // Verificăm dacă utilizatorul este membru al grupului
            var isMember = db.UserGroups.Any(ug => ug.UserId == userId && ug.GroupId == post.GroupId);
            if (!isMember && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu mai ești membru al grupului. Nu poți șterge această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("ViewGroup", "Groups", new { id = post.GroupId });
            }

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroupPostConfirmed(int id)
        {
            var post = await db.Posts
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }
            var userId = _userManager.GetUserId(User);

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            // Verificăm dacă utilizatorul este membru al grupului
            var isMember = db.UserGroups.Any(ug => ug.UserId == userId && ug.GroupId == post.GroupId);
            if (!isMember && !User.IsInRole("Moderator") && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu mai ești membru al grupului. Nu poți șterge această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("ViewGroup", "Groups", new { id = post.GroupId });
            }

            db.Posts.Remove(post);
            await db.SaveChangesAsync();

            TempData["message"] = "Postarea a fost ștearsă cu succes!";
            return RedirectToAction("ViewGroup", "Groups", new { id = post.GroupId });
        }


    }
}