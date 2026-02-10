using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Offboarding;

[Authorize(Roles = "Admin,Manager")]
public class ResultModel : PageModel
{
    private readonly IPersonnelService _personnelService;

    public ResultModel(IPersonnelService personnelService)
    {
        _personnelService = personnelService;
    }

    public PersonnelEntity? Personnel { get; set; }

    public IActionResult OnGet(int id)
    {
        Personnel = _personnelService.GetById(id);
        if (Personnel == null) return NotFound();
        return Page();
    }
}
