using System.ComponentModel.DataAnnotations;

namespace LogisticsGroup.Web.Models
{
    public class RegisterStaffViewModel
    {
        [Required(ErrorMessage = "Введіть Email")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        [StringLength(100, ErrorMessage = "{0} повинен містити мінімум {2} символів.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оберіть посаду (роль)")]
        [Display(Name = "Посада")]
        public string Role { get; set; } = string.Empty;
    }
}