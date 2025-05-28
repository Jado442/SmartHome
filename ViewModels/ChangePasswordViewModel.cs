using System.ComponentModel.DataAnnotations;

namespace SmartHome.ViewModels
{
public class ChangePasswordViewModel
{
    [EmailAddress]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm New Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    public string ConfirmNewPassword { get; set; }
}
}
