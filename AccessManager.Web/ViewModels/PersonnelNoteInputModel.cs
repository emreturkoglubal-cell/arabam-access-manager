using System.ComponentModel.DataAnnotations;

namespace AccessManager.UI.ViewModels;

public class PersonnelNoteInputModel
{
    [Required(ErrorMessage = "Not içeriği girin.")]
    [MaxLength(2000)]
    public string? Content { get; set; }
}
