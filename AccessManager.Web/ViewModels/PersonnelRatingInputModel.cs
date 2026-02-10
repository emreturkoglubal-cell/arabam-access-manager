using System.ComponentModel.DataAnnotations;

namespace AccessManager.UI.ViewModels;

public class PersonnelRatingInputModel
{
    [Range(0, 10, ErrorMessage = "Puan 0–10 arasında olmalıdır.")]
    public decimal? Rating { get; set; }

    [MaxLength(2000, ErrorMessage = "Yorum en fazla 2000 karakter olabilir.")]
    public string? ManagerComment { get; set; }
}
