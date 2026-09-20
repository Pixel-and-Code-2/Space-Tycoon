using UnityEngine;

public class CorpseFadeDespawn : MonoBehaviour
{
    public const float LingerSeconds = 5f;
    public const float FadeSeconds = 1.5f;

    [SerializeField]
    bool destroyWhenDone;

    float age;
    bool fading;
    bool done;
    Renderer[] renderers;
    CanvasGroup[] canvasGroups;
    float[] rendererAlphas;
    float[] canvasAlphas;
    MaterialPropertyBlock block;

    public static bool IsEnemyCorpse(GameObject go)
    {
        if (go == null) return false;
        if (go.GetComponent<PawnHealing>() != null || go.GetComponentInChildren<PawnHealing>(true) != null)
            return false;
        PawnDataController pdc = go.GetComponent<PawnDataController>();
        if (pdc != null && pdc.Stats != null)
        {
            string n = pdc.Stats.name ?? "";
            if (n.IndexOf("Enemy", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Tvar", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
        return go.name.StartsWith("Enemy") || go.name.StartsWith("EnemySpawned");
    }

    public static void BeginOn(GameObject go, bool destroyWhenDone)
    {
        if (go == null) return;
        if (!IsEnemyCorpse(go))
        {
            CorpseFadeDespawn existing = go.GetComponent<CorpseFadeDespawn>();
            if (existing != null)
                Object.Destroy(existing);
            return;
        }
        CorpseFadeDespawn c = go.GetComponent<CorpseFadeDespawn>();
        if (c == null) c = go.AddComponent<CorpseFadeDespawn>();
        c.destroyWhenDone = destroyWhenDone;
        c.Restart();
    }

    public void Restart()
    {
        StopAllCoroutines();
        age = 0f;
        fading = false;
        done = false;
        CacheVisuals();
        ApplyAlpha(1f);
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        enabled = true;
    }

    void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        rendererAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererAlphas[i] = 1f;
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer) continue;
            try
            {
                Material shared = r.sharedMaterial;
                if (shared != null && shared.HasProperty("_Color"))
                    rendererAlphas[i] = shared.color.a;
            }
            catch
            {
                rendererAlphas[i] = 1f;
            }
        }
        canvasAlphas = new float[canvasGroups.Length];
        for (int i = 0; i < canvasGroups.Length; i++)
            canvasAlphas[i] = canvasGroups[i] != null ? canvasGroups[i].alpha : 1f;
    }

    void Update()
    {
        if (done) return;
        age += Time.deltaTime;
        if (!fading)
        {
            if (age < LingerSeconds) return;
            fading = true;
            age = 0f;
            CacheVisuals();
            return;
        }
        float t = Mathf.Clamp01(age / FadeSeconds);
        ApplyAlpha(1f - t);
        if (t < 1f) return;
        done = true;
        Finish();
    }

    void ApplyAlpha(float a)
    {
        if (block == null) block = new MaterialPropertyBlock();
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;
                try
                {
                    Material shared = r.sharedMaterial;
                    if (shared == null || !shared.HasProperty("_Color")) continue;
                    Color c = shared.color;
                    c.a = rendererAlphas[i] * a;
                    r.GetPropertyBlock(block);
                    block.SetColor("_Color", c);
                    r.SetPropertyBlock(block);
                }
                catch
                {
                }
            }
        }
        if (canvasGroups != null)
        {
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] == null) continue;
                canvasGroups[i].alpha = canvasAlphas[i] * a;
            }
        }
    }

    void Finish()
    {
        if (destroyWhenDone)
        {
            Destroy(gameObject);
            return;
        }
        gameObject.SetActive(false);
        enabled = false;
    }

    public void OnRestoredFromSave()
    {
        Restart();
    }
}
