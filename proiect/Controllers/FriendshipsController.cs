using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect.Data;
using proiect.Models;

namespace proiect.Controllers
{
    [Authorize(Roles = "Admin,User,Moderator")]
    public class FriendshipsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public FriendshipsController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            db = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // ==========================================
        // 1. Listarea tuturor prieteniilor
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var friendships = await db.Friendships
                .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && f.Status == "confirmed")
                .Include(f => f.User1)
                .Include(f => f.User2)
                .ToListAsync();

            return View(friendships);
        }

        // ==========================================
        // 2. Afișarea unei prietenii specifice
        // ==========================================
        public async Task<IActionResult> Show(int id)
        {
            var friendship = await db.Friendships
                .Include(f => f.User1)
                .Include(f => f.User2)
                .FirstOrDefaultAsync(f => f.FriendshipId == id);

            if (friendship == null)
            {
                return NotFound("Prietenia nu a fost găsită.");
            }

            return View(friendship);
        }

        // ==========================================
        // 3. Trimiterea unei cereri de prietenie
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> AddFriend(string friendId)
        {
            


            var userId = _userManager.GetUserId(User);

            // Verifică dacă utilizatorul către care se trimite cererea există
            var friend = await db.ApplicationUsers
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == friendId);

            if (friend == null)
            {
                TempData["message"] = "Utilizatorul nu există sau este inaccesibil.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Users");
            }

            // Previne trimiterea cererii către propriul cont
            if (userId == friendId)
            {
                TempData["message"] = "Nu poți trimite cerere de prietenie către tine însuți.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Users", new { id = friendId });
            }

            // Verifică dacă există deja o cerere de prietenie sau o relație
            var existingRequest = await db.Friendships
                .FirstOrDefaultAsync(f => (f.UserId1 == userId && f.UserId2 == friendId) ||
                                          (f.UserId1 == friendId && f.UserId2 == userId));

            if (existingRequest != null)
            {
                if (existingRequest.Status == "Pending")
                {
                    TempData["message"] = "Cererea de prietenie este deja trimisă și în așteptare.";
                    TempData["messageType"] = "alert-warning";
                }
                else if (existingRequest.Status == "Confirmed")
                {
                    TempData["message"] = "Sunteți deja prieteni.";
                    TempData["messageType"] = "alert-danger";
                }
                return RedirectToAction("Show", "Users", new { id = friendId });
            }

            // Creează o cerere de prietenie nouă
            var newFriendship = new Friendship
            {
                UserId1 = userId,
                UserId2 = friendId,
                Status = "Pending"
            };

            db.Friendships.Add(newFriendship);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea de prietenie a fost trimisă.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", "Users", new { id = friendId });
        }


        // ==========================================
        // 4. Acceptarea unei cereri de prietenie
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> AcceptFriendRequest(int id)
        {
            var userId = _userManager.GetUserId(User);

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.FriendshipId == id &&
                                          ((f.UserId1 == userId && f.Status == "pending") ||
                                           (f.UserId2 == userId && f.Status == "pending")));

            if (friendship == null)
            {
                TempData["message"] = "Cererea de prietenie nu există sau nu mai este în așteptare.";
                return RedirectToAction("Index");
            }

            friendship.Status = "confirmed";
            db.Friendships.Update(friendship);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea de prietenie a fost acceptată.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. Refuzarea unei cereri de prietenie
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,User,Moderator")]
        public async Task<IActionResult> DeclineFriendRequest(int id)
        {
            var userId = _userManager.GetUserId(User);

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.FriendshipId == id &&
                                          ((f.UserId1 == userId && f.Status == "pending") ||
                                           (f.UserId2 == userId && f.Status == "pending")));

            if (friendship == null)
            {
                TempData["message"] = "Cererea de prietenie nu există sau nu mai este în așteptare.";
                return RedirectToAction("Index");
            }

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea de prietenie a fost refuzată.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 6. Ștergerea unei prietenii
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFriend(int id)
        {
            var userId = _userManager.GetUserId(User);

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.FriendshipId == id &&
                                          (f.UserId1 == userId || f.UserId2 == userId));

            if (friendship == null)
            {
                TempData["message"] = "Prietenia nu a fost găsită.";
                return RedirectToAction("Index");
            }

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();

            TempData["message"] = "Prietenia a fost ștearsă.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 7. Afișarea cererilor primite
        // ==========================================
        public async Task<IActionResult> PendingRequests()
        {
            var userId = _userManager.GetUserId(User);

            var pendingRequests = await db.Friendships
                .Where(f => f.UserId2 == userId && f.Status == "Pending")
                .Include(f => f.User1) // Include utilizatorul care a trimis cererea
                .ToListAsync();

            return View(pendingRequests);
        }

        // ==========================================
        // 8. Numar pending requests
        // ==========================================

        [HttpGet]
        public async Task<int> GetPendingRequestsCount()
        {
            var userId = _userManager.GetUserId(User);

            var pendingCount = await db.Friendships
                .Where(f => f.UserId2 == userId && f.Status == "Pending")
                .CountAsync();

            return pendingCount;
        }



    }
}
