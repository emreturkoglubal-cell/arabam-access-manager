using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IAssetService
{
    IReadOnlyList<Asset> GetAll();
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    IReadOnlyList<Asset> GetByType(AssetType type);
    Asset? GetById(Guid id);
    IReadOnlyList<AssetAssignment> GetActiveAssignmentsByPersonnel(Guid personnelId);
    IReadOnlyList<AssetAssignment> GetAssignmentHistoryByAsset(Guid assetId);
    AssetAssignment? GetActiveAssignmentForAsset(Guid assetId);
    AssetAssignment? GetAssignmentById(Guid assignmentId);
    IReadOnlyList<AssetAssignmentNote> GetNotesForAssignment(Guid assignmentId);
    void AddNoteToAssignment(Guid assignmentId, string content, Guid? createdByUserId, string? createdByUserName);

    Asset Create(Asset asset);
    void Update(Asset asset);
    void Delete(Guid assetId);

    AssetAssignment Assign(Guid assetId, Guid personnelId, string? notes, Guid? assignedByUserId, string? assignedByUserName);
    void Return(Guid assignmentId, string? returnCondition, string? notes);
}
