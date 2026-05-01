# 🌐 NetworkSignalCore

> **SignalR-based networking framework для Unity** — мощная альтернатива Mirror, построенная на ASP.NET Core SignalR с поддержкой JWT-аутентификации, матчмейкинга, комнат, анти-чита и автоматической синхронизации состояния.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![netstandard](https://img.shields.io/badge/netstandard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Unity](https://img.shields.io/badge/Unity-2021%2B-000000?logo=unity)](https://unity.com/)
[![SignalR](https://img.shields.io/badge/ASP.NET_Core-SignalR-512BD4)](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## 📋 Содержание

- [Что такое NetworkSignalCore?](#-что-такое-networksignalcore)
- [Ключевые возможности](#-ключевые-возможности)
- [Требования](#-требования)
- [Быстрый старт](#-быстрый-старт)
- [Установка](#-установка)
- [Структура проектов](#-структура-проектов)
- [Сервер: подробное руководство](#-сервер-подробное-руководство)
- [Anti-cheat: подробное руководство](#-anti-cheat-подробное-руководство)
- [Клиент: подробное руководство](#-клиент-подробное-руководство)
- [Unity: подробное руководство](#-unity-подробное-руководство)
- [Матчмейкинг: полное руководство](#-матчмейкинг-полное-руководство)
- [Комнаты и пати: полное руководство](#-комнаты-и-пати-полное-руководство)
- [Расширенные сценарии](#-расширенные-сценарии)
- [Безопасность](#-безопасность)
- [Справочник API](#-справочник-api)
- [FAQ](#-faq)

---

## 🔍 Что такое NetworkSignalCore?

**NetworkSignalCore** — это полноценный networking framework для разработки многопользовательских игр на Unity. В отличие от Mirror (который работает поверх низкоуровневых транспортов), NetworkSignalCore строится на **ASP.NET Core SignalR** — надёжной, хорошо задокументированной технологии от Microsoft, которая обеспечивает WebSocket-соединение с автоматическим fallback на Server-Sent Events и Long Polling.

### Почему SignalR вместо Mirror?

| Критерий | Mirror | NetworkSignalCore |
|---|---|---|
| Транспорт | UDP/TCP/WebSocket | WebSocket (SignalR) |
| Сервер | Dedicated / Headless Unity | ASP.NET Core (любой хостинг) |
| Аутентификация | Нет (или самописная) | JWT из коробки |
| Масштабируемость | Ограничена | SignalR Backplane (Redis) |
| Матчмейкинг | Нет | Встроенный |
| Anti-cheat | Нет | Встроенный pipeline |
| DI-контейнер | Нет | ASP.NET Core DI |
| Развёртывание | Сложно | Docker / Azure / любой хостинг |

---

## ✨ Ключевые возможности

- **🔐 JWT-аутентификация** — встроенная система токенов с поддержкой кастомных провайдеров
- **🏠 Система комнат** — создание, управление, приватные комнаты с паролем
- **🎯 Матчмейкинг** — автоматический подбор игроков по ELO, региону, режиму игры
- **🛡️ Anti-cheat** — валидация действий через атрибуты и кастомные валидаторы
- **🔄 SyncVar** — автоматическая синхронизация переменных между сервером и клиентами
- **📡 RPC** — `[ServerRpc]` и `[ClientRpc]` атрибуты в стиле Mirror
- **🎮 Unity-совместимость** — готовые MonoBehaviour компоненты
- **⚡ Rate Limiting** — защита от спама запросов
- **🔁 Авто-реконнект** — автоматическое восстановление соединения
- **💉 Dependency Injection** — полная интеграция с ASP.NET Core DI

---

## 📌 Требования

### Сервер
- **.NET 8.0** или новее
- ASP.NET Core 8.0
- NuGet-пакеты: `NetworkSignalCore.Server`

### Клиент (Unity)
- **Unity 2021.3 LTS** или новее
- **.NET Standard 2.1** (настройка в Player Settings → Api Compatibility Level)
- DLL: `NetworkSignalCore.Core.dll`, `NetworkSignalCore.Client.dll`

### Общие зависимости
- `Microsoft.AspNetCore.SignalR.Client` ≥ 8.0
- `System.Text.Json` ≥ 8.0

---

## 🚀 Быстрый старт

Этот раздел покажет, как поднять сервер и подключить Unity-клиент **за 5 минут**.

### Шаг 1 — Создаём ASP.NET Core проект

```bash
dotnet new web -n MyGame.Server
cd MyGame.Server
dotnet add package NetworkSignalCore.Server
```

### Шаг 2 — Настраиваем Program.cs

```csharp
// Program.cs
using NetworkSignalCore.Server.Extensions;
using MyGame.Server.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем все сервисы NetworkSignalCore
builder.Services.AddNetworkSignalCore(
    configureServer: opt =>
    {
        opt.MaxPlayersPerRoom = 4;          // максимум 4 игрока в комнате
        opt.SyncTickRateMs = 50;            // синхронизация 20 раз в секунду
        opt.RequireAuthForAllRpcs = true;   // все RPC требуют аутентификации
    },
    configureAuth: opt =>
    {
        // ВАЖНО: замените на надёжный секрет в production!
        opt.SecretKey = "my-super-secret-key-min-32-chars!!";
        opt.Issuer = "MyGame";
        opt.Audience = "Players";
        opt.TokenLifetime = TimeSpan.FromHours(8);
    }
);

// Регистрируем наш игровой контроллер
builder.Services.AddNetworkController<GameController>();

var app = builder.Build();

// Подключаем NetworkSignalCore middleware и хаб
app.UseNetworkSignalCore("/network");

app.Run();
```

### Шаг 3 — Создаём игровой контроллер

```csharp
// Controllers/GameController.cs
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Server.Rpc;

public class GameController : NetworkController
{
    // [ServerRpc] — метод вызывается клиентом на сервере
    [ServerRpc(RequireAuth = true)]
    public async Task Move(MovePayload payload)
    {
        // Caller — это PlayerInfo текущего игрока
        Console.WriteLine($"{Caller?.DisplayName} перемещается в {payload.Position}");

        // Отправляем обновление всем в комнате
        await SendToRoomAsync("PlayerMoved", new
        {
            PlayerId = Caller?.PlayerId,
            payload.Position
        });
    }

    // [ClientRpc] — метод вызывается сервером на клиенте
    [ClientRpc]
    public void OnGameStarted(GameStartedData data) { /* определяется на клиенте */ }
}

// Вспомогательные классы для данных
public record MovePayload(float X, float Y, float Z)
{
    public string Position => $"({X}, {Y}, {Z})";
}

public record GameStartedData(string MapName, int PlayerCount);
```

### Шаг 4 — Подключаем Unity-клиент

```csharp
// Assets/Scripts/GameNetworkManager.cs
using UnityEngine;
using NetworkSignalCore.Client;
using NetworkSignalCore.Core.Messages;

public class GameNetworkManager : MonoBehaviour
{
    private NetworkClient _client;

    private async void Start()
    {
        // Создаём клиент с настройками
        _client = new NetworkClient(opt =>
        {
            opt.ServerUrl = "http://localhost:5000/network";
            opt.AutoReconnect = true;
            opt.MaxReconnectAttempts = 5;
        });

        // Подписываемся на события
        _client.OnConnected += () => Debug.Log("Подключено!");
        _client.OnDisconnected += ex => Debug.LogWarning($"Отключено: {ex?.Message}");

        // Подключаемся
        await _client.ConnectAsync();

        // Аутентифицируемся (токен получается от вашего бэкенда)
        bool ok = await _client.AuthenticateAsync("player-jwt-token");
        if (ok) Debug.Log($"Авторизован как: {_client.LocalPlayer?.DisplayName}");

        // Создаём комнату
        await _client.Rooms.CreateRoomAsync(new CreateRoomRequest
        {
            Name = "Тестовая комната",
            MaxPlayers = 4,
            IsPrivate = false,
            GameMode = "deathmatch"
        });
    }

    private async void Update()
    {
        // Отправляем позицию на сервер (через NetworkBehaviour в реальном проекте)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Используйте NetworkBehaviourBase для организованного кода
        }
    }

    private void OnDestroy()
    {
        _client?.DisconnectAsync().GetAwaiter().GetResult();
        _client?.Dispose();
    }
}
```

### Шаг 5 — Запускаем

```bash
cd MyGame.Server
dotnet run
# Сервер запущен на http://localhost:5000
# SignalR Hub доступен по адресу ws://localhost:5000/network
```

---

## 📦 Установка

### Сервер (NuGet)

```bash
# Основной пакет для сервера
dotnet add package NetworkSignalCore.Server

# Только ядро (атрибуты, интерфейсы, модели)
dotnet add package NetworkSignalCore.Core
```

### Клиент (NuGet — для консольных/Blazor приложений)

```bash
dotnet add package NetworkSignalCore.Client
```

### Unity (DLL)

1. Скачайте релиз с GitHub Releases
2. Скопируйте в папку `Assets/Plugins/NetworkSignalCore/`:
   - `NetworkSignalCore.Core.dll`
   - `NetworkSignalCore.Client.dll`
   - Зависимости: `Microsoft.AspNetCore.SignalR.Client.dll` и её транзитивные зависимости
3. Скопируйте Unity-компоненты из папки `unity/` в `Assets/Scripts/NetworkSignalCore/`:
   - `NetworkManager.cs`
   - `NetworkBehaviour.cs`
   - `NetworkTransform.cs`
   - `NetworkIdentity.cs`
4. Установите в **Player Settings → Other Settings → Api Compatibility Level**: `.NET Standard 2.1`

### Структура папок в Unity после установки

```
Assets/
├── Plugins/
│   └── NetworkSignalCore/
│       ├── NetworkSignalCore.Core.dll
│       ├── NetworkSignalCore.Client.dll
│       └── (зависимости SignalR)
└── Scripts/
    └── NetworkSignalCore/
        ├── NetworkManager.cs
        ├── NetworkBehaviour.cs
        ├── NetworkTransform.cs
        └── NetworkIdentity.cs
```

---

## 🏗️ Структура проектов

```
NetworkSignalCore/
├── NetworkSignalCore.Core/          # netstandard2.1 — общее ядро
│   ├── Abstractions/                # Интерфейсы: IAuthProvider, IRoomManager, IMatchmaker...
│   ├── Attributes/                  # [ServerRpc], [ClientRpc], [SyncVar], [Validated]
│   ├── Models/                      # PlayerInfo, RoomInfo, MatchRequest...
│   ├── Messages/                    # CreateRoomRequest, JoinRoomRequest...
│   └── Sync/                        # SyncVar<T>
│
├── NetworkSignalCore.Server/        # net8.0 — серверная библиотека
│   ├── Hubs/                        # SignalR NetworkHub
│   ├── Rpc/                         # NetworkController, диспетчер RPC
│   ├── Services/                    # SessionManager, RoomManager, Matchmaker...
│   ├── Auth/                        # JWT реализация IAuthProvider
│   ├── AntiCheat/                   # Движок валидации
│   ├── Options/                     # NetworkServerOptions, NetworkAuthOptions
│   └── Extensions/                  # AddNetworkSignalCore(), UseNetworkSignalCore()
│
├── NetworkSignalCore.Client/        # netstandard2.1 — клиентская библиотека
│   ├── NetworkClient.cs             # Главный класс клиента
│   ├── Rpc/                         # NetworkBehaviourBase
│   ├── Managers/                    # ClientRoomManager, ClientMatchmakingManager, ClientSyncManager
│   └── Options/                     # NetworkClientOptions
│
└── unity/                           # Unity MonoBehaviour компоненты (исходники)
    ├── NetworkManager.cs
    ├── NetworkBehaviour.cs
    ├── NetworkTransform.cs
    └── NetworkIdentity.cs
```

---

## 🖥️ Сервер: подробное руководство

### Настройка и запуск

#### `AddNetworkSignalCore` — регистрация сервисов

```csharp
// Program.cs
builder.Services.AddNetworkSignalCore(
    configureServer: opt =>
    {
        opt.MaxPlayersPerRoom = 16;          // максимум игроков в одной комнате
        opt.MatchmakingIntervalMs = 2000;    // интервал матчмейкинга (мс)
        opt.SyncTickRateMs = 50;             // тик синхронизации = 20 Hz
        opt.RateLimitCallsPerSecond = 30;    // лимит RPC вызовов в секунду
        opt.RequireAuthForAllRpcs = true;    // все RPC требуют JWT
        opt.MatchAcceptTimeout = TimeSpan.FromSeconds(15); // таймаут принятия матча
        opt.MaxRooms = 100;                  // максимум активных комнат
        opt.EloDifferenceThreshold = 400;    // максимальная разница ELO для матча
    },
    configureAuth: opt =>
    {
        opt.SecretKey = "change-me-in-production-min-32-chars!!"; // минимум 32 символа!
        opt.Issuer = "NetworkSignalCore";
        opt.Audience = "NetworkSignalCore.Clients";
        opt.TokenLifetime = TimeSpan.FromHours(24);
    }
);
```

#### `UseNetworkSignalCore` — подключение middleware

```csharp
// app — это WebApplication
app.UseNetworkSignalCore("/network"); // путь к SignalR Hub

// Клиент подключается по адресу:
// ws://yourserver.com/network
// или для HTTPS: wss://yourserver.com/network
```

#### Полная конфигурация с кастомными провайдерами

```csharp
// Регистрация кастомных реализаций
builder.Services.AddSingleton<IAuthProvider, DatabaseAuthProvider>();
builder.Services.AddSingleton<IMatchmaker, RatingMatchmaker>();

// Регистрация контроллеров
builder.Services.AddNetworkController<GameController>();
builder.Services.AddNetworkController<ChatController>();
builder.Services.AddNetworkController<AdminController>();

// Инжекция ваших игровых сервисов
builder.Services.AddSingleton<IGameStateService, GameStateService>();
builder.Services.AddScoped<IPlayerStatsRepository, PlayerStatsRepository>();
```

---

### NetworkController — базовый класс серверных контроллеров

`NetworkController` — это аналог `NetworkBehaviour` в Mirror, но работающий на сервере. Каждый публичный метод с атрибутом `[ServerRpc]` автоматически регистрируется как обработчик SignalR-сообщения.

```csharp
// NetworkSignalCore.Server.Rpc
public abstract class NetworkController
{
    // Идентификатор SignalR-соединения текущего вызывающего
    protected string CallerConnectionId { get; }

    // Информация об аутентифицированном игроке (null если не аутентифицирован)
    protected PlayerInfo? Caller { get; }

    // Менеджер сессий — доступ к игрокам
    protected ISessionManager Sessions { get; }

    // Менеджер комнат — управление комнатами
    protected IRoomManager Rooms { get; }

    // Отправить сообщение только текущему вызывающему
    protected Task SendToCallerAsync(string method, object? payload = null);

    // Отправить сообщение конкретному игроку по playerId
    protected Task SendToPlayerAsync(string playerId, string method, object? payload = null);

    // Отправить сообщение всем игрокам в той же комнате, что и вызывающий
    protected Task SendToRoomAsync(string method, object? payload = null);

    // Отправить сообщение произвольной SignalR-группе
    protected Task SendToGroupAsync(string groupId, string method, object? payload = null);

    // Отправить сообщение ВСЕМ подключённым клиентам
    protected Task SendToAllAsync(string method, object? payload = null);
}
```

#### Полный пример боевого контроллера

```csharp
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Models;
using NetworkSignalCore.Server.Rpc;

public class CombatController : NetworkController
{
    private readonly IGameStateService _gameState;
    private readonly ILogger<CombatController> _logger;

    // Сервисы инжектируются через конструктор (ASP.NET Core DI)
    public CombatController(IGameStateService gameState, ILogger<CombatController> logger)
    {
        _gameState = gameState;
        _logger = logger;
    }

    /// <summary>
    /// Атака игрока. Требует аутентификации и нахождения в комнате.
    /// </summary>
    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task Attack(AttackPayload payload)
    {
        // Caller гарантированно не null благодаря RequireAuth = true
        var attacker = Caller!;

        // Получаем жертву из сессионного менеджера
        var victim = Sessions.GetPlayerById(payload.TargetPlayerId);
        if (victim == null)
        {
            // Отправляем ошибку только атакующему
            await SendToCallerAsync("AttackFailed", new { Reason = "Игрок не найден" });
            return;
        }

        // Проверяем игровую логику
        var result = await _gameState.ProcessAttackAsync(attacker.PlayerId, payload);
        if (!result.Success)
        {
            await SendToCallerAsync("AttackFailed", new { result.Reason });
            return;
        }

        _logger.LogInformation("{Attacker} атаковал {Victim} на {Damage} урона",
            attacker.DisplayName, victim.DisplayName, result.Damage);

        // Уведомляем жертву
        await SendToPlayerAsync(victim.PlayerId, "TakeDamage", new
        {
            AttackerId = attacker.PlayerId,
            result.Damage,
            result.RemainingHealth
        });

        // Уведомляем всю комнату (для отображения эффектов)
        await SendToRoomAsync("AttackAnimation", new
        {
            AttackerId = attacker.PlayerId,
            TargetId = victim.PlayerId,
            payload.WeaponType
        });

        // Если жертва погибла — объявляем всем
        if (result.RemainingHealth <= 0)
        {
            await SendToRoomAsync("PlayerKilled", new
            {
                KillerId = attacker.PlayerId,
                VictimId = victim.PlayerId
            });
        }
    }

    /// <summary>
    /// Использование лечилки. Без обязательной аутентификации (демо).
    /// </summary>
    [ServerRpc(MethodName = "UseHeal", RequireAuth = true, RequireRoom = false)]
    public async Task UseHealingItem(HealPayload payload)
    {
        var player = Caller!;
        var healAmount = Math.Clamp(payload.Amount, 0, 100);
        var newHp = await _gameState.HealPlayerAsync(player.PlayerId, healAmount);

        // Обновляем только вызывающего
        await SendToCallerAsync("HealthUpdated", new { Health = newHp });
    }
}

// Модели данных
public record AttackPayload(string TargetPlayerId, string WeaponType, float X, float Y, float Z);
public record HealPayload(float Amount);
```

---

### Атрибут `[ServerRpc]`

Помечает метод контроллера как обработчик входящего вызова от клиента.

```csharp
// Все параметры опциональны
[ServerRpc(
    MethodName = null,      // имя метода для SignalR (по умолчанию — имя C#-метода)
    RequireAuth = true,     // требовать JWT-аутентификацию
    RequireRoom = false     // требовать нахождение в комнате
)]
```

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `MethodName` | `string?` | `null` (имя метода) | Переопределить имя события SignalR |
| `RequireAuth` | `bool` | `true` | Блокировать неаутентифицированных |
| `RequireRoom` | `bool` | `false` | Блокировать не состоящих в комнате |

```csharp
// Примеры

// Метод вызывается клиентом как "Move"
[ServerRpc]
public async Task Move(MovePayload payload) { }

// Переименованный метод: клиент вызывает "pm" вместо "SendPrivateMessage"
[ServerRpc(MethodName = "pm")]
public async Task SendPrivateMessage(PrivateMessagePayload payload) { }

// Не требует аутентификации (например, для гостевого доступа)
[ServerRpc(RequireAuth = false)]
public async Task Ping() => await SendToCallerAsync("Pong");

// Требует нахождения в комнате
[ServerRpc(RequireAuth = true, RequireRoom = true)]
public async Task ReadyUp() { }
```

---

### Атрибут `[ClientRpc]`

Помечает метод контроллера как клиентский обработчик (документация для клиентских разработчиков). Реальная логика определяется на клиенте через `NetworkBehaviourBase`.

```csharp
[ClientRpc(MethodName = null)]  // MethodName переопределяет имя события
```

```csharp
public class GameController : NetworkController
{
    // [ClientRpc] — это документационный маркер.
    // Метод SendToRoomAsync("GameStarted", ...) должен соответствовать
    // обработчику "GameStarted" на клиенте.
    [ClientRpc]
    public void GameStarted(GameStartData data) { /* заглушка */ }

    [ClientRpc(MethodName = "gs")]  // клиент слушает "gs"
    public void GameStartedShort(GameStartData data) { /* заглушка */ }
}
```

---

### `SyncVar<T>` — синхронизируемые переменные

`SyncVar<T>` — обёртка над значением, которая отслеживает изменения (dirty-флаг) и позволяет серверу автоматически рассылать обновления клиентам.

```csharp
using NetworkSignalCore.Core.Sync;

// Создание SyncVar с начальным значением
var health = new SyncVar<float>(100f);
var playerName = new SyncVar<string>("Безымянный");
var position = new SyncVar<Vector3Data>(new Vector3Data(0, 0, 0));

// Изменение значения (автоматически помечает как dirty)
health.Value = 80f;
playerName.Value = "Герой";

// Проверка dirty-флага
if (health.IsDirty)
{
    Console.WriteLine("Здоровье изменилось — нужна синхронизация");
    health.ClearDirty(); // сбрасываем флаг после отправки
}

// Implicit cast — работает как обычное значение
float currentHp = (float)health;  // 80f
string name = (string)playerName; // "Герой"
```

#### Пример: синхронизация состояния игрока

```csharp
public class PlayerStateController : NetworkController
{
    // Состояние игроков хранится в сервисе
    private readonly IPlayerStateService _playerState;

    public PlayerStateController(IPlayerStateService playerState)
    {
        _playerState = playerState;
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task UpdateHealth(float newHealth)
    {
        var playerId = Caller!.PlayerId;
        var state = _playerState.GetState(playerId);

        // Устанавливаем новое значение — SyncVar запомнит изменение
        state.Health.Value = Math.Clamp(newHealth, 0f, 100f);

        if (state.Health.IsDirty)
        {
            // Рассылаем обновление всей комнате
            await SendToRoomAsync("SyncState", new
            {
                NetworkId = playerId,
                Field = "health",
                Value = state.Health.Value
            });
            state.Health.ClearDirty();
        }
    }
}

// Пример хранилища состояния игрока
public class PlayerState
{
    public SyncVar<float> Health { get; } = new SyncVar<float>(100f);
    public SyncVar<float> Mana { get; } = new SyncVar<float>(50f);
    public SyncVar<int> Score { get; } = new SyncVar<int>(0);
    public SyncVar<string> StatusEffect { get; } = new SyncVar<string>("none");
}
```

#### Атрибут `[SyncVar]` на контроллере

```csharp
// Атрибут [SyncVar] помечает поле для автоматической синхронизации
// FieldName — имя поля в протоколе (по умолчанию — имя поля)
// RoomOnly — синхронизировать только внутри комнаты (по умолчанию true)

public class MatchController : NetworkController
{
    [SyncVar(FieldName = "matchTimer", RoomOnly = true)]
    private SyncVar<int> _matchTimer = new SyncVar<int>(300); // 5 минут

    [SyncVar(FieldName = "score", RoomOnly = true)]
    private SyncVar<int> _teamScore = new SyncVar<int>(0);

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task ScorePoint()
    {
        _teamScore.Value++;

        if (_teamScore.IsDirty)
        {
            // Фреймворк автоматически рассылает SyncState клиентам
            await SendToRoomAsync("SyncState", new
            {
                NetworkId = "match",
                Field = "score",
                Value = _teamScore.Value.ToString()
            });
            _teamScore.ClearDirty();
        }
    }
}
```

---

### Комнаты — управление через `IRoomManager`

`IRoomManager` доступен в `NetworkController` через свойство `Rooms`.

```csharp
public class LobbyController : NetworkController
{
    [ServerRpc(RequireAuth = true)]
    public async Task CreateRoom(CreateRoomPayload payload)
    {
        var result = await Rooms.CreateRoomAsync(CallerConnectionId, new CreateRoomRequest
        {
            Name = payload.Name,
            MaxPlayers = payload.MaxPlayers,
            IsPrivate = payload.IsPrivate,
            Password = payload.Password,      // null для публичных комнат
            GameMode = payload.GameMode,
            Metadata = payload.Metadata       // произвольные данные
        });

        if (result.Success)
        {
            // Получаем информацию о созданной комнате
            var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
            await SendToCallerAsync("RoomCreated", room);
        }
        else
        {
            await SendToCallerAsync("Error", new { result.Message });
        }
    }

    [ServerRpc(RequireAuth = true)]
    public async Task JoinRoom(string roomId, string? password = null)
    {
        var result = await Rooms.JoinRoomAsync(CallerConnectionId, new JoinRoomRequest
        {
            RoomId = roomId,
            Password = password
        });

        if (result.Success)
        {
            var room = Rooms.GetRoom(roomId);
            // Уведомляем всю комнату о новом игроке
            await SendToRoomAsync("PlayerJoined", new
            {
                Player = Caller,
                Room = room
            });
        }
        else
        {
            await SendToCallerAsync("JoinFailed", new { result.Message });
        }
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task LeaveRoom()
    {
        var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
        var result = await Rooms.LeaveRoomAsync(CallerConnectionId);

        if (result.Success)
        {
            // Уведомляем оставшихся игроков
            await SendToRoomAsync("PlayerLeft", new { PlayerId = Caller?.PlayerId });
            await SendToCallerAsync("LeftRoom");
        }
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task KickPlayer(string targetPlayerId)
    {
        var result = await Rooms.KickPlayerAsync(CallerConnectionId, new KickPlayerRequest
        {
            TargetPlayerId = targetPlayerId,
            Reason = "Kicked by host"
        });

        if (result.Success)
        {
            await SendToRoomAsync("PlayerKicked", new { PlayerId = targetPlayerId });
        }
        else
        {
            await SendToCallerAsync("KickFailed", new { result.Message });
        }
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task StartGame()
    {
        var result = await Rooms.StartGameAsync(CallerConnectionId);

        if (result.Success)
        {
            // Уведомляем всю комнату о начале игры
            await SendToRoomAsync("GameStarted", new
            {
                MapName = "Arena_01",
                StartTime = DateTimeOffset.UtcNow
            });
        }
        else
        {
            await SendToCallerAsync("StartFailed", new { result.Message });
        }
    }

    [ServerRpc(RequireAuth = true)]
    public async Task GetPublicRooms()
    {
        var rooms = Rooms.GetPublicRooms();
        await SendToCallerAsync("RoomList", rooms);
    }
}
```

---

### Кастомный `IAuthProvider`

По умолчанию NetworkSignalCore использует JWT с симметричным ключом. Для интеграции с вашей базой данных реализуйте `IAuthProvider`.

```csharp
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

public class DatabaseAuthProvider : IAuthProvider
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<DatabaseAuthProvider> _logger;

    public DatabaseAuthProvider(
        IUserRepository userRepo,
        ITokenStore tokenStore,
        ILogger<DatabaseAuthProvider> logger)
    {
        _userRepo = userRepo;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    /// <summary>
    /// Вызывается при подключении клиента. Проверяет токен в вашей базе.
    /// </summary>
    public async Task<AuthResult> AuthenticateAsync(string token, CancellationToken ct)
    {
        try
        {
            // Ищем токен в базе данных
            var session = await _tokenStore.FindSessionAsync(token, ct);
            if (session == null)
            {
                _logger.LogWarning("Попытка входа с неизвестным токеном");
                return AuthResult.Fail("Неизвестный токен");
            }

            // Проверяем срок действия
            if (session.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return AuthResult.Fail("Токен истёк");
            }

            // Загружаем пользователя
            var user = await _userRepo.FindByIdAsync(session.UserId, ct);
            if (user == null)
            {
                return AuthResult.Fail("Пользователь не найден");
            }

            // Проверяем бан
            if (user.IsBanned)
            {
                return AuthResult.Fail($"Аккаунт заблокирован: {user.BanReason}");
            }

            // Формируем PlayerInfo
            var playerInfo = new PlayerInfo
            {
                PlayerId = user.Id.ToString(),
                DisplayName = user.Username,
                Elo = user.Rating,
                Metadata = new Dictionary<string, string>
                {
                    ["level"] = user.Level.ToString(),
                    ["rank"] = user.Rank,
                    ["gamesPlayed"] = user.GamesPlayed.ToString()
                }
            };

            return AuthResult.Ok(playerInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при аутентификации");
            return AuthResult.Fail("Внутренняя ошибка сервера");
        }
    }

    /// <summary>
    /// Генерирует токен для игрока (например, после входа в игру).
    /// </summary>
    public async Task<string> GenerateTokenAsync(PlayerInfo player, CancellationToken ct)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        await _tokenStore.StoreSessionAsync(new SessionRecord
        {
            Token = token,
            UserId = Guid.Parse(player.PlayerId),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
        }, ct);

        return token;
    }

    /// <summary>
    /// Отзыв токена (выход из игры, смена пароля).
    /// </summary>
    public async Task RevokeTokenAsync(string token, CancellationToken ct)
    {
        await _tokenStore.DeleteSessionAsync(token, ct);
    }
}

// Регистрация в Program.cs (ПОСЛЕ AddNetworkSignalCore):
// builder.Services.AddSingleton<IAuthProvider, DatabaseAuthProvider>();
```

---

### Кастомный `IMatchmaker`

```csharp
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;
using NetworkSignalCore.Core.Messages;

public class RatingMatchmaker : IMatchmaker
{
    private const int MaxEloDifference = 300;

    /// <summary>
    /// Определяет, можно ли поставить двух игроков в один матч.
    /// Вызывается движком матчмейкинга для каждой пары в очереди.
    /// </summary>
    public bool CanMatch(MatchRequest a, MatchRequest b)
    {
        // Должен быть одинаковый режим игры
        if (a.GameMode != b.GameMode) return false;

        // Должен быть одинаковый регион (или хотя бы один не указал)
        if (!string.IsNullOrEmpty(a.Region) &&
            !string.IsNullOrEmpty(b.Region) &&
            a.Region != b.Region) return false;

        // Разница рейтинга не должна превышать порог
        if (Math.Abs(a.Elo - b.Elo) > MaxEloDifference) return false;

        // Если у одного игрока пати — у другого тоже должна быть совместимая
        if (a.PartySize != b.PartySize) return false;

        return true;
    }

    /// <summary>
    /// Основная логика поиска матча. Движок сам управляет очередью,
    /// вызывая CanMatch — этот метод нужен для сложных сценариев.
    /// </summary>
    public Task<MatchResult> FindMatchAsync(MatchRequest request, CancellationToken ct)
    {
        // Движок матчмейкинга вызывает CanMatch самостоятельно.
        // Возвращаем Fail — движок продолжит подбор.
        return Task.FromResult(new MatchResult { Success = false });
    }
}

// Регистрация:
// builder.Services.AddSingleton<IMatchmaker, RatingMatchmaker>();
```

---

## 🛡️ Anti-cheat: подробное руководство

### Атрибут `[Validated]`

Помечает метод контроллера для прохождения валидации перед выполнением.

```csharp
using NetworkSignalCore.Core.Attributes;

public class MovementController : NetworkController
{
    // Перед выполнением Move будет вызван MovementValidator
    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    [Validated(typeof(MovementValidator))]
    public async Task Move(MovePayload payload)
    {
        // Этот код выполнится только если MovementValidator вернул Ok
        await SendToRoomAsync("PlayerMoved", new
        {
            PlayerId = Caller!.PlayerId,
            payload.X, payload.Y, payload.Z
        });
    }

    [ServerRpc(RequireAuth = true)]
    [Validated(typeof(ActionValidator))]
    public async Task UseAbility(AbilityPayload payload)
    {
        // Валидируется кулдаун, ресурсы и т.д.
        await SendToRoomAsync("AbilityUsed", payload);
    }
}
```

---

### Реализация `IAntiCheatValidator`

```csharp
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

/// <summary>
/// Валидатор скорости передвижения.
/// Определяет телепортацию и speed-hack.
/// </summary>
public class MovementValidator : IAntiCheatValidator
{
    private readonly IPlayerPositionTracker _tracker;
    private const float MaxSpeedPerSecond = 10f;   // максимум 10 единиц/сек
    private const float MaxDeltaTime = 1f;          // защита от старых пакетов

    public MovementValidator(IPlayerPositionTracker tracker)
    {
        _tracker = tracker;
    }

    public ValidationResult Validate(
        string connectionId,
        PlayerInfo player,
        string method,
        object? payload)
    {
        // Deserialize payload
        if (payload is not MovePayload move)
            return ValidationResult.Fail("Некорректный формат данных движения");

        var lastPosition = _tracker.GetLastPosition(player.PlayerId);
        if (lastPosition == null)
        {
            // Первый пакет — запоминаем и разрешаем
            _tracker.UpdatePosition(player.PlayerId, move.X, move.Y, move.Z);
            return ValidationResult.Ok();
        }

        // Вычисляем расстояние
        float dx = move.X - lastPosition.X;
        float dy = move.Y - lastPosition.Y;
        float dz = move.Z - lastPosition.Z;
        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        // Вычисляем время с последнего пакета
        float elapsed = (float)(DateTimeOffset.UtcNow - lastPosition.Timestamp).TotalSeconds;
        elapsed = Math.Min(elapsed, MaxDeltaTime); // ограничиваем

        // Проверяем скорость
        float speed = distance / Math.Max(elapsed, 0.016f); // не менее одного кадра
        if (speed > MaxSpeedPerSecond * 1.5f) // 50% буфер для лага
        {
            return ValidationResult.Fail(
                $"Speed hack detected: speed={speed:F1}, max={MaxSpeedPerSecond}");
        }

        // Обновляем позицию
        _tracker.UpdatePosition(player.PlayerId, move.X, move.Y, move.Z);
        return ValidationResult.Ok();
    }
}

/// <summary>
/// Валидатор кулдауна способностей.
/// </summary>
public class ActionValidator : IAntiCheatValidator
{
    private readonly ICooldownService _cooldowns;

    public ActionValidator(ICooldownService cooldowns)
    {
        _cooldowns = cooldowns;
    }

    public ValidationResult Validate(
        string connectionId,
        PlayerInfo player,
        string method,
        object? payload)
    {
        if (payload is not AbilityPayload ability)
            return ValidationResult.Fail("Некорректный payload");

        // Проверяем кулдаун
        var remaining = _cooldowns.GetRemainingCooldown(player.PlayerId, ability.AbilityId);
        if (remaining > TimeSpan.Zero)
        {
            return ValidationResult.Fail(
                $"Способность на кулдауне ещё {remaining.TotalSeconds:F1} сек");
        }

        return ValidationResult.Ok();
    }
}
```

---

### Rate Limiting

Rate limiting настраивается в `NetworkServerOptions` и применяется автоматически:

```csharp
builder.Services.AddNetworkSignalCore(
    configureServer: opt =>
    {
        // Максимум 30 RPC-вызовов в секунду на одно соединение
        opt.RateLimitCallsPerSecond = 30;
    }
);
```

Когда лимит превышен, клиент получает сообщение об ошибке и вызов игнорируется.

---

### Server Authority — принципы

NetworkSignalCore придерживается принципа **Server Authority**: сервер является единственным источником истины.

**Правила:**

1. **Никогда не доверяйте клиенту** — все критичные изменения состояния проверяются на сервере
2. **Используйте `[Validated]`** для всех методов, влияющих на игровое состояние
3. **Хранение состояния на сервере** — клиент получает обновления, но не владеет данными
4. **Проверка диапазонов** — всегда `Math.Clamp` входные данные

```csharp
// ❌ Плохо — доверяем клиенту
[ServerRpc]
public async Task SetHealth(float health)
{
    // Клиент может прислать любое значение!
    _playerHealth[Caller!.PlayerId] = health;
}

// ✅ Хорошо — сервер вычисляет результат сам
[ServerRpc]
[Validated(typeof(AttackValidator))]
public async Task TakeDamage(AttackPayload attack)
{
    // Сервер сам вычисляет урон на основе игровых правил
    var damage = _combatService.CalculateDamage(Caller!.PlayerId, attack);
    var newHealth = Math.Clamp(_health[Caller.PlayerId] - damage, 0, 100);
    _health[Caller.PlayerId] = newHealth;

    await SendToCallerAsync("HealthUpdated", new { Health = newHealth });
}
```

---

## 💻 Клиент: подробное руководство

### `NetworkClient` — подключение и аутентификация

```csharp
using NetworkSignalCore.Client;

// Способ 1: через configure-callback
var client = new NetworkClient(opt =>
{
    opt.ServerUrl = "https://game.myserver.com/network";
    opt.AuthToken = null;                               // токен устанавливается позже
    opt.ReconnectDelay = TimeSpan.FromSeconds(3);       // задержка перед реконнектом
    opt.MaxReconnectAttempts = 5;                       // максимум 5 попыток
    opt.AutoReconnect = true;                           // включить авто-реконнект
    opt.EnableLogging = true;                           // логирование SignalR
});

// Способ 2: через объект опций
var options = new NetworkClientOptions
{
    ServerUrl = "https://game.myserver.com/network",
    AutoReconnect = true,
    MaxReconnectAttempts = 10,
    ReconnectDelay = TimeSpan.FromSeconds(5)
};
var client2 = new NetworkClient(options);

// Подписка на события жизненного цикла
client.OnConnected += () =>
{
    Console.WriteLine("Успешно подключено к серверу");
};

client.OnDisconnected += ex =>
{
    if (ex != null)
        Console.WriteLine($"Отключено с ошибкой: {ex.Message}");
    else
        Console.WriteLine("Соединение закрыто");
};

client.OnError += (method, message) =>
{
    Console.WriteLine($"Ошибка при вызове {method}: {message}");
};

// Подключение
await client.ConnectAsync();
Console.WriteLine($"Подключено: {client.IsConnected}");

// Аутентификация (токен получается от вашего auth-сервера)
var token = await authService.LoginAsync(username, password);
bool authenticated = await client.AuthenticateAsync(token);
Console.WriteLine($"Авторизован: {authenticated}, игрок: {client.LocalPlayer?.DisplayName}");

// Отправка пинга для проверки соединения
await client.PingAsync();

// Graceful disconnect
await client.DisconnectAsync();
client.Dispose();
```

---

### `NetworkBehaviourBase` — организация клиентского кода

`NetworkBehaviourBase` — это клиентский аналог `NetworkController`. Регистрируется для конкретного контроллера и обрабатывает входящие `[ClientRpc]` и исходящие `[ServerRpc]`.

```csharp
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Client;
using NetworkSignalCore.Core.Attributes;

public class PlayerBehaviour : NetworkBehaviourBase
{
    // Имя контроллера на сервере (по умолчанию — имя класса без "Behaviour")
    protected override string ControllerName => "PlayerController";

    private float _health = 100f;

    public PlayerBehaviour(NetworkClient client) : base(client)
    {
        // Регистрируем обработчики входящих сообщений (ClientRpc)
        client.RegisterBehaviour(ControllerName, this);
    }

    protected override void OnConnected()
    {
        Console.WriteLine("PlayerBehaviour: соединение установлено");
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine("PlayerBehaviour: соединение потеряно");
    }

    // --- Исходящие вызовы (ServerRpc) ---

    // Вызывает [ServerRpc] "Move" на сервере
    public async Task MoveAsync(float x, float y, float z)
    {
        await ServerRpcAsync("Move", new { X = x, Y = y, Z = z });
    }

    // Вызывает [ServerRpc] "Attack" на сервере
    public async Task AttackAsync(string targetId, string weapon)
    {
        await ServerRpcAsync("Attack", new
        {
            TargetPlayerId = targetId,
            WeaponType = weapon
        });
    }

    // --- Входящие вызовы (ClientRpc) ---
    // Сервер вызывает SendToCallerAsync("TakeDamage", ...) или SendToRoomAsync(...)

    [ClientRpc]
    public void TakeDamage(DamageData data)
    {
        _health -= data.Damage;
        Console.WriteLine($"Получено {data.Damage} урона. HP: {_health}");

        if (_health <= 0)
        {
            OnPlayerDied();
        }
    }

    [ClientRpc]
    public void HealthUpdated(HealthData data)
    {
        _health = data.Health;
        Console.WriteLine($"HP обновлён: {_health}");
    }

    [ClientRpc]
    public void PlayerMoved(PlayerMovedData data)
    {
        Console.WriteLine($"Игрок {data.PlayerId} переместился");
    }

    private void OnPlayerDied()
    {
        Console.WriteLine("Игрок погиб!");
    }
}

// Использование:
var playerBehaviour = new PlayerBehaviour(client);
await playerBehaviour.MoveAsync(1.5f, 0f, 3.2f);
```

---

### `ClientRoomManager` — управление комнатами

```csharp
using NetworkSignalCore.Client;

var rooms = client.Rooms;

// Подписка на события
rooms.OnRoomUpdated += room =>
{
    Console.WriteLine($"Комната обновлена: {room.Name}, игроков: {room.PlayerCount}/{room.MaxPlayers}");
    Console.WriteLine($"Состояние: {room.State}"); // Lobby, Starting, InGame, Finished
};

rooms.OnRoomListReceived += roomList =>
{
    Console.WriteLine($"Получен список: {roomList.Length} комнат");
    foreach (var room in roomList)
    {
        var privacy = room.IsPrivate ? "(приватная)" : "(публичная)";
        var locked = room.HasPassword ? "🔒" : "🔓";
        Console.WriteLine($"  {locked} {room.Name} {privacy} — {room.PlayerCount}/{room.MaxPlayers}");
    }
};

// Создать публичную комнату
await rooms.CreateRoomAsync(new CreateRoomRequest
{
    Name = "Весёлые ребята",
    MaxPlayers = 8,
    IsPrivate = false,
    GameMode = "team_deathmatch",
    Metadata = new Dictionary<string, string> { ["map"] = "Dust2" }
});

// Создать приватную комнату с паролем
await rooms.CreateRoomAsync(new CreateRoomRequest
{
    Name = "Клановые игры",
    MaxPlayers = 10,
    IsPrivate = true,
    Password = "secret123",
    GameMode = "clan_war"
});

// Войти в комнату (без пароля)
await rooms.JoinRoomAsync("room-id-here");

// Войти в комнату с паролем
await rooms.JoinRoomAsync("room-id-here", password: "secret123");

// Получить список публичных комнат
var publicRooms = await rooms.GetRoomListAsync();

// Выгнать игрока (только хост)
await rooms.KickPlayerAsync("player-id", reason: "AFK");

// Начать игру (только хост)
await rooms.StartGameAsync();

// Покинуть комнату
await rooms.LeaveRoomAsync();

// Текущая комната
var currentRoom = rooms.CurrentRoom;
if (currentRoom != null)
{
    Console.WriteLine($"Я в комнате: {currentRoom.Name}");
    Console.WriteLine($"Владелец: {currentRoom.OwnerId}");
    Console.WriteLine($"Игроки: {string.Join(", ", currentRoom.Players.Select(p => p.DisplayName))}");
}
```

---

### `ClientMatchmakingManager` — поиск матча

```csharp
using NetworkSignalCore.Client;
using NetworkSignalCore.Core.Messages;

var mm = client.Matchmaking;

// Подписка на событие нахождения матча
mm.OnMatchFound += notification =>
{
    Console.WriteLine($"Матч найден! Комната: {notification.RoomName}");
    Console.WriteLine($"Игроки: {string.Join(", ", notification.PlayerIds)}");
    Console.WriteLine($"Принять в течение {notification.AcceptTimeoutSeconds} секунд");

    // Автопринятие или показ UI
    Task.Run(() => mm.AcceptMatchAsync(notification.RoomId));
};

// Встать в очередь
await mm.JoinQueueAsync(new JoinQueueRequest
{
    GameMode = "ranked",
    Region = "eu-west",
    Elo = 1450,
    PartySize = 1,
    Filters = new Dictionary<string, string>
    {
        ["preferredMap"] = "any",
        ["voiceChat"] = "true"
    }
});

Console.WriteLine($"В очереди: {mm.IsInQueue}");

// Выйти из очереди
await mm.LeaveQueueAsync();

// Принять матч
await mm.AcceptMatchAsync("match-room-id");

// Отклонить матч
await mm.DeclineMatchAsync("match-room-id");
```

---

### `ClientSyncManager` — получение SyncVar обновлений

```csharp
using NetworkSignalCore.Client;

var sync = client.Sync;

// Реализуем ISyncStateReceiver для получения обновлений
public class PlayerSyncReceiver : ISyncStateReceiver
{
    public float Health { get; private set; } = 100f;
    public int Score { get; private set; } = 0;
    public string Status { get; private set; } = "alive";

    public void OnSyncStateReceived(string field, string valueJson)
    {
        switch (field)
        {
            case "health":
                Health = JsonSerializer.Deserialize<float>(valueJson);
                Console.WriteLine($"HP: {Health}");
                break;

            case "score":
                Score = JsonSerializer.Deserialize<int>(valueJson);
                Console.WriteLine($"Счёт: {Score}");
                break;

            case "status":
                Status = JsonSerializer.Deserialize<string>(valueJson) ?? "unknown";
                Console.WriteLine($"Статус: {Status}");
                break;
        }
    }
}

// Регистрация
var receiver = new PlayerSyncReceiver();
sync.Register("player-network-id", receiver);

// Отмена регистрации при уничтожении объекта
sync.Unregister("player-network-id");

// Ручное применение синхронизации (обычно не нужно — вызывается автоматически)
sync.ApplySync("player-network-id", "health", "85.5");
```

---

### Авто-реконнект

```csharp
var client = new NetworkClient(opt =>
{
    opt.AutoReconnect = true;
    opt.ReconnectDelay = TimeSpan.FromSeconds(3);   // 3 секунды между попытками
    opt.MaxReconnectAttempts = 5;                    // после 5 неудач — сдаёмся
});

client.OnDisconnected += ex =>
{
    if (ex != null)
        Console.WriteLine($"Потеряно соединение: {ex.Message}. Пробуем реконнект...");
};

client.OnConnected += () =>
{
    Console.WriteLine("Соединение восстановлено!");
    // После реконнекта нужно заново аутентифицироваться
    _ = client.AuthenticateAsync(savedToken);
};
```

---

## 🎮 Unity: подробное руководство

### Установка DLL в Unity

1. Скопируйте в `Assets/Plugins/NetworkSignalCore/`:
   ```
   NetworkSignalCore.Core.dll
   NetworkSignalCore.Client.dll
   Microsoft.AspNetCore.SignalR.Client.dll
   Microsoft.AspNetCore.SignalR.Common.dll
   Microsoft.AspNetCore.Connections.Abstractions.dll
   Microsoft.Extensions.Logging.Abstractions.dll
   ```

2. Настройте **Player Settings**:
   - `Edit → Project Settings → Player`
   - `Other Settings → Api Compatibility Level` → `.NET Standard 2.1`

3. Для каждого DLL в Inspector установите:
   - `SDK`: Any SDK
   - `CPU`: Any CPU
   - Снимите галочку `Validate References` если появляются ошибки

---

### `NetworkManager` — синглтон в сцене

`NetworkManager` — это MonoBehaviour-синглтон, который управляет жизненным циклом `NetworkClient`.

```csharp
// Assets/Scripts/NetworkSignalCore/NetworkManager.cs (из unity/ папки)
// Использование:

// Получение доступа из любого скрипта
var client = NetworkManager.Instance.Client;

// Пример: MonoBehaviour в сцене
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:5000/network";
    [SerializeField] private InputField usernameInput;
    [SerializeField] private InputField passwordInput;

    private async void Start()
    {
        // NetworkManager автоматически создаёт NetworkClient
        // при первом обращении к Instance
    }

    public async void OnLoginButtonClick()
    {
        // 1. Получаем токен от вашего REST API
        var token = await GetTokenFromAuthServer(
            usernameInput.text,
            passwordInput.text
        );

        // 2. Подключаемся и аутентифицируемся
        await NetworkManager.Instance.Client.ConnectAsync();
        bool ok = await NetworkManager.Instance.Client.AuthenticateAsync(token);

        if (ok)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("Аутентификация не удалась!");
        }
    }

    private async Task<string> GetTokenFromAuthServer(string username, string password)
    {
        using var www = UnityEngine.Networking.UnityWebRequest.Post(
            "http://localhost:5000/auth/login",
            $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}",
            "application/json"
        );
        await www.SendWebRequest();
        // Парсим токен из ответа
        return www.downloadHandler.text; // упрощённо
    }
}
```

---

### `NetworkBehaviour` — базовый компонент игровых объектов

```csharp
// Assets/Scripts/Player/PlayerController.cs
using UnityEngine;
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Core.Attributes;

// Наследуемся от NetworkBehaviour (MonoBehaviour + NetworkBehaviourBase)
public class PlayerController : NetworkBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Настройки оружия")]
    [SerializeField] private string defaultWeapon = "pistol";

    private float _health = 100f;
    private bool _isLocalPlayer;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Проверяем, является ли этот объект локальным игроком
        var localPlayerId = NetworkManager.Instance.Client.LocalPlayer?.PlayerId;
        _isLocalPlayer = GetComponent<NetworkIdentity>().NetworkId == localPlayerId;
    }

    private void Update()
    {
        if (!_isLocalPlayer) return; // только локальный игрок управляет собой

        HandleMovementInput();
        HandleShootingInput();
    }

    private void HandleMovementInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            var pos = transform.position + new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;

            // Отправляем новую позицию на сервер
            _ = ServerRpcAsync("Move", new
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            });
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _ = ServerRpcAsync("Jump");
        }
    }

    private void HandleShootingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast для определения цели
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
            {
                var target = hit.collider.GetComponent<NetworkIdentity>();
                if (target != null)
                {
                    _ = ServerRpcAsync("Attack", new
                    {
                        TargetPlayerId = target.NetworkId,
                        WeaponType = defaultWeapon
                    });
                }
            }
        }
    }

    // --- Обработчики ClientRpc (сервер → клиент) ---

    [ClientRpc]
    public void TakeDamage(TakeDamageData data)
    {
        _health -= data.Damage;
        UpdateHealthBar(_health);

        // Визуальный эффект попадания
        ShowHitEffect(data.AttackerId);

        if (_health <= 0)
        {
            OnDeath();
        }
    }

    [ClientRpc]
    public void PlayerMoved(PlayerMovedData data)
    {
        // Обновляем позицию другого игрока (не локального)
        var player = FindPlayerById(data.PlayerId);
        if (player != null && !player._isLocalPlayer)
        {
            // NetworkTransform автоматически интерполирует позицию
        }
    }

    [ClientRpc]
    public void GameStarted(GameStartData data)
    {
        Debug.Log($"Игра началась! Карта: {data.MapName}");
    }

    // --- Вспомогательные методы ---

    private void UpdateHealthBar(float hp)
    {
        // Обновляем UI
        Debug.Log($"HP: {hp}/100");
    }

    private void ShowHitEffect(string attackerId)
    {
        // Показываем эффект попадания
    }

    private void OnDeath()
    {
        Debug.Log("Игрок погиб!");
        // Показываем экран смерти
    }

    private PlayerController FindPlayerById(string playerId)
    {
        // Поиск через NetworkIdentity
        foreach (var identity in FindObjectsOfType<NetworkIdentity>())
        {
            if (identity.NetworkId == playerId)
                return identity.GetComponent<PlayerController>();
        }
        return null;
    }
}

// Модели данных
[System.Serializable]
public class TakeDamageData { public float Damage; public string AttackerId; }
[System.Serializable]
public class PlayerMovedData { public string PlayerId; public float X, Y, Z; }
[System.Serializable]
public class GameStartData { public string MapName; public int PlayerCount; }
```

---

### `NetworkTransform` — автосинхронизация позиции

`NetworkTransform` автоматически отправляет обновления позиции/поворота на сервер и интерполирует их на клиенте.

```csharp
// Настройка в Unity Inspector:
// 1. Добавьте компонент NetworkTransform на игровой объект
// 2. Добавьте компонент NetworkIdentity (обязательно!)
// 3. Настройте параметры:

// В скрипте можно управлять параметрами:
public class NetworkTransformSetup : MonoBehaviour
{
    private NetworkTransform _netTransform;

    private void Awake()
    {
        _netTransform = GetComponent<NetworkTransform>();
    }

    private void Start()
    {
        // NetworkTransform автоматически определяет,
        // является ли объект локальным (для отправки) или удалённым (для приёма)

        var identity = GetComponent<NetworkIdentity>();
        bool isLocal = identity.NetworkId == NetworkManager.Instance.Client.LocalPlayer?.PlayerId;

        if (isLocal)
        {
            // Для локального игрока — отправляем позицию
            // NetworkTransform сделает это автоматически по тику
        }
        else
        {
            // Для удалённых — интерполируем получаемые данные
        }
    }
}
```

---

### `NetworkIdentity` — уникальный сетевой ID

```csharp
// NetworkIdentity автоматически присваивает networkId объекту
// networkId обычно = PlayerId для игроков

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    public void SpawnPlayer(PlayerInfo playerInfo)
    {
        var go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        var identity = go.GetComponent<NetworkIdentity>();

        // Устанавливаем NetworkId = PlayerId
        identity.SetNetworkId(playerInfo.PlayerId);

        // Теперь ClientSyncManager будет направлять SyncState на этот объект
        NetworkManager.Instance.Client.Sync.Register(
            playerInfo.PlayerId,
            go.GetComponent<ISyncStateReceiver>()
        );
    }
}
```

---

### Полный пример: игрок с движением, стрельбой и чатом

```csharp
// Assets/Scripts/Player/CompletePlayerExample.cs

using UnityEngine;
using UnityEngine.UI;
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Core.Attributes;
using System.Collections.Generic;

public class CompletePlayer : NetworkBehaviour
{
    // === Компоненты ===
    private CharacterController _characterController;
    private NetworkIdentity _identity;

    // === UI ===
    [SerializeField] private Text chatDisplay;
    [SerializeField] private InputField chatInput;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text scoreText;

    // === Состояние ===
    private float _health = 100f;
    private int _score = 0;
    private bool _isLocalPlayer;
    private Vector3 _networkPosition;    // позиция с сервера для интерполяции
    private Quaternion _networkRotation; // поворот с сервера для интерполяции
    private readonly List<string> _chatMessages = new();

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _identity = GetComponent<NetworkIdentity>();
    }

    private void Start()
    {
        var client = NetworkManager.Instance.Client;
        _isLocalPlayer = _identity.NetworkId == client.LocalPlayer?.PlayerId;

        // Регистрируем как получателя SyncVar обновлений
        client.Sync.Register(_identity.NetworkId, new PlayerSyncAdapter(this));

        _networkPosition = transform.position;
        _networkRotation = transform.rotation;
    }

    private void Update()
    {
        if (_isLocalPlayer)
        {
            HandleInput();
        }
        else
        {
            // Плавная интерполяция для удалённых игроков
            transform.position = Vector3.Lerp(
                transform.position, _networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, _networkRotation, Time.deltaTime * 10f);
        }
    }

    private void HandleInput()
    {
        // Движение
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        var move = new Vector3(h, 0, v) * 5f * Time.deltaTime;

        if (move.magnitude > 0.01f)
        {
            _characterController.Move(move);

            // Отправляем позицию на сервер (с частотой не более 20 раз/сек)
            _ = ServerRpcAsync("Move", new
            {
                X = transform.position.x,
                Y = transform.position.y,
                Z = transform.position.z,
                RotY = transform.eulerAngles.y
            });
        }

        // Стрельба
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(
                Camera.main.ScreenPointToRay(Input.mousePosition),
                out var hit, 100f))
            {
                var targetIdentity = hit.collider.GetComponent<NetworkIdentity>();
                if (targetIdentity != null)
                {
                    _ = ServerRpcAsync("Shoot", new
                    {
                        TargetId = targetIdentity.NetworkId,
                        HitPoint = new { hit.point.x, hit.point.y, hit.point.z }
                    });
                }
            }
        }

        // Чат
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInput.text))
        {
            _ = ServerRpcAsync("ChatMessage", new { Text = chatInput.text.Trim() });
            chatInput.text = "";
        }
    }

    // === ClientRpc обработчики ===

    [ClientRpc]
    public void OnDamaged(DamageInfo info)
    {
        _health = Mathf.Clamp(_health - info.Damage, 0f, 100f);
        healthBar.value = _health / 100f;

        if (_health <= 0f)
            OnDied(info.KillerId);
    }

    [ClientRpc]
    public void OnHealed(float amount)
    {
        _health = Mathf.Clamp(_health + amount, 0f, 100f);
        healthBar.value = _health / 100f;
    }

    [ClientRpc]
    public void OnScoreChanged(int newScore)
    {
        _score = newScore;
        scoreText.text = $"Счёт: {_score}";
    }

    [ClientRpc]
    public void OnChatReceived(ChatMessageData data)
    {
        _chatMessages.Add($"[{data.SenderName}]: {data.Text}");
        if (_chatMessages.Count > 50) _chatMessages.RemoveAt(0);

        chatDisplay.text = string.Join("\n", _chatMessages[^10..]); // последние 10 сообщений
    }

    [ClientRpc]
    public void OnPlayerPositionUpdate(PositionUpdateData data)
    {
        if (!_isLocalPlayer)
        {
            _networkPosition = new Vector3(data.X, data.Y, data.Z);
            _networkRotation = Quaternion.Euler(0, data.RotY, 0);
        }
    }

    private void OnDied(string killerId)
    {
        Debug.Log($"Убит игроком {killerId}");
        // Показываем экран смерти, запускаем таймер возрождения
        _ = ServerRpcAsync("RequestRespawn");
    }

    private void OnDestroy()
    {
        NetworkManager.Instance?.Client?.Sync.Unregister(_identity.NetworkId);
    }
}

// Адаптер для SyncVar обновлений
public class PlayerSyncAdapter : ISyncStateReceiver
{
    private readonly CompletePlayer _player;
    public PlayerSyncAdapter(CompletePlayer player) => _player = player;

    public void OnSyncStateReceived(string field, string valueJson)
    {
        switch (field)
        {
            case "health":
                float hp = JsonUtility.FromJson<FloatWrapper>($"{{\"v\":{valueJson}}}").v;
                _player.OnHealed(hp - 100f); // упрощённо
                break;
            case "score":
                int score = int.Parse(valueJson.Trim('"'));
                _player.OnScoreChanged(score);
                break;
        }
    }
}

[System.Serializable] class FloatWrapper { public float v; }

// Модели данных
[System.Serializable] public class DamageInfo { public float Damage; public string KillerId; }
[System.Serializable] public class ChatMessageData { public string SenderName; public string Text; }
[System.Serializable] public class PositionUpdateData { public float X, Y, Z, RotY; }
```

---

## 🎯 Матчмейкинг: полное руководство

### Серверная часть

Матчмейкинг управляется `MatchmakingManager` (встроенный), который использует `IMatchmaker` для определения совместимости игроков.

```csharp
// Сервер: контроллер для очереди матчмейкинга
public class MatchmakingController : NetworkController
{
    [ServerRpc(RequireAuth = true)]
    public async Task JoinQueue(JoinQueuePayload payload)
    {
        // Серверный матчмейкинг запускается автоматически
        // Клиент встаёт в очередь через ClientMatchmakingManager
        // Когда матч найден — клиент получает MatchFoundNotification
        await SendToCallerAsync("QueueJoined", new { Status = "searching" });
    }

    [ServerRpc(RequireAuth = true)]
    public async Task AcceptMatch(string matchRoomId)
    {
        // Подтверждение матча — создаётся комната
        var room = Rooms.GetRoom(matchRoomId);
        if (room != null)
        {
            await SendToCallerAsync("MatchAccepted", room);
        }
    }

    [ServerRpc(RequireAuth = true)]
    public async Task DeclineMatch(string matchRoomId)
    {
        await SendToCallerAsync("MatchDeclined");
        // Возвращаем игрока в очередь или завершаем поиск
    }
}
```

### Клиентская часть

```csharp
// LobbyScene.cs в Unity
public class LobbyScene : MonoBehaviour
{
    [SerializeField] private GameObject matchFoundPanel;
    [SerializeField] private Text matchFoundText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Slider acceptTimer;

    private string _pendingMatchRoomId;
    private float _acceptTimeoutSeconds;
    private float _acceptTimer;
    private bool _matchPending;

    private void Start()
    {
        var mm = NetworkManager.Instance.Client.Matchmaking;

        mm.OnMatchFound += OnMatchFound;

        acceptButton.onClick.AddListener(OnAcceptMatch);
        declineButton.onClick.AddListener(OnDeclineMatch);
    }

    public async void StartSearching()
    {
        var client = NetworkManager.Instance.Client;
        var localPlayer = client.LocalPlayer!;

        await client.Matchmaking.JoinQueueAsync(new JoinQueueRequest
        {
            GameMode = "ranked_5v5",
            Region = "eu-west",
            Elo = localPlayer.Elo,
            PartySize = 1
        });

        Debug.Log("Поиск матча начат...");
    }

    private void OnMatchFound(MatchFoundNotification notification)
    {
        _pendingMatchRoomId = notification.RoomId;
        _acceptTimeoutSeconds = notification.AcceptTimeoutSeconds;
        _acceptTimer = _acceptTimeoutSeconds;
        _matchPending = true;

        // Показываем UI на главном потоке
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            matchFoundPanel.SetActive(true);
            matchFoundText.text = $"Матч найден!\n{notification.PlayerIds.Length} игроков";
            acceptTimer.maxValue = _acceptTimeoutSeconds;
            acceptTimer.value = _acceptTimeoutSeconds;
        });
    }

    private void Update()
    {
        if (!_matchPending) return;

        _acceptTimer -= Time.deltaTime;
        acceptTimer.value = _acceptTimer;

        if (_acceptTimer <= 0f)
        {
            // Время вышло — автоматически отклоняем
            OnDeclineMatch();
        }
    }

    private async void OnAcceptMatch()
    {
        _matchPending = false;
        matchFoundPanel.SetActive(false);
        await NetworkManager.Instance.Client.Matchmaking.AcceptMatchAsync(_pendingMatchRoomId);
        // Переходим в игру
        SceneManager.LoadScene("Game");
    }

    private async void OnDeclineMatch()
    {
        _matchPending = false;
        matchFoundPanel.SetActive(false);
        await NetworkManager.Instance.Client.Matchmaking.DeclineMatchAsync(_pendingMatchRoomId);
    }

    public async void StopSearching()
    {
        await NetworkManager.Instance.Client.Matchmaking.LeaveQueueAsync();
    }
}
```

### Пользовательский матчмейкинг

Расширенный пример с учётом пати и MMR:

```csharp
public class AdvancedMatchmaker : IMatchmaker
{
    // Параметры матчмейкинга
    private const int BaseEloDiff = 200;        // начальный порог ELO
    private const int EloDiffPerMinute = 50;    // расширяем порог на 50 за каждую минуту ожидания
    private const int MaxEloDiff = 600;         // максимальный порог

    public bool CanMatch(MatchRequest a, MatchRequest b)
    {
        if (a.GameMode != b.GameMode) return false;
        if (a.Region != b.Region && !string.IsNullOrEmpty(a.Region) && !string.IsNullOrEmpty(b.Region))
            return false;

        // Вычисляем время ожидания
        var waitA = (DateTimeOffset.UtcNow - a.QueuedAt).TotalMinutes;
        var waitB = (DateTimeOffset.UtcNow - b.QueuedAt).TotalMinutes;
        var avgWait = (waitA + waitB) / 2;

        // Расширяем допустимую разницу ELO с течением времени
        var allowedEloDiff = Math.Min(
            BaseEloDiff + (int)(avgWait * EloDiffPerMinute),
            MaxEloDiff
        );

        if (Math.Abs(a.Elo - b.Elo) > allowedEloDiff) return false;

        // Проверяем совместимость пати
        if (a.PartySize + b.PartySize > 10) return false; // максимум 10 человек в команде

        return true;
    }

    public Task<MatchResult> FindMatchAsync(MatchRequest request, CancellationToken ct)
    {
        // Движок сам управляет очередью через CanMatch
        return Task.FromResult(new MatchResult { Success = false });
    }
}
```

---

## 🏠 Комнаты и пати: полное руководство

### Создание публичных/приватных комнат

```csharp
// Серверный контроллер
public class RoomController : NetworkController
{
    [ServerRpc(RequireAuth = true)]
    public async Task CreatePublicRoom(string name, string gameMode)
    {
        var result = await Rooms.CreateRoomAsync(CallerConnectionId, new CreateRoomRequest
        {
            Name = name,
            MaxPlayers = 8,
            IsPrivate = false,      // публичная — видна в списке
            Password = null,        // без пароля
            GameMode = gameMode,
            Metadata = new Dictionary<string, string>
            {
                ["version"] = "1.0",
                ["region"] = "eu"
            }
        });

        if (result.Success)
        {
            var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
            // Уведомляем всех о новой комнате
            await SendToAllAsync("RoomCreated", room);
            await SendToCallerAsync("RoomJoined", room);
        }
    }

    [ServerRpc(RequireAuth = true)]
    public async Task CreatePrivateRoom(CreatePrivateRoomPayload payload)
    {
        var result = await Rooms.CreateRoomAsync(CallerConnectionId, new CreateRoomRequest
        {
            Name = payload.Name,
            MaxPlayers = payload.MaxPlayers,
            IsPrivate = true,           // приватная — не видна в общем списке
            Password = payload.Password, // опциональный пароль
            GameMode = payload.GameMode
        });

        if (result.Success)
        {
            var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
            await SendToCallerAsync("PrivateRoomCreated", new
            {
                Room = room,
                InviteCode = GenerateInviteCode(room.RoomId) // ваша логика
            });
        }
    }
}
```

### Система лобби

```csharp
public class LobbyController : NetworkController
{
    [ServerRpc(RequireAuth = true)]
    public async Task GetLobbyState()
    {
        var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
        if (room == null)
        {
            await SendToCallerAsync("Error", new { Message = "Вы не в комнате" });
            return;
        }

        // Отправляем полное состояние лобби
        await SendToCallerAsync("LobbyState", new
        {
            Room = room,
            Players = room.Players,
            IsOwner = room.OwnerId == Caller?.PlayerId,
            CanStart = room.PlayerCount >= 2 && room.State == RoomState.Lobby
        });
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task SetReady(bool isReady)
    {
        // Сохраняем статус готовности в метаданных сессии
        var player = Caller!;

        await SendToRoomAsync("PlayerReadyChanged", new
        {
            PlayerId = player.PlayerId,
            IsReady = isReady
        });

        // Проверяем, все ли готовы
        var room = Rooms.GetRoomByConnectionId(CallerConnectionId);
        // Логика проверки готовности всех игроков...
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task StartGame()
    {
        var room = Rooms.GetRoomByConnectionId(CallerConnectionId);

        // Только хост может начать игру
        if (room?.OwnerId != Caller?.PlayerId)
        {
            await SendToCallerAsync("Error", new { Message = "Только хост может начать игру" });
            return;
        }

        if (room.PlayerCount < 2)
        {
            await SendToCallerAsync("Error", new { Message = "Недостаточно игроков" });
            return;
        }

        var result = await Rooms.StartGameAsync(CallerConnectionId);
        if (result.Success)
        {
            await SendToRoomAsync("GameStarting", new
            {
                CountdownSeconds = 5,
                MapName = room.Metadata.GetValueOrDefault("map", "default_map")
            });
        }
    }
}
```

### Управление пати

```csharp
// PartyInfo { PartyId, LeaderId, Members[], MaxSize, InviteCode?, Metadata }

public class PartyController : NetworkController
{
    private readonly IPartyService _partyService;

    public PartyController(IPartyService partyService)
    {
        _partyService = partyService;
    }

    [ServerRpc(RequireAuth = true)]
    public async Task CreateParty(int maxSize = 5)
    {
        var party = await _partyService.CreatePartyAsync(Caller!.PlayerId, maxSize);

        await SendToCallerAsync("PartyCreated", new
        {
            PartyId = party.PartyId,
            InviteCode = party.InviteCode,
            LeaderId = party.LeaderId
        });
    }

    [ServerRpc(RequireAuth = true)]
    public async Task InviteToParty(string targetPlayerId)
    {
        var party = await _partyService.GetPartyByLeaderAsync(Caller!.PlayerId);
        if (party == null)
        {
            await SendToCallerAsync("Error", new { Message = "Вы не лидер пати" });
            return;
        }

        // Отправляем приглашение целевому игроку
        await SendToPlayerAsync(targetPlayerId, "PartyInvite", new
        {
            PartyId = party.PartyId,
            InviteCode = party.InviteCode,
            LeaderName = Caller.DisplayName
        });
    }

    [ServerRpc(RequireAuth = true)]
    public async Task JoinPartyByCode(string inviteCode)
    {
        var result = await _partyService.JoinPartyAsync(Caller!.PlayerId, inviteCode);
        if (result.Success)
        {
            var party = result.Party!;
            // Уведомляем всю пати
            foreach (var member in party.Members)
            {
                await SendToPlayerAsync(member.PlayerId, "PartyMemberJoined", new
                {
                    NewMember = Caller,
                    Party = party
                });
            }
        }
        else
        {
            await SendToCallerAsync("Error", new { result.Message });
        }
    }

    [ServerRpc(RequireAuth = true)]
    public async Task LeaveParty()
    {
        var result = await _partyService.LeavePartyAsync(Caller!.PlayerId);
        if (result.Success)
        {
            await SendToCallerAsync("LeftParty");
            // Уведомить остальных членов...
        }
    }
}
```

---

## 🔧 Расширенные сценарии

### Кастомный `INetworkSerializer` (замена JSON на MessagePack)

```csharp
// Реализуйте INetworkSerializer для замены стандартного JSON-сериализатора
public class MessagePackSerializer : INetworkSerializer
{
    public byte[] Serialize<T>(T value)
        => MessagePackSerializer.Serialize(value);

    public T? Deserialize<T>(byte[] data)
        => MessagePackSerializer.Deserialize<T>(data);

    public T? Deserialize<T>(string json)
        => MessagePackSerializer.Deserialize<T>(
            Convert.FromBase64String(json));
}

// Регистрация:
builder.Services.AddSingleton<INetworkSerializer, MessagePackSerializer>();
```

### Несколько контроллеров

```csharp
// Program.cs — регистрируем все контроллеры
builder.Services.AddNetworkController<MovementController>();  // движение
builder.Services.AddNetworkController<CombatController>();    // бой
builder.Services.AddNetworkController<ChatController>();      // чат
builder.Services.AddNetworkController<AdminController>();     // администрирование

// На клиенте регистрируем соответствующие Behaviour:
var movementBehaviour = new MovementBehaviour(client);
var combatBehaviour = new CombatBehaviour(client);
var chatBehaviour = new ChatBehaviour(client);
```

### Инжекция сервисов в NetworkController

```csharp
// Любой ASP.NET Core сервис может быть инжектирован в контроллер
public class AdvancedController : NetworkController
{
    private readonly IGameDatabase _db;
    private readonly ILeaderboardService _leaderboard;
    private readonly INotificationService _notifications;
    private readonly ILogger<AdvancedController> _logger;
    private readonly IMemoryCache _cache;

    public AdvancedController(
        IGameDatabase db,
        ILeaderboardService leaderboard,
        INotificationService notifications,
        ILogger<AdvancedController> logger,
        IMemoryCache cache)
    {
        _db = db;
        _leaderboard = leaderboard;
        _notifications = notifications;
        _logger = logger;
        _cache = cache;
    }

    [ServerRpc(RequireAuth = true)]
    public async Task GetLeaderboard()
    {
        // Кэшируем на 60 секунд
        if (!_cache.TryGetValue("leaderboard", out var cached))
        {
            cached = await _leaderboard.GetTopPlayersAsync(100);
            _cache.Set("leaderboard", cached, TimeSpan.FromSeconds(60));
        }

        await SendToCallerAsync("LeaderboardData", cached);
    }
}
```

### Метрики и мониторинг

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<NetworkSignalCoreHealthCheck>("networksignalcore");

// Пример health check:
public class NetworkSignalCoreHealthCheck : IHealthCheck
{
    private readonly ISessionManager _sessions;
    private readonly IRoomManager _rooms;

    public NetworkSignalCoreHealthCheck(ISessionManager sessions, IRoomManager rooms)
    {
        _sessions = sessions;
        _rooms = rooms;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var players = _sessions.GetAllPlayers().Count;
        var rooms = _rooms.GetPublicRooms().Count;

        var data = new Dictionary<string, object>
        {
            ["activePlayers"] = players,
            ["activeRooms"] = rooms
        };

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Онлайн: {players} игроков, {rooms} комнат",
            data));
    }
}

// Доступно по адресу: GET /health
app.MapHealthChecks("/health");
```

---

## 🔒 Безопасность

### JWT Best Practices

```csharp
// NetworkAuthOptions — правильная конфигурация
configureAuth: opt =>
{
    // Читать секрет из переменной окружения, не хардкодить!
    opt.SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("JWT_SECRET_KEY не задан!");

    // Минимум 32 символа для HMAC-SHA256
    if (opt.SecretKey.Length < 32)
        throw new InvalidOperationException("JWT_SECRET_KEY должен быть минимум 32 символа!");

    opt.Issuer = "MyGame";
    opt.Audience = "Players";

    // Короткое время жизни + refresh tokens
    opt.TokenLifetime = TimeSpan.FromHours(1);
}
```

```csharp
// appsettings.json — НЕ хранить секреты здесь!
// Используйте:
// - Environment Variables
// - Azure Key Vault
// - HashiCorp Vault
// - .NET Secret Manager (только для разработки)

// Пример с dotnet user-secrets (только dev):
// dotnet user-secrets set "Jwt:SecretKey" "my-dev-secret-32-chars-minimum!!"
```

### Rate Limiting конфигурация

```csharp
builder.Services.AddNetworkSignalCore(
    configureServer: opt =>
    {
        // Глобальный лимит
        opt.RateLimitCallsPerSecond = 30;

        // Рекомендуемые значения по типу игры:
        // FPS/Action: 60 (60 fps движение)
        // RPG/Strategy: 10-20
        // Turn-based: 5
    }
);
```

### Пример полной защиты от чита на движение

```csharp
// Комплексная система защиты
public class ComprehensiveMovementValidator : IAntiCheatValidator
{
    private readonly IPlayerStateService _stateService;
    private readonly IViolationTracker _violations;
    private readonly ILogger<ComprehensiveMovementValidator> _logger;

    // Константы физики игры
    private const float MaxMoveSpeed = 10f;     // макс. скорость
    private const float MaxJumpHeight = 3f;     // макс. высота прыжка
    private const float MaxTeleportDist = 20f;  // максимально допустимое расстояние за пакет
    private const int MaxViolationsBeforeBan = 10;

    public ValidationResult Validate(
        string connectionId,
        PlayerInfo player,
        string method,
        object? payload)
    {
        if (payload is not MovePayload move)
            return ValidationResult.Fail("Неверный формат");

        var state = _stateService.GetState(player.PlayerId);

        // 1. Проверка диапазонов координат (защита от out-of-bounds)
        if (MathF.Abs(move.X) > 10000f || MathF.Abs(move.Z) > 10000f)
        {
            return RecordViolation(player, "Координаты вне допустимого диапазона");
        }

        // 2. Проверка скорости
        float elapsed = (float)(DateTimeOffset.UtcNow - state.LastUpdateTime).TotalSeconds;
        elapsed = Math.Clamp(elapsed, 0.001f, 1f);

        float distance = Vector3Distance(state.LastPosition, (move.X, move.Y, move.Z));
        float speed = distance / elapsed;

        if (speed > MaxMoveSpeed * 2f) // двойной буфер для лага
        {
            return RecordViolation(player, $"Speed hack: {speed:F1} > {MaxMoveSpeed * 2f}");
        }

        // 3. Проверка телепортации
        if (distance > MaxTeleportDist)
        {
            return RecordViolation(player, $"Телепортация: {distance:F1} единиц за пакет");
        }

        // 4. Проверка высоты (noclip/fly hack)
        if (move.Y - state.LastPosition.Y > MaxJumpHeight)
        {
            return RecordViolation(player, $"Fly hack: подъём на {move.Y - state.LastPosition.Y:F1}");
        }

        // Всё хорошо — обновляем состояние
        state.LastPosition = (move.X, move.Y, move.Z);
        state.LastUpdateTime = DateTimeOffset.UtcNow;

        return ValidationResult.Ok();
    }

    private ValidationResult RecordViolation(PlayerInfo player, string reason)
    {
        int count = _violations.Increment(player.PlayerId);
        _logger.LogWarning("Нарушение [{Count}] от {Player}: {Reason}",
            count, player.DisplayName, reason);

        if (count >= MaxViolationsBeforeBan)
        {
            _logger.LogError("Игрок {Player} заблокирован за читерство!", player.DisplayName);
            // Инициировать бан
        }

        return ValidationResult.Fail(reason);
    }

    private static float Vector3Distance(
        (float X, float Y, float Z) a,
        (float X, float Y, float Z) b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
```

---

## 📚 Справочник API

### Серверные классы

| Класс / Интерфейс | Пространство имён | Описание |
|---|---|---|
| `NetworkController` | `NetworkSignalCore.Server.Rpc` | Базовый класс серверного контроллера |
| `IAuthProvider` | `NetworkSignalCore.Core.Abstractions` | Интерфейс провайдера аутентификации |
| `ISessionManager` | `NetworkSignalCore.Core.Abstractions` | Управление сессиями игроков |
| `IRoomManager` | `NetworkSignalCore.Core.Abstractions` | Управление комнатами |
| `IMatchmaker` | `NetworkSignalCore.Core.Abstractions` | Интерфейс матчмейкера |
| `IAntiCheatValidator` | `NetworkSignalCore.Core.Abstractions` | Интерфейс анти-чит валидатора |
| `NetworkServerOptions` | `NetworkSignalCore.Server.Options` | Настройки сервера |
| `NetworkAuthOptions` | `NetworkSignalCore.Server.Options` | Настройки JWT |

### Клиентские классы

| Класс | Пространство имён | Описание |
|---|---|---|
| `NetworkClient` | `NetworkSignalCore.Client` | Главный клиентский класс |
| `NetworkBehaviourBase` | `NetworkSignalCore.Client.Rpc` | Базовый класс клиентского поведения |
| `ClientRoomManager` | `NetworkSignalCore.Client` | Управление комнатами на клиенте |
| `ClientMatchmakingManager` | `NetworkSignalCore.Client` | Матчмейкинг на клиенте |
| `ClientSyncManager` | `NetworkSignalCore.Client` | Синхронизация состояния |
| `NetworkClientOptions` | `NetworkSignalCore.Client` | Настройки клиента |

### Общие классы

| Класс | Пространство имён | Описание |
|---|---|---|
| `PlayerInfo` | `NetworkSignalCore.Core.Models` | Информация об игроке |
| `RoomInfo` | `NetworkSignalCore.Core.Models` | Информация о комнате |
| `MatchRequest` | `NetworkSignalCore.Core.Models` | Запрос на матч |
| `MatchResult` | `NetworkSignalCore.Core.Models` | Результат матча |
| `PartyInfo` | `NetworkSignalCore.Core.Models` | Информация о пати |
| `SyncVar<T>` | `NetworkSignalCore.Core.Sync` | Синхронизируемая переменная |
| `CreateRoomRequest` | `NetworkSignalCore.Core.Messages` | Запрос создания комнаты |
| `JoinRoomRequest` | `NetworkSignalCore.Core.Messages` | Запрос входа в комнату |
| `MatchFoundNotification` | `NetworkSignalCore.Core.Messages` | Уведомление о найденном матче |

### Атрибуты

| Атрибут | Описание |
|---|---|
| `[ServerRpc]` | Помечает метод как обработчик клиентского вызова |
| `[ClientRpc]` | Помечает метод как клиентский обработчик (документация) |
| `[SyncVar]` | Помечает поле для автоматической синхронизации |
| `[Validated]` | Подключает анти-чит валидатор к методу |

### `NetworkServerOptions` — все параметры

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `MaxPlayersPerRoom` | `int` | `16` | Максимум игроков в комнате |
| `MatchmakingIntervalMs` | `int` | `2000` | Интервал матчмейкинга (мс) |
| `SyncTickRateMs` | `int` | `50` | Тик синхронизации (мс), 50 = 20 Hz |
| `RateLimitCallsPerSecond` | `int` | `30` | Лимит RPC вызовов в секунду |
| `RequireAuthForAllRpcs` | `bool` | `true` | Требовать JWT для всех RPC |
| `MatchAcceptTimeout` | `TimeSpan` | `15s` | Таймаут принятия матча |
| `MaxRooms` | `int` | `100` | Максимум активных комнат |
| `EloDifferenceThreshold` | `int` | `400` | Максимальная разница ELO |

### `NetworkClientOptions` — все параметры

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `ServerUrl` | `string` | `"http://localhost:5000/network"` | URL SignalR Hub |
| `AuthToken` | `string?` | `null` | JWT токен для авто-аутентификации |
| `ReconnectDelay` | `TimeSpan` | `3s` | Задержка перед реконнектом |
| `MaxReconnectAttempts` | `int` | `5` | Максимум попыток реконнекта |
| `AutoReconnect` | `bool` | `true` | Включить авто-реконнект |
| `EnableLogging` | `bool` | `true` | Включить логирование SignalR |

### `RoomState` — состояния комнаты

| Значение | Описание |
|---|---|
| `Lobby` | Ожидание игроков |
| `Starting` | Начало игры (таймер) |
| `InGame` | Игра идёт |
| `Finished` | Игра завершена |

---

## ❓ FAQ

### Q: Чем NetworkSignalCore отличается от Mirror?

**A:** Mirror — это полностью серверная архитектура с отдельным Unity-сервером (headless build). NetworkSignalCore использует ASP.NET Core сервер, который значительно проще развёртывать, масштабировать и интегрировать с другими сервисами (базами данных, аутентификацией, платёжными системами). При этом NetworkSignalCore поддерживает WebSockets через SignalR, что даёт сравнимую производительность для большинства типов игр.

---

### Q: Подходит ли NetworkSignalCore для быстрых шутеров (FPS)?

**A:** SignalR/WebSocket имеет чуть больше накладных расходов по сравнению с чистым UDP, поэтому для игр с требованием латентности < 50ms (профессиональные FPS) лучше рассмотреть другие варианты. Для казуальных FPS, top-down шутеров, battle royale и большинства многопользовательских игр NetworkSignalCore вполне подходит. Настройте `SyncTickRateMs = 16` (60 Hz) для максимальной частоты обновлений.

---

### Q: Как масштабировать сервер на несколько инстансов?

**A:** Используйте SignalR Backplane с Redis:

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis("redis-connection-string", options =>
    {
        options.Configuration.ChannelPrefix = "MyGame";
    });
```

Также убедитесь, что `ISessionManager` и `IRoomManager` используют distributed storage (Redis/DB), а не in-memory.

---

### Q: Как получить токен для аутентификации?

**A:** NetworkSignalCore не включает REST API для получения токена — это намеренно, так как auth-сервер обычно является отдельным сервисом. Создайте отдельный endpoint:

```csharp
// AuthController.cs в вашем ASP.NET Core приложении
app.MapPost("/auth/login", async (LoginRequest req, IAuthProvider auth) =>
{
    var playerInfo = new PlayerInfo { PlayerId = req.UserId, DisplayName = req.Username };
    var token = await auth.GenerateTokenAsync(playerInfo, default);
    return Results.Ok(new { token });
});
```

---

### Q: Можно ли использовать NetworkSignalCore без Unity?

**A:** Да! `NetworkSignalCore.Client` — это netstandard2.1 библиотека, которая работает в любом .NET приложении: консольные игры, Godot (через Mono), Blazor и т.д. Unity-компоненты в папке `unity/` — это лишь удобные обёртки для MonoBehaviour.

---

### Q: Как отправить сообщение конкретной группе игроков (например, команде)?

**A:** Используйте `SendToGroupAsync`:

```csharp
// Добавляем игрока в группу команды при входе в игру
// (через SignalR Groups API или обёртку фреймворка)

// В NetworkController:
await SendToGroupAsync($"team-{teamId}", "TeamMessage", new { Text = "Вперёд!" });
```

---

### Q: Как тестировать сетевой код?

**A:** Используйте `Microsoft.AspNetCore.SignalR.Client.Testing` или создайте mock `ISessionManager` / `IRoomManager`:

```csharp
// Пример unit-теста контроллера
[Test]
public async Task Move_ShouldBroadcastToRoom()
{
    var sessionMock = new Mock<ISessionManager>();
    var roomMock = new Mock<IRoomManager>();

    sessionMock.Setup(s => s.GetPlayer(It.IsAny<string>()))
        .Returns(new PlayerInfo { PlayerId = "player1", DisplayName = "Test" });

    var controller = new MovementController(/* inject mocks */);
    // Тестируем логику...
}
```

---

### Q: Как обновить игровое состояние для всех игроков в комнате с заданной частотой?

**A:** Используйте `IHostedService` или `BackgroundService`:

```csharp
public class GameTickService : BackgroundService
{
    private readonly ISessionManager _sessions;
    private readonly IRoomManager _rooms;
    private readonly IHubContext<NetworkHub> _hub;
    private readonly TimeSpan _tickRate = TimeSpan.FromMilliseconds(50); // 20 Hz

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await ProcessGameTick();
            await Task.Delay(_tickRate, ct);
        }
    }

    private async Task ProcessGameTick()
    {
        foreach (var room in _rooms.GetPublicRooms())
        {
            if (room.State == RoomState.InGame)
            {
                // Собираем состояние всей комнаты и рассылаем
                var state = CollectRoomState(room);
                await _hub.Clients.Group(room.RoomId).SendAsync("WorldState", state);
            }
        }
    }
}

// Регистрация:
builder.Services.AddHostedService<GameTickService>();
```

---

### Q: Поддерживается ли SSL/TLS?

**A:** Да, это стандартная функция ASP.NET Core. Настройте HTTPS в `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "cert-password"
        }
      }
    }
  }
}
```

Клиент автоматически использует WSS при подключении по HTTPS URL:
```csharp
opt.ServerUrl = "https://game.myserver.com/network"; // → wss://
```

---

*NetworkSignalCore — сделано с ❤️ для разработчиков игр*
