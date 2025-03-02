using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proiect.Models
{
    public class Group
    {

        public Group()
        {
            UserGroups = new List<UserGroup>();
        }

        [Key]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [MaxLength(50, ErrorMessage = "Numele grupului nu poate avea mai mult de 50 de caractere!")]
        [MinLength(3, ErrorMessage = "Numele grupului nu poate avea mai putin de 3 caractere!")]
        public string GroupName { get; set; }

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(250, ErrorMessage = "Descrierea grupului nu poate avea mai mult de 250 de caractere!")]
        [MinLength(20, ErrorMessage = "Descrierea grupului nu poate avea mai putin de 20 de caractere!")]
        public string GroupDescription { get; set; }

        [Required(ErrorMessage = "Alegeti vizibilitatea grupului")]
        public bool Private { get; set; }

        public DateTime GroupDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Setati un numar maxim de participanti")]
        [Range(3, 1000, ErrorMessage = "Valoarea trebuie să fie între 3 și 1000.")]
        public int Max { get; set; }


        // FK către utilizatorul care a creat grupul
        public string ManagerGroupId { get; set; }

        [ForeignKey("ManagerGroupId")]
     
        public virtual ApplicationUser? ManagerGroup { get; set; }

        // Relația one-to-many: Un grup poate avea multe comentarii
        public ICollection<Comment>? Comments { get; set; }

        // Colecția de UserGroup pentru relația many-to-many
        [NotMapped]
        public ICollection<UserGroup> UserGroups { get; set; }


        // Relația one-to-many: Un grup poate avea mai multe postări
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}