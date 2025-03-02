using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect.Data;
using proiect.Models;

namespace proiect.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public GroupsController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            db = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // ==========================================
        // 1. Afișează toate grupurile
        // ==========================================
        public IActionResult Index()
        {
            var groups = db.Groups.Include(g => g.UserGroups)
                                  .ThenInclude(ug => ug.User)
                                  .ToList();
            return View(groups);
        }

        // ==========================================
        // 2. Afișează detaliile unui grup
        // ==========================================
        public async Task<IActionResult> Show(int id)
        {
            var group = await db.Groups.Include(g => g.UserGroups)
                                        .ThenInclude(ug => ug.User)
                                        .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var isMember = group.UserGroups.Any(ug => ug.UserId == userId);
            ViewBag.IsMember = isMember;

            return View(group);
        }


        // ==========================================
        // 3. Crearea unui grup nou
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Moderator")]
        public IActionResult New()
        {
            return View();
        }


        [HttpPost]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> New(Group model)
        {
            // Eliminăm câmpul ManagerGroupId din validare
            ModelState.Remove(nameof(model.ManagerGroupId));

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Datele introduse nu sunt valide.";
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Utilizatorul nu este autentificat.";
                return RedirectToAction("Index", "Home");
            }

            var group = new Group
            {
                GroupName = model.GroupName,
                GroupDescription = model.GroupDescription,
                Private = model.Private,
                Max = model.Max,
                ManagerGroupId = userId,
                GroupDate = DateTime.UtcNow
            };

            try
            {
                db.Groups.Add(group);
                await db.SaveChangesAsync();

                var userGroup = new UserGroup
                {
                    UserId = userId,
                    GroupId = group.GroupId,
                    RegistrationDate = DateTime.UtcNow,
                    IsModerator = true, // Creatorul este automat moderator
                    IsApproved = true   // Creatorul este automat aprobat
                };
                db.UserGroups.Add(userGroup);
                await db.SaveChangesAsync();

                TempData["success"] = "Grupul a fost creat cu succes.";
                return RedirectToAction("Index", "Groups");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                TempData["error"] = "A apărut o eroare la crearea grupului.";
                return View(model);
            }
        }


        // ==========================================
        // 4. Editarea unui grup
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await db.Groups
                .Include(g => g.ManagerGroup) // Include relațiile necesare, dacă este cazul
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !(await _userManager.IsInRoleAsync(user, "Moderator")))
            {
                TempData["message"] = "Nu aveți permisiunea de a edita acest grup.";
                return RedirectToAction("Index");
            }

            return View(group);
        }


        [HttpPost]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> Edit(Group group)
        {
            //Console.WriteLine($"[DEBUG] Private value received in POST: {group.Private}");

            // Eliminăm câmpurile care nu sunt relevante pentru validare
            ModelState.Remove(nameof(group.ManagerGroup));
            ModelState.Remove(nameof(group.ManagerGroupId));
            ModelState.Remove(nameof(group.UserGroups));

            if (ModelState.IsValid)
            {
                var existingGroup = await db.Groups
                    .Include(g => g.ManagerGroup) // Include relațiile necesare
                    .FirstOrDefaultAsync(g => g.GroupId == group.GroupId);

                if (existingGroup == null)
                {
                    TempData["error"] = "Grupul nu a fost găsit.";
                    return RedirectToAction("Index");
                }

                // Actualizăm câmpurile editabile
                existingGroup.GroupName = group.GroupName;
                existingGroup.GroupDescription = group.GroupDescription;
                existingGroup.Max = group.Max;
                existingGroup.Private = group.Private;

                // Console.WriteLine($"[DEBUG] Updated Private value: {existingGroup.Private}");

                try
                {
                    db.Groups.Update(existingGroup);
                    await db.SaveChangesAsync();

                    TempData["success"] = $"Grupul \"{existingGroup.GroupName}\" a fost actualizat cu succes!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    TempData["error"] = "A apărut o eroare la actualizarea grupului.";
                    return View(group);
                }
            }

            TempData["error"] = "Datele introduse nu sunt valide.";
            return View(group);
        }

        // ==========================================
        // 5. Ștergerea unui grup
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await db.Groups.Include(g => g.Posts).FirstOrDefaultAsync(g => g.GroupId == id);
            if (group == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            // Verificăm dacă utilizatorul curent e Moderator
            if (!(await _userManager.IsInRoleAsync(user, "Moderator")))
            {
                TempData["message"] = "Nu aveți permisiunea de a șterge acest grup.";
                return RedirectToAction("Index");
            }

            // Șterge postările asociate
            db.Posts.RemoveRange(group.Posts);

            // Șterge grupul
            db.Groups.Remove(group);
            await db.SaveChangesAsync();

            TempData["message"] = "Grupul a fost șters cu succes!";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 6. Alăturarea unui utilizator unui grup
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var group = await db.Groups.Include(g => g.UserGroups).FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
            {
                return NotFound("Grupul nu a fost găsit.");
            }

            var userId = _userManager.GetUserId(User);

            // Verificăm dacă utilizatorul este deja membru sau are o cerere în așteptare
            var existingMembership = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (existingMembership != null)
            {
                TempData["message"] = "Ești deja membru al acestui grup sau ai o cerere în așteptare.";
                return RedirectToAction("Show", "Groups", new { id = groupId });
            }

            // Verificăm dacă numărul maxim de participanți a fost atins
            if (group.UserGroups.Count >= group.Max)
            {
                TempData["message"] = "Grupul a atins numărul maxim de participanți.";
                return RedirectToAction("Show", "Groups", new { id = groupId });
            }

            // Adăugăm cererea în UserGroups
            var userGroup = new UserGroup
            {
                GroupId = groupId,
                UserId = userId,
                RegistrationDate = DateTime.Now,
                IsApproved = !group.Private // Cererile sunt aprobate automat doar pentru grupurile publice
            };

            db.UserGroups.Add(userGroup);
            await db.SaveChangesAsync();

            TempData["message"] = group.Private
                ? "Cererea de alăturare a fost trimisă. Așteaptă aprobarea moderatorului."
                : "Te-ai alăturat grupului cu succes!";

            return RedirectToAction("Show", "Groups", new { id = groupId });
        }


        // ==========================================
        // 7. Părăsirea unui grup
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var group = await db.Groups.Include(g => g.UserGroups).FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
            {
                return NotFound("Grupul nu a fost găsit.");
            }

            var userId = _userManager.GetUserId(User);

            // Găsim relația dintre utilizator și grup
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
            {
                TempData["message"] = "Nu ești membru al acestui grup.";
                return RedirectToAction("Show", "Groups", new { id = groupId });
            }

            db.UserGroups.Remove(userGroup);
            await db.SaveChangesAsync();

            TempData["message"] = "Ai părăsit grupul.";
            return RedirectToAction("Show", "Groups", new { id = groupId });
        }


        // ================================
        // 8. Cererile de join in grup
        // ================================
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> PendingGroupRequests()
        {
            var pendingRequests = await db.UserGroups
                .Where(ug => !ug.IsApproved) // Cereri neaprobate
                .Include(ug => ug.User)
                .Include(ug => ug.Group)
                .ToListAsync();

            return View(pendingRequests);
        }


        // ==============================================================
        // 9. Aprobare cerere de alăturare a unui grup (pentru moderator)
        // ==============================================================
        [HttpPost]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> ApproveGroupRequest(int groupId, string userId)
        {
            var request = await db.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (request == null)
            {
                TempData["message"] = "Cererea nu a fost găsită.";
                return RedirectToAction("PendingGroupRequests");
            }

            request.IsApproved = true;
            db.UserGroups.Update(request);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea de alăturare a fost aprobată.";
            return RedirectToAction("PendingGroupRequests");
        }



        // ==============================================================
        // 10. Respingere cerere de alăturare a unui grup (pentru moderator)
        // ==============================================================
        [HttpPost]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> DeclineGroupRequest(int groupId, string userId)
        {
            var request = await db.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (request == null)
            {
                TempData["message"] = "Cererea nu a fost găsită.";
                return RedirectToAction("PendingGroupRequests");
            }

            db.UserGroups.Remove(request);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea de alăturare a fost respinsă.";
            return RedirectToAction("PendingGroupRequests");
        }





        // ================================
        // 11. Postari realizate de useri
        // ================================
        [HttpPost]
        public async Task<IActionResult> PostMessage(int groupId, string message)
        {
            var userId = _userManager.GetUserId(User);
            var group = await db.Groups.Include(g => g.UserGroups)
                                       .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null || !group.UserGroups.Any(ug => ug.UserId == userId))
            {
                TempData["error"] = "Nu ai permisiunea de a posta în acest grup.";
                return RedirectToAction("ViewGroup", new { id = groupId });
            }

            var post = new Post
            {
                PostContent = message,
                UserId = userId,
                GroupId = groupId,
                PostDate = DateTime.Now
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();

            TempData["success"] = "Mesajul a fost postat cu succes!";
            return RedirectToAction("ViewGroup", new { id = groupId });
        }

        // ==========================================
        // 12. Vizualizare grup ( din rolul de User)
        // ==========================================
        public async Task<IActionResult> ViewGroup(int id)
        {
            var group = await db.Groups
                    .Include(g => g.Posts.OrderByDescending(post => post.PostDate)) // Include postările grupului
                    .Include(g => g.UserGroups) // Include membrii grupului
                    .FirstOrDefaultAsync(g => g.GroupId == id);


            if (group == null)
            {
                return NotFound("Grupul nu a fost găsit.");
            }

            var userId = _userManager.GetUserId(User);
            var isMember = group.UserGroups.Any(ug => ug.UserId == userId && ug.IsApproved);


            if (group.Private && !isMember)
            {
                TempData["error"] = "Nu ai acces la acest grup, deoarece este privat.";
                return RedirectToAction("Index");
            }

            ViewBag.IsMember = isMember;
            return View(group);
        }


        // ==========================================
        // 13. Vezi participanti
        // ==========================================

        [HttpGet]
        [Authorize(Roles = "Admin, Moderator")]
        public async Task<IActionResult> ViewParticipants(int id)
        {
            var group = await db.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            var participants = group.UserGroups
                .Where(ug => ug.IsApproved)
                .Select(ug => new
                {
                    ug.User.Id,
                    ug.User.UserName,
                    ug.User.Email,
                    ug.IsModerator,
                    ug.RegistrationDate
                }).ToList();


            ViewBag.GroupId = id;
            ViewBag.GroupName = group.GroupName;
            return View(participants);
        }

        // ==========================================
        // 14. Stergere participant
        // ==========================================

        [HttpPost]
        [Authorize(Roles = "Admin, Moderator")]
        public async Task<IActionResult> RemoveParticipant(int groupId, string userId)
        {
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
            {
                TempData["error"] = "Utilizatorul nu este membru al grupului.";
                return RedirectToAction("ViewParticipants", new { id = groupId });
            }

            db.UserGroups.Remove(userGroup);
            await db.SaveChangesAsync();

            TempData["success"] = "Utilizatorul a fost eliminat din grup.";
            return RedirectToAction("ViewParticipants", new { id = groupId });
        }

        // ===========================================
        // 15. Functie pentru iconita de Requests Group
        // ===========================================
        [HttpGet]
        [Authorize(Roles = "Moderator")]
        public async Task<int> GetPendingGroupRequestsCount()
        {
            // Calculăm numărul de cereri de alăturare neaprobate
            var pendingCount = await db.UserGroups
                .Where(ug => !ug.IsApproved) // Cereri neaprobate
                .CountAsync();

            return pendingCount;
        }

    }
}
