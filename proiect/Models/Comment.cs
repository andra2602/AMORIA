using System.ComponentModel.DataAnnotations;

namespace proiect.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu")]
        [StringLength(200, ErrorMessage = "Comentariul nu poate avea mai mult de 200 de caractere!")]
        [MinLength(3, ErrorMessage = "Comentariul nu poate avea mai putin de 3 caractere!")]
        public string CommentContent { get; set; }

        public DateTime CommentDate { get; set; } = DateTime.Now;



        //un comentariu apartine unei postari (FK)
        public int PostId { get; set; }
        public virtual Post? Post { get; set; }



        //un comentariu apartine unui grup (FK)
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }





        //cheie externa(FK) - un com este postat de catre un user
        public string? UserId { get; set; }

        //proprietate virtuala - un comentariu este postat de catre un user
        public virtual ApplicationUser? User { get; set; }



        // un comentariu poate fi un reply la alt comentariu (FK)
        public int? ParentCommentId { get; set; }
        public virtual Comment? ParentComment { get; set; }
        public virtual ICollection<Comment>? Replies { get; set; } = new List<Comment>();


    }
}
