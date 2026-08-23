# PawnBrain выделение материалом

В `PawnBrain.OnSelect` / `OnDeselect` (`Assets/Scripts/PawnBrain/PawnBrain.cs`) при выборе пawn:

```csharp
skinnedMeshRenderer.material = selectedMaterial;  // выбор
skinnedMeshRenderer.material = defaultMaterial;   // снятие выбора
```

Если на персонаже **новая текстурная модель**, а в `PawnBrain` остались старые `Z_A_IA_BLUE` / `Z_A_IA_GREEN` от placeholder-меша — при клике он перекрашивается.

**Fix:** оба поля = реальный `sharedMaterials[0]` с SkinnedMeshRenderer (или одинаковые). Тогда выбор визуально не меняет цвет.

Связано: [[Заменить модельку персонажа]].
