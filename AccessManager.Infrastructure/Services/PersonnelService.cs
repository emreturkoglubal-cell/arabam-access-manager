using AccessManager.Application;
using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class PersonnelService : IPersonnelService
{
    private readonly IPersonnelRepository _repo;
    private readonly IPersonnelAccessRepository _accessRepo;

    public PersonnelService(IPersonnelRepository repo, IPersonnelAccessRepository accessRepo)
    {
        _repo = repo;
        _accessRepo = accessRepo;
    }

    public IReadOnlyList<Personnel> GetAll() => _repo.GetAll();

    public IReadOnlyList<Personnel> GetActive() => _repo.GetActive();

    public PagedResult<Personnel> GetPaged(int? departmentId, bool activeOnly, int page, int pageSize)
    {
        var (items, totalCount) = _repo.GetPaged(departmentId, activeOnly, page, pageSize);
        return new PagedResult<Personnel>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public Personnel? GetById(int id) => _repo.GetById(id);

    public Personnel? GetBySicilNo(string sicilNo) => _repo.GetBySicilNo(sicilNo);

    public IReadOnlyList<Personnel> GetByManagerId(int managerId) => _repo.GetByManagerId(managerId);

    public IReadOnlyList<Personnel> GetByDepartmentId(int departmentId) => _repo.GetByDepartmentId(departmentId);

    public (Personnel? personnel, List<PersonnelAccess> accesses) GetWithAccesses(int personnelId)
    {
        var p = _repo.GetById(personnelId);
        if (p == null) return (null, new List<PersonnelAccess>());
        var accesses = _accessRepo.GetByPersonnel(personnelId);
        return (p, accesses.ToList());
    }

    public Personnel Add(Personnel personnel)
    {
        ArgumentNullException.ThrowIfNull(personnel);
        personnel.Id = _repo.Insert(personnel);
        return personnel;
    }

    public void Update(Personnel personnel)
    {
        ArgumentNullException.ThrowIfNull(personnel);
        _repo.Update(personnel);
    }

    public void SetOffboarded(int personnelId, DateTime endDate)
    {
        _repo.SetOffboarded(personnelId, endDate);
    }

    public void UpdateRating(int personnelId, decimal? rating, string? managerComment)
    {
        _repo.UpdateRating(personnelId, rating, managerComment);
    }

    public IReadOnlyList<PersonnelNote> GetNotes(int personnelId) => _repo.GetNotes(personnelId);

    public void AddNote(int personnelId, string content, int? createdByUserId, string? createdByUserName)
    {
        if (_repo.GetById(personnelId) == null) return;
        var note = new PersonnelNote
        {
            PersonnelId = personnelId,
            Content = content?.Trim() ?? string.Empty,
            CreatedAt = SystemTime.Now,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName ?? "?"
        };
        _repo.AddNote(note);
    }
}
