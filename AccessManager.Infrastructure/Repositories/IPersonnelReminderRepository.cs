using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IPersonnelReminderRepository
{
    IReadOnlyList<PersonnelReminder> GetByPersonnelId(int personnelId);
    IReadOnlyList<PersonnelReminder> GetDueReminders(DateTime date);
    PersonnelReminder? GetById(int id);
    int Insert(PersonnelReminder reminder);
    void MarkSent(int id, DateTime sentAt);
}
