using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface ITeamRepository
{
    IReadOnlyList<Team> GetAll();
    IReadOnlyList<Team> GetByDepartmentId(int departmentId);
    Team? GetById(int id);
    int Insert(Team team);
    void Update(Team team);
}
