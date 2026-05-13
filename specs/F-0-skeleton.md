# Скелет решения (инфраструктура).

Базовый каркас проекта. Бизнес-логики ещё нет — только структура, подключение к Ollama и заготовка под Postgres/EF Core.

## Структура решения

```
quest-1/
├── global.json          — пин SDK на .NET 9 (9.0.203)
├── quest.sln
├── quest.web/           — ASP.NET Core MVC, .NET 9
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── HomeController.cs
│   │   └── OllamaController.cs      — диагностические эндпоинты
│   ├── Services/
│   │   └── Ollama/
│   │       ├── OllamaOptions.cs     — секция конфига `Ollama`
│   │       └── OllamaClient.cs      — HTTP-клиент к Ollama API
│   └── Views/
├── quest.db/            — class library, EF Core + Npgsql
└── specs/
```

`quest.web` ссылается на `quest.db`. `quest.db` пока пустой — модели и `DbContext` появятся при реализации первой фичи (см. [F-1-start.md](F-1-start.md)).

## Технологии (зафиксировано)

- **.NET 9** (`global.json` пинит SDK 9.0.203).
- **EF Core 9 + Npgsql.EntityFrameworkCore.PostgreSQL 9** в `quest.db`.
- **Ollama** — локальный HTTP API (`http://localhost:11434`).
- **Bootstrap + Bootstrap Icons** — пойдёт через шаблон MVC (CDN по умолчанию, заменим на libman/npm позже).
- **TypeScript** — отложен до первой фронтовой фичи.

## Подключение к Ollama

Конфиг в `appsettings.json`, секция `Ollama`:

- `BaseUrl` — адрес Ollama.
- `RequestTimeoutSeconds` — таймаут HTTP (по умолчанию 600 с, генерация может быть долгой).
- `DefaultModel` — модель для генерации текста по умолчанию (сейчас `gemma4:31b-it-q8_0`).
- `EmbeddingModel` — модель для эмбеддингов (`nomic-embed-text:latest`).
- `Models` — список доступных локально моделей, сгруппированных по семействам:
  - `Gemma4`, `Qwen3`, `QwQ`, `Mistral`, `Embeddings`.

Список зафиксирован вручную по результату `GET /api/tags`. Image-generation модели в список не включены. При смене набора локальных моделей конфиг нужно обновить.

`OllamaClient` (`quest.web/Services/Ollama/OllamaClient.cs`) умеет:
- `ListInstalledAsync()` → `GET /api/tags` — что реально установлено в Ollama;
- `GenerateAsync(model, prompt)` → `POST /api/generate` (без стриминга).

Регистрация через `AddHttpClient<OllamaClient>` в `Program.cs`.

## Диагностические эндпоинты

- `GET /api/ollama/configured` — что лежит в конфиге.
- `GET /api/ollama/installed` — что фактически установлено в Ollama сейчас.

Удобно сравнить, не разошёлся ли конфиг с реальностью.

## База данных

Postgres 17.4 (Postgres.app), БД `quest`, пользователь `postgres/postgres`. Строка подключения в `ConnectionStrings:Quest`.

В `quest.db` заведён пустой `QuestDbContext` — entity появятся при реализации первой фичи. Миграции не настроены, появятся вместе с первыми моделями.

Эндпоинт `GET /api/db/ping` отдаёт `canConnect` и версию сервера — удобно проверить, жива ли БД.

## Что НЕ сделано в этом шаге

- Нет моделей домена ([Мир], [Сеттинг], [Локация] и т.д.) и миграций.
- Нет UI квеста — только дефолтные страницы из шаблона MVC.
- Нет TypeScript-сборки.
- Нет интеграции с Ollama в реальном пользовательском сценарии — только диагностика.
