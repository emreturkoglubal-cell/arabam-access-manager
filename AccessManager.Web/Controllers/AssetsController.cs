using AccessManager.Application.Interfaces;
using AccessManager.Domain.Constants;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class AssetsController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IPersonnelService _personnelService;
    private readonly ICurrentUserService _currentUser;

    public AssetsController(IAssetService assetService, IPersonnelService personnelService, ICurrentUserService currentUser)
    {
        _assetService = assetService;
        _personnelService = personnelService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Index(AssetStatus? status, AssetType? type)
    {
        var assets = _assetService.GetAll();
        if (status.HasValue) assets = assets.Where(a => a.Status == status.Value).ToList();
        if (type.HasValue) assets = assets.Where(a => a.AssetType == type.Value).ToList();

        var assignmentByAsset = new Dictionary<int, AssetAssignment>();
        var personNames = new Dictionary<int, string>();
        foreach (var a in assets.Where(x => x.Status == AssetStatus.Assigned))
        {
            var assign = _assetService.GetActiveAssignmentForAsset(a.Id);
            if (assign != null)
            {
                assignmentByAsset[a.Id] = assign;
                var p = _personnelService.GetById(assign.PersonnelId);
                personNames[assign.PersonnelId] = p != null ? $"{p.FirstName} {p.LastName}" : "—";
            }
        }

        ViewBag.Assets = assets;
        ViewBag.AssignmentByAsset = assignmentByAsset;
        ViewBag.PersonNames = personNames;
        ViewBag.FilterStatus = status;
        ViewBag.FilterType = type;
        return View();
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create()
    {
        return View(new AssetEditInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create(AssetEditInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Ad gerekli.");
            return View(input);
        }
        var asset = new Asset
        {
            AssetType = input.AssetType,
            Name = input.Name.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(input.SerialNumber) ? null : input.SerialNumber.Trim(),
            BrandModel = string.IsNullOrWhiteSpace(input.BrandModel) ? null : input.BrandModel.Trim(),
            Status = input.Status,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            PurchaseDate = input.PurchaseDate
        };
        _assetService.Create(asset);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id)
    {
        var asset = _assetService.GetById(id);
        if (asset == null) return NotFound();
        ViewBag.Asset = asset;
        return View(new AssetEditInputModel
        {
            AssetType = asset.AssetType,
            Name = asset.Name,
            SerialNumber = asset.SerialNumber,
            BrandModel = asset.BrandModel,
            Status = asset.Status,
            Notes = asset.Notes,
            PurchaseDate = asset.PurchaseDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id, AssetEditInputModel input)
    {
        var asset = _assetService.GetById(id);
        if (asset == null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Ad gerekli.");
            ViewBag.Asset = asset;
            return View(input);
        }
        asset.AssetType = input.AssetType;
        asset.Name = input.Name.Trim();
        asset.SerialNumber = string.IsNullOrWhiteSpace(input.SerialNumber) ? null : input.SerialNumber.Trim();
        asset.BrandModel = string.IsNullOrWhiteSpace(input.BrandModel) ? null : input.BrandModel.Trim();
        asset.Status = input.Status;
        asset.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        asset.PurchaseDate = input.PurchaseDate;
        _assetService.Update(asset);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Delete(int id)
    {
        var asset = _assetService.GetById(id);
        if (asset == null) return NotFound();
        ViewBag.Asset = asset;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            _assetService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    [HttpGet]
    public IActionResult Assign(int id)
    {
        var asset = _assetService.GetById(id);
        if (asset == null) return NotFound();
        if (asset.Status == AssetStatus.Assigned)
        {
            TempData["Error"] = "Bu donanım zaten zimmette.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Asset = asset;
        ViewBag.PersonnelList = _personnelService.GetActive();
        return View(new AssetAssignInputModel { AssetId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Assign(AssetAssignInputModel input)
    {
        var asset = _assetService.GetById(input.AssetId);
        if (asset == null) return NotFound();
        try
        {
            _assetService.Assign(input.AssetId, input.PersonnelId, input.Notes, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Asset = asset;
            ViewBag.PersonnelList = _personnelService.GetActive();
            return View(input);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Asset = asset;
            ViewBag.PersonnelList = _personnelService.GetActive();
            return View(input);
        }
    }

    [HttpGet]
    public IActionResult Return(int id)
    {
        var assignment = _assetService.GetAssignmentById(id);
        if (assignment == null) return NotFound();
        if (assignment.ReturnedAt.HasValue)
        {
            TempData["Error"] = "Bu zimmet zaten iade edilmiş.";
            return RedirectToAction(nameof(Index));
        }
        var asset = _assetService.GetById(assignment.AssetId);
        var personnel = _personnelService.GetById(assignment.PersonnelId);
        ViewBag.Assignment = assignment;
        ViewBag.Asset = asset;
        ViewBag.PersonnelName = personnel != null ? $"{personnel.FirstName} {personnel.LastName}" : "—";
        return View(new AssetReturnInputModel { AssignmentId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Return(AssetReturnInputModel input)
    {
        try
        {
            _assetService.Return(input.AssignmentId, input.ReturnCondition, input.Notes);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Return), new { id = input.AssignmentId });
        }
    }
}
