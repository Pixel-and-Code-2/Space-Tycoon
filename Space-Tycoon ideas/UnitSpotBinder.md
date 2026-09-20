# Unit Spot Binder — привязка врагов к точкам на карте

## Быстрый старт (обычный враг на карте)

1. Поставь префаб врага в `Enemies_lvlN` (или `Room*`) в нужной позиции.
2. **Space-Tycoon → Unit Spot → Bind All In Scene** — по позиции:
   - зарегистрирует в ближайший `listOfTriggers`
   - добавит в покрывающий `WarFog.othersToInclude` (проверка по XZ, Y тумана игнорируется)
3. Объект `UnitSpotBinder` **не обязателен** и в сцене сохранять не нужно.

Опционально (если нужны combatantStats / aiOverwrite при спавне из биндера):

1. **Create Binder At Selection** → Mode=`SceneEnemy`, prefab, stats, **Ai Overwrite**.
2. **Bind / Refresh Now** — создаст/подвинет инстанс и пропишет поля; биндер потом можно удалить и снова сделать Bind All.

## Карантин / delayed (отложенный спавн)

1. Точка под `DelayedTriggers/lvlN/DTrigN` как `PosM`, **или** биндер Mode=`Quarantine`.
2. В `TurnManager → listOfDelayedTriggers → enemySpawnPoints`: `enemy` + `where` (+ опционально `aiOverwrite`).
3. Сцена: префаб **не** лежит как враг — только `Pos`.

Конвертация уже стоящего врага:

**Space-Tycoon → Unit Spot → Convert Selected To Delayed Pose**  
(инстанс → `Pos` под ближайший DTrig, запись в spawnPoints, инстанс удаляется)

## Убрать выделенных

**Space-Tycoon → Unit Spot → Remove Selected** — стирает объект и снимает из triggers / delayed / WarFog.

## Кнопки меню

- **Create Binder At Selection** — опциональный авторский helper
- **Bind All In Scene** — биндеры (если есть) + все враги в `Enemies_lvl*` по позиции
- **Convert Selected To Delayed Pose**
- **Remove Selected**

## Окончание работы

**Space-Tycoon → Bake Default Save (Play Mode)** — и сохрани сцену.
