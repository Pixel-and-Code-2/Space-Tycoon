# Unit Spot Binder — привязка врагов к точкам на карте

## Быстрый старт (обычный враг на карте)

1. Пустой объект в нужном месте
  или **Space-Tycoon → Unit Spot → Create Binder At Selection**.
2. `UnitSpotBinder`:
  - **Mode** = `SceneEnemy`
  - **Enemy Prefab** = `EnemyShooter` / `EnemyNormal` / …
  - **Combatant Stats** (опционально) = ассет из `Resources/Combatants/`
3. **Bind / Refresh Now** (или OnValidate после сохранения).

## Карантин (отложенный спавн)

1. Точка + `UnitSpotBinder`, **Mode** = `Quarantine`.
2. **Enemy Prefab** = префаб на старт карантина.
3. Bind: точка уезжает под нужный `DTrigN` как `PosN`, Y = 0.
4. В сцене префаб **не** инстанцируется.
5. В `TurnManager → listOfDelayedTriggers → enemySpawnPoints`: `enemy` + `where` = эта точка.



## Убрать выделенных

**Space-Tycoon → Unit Spot → Remove Selected**

- Выделяем персонажа или его часть
- Нажимаем кнопку в меню
- Он стирается и дерегистрируется ото всего
- Все персонажи лежат отсортированные в MainComponent > Enemies_lvl объектах
- Все подготовленные к карантину персонажи лежат в виде MainComponent > DelayedTriggers > lvlN > DTrig1 > PosM. Нажав на PosM объект, а затем кнопку F можно легко увидеть где заспавнится враг. Убирание работает на этом объекте.

## Кнопки меню

- **Create Binder At Selection**
- **Bind All In Scene**
- **Remove Selected**

## Окончание работы

После того, как всё передвинуто и расставлено нужно сделать сохранение по умолчанию: **Space-Tycoon → Bake Default Save (Play Mode)** - редактор сам сделает итерацию сохранения.

