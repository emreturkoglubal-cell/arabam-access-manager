using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly MockDataStore _store;
    private readonly IAuditService _auditService;

    public AssetService(MockDataStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public IReadOnlyList<Asset> GetAll() => _store.Assets.ToList();

    public IReadOnlyList<Asset> GetByStatus(AssetStatus status) =>
        _store.Assets.Where(a => a.Status == status).ToList();

    public IReadOnlyList<Asset> GetByType(AssetType type) =>
        _store.Assets.Where(a => a.AssetType == type).ToList();

    public Asset? GetById(Guid id) => _store.Assets.FirstOrDefault(a => a.Id == id);

    public IReadOnlyList<AssetAssignment> GetActiveAssignmentsByPersonnel(Guid personnelId) =>
        _store.AssetAssignments
            .Where(x => x.PersonnelId == personnelId && x.ReturnedAt == null)
            .OrderByDescending(x => x.AssignedAt)
            .ToList();

    public IReadOnlyList<AssetAssignment> GetAssignmentHistoryByAsset(Guid assetId) =>
        _store.AssetAssignments
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.AssignedAt)
            .ToList();

    public AssetAssignment? GetActiveAssignmentForAsset(Guid assetId) =>
        _store.AssetAssignments.FirstOrDefault(x => x.AssetId == assetId && x.ReturnedAt == null);

    public AssetAssignment? GetAssignmentById(Guid assignmentId) =>
        _store.AssetAssignments.FirstOrDefault(x => x.Id == assignmentId);

    public IReadOnlyList<AssetAssignmentNote> GetNotesForAssignment(Guid assignmentId) =>
        _store.AssetAssignmentNotes
            .Where(n => n.AssetAssignmentId == assignmentId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

    public void AddNoteToAssignment(Guid assignmentId, string content, Guid? createdByUserId, string? createdByUserName)
    {
        var assignment = GetAssignmentById(assignmentId);
        if (assignment == null) return;
        var note = new AssetAssignmentNote
        {
            Id = Guid.NewGuid(),
            AssetAssignmentId = assignmentId,
            Content = content?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName ?? "?"
        };
        _store.AssetAssignmentNotes.Add(note);
    }

    public Asset Create(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        asset.Id = Guid.NewGuid();
        asset.CreatedAt = DateTime.UtcNow;
        if (asset.Status == default) asset.Status = AssetStatus.Available;
        _store.Assets.Add(asset);
        _auditService.Log(AuditAction.AssetCreated, null, "Sistem", "Asset", asset.Id.ToString(), asset.Name);
        return asset;
    }

    public void Update(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var idx = _store.Assets.FindIndex(a => a.Id == asset.Id);
        if (idx >= 0)
        {
            _store.Assets[idx] = asset;
            _auditService.Log(AuditAction.AssetUpdated, null, "Sistem", "Asset", asset.Id.ToString(), asset.Name);
        }
    }

    public void Delete(Guid assetId)
    {
        var asset = GetById(assetId);
        if (asset == null) return;
        var active = GetActiveAssignmentForAsset(assetId);
        if (active != null)
            throw new InvalidOperationException("Zimmette olan donanım silinemez. Önce iade alın.");
        _store.Assets.RemoveAll(a => a.Id == assetId);
        _auditService.Log(AuditAction.AssetDeleted, null, "Sistem", "Asset", assetId.ToString(), asset.Name);
    }

    public AssetAssignment Assign(Guid assetId, Guid personnelId, string? notes, Guid? assignedByUserId, string? assignedByUserName)
    {
        var asset = GetById(assetId);
        if (asset == null) throw new ArgumentException("Donanım bulunamadı.", nameof(assetId));
        if (asset.Status == AssetStatus.Assigned)
            throw new InvalidOperationException("Bu donanım zaten zimmette.");
        if (asset.Status == AssetStatus.Retired)
            throw new InvalidOperationException("Hurdaya çıkarılmış donanım zimmetlenemez.");

        var personnel = _store.Personnel.FirstOrDefault(p => p.Id == personnelId);
        if (personnel == null) throw new ArgumentException("Personel bulunamadı.", nameof(personnelId));

        var assignment = new AssetAssignment
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            PersonnelId = personnelId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId,
            AssignedByUserName = assignedByUserName ?? "—",
            Notes = notes
        };
        _store.AssetAssignments.Add(assignment);
        asset.Status = AssetStatus.Assigned;
        _auditService.Log(AuditAction.AssetAssigned, assignedByUserId, assignedByUserName ?? "?", "Asset", asset.Id.ToString(),
            $"{asset.Name} → {personnel.FirstName} {personnel.LastName}");
        return assignment;
    }

    public void Return(Guid assignmentId, string? returnCondition, string? notes)
    {
        var assignment = _store.AssetAssignments.FirstOrDefault(x => x.Id == assignmentId);
        if (assignment == null) throw new ArgumentException("Zimmet kaydı bulunamadı.", nameof(assignmentId));
        if (assignment.ReturnedAt.HasValue)
            throw new InvalidOperationException("Bu zimmet zaten iade edilmiş.");

        assignment.ReturnedAt = DateTime.UtcNow;
        assignment.ReturnCondition = returnCondition;
        if (notes != null) assignment.Notes = (assignment.Notes ?? "") + (string.IsNullOrEmpty(assignment.Notes) ? "" : " | ") + "İade: " + notes;

        var asset = GetById(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Available;
            var personnel = _store.Personnel.FirstOrDefault(p => p.Id == assignment.PersonnelId);
            _auditService.Log(AuditAction.AssetReturned, null, "Sistem", "Asset", asset.Id.ToString(),
                $"{asset.Name} iade: {(personnel != null ? personnel.FirstName + " " + personnel.LastName : "?")}");
        }
    }
}
