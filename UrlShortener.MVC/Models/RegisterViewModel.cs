using System.ComponentModel.DataAnnotations;

namespace UrlShortener.MVC.Models;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Username { get; set; } = "";

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}