using System.ComponentModel.DataAnnotations;

namespace SmartHome.ViewModels
{
    public class TwoFactorViewModel
    {
        [Required]
        [Display(Name = "Verification Code")]
        public string Code { get; set; }

        public bool RememberMe { get; set; }
        public string Email { get; set; }
    }
}
