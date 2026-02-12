using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class ReviseRequestsController : Controller
{
    private readonly IReviseRequestService _reviseRequestService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ReviseRequestsController(
        IReviseRequestService reviseRequestService,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IWebHostEnvironment webHostEnvironment)
    {
        _reviseRequestService = reviseRequestService;
        _currentUser = currentUser;
        _auditService = auditService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public IActionResult Index(ReviseRequestStatus? status)
    {
        var allRequests = _reviseRequestService.GetAll();
        var requests = status.HasValue
            ? allRequests.Where(r => r.Status == status.Value).ToList()
            : allRequests.ToList();

        // Her request için image'leri yükle
        foreach (var request in requests)
        {
            request.Images = _reviseRequestService.GetImages(request.Id).ToList();
        }

        var model = new ReviseRequestIndexViewModel
        {
            Requests = requests,
            FilterStatus = status
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Detail(int id)
    {
        var request = _reviseRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }

        var model = new ReviseRequestDetailViewModel
        {
            Request = request
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var request = _reviseRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }

        var model = new ReviseRequestEditInputModel
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description
        };

        ViewBag.Request = request;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviseRequestCreateInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var request = await _reviseRequestService.CreateAsync(
            input.Title,
            input.Description,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "Sistem",
            input.Images,
            _webHostEnvironment.WebRootPath);

        _auditService.Log(
            AuditAction.ReviseRequestCreated,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "Sistem",
            "ReviseRequest",
            request.Id.ToString(),
            $"Yeni talep oluşturuldu: {request.Title}");

        TempData["Success"] = "Talep başarıyla oluşturuldu.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviseRequestEditInputModel input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Request = _reviseRequestService.GetById(input.Id);
            return View(input);
        }

        await _reviseRequestService.UpdateAsync(
            input.Id,
            input.Title,
            input.Description,
            input.NewImages,
            input.ImagesToDelete,
            _webHostEnvironment.WebRootPath);

        _auditService.Log(
            AuditAction.ReviseRequestStatusUpdated,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "Sistem",
            "ReviseRequest",
            input.Id.ToString(),
            $"Talep güncellendi: {input.Title}");

        TempData["Success"] = "Talep başarıyla güncellendi.";
        return RedirectToAction("Detail", new { id = input.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int id, ReviseRequestStatus status)
    {
        var request = _reviseRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }

        _reviseRequestService.UpdateStatus(id, status);

        _auditService.Log(
            AuditAction.ReviseRequestStatusUpdated,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "Sistem",
            "ReviseRequest",
            id.ToString(),
            $"Talep durumu güncellendi: {status}");

        TempData["Success"] = "Talep durumu güncellendi.";
        return RedirectToAction("Detail", new { id });
    }
}
