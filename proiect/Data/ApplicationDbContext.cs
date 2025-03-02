using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proiect.Models;

namespace proiect.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //ONE TO ONE

            //definire relatie one to one User si Profile

            modelBuilder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId) // UserId devine cheia externă
                .OnDelete(DeleteBehavior.Cascade); // Ștergere în cascadă

            //-----------------------------------------------------------------

            //ONE TO MANY


            //Configurare one-to-many intre User si Friendship

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User1)
                .WithMany(u => u.FriendshipsAsUser1) //un ut. poate avea mai multi prieteni in care este initiator
                .HasForeignKey(f => f.UserId1)
                .OnDelete(DeleteBehavior.Restrict); // Previne ștergerea în cascadă


            // Relație între Friendship și ApplicationUser pentru UserId2
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User2) //user 2 este utilizatorul din cealalta parte a prieteniei
                .WithMany(u => u.FriendshipsAsUser2) //user2 poate avea mai multe prietenii in care este receptorul
                .HasForeignKey(f => f.UserId2)
                .OnDelete(DeleteBehavior.Restrict);



            // Configurare one-to-many între User și Post
            modelBuilder.Entity<Post>()
                .HasOne(p => p.User) // Fiecare postare are un utilizator
                .WithMany(u => u.Posts) // Un utilizator poate avea multe postări
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull); 



            // Configurare one-to-many între User și Comment
            modelBuilder.Entity<Comment>()
                .HasOne(p => p.User) // Fiecare comentariu are un utilizator
                .WithMany(u => u.Comments) // Un utilizator poate avea multe comentarii
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Cheia străină în Comments



            // Configurare one-to-many între Post și Comment
            modelBuilder.Entity<Comment>()
                .HasOne(p => p.Post) // Fiecare comentariu are o postare
                .WithMany(u => u.Comments) // O postare poate avea multe comentarii
                .HasForeignKey(p => p.PostId); // Cheia străină în Comments



            // Configurare one-to-many între Group și Comment
            modelBuilder.Entity<Comment>()
                .HasOne(p => p.Group) // Fiecare comentariu are un grup
                .WithMany(u => u.Comments) // Un grup poate avea multe comentarii
                .HasForeignKey(p => p.GroupId); // Cheia străină în Comments


            //-------------------------------------------------------------------


            //MANY TO MANY

            // Relație One-to-Many între ApplicationUser și Group
            modelBuilder.Entity<Group>()
                .HasOne(g => g.ManagerGroup)
                .WithMany(u => u.ManagedGroups)
                .HasForeignKey(g => g.ManagerGroupId)
                .OnDelete(DeleteBehavior.Restrict);


            //definire cheie primara compusa UserGroup
            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });


            //definire relatii many to many User si Group
            modelBuilder.Entity<UserGroup>()
                   .HasOne(ug => ug.User)
                   .WithMany(ug => ug.UserGroups) // Corectat pentru a reflecta colecția din User
                   .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(ug => ug.UserGroups) // Corectat pentru a reflecta colecția din Role
                .HasForeignKey(ug => ug.GroupId);
        }

    }
}
