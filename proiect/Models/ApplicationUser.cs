using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace proiect.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required(ErrorMessage = "Numele este obligatoriu")]
        public string UserLastName { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        public string UserFirstName { get; set; }

        [Required(ErrorMessage = "Data este obligatorie")]
        public DateTime BirthDate { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public bool? Status { get; set; }//cum vrei sa ai profilul activ/fantoma



        //----------------------------------------------------------------//


        public virtual Profile Profile { get; set; }



        // Relația one-to-many: Un utilizator poate avea multe postări
        public ICollection<Post>? Posts { get; set; }

        // Relația one-to-many: Un utilizator poate avea multe comentarii
        public ICollection<Comment>? Comments { get; set; }


        // Colecția de UserGroup pentru relația many-to-many
        public ICollection<UserGroup>? UserGroups { get; set; }


        // Relație Many-to-Many prin Friendships
        public ICollection<Friendship> FriendshipsAsUser1 { get; set; }
        public ICollection<Friendship> FriendshipsAsUser2 { get; set; }

        //variabila care retine toate rolurile pt dropdown list

        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

        // Relație One-to-Many: Un utilizator poate gestiona mai multe grupuri
        public virtual ICollection<Group> ManagedGroups { get; set; }
    }
}
