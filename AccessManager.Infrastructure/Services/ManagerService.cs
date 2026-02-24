using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class ManagerService : IManagerService
{
    private readonly IManagerRepository _managerRepo;
    private readonly IPersonnelRepository _personnelRepo;

    public ManagerService(IManagerRepository managerRepo, IPersonnelRepository personnelRepo)
    {
        _managerRepo = managerRepo;
        _personnelRepo = personnelRepo;
    }

    public IReadOnlyList<Personnel> GetLeafManagerPersonnel()
    {
        var leafManagers = _managerRepo.GetLeafManagers();
        if (leafManagers.Count == 0)
            return new List<Personnel>();
        var ids = leafManagers.Select(m => m.PersonnelId).Distinct().ToList();
        return _personnelRepo.GetByIds(ids);
    }

    public IReadOnlyList<Personnel> GetActiveManagerPersonnel()
    {
        var activeManagers = _managerRepo.GetActiveManagers();
        if (activeManagers.Count == 0)
            return new List<Personnel>();
        var personnelList = _personnelRepo.GetByIds(activeManagers.Select(m => m.PersonnelId).ToList());
        var byId = personnelList.ToDictionary(p => p.Id);
        return activeManagers.Select(m => byId[m.PersonnelId]).ToList();
    }

    public short? GetManagerLevelByPersonnelId(int personnelId)
    {
        var m = _managerRepo.GetByPersonnelId(personnelId);
        return m?.Level;
    }

    public void UpdatePersonnelManager(int personnelId, int? managerPersonnelId, short level)
    {
        var personnel = _personnelRepo.GetById(personnelId);
        if (personnel == null) return;
        personnel.ManagerId = managerPersonnelId;
        _personnelRepo.Update(personnel);

        if (managerPersonnelId.HasValue)
        {
            if (level < 1 || level > 4) level = 1;
            var existing = _managerRepo.GetByPersonnelId(managerPersonnelId.Value);
            if (existing != null)
            {
                existing.Level = level;
                _managerRepo.Update(existing);
            }
            else
            {
                _managerRepo.Insert(new Manager
                {
                    PersonnelId = managerPersonnelId.Value,
                    Level = level,
                    ParentManagerId = null,
                    IsActive = true
                });
            }
        }
    }

    public void SetPersonAsManager(int personnelId, bool isManager, int? managerPersonnelId = null)
    {
        var existing = _managerRepo.GetByPersonnelId(personnelId);
        if (isManager)
        {
            int? parentManagerId = null;
            short level = 1;
            if (managerPersonnelId.HasValue)
            {
                var parentManager = _managerRepo.GetByPersonnelId(managerPersonnelId.Value);
                if (parentManager != null)
                {
                    parentManagerId = parentManager.Id;
                    level = (short)Math.Min(4, parentManager.Level + 1);
                }
            }

            if (existing != null)
            {
                existing.ParentManagerId = parentManagerId;
                existing.Level = level;
                existing.IsActive = true;
                _managerRepo.Update(existing);
            }
            else
            {
                _managerRepo.Insert(new Manager
                {
                    PersonnelId = personnelId,
                    Level = level,
                    ParentManagerId = parentManagerId,
                    IsActive = true
                });
            }
        }
        else if (existing != null)
        {
            existing.IsActive = false;
            _managerRepo.Update(existing);
        }
    }

    public bool IsPersonManagerActive(int personnelId)
    {
        var m = _managerRepo.GetByPersonnelId(personnelId);
        return m != null && m.IsActive;
    }
}
