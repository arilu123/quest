# WeAreHereNow

Моя записная книжка по проекту. Читать в начале каждой сессии перед стартом работы. Пользователь сюда не пишет — это для меня. Обновлять после каждого своего ответа/правки.

## Где сейчас

**После рефакторинга на kv-формат (F-format-strategy).** Пользователь принял стратегию «один запрос = один артефакт, формат kv `[KEY=value]`, slug строит приложение». Реализовано: `Services/KeyValue/{KvParser,KvFormatter,KvRecord}.cs`, `Services/Slug/Slugger.cs` (русский→PascalCase транслит). WorldHeader и WorldOverview переведены на kv. Smoke-тест прошёл (Mistral, 13.7 с на 3 варианта; WorldOverview сгенерил 5-абзацное описание, slug `World.VodyanoyLabirint` собрался автоматически). Прямой тест Qwen 3.6 с нашим system-промптом — 105 с, формат строго соблюдён, парсер из smoke-теста с ним справится.

**Следующий шаг:** F-1.3 — уровни локаций (WorldPartL1..Ln) каскадом, по одному за вызов (новая стратегия).

## Что сделано

| Спека | Что | Ключевые файлы |
|---|---|---|
| [specs/F-0-skeleton.md](specs/F-0-skeleton.md) | Каркас .NET 9 + EF Core 9 + Postgres + Ollama-клиент + диагностика | `quest.web/Program.cs`, `quest.db/QuestDbContext.cs`, `Services/Ollama/` |
| [specs/F-1.1-world-header.md](specs/F-1.1-world-header.md) | Шаг 1 ИНИТ Мира: форма + пресеты стиля + **тоны судьбы (0–2)** + **темп завязки** + **масштаб** + 3 варианта в стиле задней обложки книги + approve | `Features/WorldHeader/`, `Controllers/WorldsController.cs`, `Controllers/WorldsApiController.cs`, `Views/Worlds/{Start,Show}.cshtml` |
| [specs/F-1.2-world-overview.md](specs/F-1.2-world-overview.md) | Шаг 1 Сеттинга: автономная генерация общего описания [Мира] (артефакт Kind=World) после approve заголовка | `Features/WorldOverview/`, `Controllers/WorldsApiController.cs` |
| [specs/F-format-strategy.md](specs/F-format-strategy.md) | Сквозная: kv-формат вместо JSON-schema; один артефакт за вызов; slug строит приложение через Slugger | `Services/KeyValue/`, `Services/Slug/Slugger.cs`, рефакторинг WorldHeader+WorldOverview |

[specs/F-1-start.md](specs/F-1-start.md) — исходная спека пользователя, описывает все 3 этапа создания Мира в общих чертах. Я дроблю её на под-фичи (F-1.1, F-1.2, …).

## Дорожная карта (примерно)

- **F-1.2** Шаг 2: [Уровни Сеттинга] (1-5 уровней локаций). Открытый вопрос — генерация одной JSON со всем деревом или каскадом по уровням. Обсудить перед стартом.
- **F-1.3** Шаг 3: [Население] (группы, персонажи, скрытые). В F-1-start.md строки 36 и 39 — пользовательские `(?)`. Уточнить:
  - стр.36 «вводная часть для перечислений» — что именно генерировать первым шагом по населению;
  - стр.39 «скрытые персонажи/группы» — храним их сразу или генерируем only-in-time.
- **F-1.4** Шаг 4: [Предыстория] — простой текстовый артефакт.
- **F-1.5** Шаг 5: [Истории и Задачи] (план «главы»).
- **F-2** Наполнение Мира — литературные расширенные описания каждого блока. В F-1-start.md строка 23 `(?)` — пользователь сомневался в формулировке стиля («литературный исследователь даёт описание прочитав книгу»). Уточнить.
- **F-3** Дополнение Мира — корректировки во время игры.
- **F-X** Игровой цикл: ход ИИ ↔ ход [Игрока]. Это уже после инициализации.
- **F-Y** Список миров на главной, продолжить, удалить.
- **F-Z** Оценка артефактов моделью-критиком (упомянуто в F-1-start.md, не реализовано).

## Закреплённые решения (не пересматривать без причины)

