using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _repo;

    public TeamService(ITeamRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<Team> GetAll() => _repo.GetAll();
    public IReadOnlyList<Team> GetByDepartmentId(int departmentId) => _repo.GetByDepartmentId(departmentId);
    public Team? GetById(int id) => _repo.GetById(id);

    public Team Create(int departmentId, string name, string? code)
    {
        var team = new Team { DepartmentId = departmentId, Name = name.Trim(), Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim() };
        team.Id = _repo.Insert(team);
        return team;
    }

    public void Update(Team team)
    {
        team.Name = team.Name?.Trim() ?? string.Empty;
        team.Code = string.IsNullOrWhiteSpace(team.Code) ? null : team.Code.Trim();
        _repo.Update(team);
    }
}
