using System.ComponentModel.DataAnnotations;

namespace AccessManager.UI.ViewModels;

public class ReviseRequestEditInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(500, ErrorMessage = "Başlık en fazla 500 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    public string Description { get; set; } = string.Empty;

    public List<IFormFile>? NewImages { get; set; }
    public List<int>? ImagesToDelete { get; set; }
}
