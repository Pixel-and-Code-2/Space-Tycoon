# Отладка «клики мертвы, меню живое»

Симптом: Pause/Settings работают, сцена и GameUI — нет.

## Что сделать в Play mode

1. Hierarchy → объект с `UILayersController` → ПКМ по компоненту → **Dump Click Blockers**
2. В Console смотри:
   - `timeScale` — если `0`, пауза от оверлея
   - `overlayStack` — норма: только `GameUI`. Плохо: висит `Background` / `NarrativeText`
   - `bg[i] clickCatcher=True` при стеке GameUI — catcher ест клики
3. Уже пишутся логи `[UILayers] StopGame` / `ResumeGame` — сравни с моментом бага

Пришли вывод Dump — по нему сразу видно, какой слой блокирует.
