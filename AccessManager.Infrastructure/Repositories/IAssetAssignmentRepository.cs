using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IAssetAssignmentRepository
{
    IReadOnlyList<AssetAssignment> GetByAssetId(int assetId);
    IReadOnlyList<AssetAssignment> GetByPersonnelId(int personnelId);
    AssetAssignment? GetActiveByAssetId(int assetId);
    IReadOnlyList<AssetAssignment> GetActiveByAssetIds(IReadOnlyList<int> assetIds);
    AssetAssignment? GetById(int id);
    int Insert(AssetAssignment assignment);
    void SetReturned(int id, DateTime returnedAt, string? returnCondition, string? notes);
    void AddNote(AssetAssignmentNote note);
    IReadOnlyList<AssetAssignmentNote> GetNotesByAssignmentId(int assignmentId);
}
