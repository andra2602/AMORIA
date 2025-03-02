using System.ComponentModel.DataAnnotations;

namespace proiect.Models
{
    public class Friendship
    {

        [Key]
        public int FriendshipId { get; set; }

        public string Status { get; set; }

        public string? UserId1 { get; set; } // FK către User (ca inițiator al prieteniei)
        public ApplicationUser? User1 { get; set; }

        public string? UserId2 { get; set; } // FK către User (ca receptor al prieteniei)
        public ApplicationUser? User2 { get; set; }

    }
}
