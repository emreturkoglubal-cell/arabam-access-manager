using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class PersonnelReminderService : IPersonnelReminderService
{
    private readonly IPersonnelReminderRepository _repo;

    public PersonnelReminderService(IPersonnelReminderRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<PersonnelReminder> GetByPersonnelId(int personnelId) => _repo.GetByPersonnelId(personnelId);
    public PersonnelReminder? GetById(int id) => _repo.GetById(id);

    public PersonnelReminder Create(int personnelId, DateTime reminderDate, string description, int? createdByUserId, string? createdByUserName)
    {
        var reminder = new PersonnelReminder
        {
            PersonnelId = personnelId,
            ReminderDate = reminderDate.Date,
            Description = description?.Trim() ?? string.Empty,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName
        };
        reminder.Id = _repo.Insert(reminder);
        return reminder;
    }
}
