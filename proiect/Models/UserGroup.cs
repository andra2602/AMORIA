namespace proiect.Models
{
    public class UserGroup
    {
        public string UserId { get; set; }
        public int GroupId { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual Group Group { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Indică dacă utilizatorul este un moderator al grupului
        public bool IsModerator { get; set; } = false;

        // Indică dacă utilizatorul a fost aprobat pentru a deveni membru al grupului
        public bool IsApproved { get; set; } = true;
    }
}
