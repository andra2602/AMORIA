using System.ComponentModel.DataAnnotations;

namespace proiect.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu")]
        public string PostContent { get; set; }

        public string? ImageVideoPath { get; set; }

        public string? EmbeddedVideoUrl { get; set; } // Link către videoclipul embedded (opțional)

        public DateTime PostDate { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }


        // Relația one-to-many: O postare poate avea mai multe comentarii
        public ICollection<Comment>? Comments { get; set; }



        // Relația cu Group
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }
}
