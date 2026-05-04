using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class PositionTitleTemplatesController : Controller
{
    private readonly IPositionTitleTemplateRepository _repo;
    private readonly IDepartmentService _departmentService;
    private readonly ITeamService _teamService;

    public PositionTitleTemplatesController(
        IPositionTitleTemplateRepository repo,
        IDepartmentService departmentService,
        ITeamService teamService)
    {
        _repo = repo;
        _departmentService = departmentService;
        _teamService = teamService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Rows = _repo.GetAll();
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Teams = _teamService.GetAll();
        return View();
    }

    public class CreateInput
    {
        public int? DepartmentId { get; set; }
        public int? TeamId { get; set; }
        public string? SeniorityLevel { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            TempData["PttError"] = "Ünvan metni zorunludur.";
            return RedirectToAction(nameof(Index));
        }
        _repo.Insert(new PositionTitleTemplate
        {
            DepartmentId = input.DepartmentId,
            TeamId = input.TeamId,
            SeniorityLevel = string.IsNullOrWhiteSpace(input.SeniorityLevel) ? null : input.SeniorityLevel.Trim(),
            Title = input.Title.Trim()
        });
        TempData["PttSuccess"] = "Şablon eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _repo.Delete(id);
        TempData["PttSuccess"] = "Şablon silindi.";
        return RedirectToAction(nameof(Index));
    }
}
