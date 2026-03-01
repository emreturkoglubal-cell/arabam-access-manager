using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IPersonnelReminderService
{
    IReadOnlyList<PersonnelReminder> GetByPersonnelId(int personnelId);
    PersonnelReminder? GetById(int id);
    PersonnelReminder Create(int personnelId, DateTime reminderDate, string description, int? createdByUserId, string? createdByUserName);
}
