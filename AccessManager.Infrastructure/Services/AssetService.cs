using AccessManager.Application;
using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepo;
    private readonly IAssetAssignmentRepository _assignmentRepo;
    private readonly IPersonnelRepository _personnelRepo;
    private readonly IAuditService _auditService;

    public AssetService(IAssetRepository assetRepo, IAssetAssignmentRepository assignmentRepo, IPersonnelRepository personnelRepo, IAuditService auditService)
    {
        _assetRepo = assetRepo;
        _assignmentRepo = assignmentRepo;
        _personnelRepo = personnelRepo;
        _auditService = auditService;
    }

    public IReadOnlyList<Asset> GetAll() => _assetRepo.GetAll();

    public PagedResult<Asset> GetPaged(AssetStatus? status, AssetType? type, int page, int pageSize)
    {
        var (items, totalCount) = _assetRepo.GetPaged(status, type, page, pageSize);
        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public IReadOnlyList<Asset> GetByStatus(AssetStatus status) => _assetRepo.GetByStatus(status);

    public IReadOnlyList<Asset> GetByType(AssetType type) => _assetRepo.GetByType(type);

    public Asset? GetById(int id) => _assetRepo.GetById(id);

    public IReadOnlyList<AssetAssignment> GetActiveAssignmentsByPersonnel(int personnelId)
    {
        return _assignmentRepo.GetByPersonnelId(personnelId).Where(x => x.ReturnedAt == null).OrderByDescending(x => x.AssignedAt).ToList();
    }

    public IReadOnlyList<AssetAssignment> GetAssignmentHistoryByAsset(int assetId) =>
        _assignmentRepo.GetByAssetId(assetId).OrderByDescending(x => x.AssignedAt).ToList();

    public AssetAssignment? GetActiveAssignmentForAsset(int assetId) => _assignmentRepo.GetActiveByAssetId(assetId);

    public IReadOnlyList<AssetAssignment> GetActiveAssignmentsForAssets(IReadOnlyList<int> assetIds) => _assignmentRepo.GetActiveByAssetIds(assetIds ?? Array.Empty<int>());

    public AssetAssignment? GetAssignmentById(int assignmentId) => _assignmentRepo.GetById(assignmentId);

    public IReadOnlyList<AssetAssignmentNote> GetNotesForAssignment(int assignmentId) => _assignmentRepo.GetNotesByAssignmentId(assignmentId);

    public void AddNoteToAssignment(int assignmentId, string content, int? createdByUserId, string? createdByUserName)
    {
        var assignment = _assignmentRepo.GetById(assignmentId);
        if (assignment == null) return;
        var note = new AssetAssignmentNote
        {
            AssetAssignmentId = assignmentId,
            Content = content?.Trim() ?? string.Empty,
            CreatedAt = SystemTime.Now,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName ?? "?"
        };
        _assignmentRepo.AddNote(note);
        _auditService.Log(AuditAction.AssetAssignmentNoteAdded, createdByUserId, createdByUserName ?? "?", "AssetAssignment", assignmentId.ToString(), content);
    }

    public Asset Create(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        asset.CreatedAt = SystemTime.Now;
        if (asset.Status == default) asset.Status = AssetStatus.Available;
        asset.Id = _assetRepo.Insert(asset);
        _auditService.Log(AuditAction.AssetCreated, null, "Sistem", "Asset", asset.Id.ToString(), asset.Name);
        return asset;
    }

    public void Update(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        _assetRepo.Update(asset);
        _auditService.Log(AuditAction.AssetUpdated, null, "Sistem", "Asset", asset.Id.ToString(), asset.Name);
    }

    public void Delete(int assetId)
    {
        var asset = _assetRepo.GetById(assetId);
        if (asset == null) return;
        var active = _assignmentRepo.GetActiveByAssetId(assetId);
        if (active != null)
            throw new InvalidOperationException("Zimmette olan donanım silinemez. Önce iade alın.");
        _assetRepo.Delete(assetId);
        _auditService.Log(AuditAction.AssetDeleted, null, "Sistem", "Asset", assetId.ToString(), asset.Name);
    }

    public AssetAssignment Assign(int assetId, int personnelId, string? notes, int? assignedByUserId, string? assignedByUserName)
    {
        var asset = _assetRepo.GetById(assetId);
        if (asset == null) throw new ArgumentException("Donanım bulunamadı.", nameof(assetId));
        if (asset.Status == AssetStatus.Assigned)
            throw new InvalidOperationException("Bu donanım zaten zimmette.");
        if (asset.Status == AssetStatus.Retired)
            throw new InvalidOperationException("Hurdaya çıkarılmış donanım zimmetlenemez.");

        var personnel = _personnelRepo.GetById(personnelId);
        if (personnel == null) throw new ArgumentException("Personel bulunamadı.", nameof(personnelId));

        var assignment = new AssetAssignment
        {
            AssetId = assetId,
            PersonnelId = personnelId,
            AssignedAt = SystemTime.Now,
            AssignedByUserId = assignedByUserId,
            AssignedByUserName = assignedByUserName ?? "—",
            Notes = notes
        };
        assignment.Id = _assignmentRepo.Insert(assignment);
        asset.Status = AssetStatus.Assigned;
        _assetRepo.Update(asset);
        _auditService.Log(AuditAction.AssetAssigned, assignedByUserId, assignedByUserName ?? "?", "Asset", asset.Id.ToString(),
            $"{asset.Name} → {personnel.FirstName} {personnel.LastName}");
        return assignment;
    }

    public void Return(int assignmentId, string? returnCondition, string? notes)
    {
        var assignment = _assignmentRepo.GetById(assignmentId);
        if (assignment == null) throw new ArgumentException("Zimmet kaydı bulunamadı.", nameof(assignmentId));
        if (assignment.ReturnedAt.HasValue)
            throw new InvalidOperationException("Bu zimmet zaten iade edilmiş.");

        var now = SystemTime.Now;
        _assignmentRepo.SetReturned(assignmentId, now, returnCondition, notes);

        var asset = _assetRepo.GetById(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Available;
            _assetRepo.Update(asset);
            var personnel = _personnelRepo.GetById(assignment.PersonnelId);
            _auditService.Log(AuditAction.AssetReturned, null, "Sistem", "Asset", asset.Id.ToString(),
                $"{asset.Name} iade: {(personnel != null ? personnel.FirstName + " " + personnel.LastName : "?")}");
        }
    }
}
