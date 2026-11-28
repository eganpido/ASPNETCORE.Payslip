using System.ComponentModel.DataAnnotations;

namespace PayslipMvcClient.Models
{
    public class LoginCodeViewModel
    {
        [Display(Name = "Mobile Code")]
        [Required(ErrorMessage = "The Mobile Code is required.")]
        public string Code { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}