- **.NET 9** через `global.json` (на машине стоят SDK 7/8/9/10; .NET 10 по умолчанию создаёт `.slnx` — нам пока не подходит).
- **Postgres**: Postgres.app 17.4, БД `quest`, креды `postgres/postgres`. `psql` CLI на машине НЕТ. Проверять связь через `GET /api/db/ping`.
- **Ollama**: 25 моделей установлено локально. В конфиге `appsettings.json` зафиксированы только генерация + embedding (image-генерация и SD-prompt-генератор отфильтрованы). `Ollama:Models` — упорядоченный массив объектов `{name, family, note}` (раньше был dict). Порядок в массиве = порядок в UI: сверху наиболее подходящие для русской креативной прозы (Qwen 3.6, Gemma 4 31B, Qwen 3.5), снизу — coding-модели и reasoning (QwQ). **Дефолт сейчас — `mistral-small3.2:24b-instruct-2506-q4_K_M`** (≈10 с, для F-1.1 достаточно качества и в 6 раз быстрее Qwen 3.6). Qwen 3.6 остаётся в дропдауне как «качественнее, но медленнее». Концепция: разные шаги — разные дефолт-модели; для сложных структурных шагов (дерево локаций, население) потом подключим тяжелее.
- **Хранение артефактов**: одна таблица `artifacts` с JSONB-payload и discriminator `Kind`. Каждая генерация = `Draft`, выбор = `Approved`+`Version`, старые → `Superseded`. Трассировка генерации (Prompt, RawResponse, Model, Tokens, DurationMs) хранится в записи.
- **Формат ответов моделей**: универсальный kv `[KEY=value]` с heredoc `KEY<<<…>>>` для многострочного. JSON-schema больше не используется (см. specs/F-format-strategy.md). Парсер: `KvParser.ParseSingle/ParseAll`. Принцип: **один запрос = один артефакт**, контекст уже созданных артефактов передаёт приложение. Исключение — WorldHeader (3 варианта в одном вызове для выбора).
- **Идентификатор артефакта строит приложение**, а не модель. `Slugger.ToPascal()` транслитерирует русское `NAME` в PascalCase-латиницу (`Хроники Зелёного Руина` → `KhronikiZelyonogoRuina`). Готовый `ArtifactId` = `{Kind}.{slug}` (или `{Kind}.{ParentSlug}.{LocalSlug}` для иерархии).
- **Язык MVP**: только русский. Мультиязычность отложена.
- **TypeScript**: отложен. Vanilla JS в Razor-страницах. Включим, когда фронта будет много.
- **Аутентификация**: нет, single-user.
- **Масштаб (Scale)** — 4-я ось выбора на форме старта. 5 вариантов: room/compact/regional/grand/cosmic (1–5 уровней локаций). Влияет на глубину и длительность квеста. Хранится на `World.Scale` (varchar 32), логика как у Pacing. Список — `Features/WorldHeader/WorldHeaderScales.cs`. Каждый вариант знает своё количество уровней (`Levels` int).
- **Тоны судьбы (Fates)** — сквозной атрибут [Истории], выбирается на форме старта **от 0 до 2** из 6 вариантов (adventure/moral/mystery/drama/survival/intimate). Хранится на `World.Fates` как JSON-массив в колонке `text` (раньше был `World.Fate` — одиночный `varchar(32)`). Миграция `FateToArray` — дропает `Fate`, добавляет `Fates`. В промпте два тона соединяются через «+», а system-промпт говорит «смешай их: ищи общее настроение на пересечении». DTO: `GenerateRequest.Fates` (`List<string>?`), `DraftPayload.Fates` / `ApprovedPayload.Fates` (`string[]?`). UI: toggle-чипы (клик = вкл/выкл), максимум 2 активных, невыбранные при 2 активных — приглушены + красный hover.
- **Темп завязки (Pacing)** — сквозной атрибут, 5 вариантов (action/inception/pre_storm/slow_build/from_afar) или пусто. Хранится на `World.Pacing`.
- **Аннотация ≠ Сеттинг**. Аннотация = лицо мира для игрока (обложка). Сеттинг = мозг мира для ИИ (внутреннее описание). Игрок сеттинг не видит.
- **Артефакты имеют структурированный ID**. `ArtifactId` (напр. `World.ShipNostromo`, `WorldPartL1.ShipNostromo.CapitanCockpit`) + `Name` (русское название). Slug генерирует ИИ.
- **ArtifactKind enum**: `World`=2, `WorldPartL1..L5`=3..7, `Population`=10, `Backstory`=11, `StoriesAndTasks`=12.
- **F-1.2 реализован**: после approve заголовка → автоматически в фоне генерируется артефакт `Kind=World` с общим описанием сеттинга. Промпт: `Features/WorldOverview/WorldOverviewPrompt.cs`. Сервис: `Features/WorldOverview/WorldOverviewService.cs`. Показ на Show.cshtml.
- **Bootstrap Icons**: в проект пока НЕ подключал — иконки не используются.

## Окружение / неочевидное

- Корень: `/Users/mac/work/quest-1`. Solution: `quest.sln`. Проекты: `quest.web` (MVC), `quest.db` (classlib).
- `dotnet-ef` стоит глобально в `/Users/mac/.dotnet/tools/dotnet-ef`. В новом shell может потребоваться `export PATH="$PATH:/Users/mac/.dotnet/tools"`.
- Миграции:
  ```
  dotnet ef migrations add <Name> --project quest.db --startup-project quest.web --output-dir Migrations
  dotnet ef database update          --project quest.db --startup-project quest.web
  ```
