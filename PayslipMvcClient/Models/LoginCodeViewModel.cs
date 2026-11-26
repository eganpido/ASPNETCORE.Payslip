using System.ComponentModel.DataAnnotations;

namespace PayslipMvcClient.Models
{
    public class LoginCodeViewModel
    {
        [Display(Name = "Mobile Code")]
        [Required(ErrorMessage = "Palihug i-enter ang imong Mobile Code.")]
        public string Code { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}