using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class PersonnelReminderRepository : IPersonnelReminderRepository
{
    private readonly string _connectionString;

    public PersonnelReminderRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<PersonnelReminder> GetByPersonnelId(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, reminder_date AS ReminderDate, description AS Description, sent_at AS SentAt, created_at AS CreatedAt, created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName
            FROM personnel_reminders WHERE personnel_id = @PersonnelId ORDER BY reminder_date DESC";
        return conn.Query<PersonnelReminder>(sql, new { PersonnelId = personnelId }).ToList();
    }

    public IReadOnlyList<PersonnelReminder> GetDueReminders(DateTime date)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, reminder_date AS ReminderDate, description AS Description, sent_at AS SentAt, created_at AS CreatedAt, created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName
            FROM personnel_reminders WHERE reminder_date = @Date AND sent_at IS NULL ORDER BY id";
        return conn.Query<PersonnelReminder>(sql, new { Date = date.Date }).ToList();
    }

    public PersonnelReminder? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, reminder_date AS ReminderDate, description AS Description, sent_at AS SentAt, created_at AS CreatedAt, created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName
            FROM personnel_reminders WHERE id = @Id";
        return conn.QuerySingleOrDefault<PersonnelReminder>(sql, new { Id = id });
    }

    public int Insert(PersonnelReminder reminder)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO personnel_reminders (personnel_id, reminder_date, description, created_by_user_id, created_by_user_name)
            VALUES (@PersonnelId, @ReminderDate, @Description, @CreatedByUserId, @CreatedByUserName) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { reminder.PersonnelId, reminder.ReminderDate, reminder.Description, reminder.CreatedByUserId, reminder.CreatedByUserName });
    }

    public void MarkSent(int id, DateTime sentAt)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE personnel_reminders SET sent_at = @SentAt WHERE id = @Id", new { Id = id, SentAt = sentAt });
    }
}