- Запуск (порт 5099, чистый профиль):
  ```
  cd quest.web && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5099 dotnet run --no-launch-profile
  ```
  **Важно**: `ASPNETCORE_ENVIRONMENT=Development` обязателен. Без него `MapStaticAssets` не подхватывает сгенерированный `quest.web.styles.css` (scoped CSS из `_Layout.cshtml.css`) и страницы падают на запросе стилей с 500.
- В `Program.cs` есть `UseHttpsRedirection()`. Для разработки гуляем через http://localhost:5099 — редирект не мешает, потому что launchSettings.json мы не используем (`--no-launch-profile`).
- API смоук:
  ```
  curl -X POST http://localhost:5099/api/worlds
  curl -X POST http://localhost:5099/api/worlds/<id>/header/generate -H 'Content-Type: application/json' -d '{"userHint":"...", "preset":"mythic", "fates":["mystery","survival"], "pacing":"inception", "scale":"regional", "model":"mistral-small3.2:24b-instruct-2506-q4_K_M"}'
  curl -X POST http://localhost:5099/api/worlds/<id>/header/<draftId>/approve -H 'Content-Type: application/json' -d '{"chosenIndex":0}'
  curl http://localhost:5099/api/worlds/<id>
  ```

## Известные подводные камни

- `SqlQueryRaw<string>` в EF Core 9 ждёт колонку с именем `Value` — алиасить через `AS "Value"`. Видно по DbController.
- **Ollama `format` с JSON-схемой ОТКАЗАЛИСЬ** (см. specs/F-format-strategy.md). Раньше Qwen 3.6 её игнорировал — теперь все модели общаются через kv-формат. Если будут добавляться новые типы артефактов — использовать `KvFormatter.FormatInstruction(fields)` в system-промпте и `KvParser.ParseSingle/ParseAll` для парсинга. Параметр `formatSchema` в `OllamaClient.GenerateAsync` пока оставлен опциональным, никем не используется.
- При смене набора локальных Ollama-моделей конфиг `Ollama:Models` нужно обновлять руками. Можно потом сделать авто-синхронизацию с `/api/tags`.

## Что в работе / ждёт фидбек

- **Рефакторинг на kv-формат — выполнен (2026-05-14).** Пользователь принял три ключевых решения:
  1. Все доступные модели нужны (Mistral хорош сейчас, но как он справится с большими промптами — неизвестно). Поэтому формат должен работать на всех.
  2. ID артефакта строит приложение, не модель.
  3. Один запрос = один артефакт. Иерархия (например, дерево локаций) собирается приложением, в каждом вызове модели — контекст уже созданных артефактов + просьба выдать ещё один.
  4. Исключение для WorldHeader: 3 варианта в одном вызове (UI-кейс с выбором).

  Реализовано: `Services/KeyValue/{KvParser,KvFormatter,KvRecord}.cs` + `Services/Slug/Slugger.cs`. WorldHeader и WorldOverview переписаны. JSON-schema больше не передаётся. Smoke-тесты прошли (Mistral 13.7 с, kv-парсер сработал, slug `World.VodyanoyLabirint` собрался автоматически). Прямой тест Qwen 3.6 с финальным system-промптом — 105 с, формат строго соблюдён.
- **F-1.3** Следующий шаг: уровни локаций (WorldPartL1 и далее). По новой стратегии — каждая локация = отдельный вызов, контекст = уже созданные соседи/родители. `ArtifactId` = `WorldPartL1.{WorldSlug}.{LocSlug}`. Число веток на уровне — открытый вопрос (обсудить).

## Следующий логический шаг

F-1.3: уровни локаций (WorldPartL1..Ln). По новой стратегии «один артефакт за вызов» — каждая локация = отдельный вызов модели с контекстом уже созданных. Открытые вопросы перед стартом:
- сколько локаций на уровне (фикс? зависит от Scale?);
- как решаем что уровень «достаточно заполнен» — модель сама даёт сигнал «больше не надо», или приложение задаёт квоту?
- параллелим соседние ноды или строго последовательно?

Потом F-1.4 (Население) → F-1.5 (Предыстория) → F-1.6 (Истории и Задачи) — всё автономно, всё по новой стратегии.

---

**Если пользователь начал новую сессию и попросил продолжить:** первым делом прочитать этот файл, затем `specs/`, при необходимости — последний коммит / `git status` для понимания неосознанных правок. **Не переспрашивать решения, которые здесь зафиксированы**, если пользователь явно не сказал «давай пересмотрим».
