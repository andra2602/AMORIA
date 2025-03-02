using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace proiect.Models
{
    public class Profile
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele profilului este obligatoriu")]
        [MaxLength(150, ErrorMessage = "Numele profilului poate aveam maxim 150 de caractere")]
        public string ProfileName { get; set; }

        [Required(ErrorMessage = "Descrierea profilului este obligatoriu")]
        [MaxLength(100, ErrorMessage = "Descrierea profilului poate aveam maxim 100 de caractere")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Imaginea profilului este obligatorie")]
        public string ImagePath { get; set; }

        public string? Website { get; set; }

        public string? Location { get; set; }

        [Required(ErrorMessage = "Va rugam sa alegeti o optiune")]
        public bool Visibility { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // Relație inversă către ApplicationUser

    }
}
