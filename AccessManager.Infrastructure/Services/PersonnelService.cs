using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class PersonnelService : IPersonnelService
{
    private readonly MockDataStore _store;

    public PersonnelService(MockDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Personnel> GetAll() => _store.Personnel.ToList();

    public IReadOnlyList<Personnel> GetActive() =>
        _store.Personnel.Where(p => p.Status == PersonnelStatus.Active).ToList();

    public Personnel? GetById(Guid id) => _store.Personnel.FirstOrDefault(p => p.Id == id);

    public Personnel? GetBySicilNo(string sicilNo) =>
        _store.Personnel.FirstOrDefault(p => string.Equals(p.SicilNo, sicilNo, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Personnel> GetByManagerId(Guid managerId) =>
        _store.Personnel.Where(p => p.ManagerId == managerId).ToList();

    public IReadOnlyList<Personnel> GetByDepartmentId(Guid departmentId) =>
        _store.Personnel.Where(p => p.DepartmentId == departmentId).ToList();

    public (Personnel? personnel, List<PersonnelAccess> accesses) GetWithAccesses(Guid personnelId)
    {
        var p = GetById(personnelId);
        if (p == null) return (null, new List<PersonnelAccess>());
        // Faz 1: Tüm yetkiler görünsün (aktif + pasif)
        var accesses = _store.PersonnelAccesses.Where(a => a.PersonnelId == personnelId).ToList();
        return (p, accesses);
    }

    public Personnel Add(Personnel personnel)
    {
        ArgumentNullException.ThrowIfNull(personnel);
        personnel.Id = Guid.NewGuid();
        _store.Personnel.Add(personnel);
        return personnel;
    }

    public void Update(Personnel personnel)
    {
        ArgumentNullException.ThrowIfNull(personnel);
        var idx = _store.Personnel.FindIndex(p => p.Id == personnel.Id);
        if (idx >= 0) _store.Personnel[idx] = personnel;
    }

    public void SetOffboarded(Guid personnelId, DateTime endDate)
    {
        var p = GetById(personnelId);
        if (p == null) return;
        p.EndDate = endDate;
        p.Status = PersonnelStatus.Offboarded;
        foreach (var a in _store.PersonnelAccesses.Where(x => x.PersonnelId == personnelId))
            a.IsActive = false;
    }

    public void UpdateRating(Guid personnelId, decimal? rating, string? managerComment)
    {
        var p = GetById(personnelId);
        if (p == null) return;
        p.Rating = rating;
        p.ManagerComment = string.IsNullOrWhiteSpace(managerComment) ? null : managerComment.Trim();
    }

    public IReadOnlyList<PersonnelNote> GetNotes(Guid personnelId)
    {
        return _store.PersonnelNotes
            .Where(n => n.PersonnelId == personnelId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
    }

    public void AddNote(Guid personnelId, string content, Guid? createdByUserId, string? createdByUserName)
    {
        if (GetById(personnelId) == null) return;
        var note = new PersonnelNote
        {
            Id = Guid.NewGuid(),
            PersonnelId = personnelId,
            Content = content?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName ?? "?"
        };
        _store.PersonnelNotes.Add(note);
    }
}
