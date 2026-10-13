using System.ComponentModel.DataAnnotations;

namespace TimeVerse.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address format (e.g. user@example.com).")]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your contact phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select or provide an inquiry subject.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your message or inquiry details.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        public bool IsSubmittedSuccess { get; set; } = false;
        public string? FeedbackMessage { get; set; }
    }
}
