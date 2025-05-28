using System.ComponentModel.DataAnnotations;

namespace SmartHome.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }
}
