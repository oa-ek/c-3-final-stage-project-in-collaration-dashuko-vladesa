using System.ComponentModel.DataAnnotations;

namespace LogisticsGroup.Web.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має бути від 6 символів")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердіть пароль")]
        [Compare("Password", ErrorMessage = "Паролі не співпадають.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}