# Миграция с Mirror на FishNet - Исправленная версия

## 📦 Установка FishNet

### Способ 1: Unity Package Manager (рекомендуется)

1. Открой **Window → Package Manager**
2. Нажми **+** → **Add package from git URL**
3. Вставь: `https://github.com/FirstGearGames/FishNet.git?path=/FishNet/Assets/FishNet`
4. Нажми **Add**

### Способ 2: Asset Store

1. Открой **Window → Asset Store**
2. Найди "FishNet - Free Open Source Networking"
3. Импортируй в проект

## 🔧 Настройка сцены

### 1. Удаление Mirror

1. Удали **NetworkManager** Mirror со сцены
2. Удали все **NetworkIdentity** с префабов
3. Удали папку Mirror если она есть (сделай бэкап!)

### 2. Создание NetworkManager FishNet

1. Создай пустой GameObject: **GameObject → Create Empty**
2. Назови его **NetworkManager**
3. Добавь компоненты:
   - **NetworkManager** (FishNet) - главный компонент
   - **MyNetworkManager** (из Scripts-FishNet/Network) - твой кастомный менеджер
   - **Transport** (выбери один):
     - **Tugboat** (TCP - для локальной разработки)
     - **Bayou** (UDP - для production)

4. Настрой Transport:
   - **Address**: `localhost` (для теста)
   - **Port**: `7777`

**ВАЖНО**: В отличие от Mirror, в FishNet `NetworkManager` sealed (запечатан), поэтому `MyNetworkManager` наследуется от `MonoBehaviour` и работает через композицию.

### 3. Настройка Player Prefab

1. Открой префаб игрока
2. Удали **NetworkIdentity** (Mirror) если есть
3. Добавь **NetworkObject** (FishNet)
4. Добавь скрипты из **Scripts-FishNet**:
   - `NetworkPlayerController`
   - `PlayerCombat`
   - `HealthSystem`
   - `PlayerTotemInteraction`
   - `MagicianClass`
   - `PlayerScore`

5. Добавь префаб в список **Spawnable Prefabs** NetworkManager:
   - Выбери NetworkManager на сцене
   - В инспекторе найди **Player Spawner** или **Spawnable Prefabs**
   - Добавь префаб игрока

### 4. Настройка Totem Prefab

1. Открой префаб тотема
2. Удали **NetworkIdentity**
3. Добавь **NetworkObject**
4. Добавь `TotemController` из Scripts-FishNet
5. Добавь в **Spawnable Prefabs** NetworkManager

### 5. Настройка Projectile Prefabs

1. Для каждого префаба снаряда:
   - Удали **NetworkIdentity**
   - Добавь **NetworkObject**
   - Добавь соответствующий скрипт из **Scripts-FishNet/Combat/Projectiles**
   - Добавь в **Spawnable Prefabs**

### 6. Настройка Camera

1. На камере должен быть **CameraController** из Scripts-FishNet
2. Убедись, что CameraController **НЕ** наследуется от NetworkBehaviour
3. Камера сама найдет локального игрока

### 7. GameManager

1. Создай пустой объект **GameManager**
2. Добавь **GameManager** (обычный MonoBehaviour)
3. Создай дочерний объект **NetworkSync**
4. На NetworkSync добавь **GameStateNetworkSync** (NetworkBehaviour)
5. Свяжи ссылки в инспекторе

## 🔄 Основные изменения Mirror → FishNet

### Ключевые отличия API:

| Mirror | FishNet |
|--------|---------|
| `using Mirror;` | `using FishNet.Object;` |
| `NetworkBehaviour` | `NetworkBehaviour` (тот же класс) |
| `isServer` | `base.IsServer` |
| `isClient` | `base.IsClient` |
| `isLocalPlayer` | `base.IsOwner` |
| `netId` (uint) | `ObjectId` (int) |
| `[Command]` | `[ServerRpc]` |
| `[ClientRpc]` | `[ObserversRpc]` |
| `[TargetRpc]` | `[TargetRpc]` |
| `connectionToClient` | `base.Owner` |
| `NetworkServer.Spawn()` | `base.ServerManager.Spawn()` |
| `NetworkServer.Destroy()` | `base.ServerManager.Despawn()` |
| `NetworkIdentity` | `NetworkObject` |

### Изменения в SyncVar:

**Было (Mirror):**
```csharp
[SyncVar(hook = nameof(OnHealthChanged))]
public float currentHealth = 100f;

void OnHealthChanged(float oldValue, float newValue) { }
```

**Стало (FishNet):**
```csharp
[SyncVar(OnChange = nameof(OnHealthChanged))]
public float currentHealth = 100f;

void OnHealthChanged(float prev, float next, bool asServer) { }
```

### NetworkConnection:

**Было:**
```csharp
using Mirror;
[TargetRpc]
void TargetMethod(NetworkConnection target) { }
```

**Стало:**
```csharp
using FishNet.Connection;
[TargetRpc]
void TargetMethod(NetworkConnection target) { }
```

### NetworkManager:

**Было:**
```csharp
public class MyNetworkManager : NetworkManager { } // Наследование
```

**Стало:**
```csharp
public class MyNetworkManager : MonoBehaviour 
{ 
    // Композиция - получаем ссылку на FishNet NetworkManager
    private NetworkManager _networkManager;
    
    void Awake() 
    {
        _networkManager = GetComponent<NetworkManager>();
    }
}
```

## 🚀 Запуск

### Сервер:
1. Нажми **Play** в редакторе
2. В инспекторе NetworkManager нажми **Start Server** (или запусти из кода)

### Клиент:
1. Собери билд или открой второй редактор
2. Нажми **Start Client**
3. Для локального теста: `localhost:7777`

## 🐛 Возможные ошибки и решения

### Ошибка: "The name 'Channel' does not exist"
**Решение**: Убери `Channel = Channel.Reliable` из `[SyncVar]`. В новой версии FishNet это не нужно.

### Ошибка: "cannot derive from sealed type 'NetworkManager'"
**Решение**: Не наследуйся от `NetworkManager`, используй композицию как в примере `MyNetworkManager`.

### Ошибка: "The type or namespace name 'NetworkConnection' could not be found"
**Решение**: Добавь `using FishNet.Connection;` в начало файла.

### Ошибка: "The type or namespace name 'Mirror' could not be found"
**Решение**: Убери `using Mirror;` и замени на `using FishNet.Object;`

## 📁 Структура папок

```
Assets/
├── Scripts-FishNet/           ← Новые скрипты
│   ├── Network/
│   │   ├── MyNetworkManager.cs
│   │   └── NetworkPlayerController.cs
│   ├── Combat/
│   │   ├── PlayerCombat.cs
│   │   ├── HealthSystem.cs
│   │   ├── PlayerTotemInteraction.cs
│   │   ├── TotemController.cs
│   │   ├── PlayerScore.cs
│   │   ├── GameManager.cs
│   │   ├── AimingSystem.cs
│   │   ├── CameraController.cs
│   │   └── Projectiles/
│   ├── Classes/
│   │   └── MagicianClass.cs
│   └── UI/
│       └── TotemPickUpUI.cs
└── ... (остальные папки)
```

## ❌ Удаление старых скриптов

После успешной миграции:
1. Удали папку `Assets/Mirror`
2. Удали старые скрипты из `Assets/Scripts`
3. Переименуй `Assets/Scripts-FishNet` → `Assets/Scripts`

## 📞 Поддержка FishNet

- Документация: https://fish-networking.gitbook.io/docs/
- Discord: https://discord.gg/TfhZz69
- GitHub: https://github.com/FirstGearGames/FishNet
