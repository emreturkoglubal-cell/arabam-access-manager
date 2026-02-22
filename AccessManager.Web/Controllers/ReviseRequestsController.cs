using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Revizyon talepleri: kullanıcıların sistemle ilgili düzeltme/iyileştirme talepleri. Liste (durum filtresi: Pending/Resolved), detay, oluşturma, düzenleme, durum güncelleme (UpdateStatus). İsteğe bağlı ekran görüntüsü (image) eklenebilir.
/// Yetki: Admin veya Manager.
/// </summary>
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

    /// <summary>GET /ReviseRequests/Index — Revizyon taleplerini listeler; status (Pending/Resolved) ile filtrelenebilir.</summary>
    [HttpGet]
    public IActionResult Index(ReviseRequestStatus? status)
    {
        var allRequests = _reviseRequestService.GetAll();
        var requests = status.HasValue
            ? allRequests.Where(r => r.Status == status.Value).ToList()
            : allRequests.ToList();

        if (requests.Count > 0)
        {
            var requestIds = requests.Select(r => r.Id).ToList();
            var allImages = _reviseRequestService.GetImagesByReviseRequestIds(requestIds);
            var imagesByRequest = allImages.GroupBy(i => i.ReviseRequestId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList());
            foreach (var request in requests)
                request.Images = imagesByRequest.TryGetValue(request.Id, out var imgs) ? imgs : new List<ReviseRequestImage>();
        }

        var model = new ReviseRequestIndexViewModel
        {
            Requests = requests,
            FilterStatus = status
        };

        return View(model);
    }

    /// <summary>GET /ReviseRequests/Detail/{id} — Tek revizyon talebinin detayı ve ekran görüntüleri.</summary>
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

    /// <summary>GET /ReviseRequests/Edit/{id} — Revizyon talebi düzenleme formu.</summary>
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

    /// <summary>POST /ReviseRequests/Create — Yeni revizyon talebi oluşturur; isteğe bağlı görseller yüklenebilir.</summary>
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

    /// <summary>POST /ReviseRequests/Edit — Revizyon talebinin başlık/açıklama ve görsellerini günceller.</summary>
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

    /// <summary>POST /ReviseRequests/UpdateStatus — Revizyon talebinin durumunu Pending veya Resolved olarak günceller.</summary>
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
