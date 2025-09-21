# MobileApi — API для курсовой (тестирующая система)

## Сервис для:
- создания заданий (тестов) администратором (преподавателем);
- выдачи заданий пользователям;
- отправки решений и подсчёта статистики;
- просмотра результатов (для админа и пользователя).

<br>Базовый URL: `/mobile/api/v1/`

### Технологии:
- C# (ASP.NET Core 8 Web API)
- `Newtonsoft.Json`
- Docker (`compose.yaml`)
- Абстракция БД через `Database/IDatabase.cs` (`MySQL`)

### Структура
- `MobileApi` — Web API, контроллеры и модели
- `Database` — интерфейсы работы с БД
- `compose.yaml` — запуск в Docker
- `MobileApi/Dockerfile` — сборка контейнера
- `MobileApi/files/config.json` — конфигурация проекта и подключения к БД

### Конфигурация
Файл `MobileApi/files/config.json` (монтируется в контейнер в `/app/files`). *Не храните реальные пароли в репозитории.*

Пример:
```json
{
    "MainDbHost": "db.host.local",
    "MainDbPort": 3306,
    "MainDbName": "mobile",
    "MainDbUser": "user",
    "MainDbPassword": "password"
}
```

### Запуск
#### В Docker:
- подготовьте `MobileApi/files/config.json`;
- соберите и запустите: `docker compose up --build`

Сервис будет доступен на http://localhost:5010/mobileapi/v1/.

#### Локально (SDK .NET 8):
```bash
dotnet restore
dotnet run --project MobileApi
```
По умолчанию: http://localhost:5227/mobile/api/v1/.

### Аутентификация и роли
- `admin` — администратор (преподаватель), может создавать задания и просматривать все результаты;
- `user` — пользователь (студент), может получать задания, отправлять решения и просматривать свои результаты.
- `guest` — гость, может только получать задания.

### Эндпоинты
#### Публичные
- `GET /ping` — проверка живости;
- GET /tasks?userId={id} — список доступных заданий.
  - Без заголовков вернёт только общедоступные (for_all=1).
  - С заголовками userId и token добавит персональные.
#### Аутентификация
- POST `/login` — `{ "username": "...", "password": "..." } → { id, isAdmin, token, message }`
- POST `/register` — аналогично login, создаёт пользователя.
#### Пользовательские
- GET `/user/info` [Headers: `userId`, `token`] → `{ username, statPass, statError }`
- GET `/solutions/{solutionId}` [Headers] → `SolutionInfo`
- GET `/solutions/{solutionId}/tasks` [Headers] → `TaskItem[]` (правильные ответы скрыты)
- GET `/solution-tasks?userId={id}` [Header: `token`] — список задач с уже отправленными решениями (`solutions: List<bool>`)
- POST `/solutions/{solutionId}/submit` [Headers] — отправка ответов:
```json
{ "answers": [0, 2, 1] } // индексы выбранных вариантов на каждый вопрос
```
Возвращает `true`/`false`. Обновляет статистику и счётчик попыток.
#### Админские
- GET `/users` [Headers] — список пользователей.
- POST `/tasks/create` [Headers] — создать задание:
```json
{
    "solutionInfo": {
        "title": "Тест 1",
        "description": "Коротко",
        "fullDescription": "Подробно"
    },
    "tasks": [
        {
            "question": "2+2?",
            "answerOptions": [
                { "text": "3", "isCorrect": false },
                { "text": "4", "isCorrect": true }
            ]
        }
    ],
    "userList": [
        { "id": 5, "username": "ivan" }
    ]
}
```
Если `userList` пуст, задание становится общедоступным (`for_all=1`). Админы автоматически добавляются в назначение.
- DELETE `/solutions/{solutionId}/delete` [Headers] — полное удаление задания и связанных данных.
