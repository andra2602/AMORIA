using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using proiect.Data;

namespace proiect.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificăm dacă există deja date în tabelul Roles
                if (context.Roles.Any())
                {
                    return; // Baza de date conține deja date
                }

                // Crearea rolurilor în baza de date
                context.Roles.AddRange(
                    new IdentityRole { Id = "487bed00-c2ba-4401-bc28-0eff3bf46cd5", Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = "487bed00-c2ba-4401-bc28-0eff3bf46cd6", Name = "Moderator", NormalizedName = "MODERATOR" },
                    new IdentityRole { Id = "487bed00-c2ba-4401-bc28-0eff3bf46cd7", Name = "User", NormalizedName = "USER" }
                );

                // Instanță pentru hash-ul parolelor
                var hasher = new PasswordHasher<ApplicationUser>();

                // Crearea utilizatorilor în baza de date
                var users = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = "c054f9d6-8688-499f-b20a-8aafca1d9944",
                        UserName = "admin@test.com",
                        Email = "admin@test.com",
                        NormalizedEmail = "ADMIN@TEST.COM",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Admin1!"),
                        UserFirstName = "AdminFirstName",
                        UserLastName = "AdminLastName",
                        BirthDate = new DateTime(1990, 1, 1)
                    },
                    new ApplicationUser
                    {
                        Id = "c054f9d6-8688-499f-b20a-8aafca1d9945",
                        UserName = "moderator@test.com",
                        Email = "moderator@test.com",
                        NormalizedEmail = "MODERATOR@TEST.COM",
                        NormalizedUserName = "MODERATOR@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Moderator1!"),
                        UserFirstName = "ModeratorFirstName",
                        UserLastName = "ModeratorLastName",
                        BirthDate = new DateTime(1995, 2, 2)
                    },
                    new ApplicationUser
                    {
                        Id = "c054f9d6-8688-499f-b20a-8aafca1d9946",
                        UserName = "user1@test.com",
                        Email = "user1@test.com",
                        NormalizedEmail = "USER1@TEST.COM",
                        NormalizedUserName = "USER1@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        UserFirstName = "User1FirstName",
                        UserLastName = "User1LastName",
                        BirthDate = new DateTime(2000, 3, 3)
                    },
                    new ApplicationUser
                    {
                        Id = "c054f9d6-8688-499f-b20a-8aafca1d9947",
                        UserName = "user2@test.com",
                        Email = "user2@test.com",
                        NormalizedEmail = "USER2@TEST.COM",
                        NormalizedUserName = "USER2@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User2!"),
                        UserFirstName = "User2FirstName",
                        UserLastName = "User2LastName",
                        BirthDate = new DateTime(2005, 4, 4)
                    }
                };

                context.Users.AddRange(users);

                // Crearea profilelor pentru fiecare utilizator
                var profiles = users.Select(user => new Profile
                {                  
                    ProfileName = $"{user.UserFirstName} {user.UserLastName}'s Profile",
                    Description = "Acesta este profilul utilizatorului.",
                    ImagePath = "/images/default.jpg",
                    Website = "https://example.com",
                    Location = "Unknown",
                    Visibility = true,
                    User = user
                }).ToList();

                context.Profiles.AddRange(profiles);

                // Asocierea utilizatorilor cu rolurile
                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        RoleId = "487bed00-c2ba-4401-bc28-0eff3bf46cd5",
                        UserId = "c054f9d6-8688-499f-b20a-8aafca1d9944"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "487bed00-c2ba-4401-bc28-0eff3bf46cd6",
                        UserId = "c054f9d6-8688-499f-b20a-8aafca1d9945"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "487bed00-c2ba-4401-bc28-0eff3bf46cd7",
                        UserId = "c054f9d6-8688-499f-b20a-8aafca1d9946"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "487bed00-c2ba-4401-bc28-0eff3bf46cd7",
                        UserId = "c054f9d6-8688-499f-b20a-8aafca1d9947"
                    }
                );

                // Salvarea modificărilor
                context.SaveChanges();
            }
        }
    }
}
