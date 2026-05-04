using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IPositionTitleTemplateRepository
{
    IReadOnlyList<PositionTitleTemplate> GetAll();
    int Insert(PositionTitleTemplate row);
    void Delete(int id);
    /// <summary>En spesifik eşleşen şablon başlığı; yoksa null.</summary>
    string? ResolveTitle(int? departmentId, int? teamId, string? seniorityLevel);
}
