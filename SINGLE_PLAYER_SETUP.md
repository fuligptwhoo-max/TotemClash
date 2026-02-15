# Руководство по настройке Single Player режима

## Что было сделано

Все скрипты были обновлены для работы в локальном (одиночном) режиме:
- ✅ Удален FishNet и весь сетевой код
- ✅ Созданы локальные версии GameManager, PlayerController, HealthSystem и т.д.
- ✅ Добавлена система спавна ботов (BotSpawner)
- ✅ Обновлены все UI скрипты
- ✅ Удалены старые сетевые скрипты

## Что нужно сделать в Unity Editor

### 1. Обновление сцены MainMenu

1. Открой сцену `Assets/Scenes/MainMenu.unity`
2. Найди объект **NetworkManager** (или **MyNetworkManager**) в иерархии
3. **Удали** этот объект полностью
4. Найди объект с компонентом **LobbyManager**
5. Убедись что на нём есть:
   - LobbyManager (скрипт)
   - GameSettings (скрипт)
6. Убери ссылки на NetworkManager из инспектора LobbyManager
7. Сохрани сцену

### 2. Обновление сцены SampleScene (игровая сцена)

1. Открой сцену `Assets/Scenes/SampleScene.unity`
2. **Удали** следующие объекты:
   - NetworkManager (или MyNetworkManager)
   - Все объекты с NetworkObject компонентом
3. Найди объект **GameManager**
4. Замени компонент на новый **GameManager** (если еще не заменен)
5. Убедись что GameManager имеет ссылки на:
   - timerText (TMP_Text)
   - countdownDisplay (CountdownDisplay)
   - gameOverMenu (GameOverMenu)
   - totem (TotemController)

### 3. Настройка префаба игрока (Magician)

1. Открой префаб `Assets/Prefabs/Players/Magician/Magician.prefab`
2. **Удали** следующие компоненты:
   - NetworkObject
   - NetworkPlayerController (старый)
3. **Добавь** новые компоненты:
   - PlayerController (новый локальный)
   - PlayerCombat
   - HealthSystem
   - PlayerScore
   - PlayerTotemInteraction
   - MagicianClass
   - AIBotController (только для ботов!)
4. Настрой ссылки в инспекторе для каждого компонента
5. Примени изменения к префабу

### 4. Настройка спавна игрока и ботов

1. В игровой сцене создай пустой объект **"GameSetup"**
2. Добавь на него компонент **LocalGameSpawner**
3. Назначь:
   - playerPrefab (префаб Magician)
   - countdownDisplay (ссылка на CountdownDisplay в сцене)
4. Создай пустой объект **"BotSpawner"**
5. Добавь компонент **BotSpawner**
6. Настрой:
   - botPrefab (тот же префаб Magician, но будет настроен как бот)
   - botCount (количество ботов, например 3)
   - spawnPoints (точки спавна)

### 5. Настройка UI

1. Найди объект **PauseMenu** в сцене
2. Убери ссылки на:
   - networkManager
   - serverSettingsPanel (можно удалить эту панель)
3. Найди **GameOverMenu**
4. Убери ссылки на networkManager
5. Найди **CountdownDisplay**
6. Убедись что используется новый локальный CountdownDisplay

### 6. Настройка слоёв и тегов

1. Убедись что у игрока тег **"Player"**
2. У ботов должен быть тег **"Enemy"** и слой **"Enemy"**
3. Убедись что слои настроены в Physics Settings:
   - Projectile не сталкивается со слоем Projectile
   - Игрок и боты на разных слоях для корректного авто-прицеливания

### 7. Настройка CameraController

1. Найди камеру в сцене
2. Убедись что на ней есть **CameraController**
3. Настрой target на null (будет установлен автоматически при спавне игрока)

### 8. Настройка Totem

1. Найди тотем в сцене
2. Удали с него NetworkObject (если есть)
3. Убедись что есть **TotemController** (новый локальный)
4. Настрой:
   - Rigidbody (не kinematic по умолчанию)
   - Collider

## Проверка перед запуском

### В GameManager должны быть назначены:
- [ ] timerText
- [ ] countdownDisplay  
- [ ] gameOverMenu
- [ ] totem

### В LocalGameSpawner должны быть назначены:
- [ ] playerPrefab (Magician)
- [ ] countdownDisplay

### В BotSpawner должны быть назначены:
- [ ] botPrefab (Magician)
- [ ] botCount
- [ ] spawnPoints

### В PlayerController (на префабе игрока) должны быть назначены:
- [ ] animator
- [ ] characterController
- [ ] playerCombat
- [ ] aimingSystem
- [ ] healthSystem
- [ ] totemInteraction

## Запуск игры

1. Открой сцену **MainMenu**
2. Нажми **Play**
3. Нажми **"Играть"**
4. Должна загрузиться игровая сцена
5. Должен появиться обратный отсчет (3, 2, 1, GO!)
6. После GO! игрок и боты должны получить управление

## Отладка

Если что-то не работает:

1. **Игрок не спавнится**
   - Проверь что LocalGameSpawner имеет playerPrefab
   - Проверь что префаб не содержит NetworkObject

2. **Боты не спавнятся**
   - Проверь BotSpawner.spawnPoints
   - Проверь что в префабе есть AIBotController

3. **Не работает стрельба**
   - Проверь MagicianClass на игроке
   - Проверь что fireballPrefab назначен

4. **Не поднимается тотем**
   - Проверь TotemController на объекте тотема
   - Проверь PlayerTotemInteraction на игроке

5. **UI не работает**
   - Проверь что есть EventSystem в сцене
   - Проверь GraphicRaycaster на Canvas

## Список изменённых файлов

### Новые файлы:
- Assets/Scripts/Combat/PlayerController.cs (заменил NetworkPlayerController)
- Assets/Scripts/Combat/BotSpawner.cs
- Assets/Scripts/Combat/LocalGameSpawner.cs
- Assets/Scripts/Network/GameStateManager.cs (заменил GameStateNetworkSync)

### Обновлённые файлы:
- Assets/Scripts/Combat/GameManager.cs
- Assets/Scripts/Combat/HealthSystem.cs
- Assets/Scripts/Combat/PlayerCombat.cs
- Assets/Scripts/Combat/PlayerScore.cs
- Assets/Scripts/Combat/PlayerTotemInteraction.cs
- Assets/Scripts/Combat/TotemController.cs
- Assets/Scripts/Combat/AIBotController.cs
- Assets/Scripts/Combat/AimingSystem.cs
- Assets/Scripts/Classes/MagicianClass.cs
- Assets/Scripts/Combat/Projectiles/FireBallProjectile.cs
- Assets/Scripts/Combat/Projectiles/IceSpikeProjectile.cs
- Assets/Scripts/Combat/Projectiles/LightningProjectile.cs
- Assets/Scripts/Combat/Projectiles/MeteorProjectile.cs
- Assets/Scripts/Network/GameSettings.cs
- Assets/Scripts/UI/MainMenu.cs
- Assets/Scripts/UI/PauseMenu.cs
- Assets/Scripts/UI/GameOverMenu.cs
- Assets/Scripts/UI/CountdownDisplay.cs
- Assets/Scripts/UI/LobbyManager.cs
- Assets/Scripts/UI/LocalScoreDisplay.cs

### Удалённые файлы:
- Assets/FishNet/ (вся папка)
- Assets/Scripts/Network/MyNetworkManager.cs
- Assets/Scripts/Network/NetworkPlayerController.cs
- Assets/Scripts/Network/GameStateNetworkSync.cs
- Assets/Scripts/README-Migration.md
