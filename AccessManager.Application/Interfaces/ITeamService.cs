using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface ITeamService
{
    IReadOnlyList<Team> GetAll();
    IReadOnlyList<Team> GetByDepartmentId(int departmentId);
    Team? GetById(int id);
    Team Create(int departmentId, string name, string? code);
    void Update(Team team);
}
