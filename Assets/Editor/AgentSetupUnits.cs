using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class AgentSetupUnits
{
    const string WeaponPath = "Assets/Resources/WeaponEnemyShooter.asset";
    const string EnemyDataPath = "Assets/Resources/EnemyTvarShooter.asset";
    const string EnemyPrefabPath = "Assets/Prefabs/EnemyShooter.prefab";

    public static string RunSetup()
    {
        var sb = new StringBuilder();
        CreateAssets(sb);
        CreateEnemyShooterPrefab(sb);
        ReplaceAllyModel("Gusev", "Assets/Art_update/UPDATE_AUGUST/ENGINEER_SNIPER_ANIM.fbx", "Assets/Scripts/AnimatorBrain/PlayerController.controller", false, sb);
        ReplaceAllyModel("Zeleniy", "Assets/Art_update/UPDATE_AUGUST/ENGINEER_PISTOL_ANIM.fbx", "Assets/Scripts/AnimatorBrain/PlayerControllerZelen.controller", false, sb);
        PlaceShooterInScene(sb);
        CleanupRootModels(sb);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    static void CreateAssets(StringBuilder sb)
    {
        if (!AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>(WeaponPath))
        {
            var weaponSrc = AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>("Assets/Resources/WeaponTKBK.asset");
            var weapon = Object.Instantiate(weaponSrc);
            weapon.name = "WeaponEnemyShooter";
            SetParam(weapon, "MINDMG", 5f);
            SetParam(weapon, "MAXDMG", 14f);
            SetParam(weapon, "ACC", 3f);
            SetParam(weapon, "ROF", 3f);
            SetParam(weapon, "MAG", 6f);
            SetParam(weapon, "TotalAmmo", 120f);
            AssetDatabase.CreateAsset(weapon, WeaponPath);
            sb.AppendLine("created " + WeaponPath);
        }

        if (!AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>(EnemyDataPath))
        {
            var enemySrc = AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>("Assets/Resources/EnemyTvar Small.asset");
            var enemy = Object.Instantiate(enemySrc);
            enemy.name = "EnemyTvarShooter";
            SetParam(enemy, "SHT", 6f);
            SetParam(enemy, "BRW", 4f);
            var so = new SerializedObject(enemy);
            var must = so.FindProperty("mustHaveParameters");
            var weapon = AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>(WeaponPath);
            if (must.arraySize > 0)
                must.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            AssetDatabase.CreateAsset(enemy, EnemyDataPath);
            sb.AppendLine("created " + EnemyDataPath);
        }
    }

    static void SetParam(ParameteredScriptableObject asset, string name, float value)
    {
        var so = new SerializedObject(asset);
        var list = so.FindProperty("parameters");
        for (int i = 0; i < list.arraySize; i++)
        {
            var el = list.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("name").stringValue == name)
            {
                el.FindPropertyRelative("value").floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                return;
            }
        }
    }

    static void CreateEnemyShooterPrefab(StringBuilder sb)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath))
        {
            sb.AppendLine("prefab exists " + EnemyPrefabPath);
            return;
        }

        var normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyNormal.prefab");
        var temp = (GameObject)PrefabUtility.InstantiatePrefab(normalPrefab);
        temp.name = "EnemyShooter";
        temp.layer = LayerMask.NameToLayer("Enemy");

        var data = AssetDatabase.LoadAssetAtPath<ParameteredScriptableObject>(EnemyDataPath);
        var dc = temp.GetComponent<PawnDataController>();
        if (dc != null)
        {
            var so = new SerializedObject(dc);
            so.FindProperty("initialPawnData").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        ReplaceModelOnPawn(temp,
            "Assets/Art_update/UPDATE_AUGUST/ENEMY_SHOOTER_ANIM.fbx",
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/AnimatorBrain/PlayerController.controller"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Art_update/UPDATE_AUGUST/ENEMY_SHOOTER_T.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Art_update/UPDATE_AUGUST/ENEMY_SHOOTER_T.mat"),
            true);

        var brain = temp.GetComponent<PawnBrain>();
        if (brain != null)
        {
            var so = new SerializedObject(brain);
            so.FindProperty("shootSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Media/SoundsCombat/Shoot.wav");
            so.FindProperty("reloadSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Media/SoundsCombat/Reload.mp3");
            so.FindProperty("noAmmoSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Media/SoundsCombat/NoAmmo.mp3");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(temp, EnemyPrefabPath);
        Object.DestroyImmediate(temp);
        sb.AppendLine("created " + EnemyPrefabPath);
    }

    static void PlaceShooterInScene(StringBuilder sb)
    {
        if (GameObject.Find("EnemyShooter_Test"))
        {
            sb.AppendLine("EnemyShooter_Test already in scene");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        var parent = GameObject.Find("Enemies_lvl1");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "EnemyShooter_Test";
        if (parent != null)
            instance.transform.SetParent(parent.transform, false);
        instance.transform.position = new Vector3(-2.5f, 0f, -5.5f);
        instance.transform.rotation = Quaternion.identity;

        var pawnBrain = instance.GetComponent<PawnBrain>();
        var tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm != null && pawnBrain != null)
        {
            var so = new SerializedObject(tm);
            var triggers = so.FindProperty("listOfTriggers");
            if (triggers.arraySize > 0)
            {
                var enemies = triggers.GetArrayElementAtIndex(0).FindPropertyRelative("enemies");
                int idx = enemies.arraySize;
                enemies.InsertArrayElementAtIndex(idx);
                enemies.GetArrayElementAtIndex(idx).objectReferenceValue = pawnBrain;
                so.ApplyModifiedPropertiesWithoutUndo();
                sb.AppendLine("registered in TurnManager trigger0 enemies[" + idx + "]");
            }
        }

        var ai = Object.FindFirstObjectByType<SimpleEnemyAI>();
        if (ai != null && pawnBrain != null)
        {
            ai.AddPawnToScenario(pawnBrain);
            EditorUtility.SetDirty(ai);
            sb.AppendLine("registered in SimpleEnemyAI via AddPawnToScenario");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void ReplaceAllyModel(string pawnName, string fbxPath, string controllerPath, bool isEnemy, StringBuilder sb)
    {
        var pawn = GameObject.Find(pawnName);
        if (pawn == null)
        {
            sb.AppendLine("missing pawn " + pawnName);
            return;
        }

        ReplaceModelOnPawn(pawn, fbxPath,
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath),
            null, null, isEnemy);
        if (!isEnemy)
            BindPawnMaterialsToMesh(pawn);
        sb.AppendLine("replaced model on " + pawnName);
    }

    public static string FixPostSetup()
    {
        var sb = new StringBuilder();
        foreach (var fbx in new[]
        {
            "Assets/Art_update/UPDATE_AUGUST/ENGINEER_SNIPER_ANIM.fbx",
            "Assets/Art_update/UPDATE_AUGUST/ENGINEER_PISTOL_ANIM.fbx",
            "Assets/Art_update/UPDATE_AUGUST/ENEMY_SHOOTER_ANIM.fbx",
            "Assets/Art_update/ANIM_FIX/ENGINEER_2_RELEASE_FIX.fbx",
            "Assets/Art_update/COMMAND/ENGINEER_1_RELEASE_FIX.fbx",
            "Assets/Art_update/COMMAND/ENGINEER_1_RELEASE.fbx",
        })
            FixLoopingClips(fbx, sb);
        FixWalkSpeed("Assets/Scripts/AnimatorBrain/PlayerController.controller", sb);
        FixWalkSpeed("Assets/Scripts/AnimatorBrain/PlayerControllerZelen.controller", sb);
        FixWalkSpeed("Assets/Scripts/AnimatorBrain/ZayaController.controller", sb);
        foreach (var n in new[] { "Gusev", "Zeleniy", "EnemyShooter_Test" })
            FixModelRotation(n, sb);
        FixEnemyShooterPrefabRotation(sb);
        foreach (var n in new[] { "Gusev", "Zeleniy" })
            BindPawnMaterialsToMesh(GameObject.Find(n), sb, n);
        FixAnimatorOverrides("Gusev",
            "Assets/Art_update/UPDATE_AUGUST/ENGINEER_SNIPER_ANIM.fbx",
            "Assets/Resources/AnimatorOverrides/GusevAnimator.overrideController",
            "Assets/Scripts/AnimatorBrain/PlayerController.controller", sb);
        FixAnimatorOverrides("Zeleniy",
            "Assets/Art_update/UPDATE_AUGUST/ENGINEER_PISTOL_ANIM.fbx",
            "Assets/Resources/AnimatorOverrides/ZeleniyAnimator.overrideController",
            "Assets/Scripts/AnimatorBrain/PlayerControllerZelen.controller", sb);
        FixAnimatorOverridesOnPrefab(EnemyPrefabPath,
            "Assets/Art_update/UPDATE_AUGUST/ENEMY_SHOOTER_ANIM.fbx",
            "Assets/Resources/AnimatorOverrides/EnemyShooterAnimator.overrideController",
            "Assets/Scripts/AnimatorBrain/PlayerController.controller", sb);
        RegisterWarFogForStartingEnemies("EnemyShooter_Test", "Enemy1.2", sb);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    static void FixAnimatorOverrides(string pawnName, string fbxPath, string overridePath, string baseControllerPath, StringBuilder sb)
    {
        var pawn = GameObject.Find(pawnName);
        if (pawn == null)
        {
            sb.AppendLine("missing pawn for anim " + pawnName);
            return;
        }
        var model = FindModelRoot(pawn);
        if (model == null)
        {
            sb.AppendLine("missing model for anim " + pawnName);
            return;
        }
        var animator = model.GetComponent<Animator>();
        if (animator == null)
        {
            sb.AppendLine("missing animator for anim " + pawnName);
            return;
        }
        var aoc = CreateOrUpdateOverride(overridePath, baseControllerPath, fbxPath, sb);
        if (aoc == null) return;
        animator.runtimeAnimatorController = aoc;
        BindAnimatorBrain(pawn, model, sb, pawnName);
        EditorUtility.SetDirty(animator);
    }

    static void FixAnimatorOverridesOnPrefab(string prefabPath, string fbxPath, string overridePath, string baseControllerPath, StringBuilder sb)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var model = FindModelRoot(root);
        if (model != null)
        {
            var animator = model.GetComponent<Animator>();
            var aoc = CreateOrUpdateOverride(overridePath, baseControllerPath, fbxPath, sb);
            if (animator != null && aoc != null)
            {
                animator.runtimeAnimatorController = aoc;
                BindAnimatorBrain(root, model, sb, root.name);
                EditorUtility.SetDirty(animator);
            }
        }
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        var sceneEnemy = GameObject.Find("EnemyShooter_Test");
        if (sceneEnemy != null)
            FixAnimatorOverrides("EnemyShooter_Test", fbxPath, overridePath, baseControllerPath, sb);
    }

    static GameObject FindModelRoot(GameObject pawn)
    {
        foreach (Transform child in pawn.transform)
        {
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                return child.gameObject;
        }
        return null;
    }

    static AnimatorOverrideController CreateOrUpdateOverride(string overridePath, string baseControllerPath, string fbxPath, StringBuilder sb)
    {
        var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
        if (baseController == null)
        {
            sb.AppendLine("missing base controller " + baseControllerPath);
            return null;
        }
        EnsureFolder("Assets/Resources/AnimatorOverrides");
        var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (aoc == null)
        {
            aoc = new AnimatorOverrideController { runtimeAnimatorController = baseController };
            AssetDatabase.CreateAsset(aoc, overridePath);
            sb.AppendLine("created " + overridePath);
        }
        else
        {
            aoc.runtimeAnimatorController = baseController;
        }

        var newClips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__"))
            .ToList();
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var baseClip in aoc.animationClips)
        {
            var replacement = FindMatchingClip(newClips, baseClip.name);
            if (replacement != null)
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseClip, replacement));
        }
        aoc.ApplyOverrides(overrides);
        EditorUtility.SetDirty(aoc);
        sb.AppendLine("override " + overridePath + " clips=" + overrides.Count);
        return aoc;
    }

    static AnimationClip FindMatchingClip(List<AnimationClip> clips, string baseClipName)
    {
        foreach (var clip in clips)
        {
            if (clip.name == baseClipName) return clip;
            int pipe = clip.name.LastIndexOf('|');
            if (pipe >= 0 && clip.name.Substring(pipe + 1) == baseClipName) return clip;
        }
        foreach (var clip in clips)
        {
            if (clip.name.EndsWith(baseClipName)) return clip;
        }
        if (baseClipName.Length == 3 && baseClipName.Contains("_"))
        {
            string expanded = baseClipName[0] + "_" + BaseSuffix(baseClipName);
            foreach (var clip in clips)
            {
                if (clip.name.EndsWith(expanded)) return clip;
            }
        }
        return null;
    }

    static string BaseSuffix(string shortName)
    {
        switch (shortName)
        {
            case "4_I": return "IDLE";
            case "1_M": return "MOVE";
            case "2_A": return "ATTACK";
            case "3_D": return "DEATH";
            case "5_H": return "HIT";
            default: return shortName.Substring(2);
        }
    }

    static void BindAnimatorBrain(GameObject pawn, GameObject model, StringBuilder sb, string label)
    {
        var pawnBrain = pawn.GetComponent<PawnBrain>();
        var animatorBrain = model.GetComponent<AnimatorBrainPlayer>();
        if (pawnBrain == null || animatorBrain == null) return;
        var so = new SerializedObject(pawnBrain);
        so.FindProperty("animatorBrain").objectReferenceValue = animatorBrain;
        so.ApplyModifiedPropertiesWithoutUndo();
        sb.AppendLine("animatorBrain " + label);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static void FixLoopingClips(string fbxPath, StringBuilder sb)
    {
        int changed = 0;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            var clip = obj as AnimationClip;
            if (clip == null || clip.name.StartsWith("__")) continue;
            if (!clip.name.Contains("4_IDLE") && !clip.name.Contains("1_MOVE")) continue;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime) continue;
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            changed++;
        }
        if (changed == 0) return;
        AssetDatabase.SaveAssets();
        sb.AppendLine("loop " + fbxPath + " clips=" + changed);
    }

    static void FixWalkSpeed(string controllerPath, StringBuilder sb)
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null) return;
        bool changed = false;
        foreach (var layer in controller.layers)
        {
            foreach (var child in layer.stateMachine.states)
            {
                if (child.state.name != "1_MOVE" && child.state.name != "1_M") continue;
                if (Mathf.Approximately(child.state.speed, 1f / 2.7f)) continue;
                child.state.speed = 1f / 2.7f;
                changed = true;
            }
        }
        if (!changed) return;
        EditorUtility.SetDirty(controller);
        sb.AppendLine("walk speed " + controllerPath);
    }

    static void FixModelRotation(string pawnName, StringBuilder sb)
    {
        var pawn = GameObject.Find(pawnName);
        if (pawn == null) return;
        foreach (Transform child in pawn.transform)
        {
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() == null) continue;
            child.localRotation = Quaternion.identity;
            sb.AppendLine("rotation " + pawnName + "/" + child.name);
        }
    }

    static void FixEnemyShooterPrefabRotation(StringBuilder sb)
    {
        var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        foreach (Transform child in root.transform)
        {
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() == null) continue;
            child.localRotation = Quaternion.identity;
        }
        PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        sb.AppendLine("prefab rotation " + EnemyPrefabPath);
    }

    static void BindPawnMaterialsToMesh(GameObject pawn, StringBuilder sb, string label = null)
    {
        if (pawn == null) return;
        var smr = pawn.GetComponentInChildren<SkinnedMeshRenderer>();
        var brain = pawn.GetComponent<PawnBrain>();
        if (smr == null || brain == null) return;
        Material mat = smr.sharedMaterials.Length > 0 ? smr.sharedMaterials[0] : null;
        var so = new SerializedObject(brain);
        so.FindProperty("defaultMaterial").objectReferenceValue = mat;
        so.FindProperty("selectedMaterial").objectReferenceValue = mat;
        so.FindProperty("skinnedMeshRenderer").objectReferenceValue = smr;
        so.ApplyModifiedPropertiesWithoutUndo();
        sb.AppendLine("materials " + (label ?? pawn.name));
    }

    static void RegisterWarFogForStartingEnemies(string enemyName, string referenceEnemyName, StringBuilder sb)
    {
        var enemy = GameObject.Find(enemyName);
        var reference = GameObject.Find(referenceEnemyName);
        if (enemy == null || reference == null) return;
        foreach (var wf in Object.FindObjectsByType<WarFog>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(wf);
            var list = so.FindProperty("othersToInclude");
            bool hasRef = false;
            bool hasEnemy = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                var go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (go == reference) hasRef = true;
                if (go == enemy) hasEnemy = true;
            }
            if (!hasRef || hasEnemy) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = enemy;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wf);
            sb.AppendLine("war fog " + wf.gameObject.name + " +" + enemyName);
        }
    }

    static void BindPawnMaterialsToMesh(GameObject pawn)
    {
        BindPawnMaterialsToMesh(pawn, null, null);
    }

    static void ReplaceModelOnPawn(GameObject pawn, string fbxPath, RuntimeAnimatorController controller, Material defaultMat, Material selectedMat, bool isEnemy)
    {
        var keep = new HashSet<string> { "PlayerStatusCircle", "EndPoint", "PathStart", "PathEnd" };
        var remove = new List<GameObject>();
        foreach (Transform child in pawn.transform)
        {
            if (keep.Contains(child.name)) continue;
            if (child.name.StartsWith("Path")) continue;
            if (child.GetComponent<LineRenderer>() != null) continue;
            if (child.GetComponent<Animator>() != null || child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                remove.Add(child.gameObject);
        }
        foreach (var go in remove)
            Object.DestroyImmediate(go);

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        model.transform.SetParent(pawn.transform, false);
        model.transform.localPosition = new Vector3(0f, -0.84f, 0f);
        model.transform.localRotation = Quaternion.identity;
        model.transform.SetAsFirstSibling();
        SetLayerRecursively(model, 10);

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        if (model.GetComponent<AnimatorBrainPlayer>() == null)
            model.AddComponent<AnimatorBrainPlayer>();

        var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
        var pawnBrain = pawn.GetComponent<PawnBrain>();
        if (pawnBrain != null)
        {
            var so = new SerializedObject(pawnBrain);
            so.FindProperty("skinnedMeshRenderer").objectReferenceValue = smr;
            if (defaultMat != null) so.FindProperty("defaultMaterial").objectReferenceValue = defaultMat;
            if (selectedMat != null) so.FindProperty("selectedMaterial").objectReferenceValue = selectedMat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    static void CleanupRootModels(StringBuilder sb)
    {
        foreach (var n in new[] { "ENEMY_SHOOTER_ANIM", "ENGINEER_PISTOL_ANIM", "ENGINEER_SNIPER_ANIM", "ENGINEER_1_RELEASE_FIX", "ENGINEER_2_RELEASE_FIX" })
        {
            var go = GameObject.Find(n);
            if (go != null && go.transform.parent == null)
            {
                Object.DestroyImmediate(go);
                sb.AppendLine("removed root " + n);
            }
        }
    }
}
