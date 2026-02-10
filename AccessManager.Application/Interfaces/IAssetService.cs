using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IAssetService
{
    IReadOnlyList<Asset> GetAll();
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    IReadOnlyList<Asset> GetByType(AssetType type);
    Asset? GetById(int id);
    IReadOnlyList<AssetAssignment> GetActiveAssignmentsByPersonnel(int personnelId);
    IReadOnlyList<AssetAssignment> GetAssignmentHistoryByAsset(int assetId);
    AssetAssignment? GetActiveAssignmentForAsset(int assetId);
    AssetAssignment? GetAssignmentById(int assignmentId);
    IReadOnlyList<AssetAssignmentNote> GetNotesForAssignment(int assignmentId);
    void AddNoteToAssignment(int assignmentId, string content, int? createdByUserId, string? createdByUserName);

    Asset Create(Asset asset);
    void Update(Asset asset);
    void Delete(int assetId);

    AssetAssignment Assign(int assetId, int personnelId, string? notes, int? assignedByUserId, string? assignedByUserName);
    void Return(int assignmentId, string? returnCondition, string? notes);
}
