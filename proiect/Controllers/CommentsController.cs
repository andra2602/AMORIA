using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect.Data;
using proiect.Models;
using System.Security.Claims;

namespace proiect.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public CommentsController(ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            db = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }


        [HttpGet]
        [Authorize]
        public IActionResult CommentsForGroup(int groupId)
        {
            var comments = db.Comments
                .Where(c => c.GroupId == groupId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CommentDate)
                .ToList();

            return View(comments);
        }



        // Adaugarea unui comentariu asociat unei postari/unui grup in baza de date

        [HttpPost]
        public IActionResult New(Comment comm, string entityType, int entityId, int? parentCommentId = null)
        {
            comm.CommentDate = DateTime.Now;
            comm.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Asociază comentariul cu utilizatorul autentificat.
            comm.ParentCommentId = parentCommentId; // Setează ParentCommentId pentru reply-uri (dacă există).

            if (ModelState.IsValid)
            {
                if (entityType == "Post")
                {
                    // Obține postarea și verifică dacă există
                    var post = db.Posts.Include(p => p.Group).FirstOrDefault(p => p.PostId == entityId);
                    if (post == null)
                    {
                        TempData["error"] = "Postarea la care vrei să adaugi un comentariu nu există.";
                        return RedirectToAction("Index", "Posts");
                    }

                    // Dacă postarea aparține unui grup, verifică dacă utilizatorul este membru al acelui grup
                    if (post.GroupId.HasValue)
                    {
                        bool isUserInGroup = db.UserGroups.Any(ug => ug.UserId == comm.UserId && ug.GroupId == post.GroupId.Value);
                        if (!isUserInGroup)
                        {
                            TempData["error"] = "Nu ai permisiunea de a comenta la această postare din grup.";
                            return Redirect("/Groups/Show/" + post.GroupId);
                        }
                    }

                    // Asociază comentariul cu postarea
                    comm.PostId = entityId;
                }
                else if (entityType == "Group")
                {
                    // Verifică dacă utilizatorul este membru al grupului
                    bool isUserInGroup = db.UserGroups.Any(ug => ug.UserId == comm.UserId && ug.GroupId == entityId);
                    if (!isUserInGroup)
                    {
                        TempData["error"] = "Nu ai permisiunea de a comenta în acest grup.";
                        return Redirect("/Groups/Show/" + entityId);
                    }

                    // Verifică dacă grupul există
                    var groupExists = db.Groups.Any(g => g.GroupId == entityId);
                    if (!groupExists)
                    {
                        TempData["error"] = "Grupul la care încerci să comentezi nu există.";
                        return RedirectToAction("Index", "Groups");
                    }

                    // Asociază comentariul cu grupul
                    comm.GroupId = entityId;
                }
                else
                {
                    TempData["error"] = "Tipul de entitate specificat este invalid.";
                    return RedirectToAction("Index", "Home");
                }

                try
                {
                    db.Comments.Add(comm);
                    db.SaveChanges();

                    // Redirecționează utilizatorul către pagina corectă după salvare
                    if (entityType == "Post")
                    {
                        var post = db.Posts.FirstOrDefault(p => p.PostId == entityId);
                        if (post != null && post.GroupId.HasValue)
                        {
                            return RedirectToAction("ShowGroupPost", "Posts", new { id = entityId });
                        }
                        return RedirectToAction("Show", "Posts", new { id = entityId });
                    }
                    else if (entityType == "Group")
                    {
                        return Redirect("/Groups/Show/" + entityId);
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = "A apărut o eroare: " + ex.Message;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(comm);
        }




        // Stergerea unui comentariu asociat unei postari din baza de date
        // Se poate sterge comentariul doar de catre userii cu rolul de Admin 
        // sau de catre utilizatorii cu rolul de User sau Moderator, doar daca 
        // acel comentariu a fost postat de catre acestia

        [HttpPost]
        [Authorize(Roles = "User,Admin,Moderator")]
        public IActionResult DeleteCommentFromPost(int id)
        {
            // Găsim comentariul principal
            Comment comm = db.Comments.Find(id);

            if (comm == null)
            {
                return NotFound();
            }

            // Verificăm dacă utilizatorul curent este autorul comentariului, admin sau moderator al grupului
            var userId = _userManager.GetUserId(User);

            // Obținem postarea asociată comentariului (dacă există)
            var post = db.Posts.Include(p => p.Group).FirstOrDefault(p => p.PostId == comm.PostId);

            bool isGroupModerator = false;
            bool isMember = false;

            if (post?.GroupId != null)
            {
                // Verificăm dacă utilizatorul este moderator al grupului
                isGroupModerator = db.UserGroups.Any(ug => ug.GroupId == post.GroupId && ug.UserId == userId && ug.IsModerator);
                // Verificăm dacă utilizatorul este încă membru al grupului
                isMember = db.UserGroups.Any(ug => ug.GroupId == post.GroupId && ug.UserId == userId);
            }

            // Dacă utilizatorul nu este autorul comentariului, admin, moderator sau membru al grupului, refuzăm ștergerea
            if (comm.UserId != userId && !User.IsInRole("Admin") && !isGroupModerator)
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest comentariu.";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
            // Dacă utilizatorul nu mai este membru al grupului, refuzăm ștergerea
            if (post.GroupId != null && !isMember && !User.IsInRole("Admin") && !isGroupModerator)
            {
                TempData["message"] = "Nu mai ești membru al grupului. Nu poți șterge acest comentariu.";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            if (comm.UserId == userId || User.IsInRole("Admin") || isGroupModerator)
            {
                // Șterge comentariul și toate reply-urile asociate
                DeleteCommentAndReplies(id);

                TempData["message"] = "Comentariul și răspunsurile asociate au fost șterse.";
                TempData["messageType"] = "alert-success";

                return Redirect("/Posts/Show/" + comm.PostId);
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest comentariu.";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }

        // Metoda recursivă pentru a șterge comentariul și toate răspunsurile asociate
        private void DeleteCommentAndReplies(int commentId)
        {
            // Găsim toate comentariile care au acest comentariu ca părinte
            var childComments = db.Comments.Where(c => c.ParentCommentId == commentId).ToList();

            foreach (var child in childComments)
            {
                // Apelăm recursiv pentru fiecare comentariu copil
                DeleteCommentAndReplies(child.CommentId);
            }

            // Ștergem comentariul curent
            var commentToDelete = db.Comments.Find(commentId);
            if (commentToDelete != null)
            {
                db.Comments.Remove(commentToDelete);
            }

            db.SaveChanges();
        }


        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent
        // Editarea unui comentariu asociat unui articol
        // [HttpGet] - se executa implicit
        // Se poate edita un comentariu doar de catre utilizatorul care a postat comentariul respectiv 
        // Adminii pot edita orice comentariu, chiar daca nu a fost postat de ei

        [Authorize(Roles = "User")]
        public IActionResult EditCommentFromPost(int id)
        {
            Comment comm = db.Comments.Find(id);
            if (comm == null)
            {
                return NotFound();
            }

            // Obținem postarea asociată comentariului
            var post = db.Posts.Include(p => p.Group).FirstOrDefault(p => p.PostId == comm.PostId);

            if (post == null)
            {
                TempData["message"] = "Postarea asociată acestui comentariu nu mai există.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Groups");
            }

            // Verificăm dacă utilizatorul este autorul comentariului
            if (comm.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest comentariu.";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            // Verificăm dacă utilizatorul este membru al grupului (dacă postarea aparține unui grup)
            if (post.GroupId != null)
            {
                bool isMember = db.UserGroups.Any(ug => ug.GroupId == post.GroupId && ug.UserId == _userManager.GetUserId(User));
                if (!isMember)
                {
                    TempData["message"] = "Nu mai ești membru al grupului. Nu poți edita acest comentariu.";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Posts/Show/" + comm.PostId);
                }
            }

            return View(comm);
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult EditCommentFromPost(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);
            if (comm == null)
            {
                return NotFound();
            }

            // Obținem postarea asociată comentariului
            var post = db.Posts.Include(p => p.Group).FirstOrDefault(p => p.PostId == comm.PostId);

            if (post == null)
            {
                TempData["message"] = "Postarea asociată acestui comentariu nu mai există.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Groups");
            }

            // Verificăm dacă utilizatorul este autorul comentariului
            if (comm.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest comentariu.";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            // Verificăm dacă utilizatorul este membru al grupului (dacă postarea aparține unui grup)
            if (post.GroupId != null)
            {
                bool isMember = db.UserGroups.Any(ug => ug.GroupId == post.GroupId && ug.UserId == _userManager.GetUserId(User));
                if (!isMember)
                {
                    TempData["message"] = "Nu mai ești membru al grupului. Nu poți edita acest comentariu.";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Posts/Show/" + comm.PostId);
                }
            }

            // Salvăm modificările dacă totul este valid
            if (ModelState.IsValid)
            {
                comm.CommentContent = requestComment.CommentContent;

                db.SaveChanges();
                TempData["message"] = "Comentariul a fost editat cu succes.";
                TempData["messageType"] = "alert-success";

                return Redirect("/Posts/Show/" + comm.PostId);
            }
            else
            {
                return View(requestComment);
            }
        }


    }
}