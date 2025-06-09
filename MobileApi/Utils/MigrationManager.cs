using Database;

namespace MobileApi.Utils;

public class MigrationManager(IDatabase db, int version)
{
    /// <summary>
    /// Интерфейс работы с базой данных.
    /// </summary>
    private readonly IDatabase _db = db;

    /// <summary>
    /// Версия схемы базы данных.
    /// </summary>
    private readonly int _version = version;

    /// <summary>
    /// Выполняет миграцию базы данных до текущей версии.
    /// </summary>
    public async Task Migrate()
    {
        if (_version <= 1)
        {
            await MigrateV1();
        }
    }

    /// <summary>
    /// Выполняет миграцию схемы базы данных до версии 1.
    /// </summary>
    private async Task MigrateV1()
    {
        await _db.CreateTable(
            "users",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("username", typeof(string)) { CanNull = false },
            new DbParam("password", typeof(string)) { CanNull = false },
            new DbParam("pass_count", typeof(int)) { CanNull = false },
            new DbParam("count", typeof(int)) { CanNull = false },
            new DbParam("error_count", typeof(int)) { CanNull = false },
            new DbParam("count_tasks", typeof(int)) { CanNull = false },
            new DbParam("is_admin", typeof(bool)) { CanNull = false },
            new DbParam("token", typeof(string)) { CanNull = false }
        );

        await _db.CreateTable(
            "user_solution",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("user_id", typeof(int)) { CanNull = false },
            new DbForeignKey("user_id", "users", "id"),
            new DbParam("solution", typeof(string)) { CanNull = true }
        );

        await _db.CreateTable(
            "task_items",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("task_id", typeof(int)) { CanNull = true },
            new DbForeignKey("task_id", "tasks", "id"),
            new DbParam("question", typeof(string)) { CanNull = true },
            new DbParam("options", typeof(string)) { CanNull = true }
        );

        await _db.CreateTable(
            "tasks",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("title", typeof(string)) { CanNull = true },
            new DbParam("description", typeof(string)) { CanNull = true },
            new DbParam("full_description", typeof(string)) { CanNull = true },
            new DbParam("count_attempts", typeof(int)) { CanNull = true },
            new DbParam("user_ids", typeof(string)) { CanNull = true }
        );

        await _db.CreateTable(
            "task_user",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("user_id", typeof(int)),
            new DbForeignKey("user_id", "users", "id"),
            new DbParam("task_id", typeof(int)),
            new DbForeignKey("task_id", "tasks", "id")
        );

        await _db.CreateTable(
            "task_user_attempts",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("task_user_id", typeof(int)),
            new DbForeignKey("task_user_id", "task_user", "id"),
            new DbParam("count_attempts", typeof(int))
        );
    }
}