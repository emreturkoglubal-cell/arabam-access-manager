using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Offboarding;

public class ResultModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IPersonnelAccessService _accessService;
    private readonly ISystemService _systemService;

    public ResultModel(IPersonnelService personnelService, IPersonnelAccessService accessService, ISystemService systemService)
    {
        _personnelService = personnelService;
        _accessService = accessService;
        _systemService = systemService;
    }

    public Models.Personnel? Personnel { get; set; }

    public IActionResult OnGet(Guid id)
    {
        Personnel = _personnelService.GetById(id);
        if (Personnel == null) return NotFound();
        return Page();
    }
}
