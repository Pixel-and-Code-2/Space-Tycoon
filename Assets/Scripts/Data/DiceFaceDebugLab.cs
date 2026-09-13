using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DiceFaceDebugLab : MonoBehaviour
{
    [SerializeField]
    private DiceShapeConfig config;
    [SerializeField]
    private DiceFaceMap liveDie;
    [SerializeField]
    private bool throwOnStart = true;
    [SerializeField]
    private float throwForce = 6f;
    [SerializeField]
    private float spawnHeight = 3f;
    [SerializeField]
    private bool drawAlways = true;
    [SerializeField]
    private bool applyConfigEveryFrame = true;

    readonly List<TextMeshPro> labels = new List<TextMeshPro>();

    void Start()
    {
        EnsureDie();
        ApplyConfig();
        RebuildLabels();
        if (throwOnStart)
            StartCoroutine(ThrowRoutine());
    }

    void Update()
    {
        if (applyConfigEveryFrame)
            ApplyConfig();
        SyncLabels();
    }

    void OnDrawGizmos()
    {
        if (!drawAlways || config == null) return;
        Transform t = liveDie != null ? liveDie.transform : transform;
        DrawSticks(t, true);
    }

    void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Transform t = liveDie != null ? liveDie.transform : transform;
        DrawSticks(t, true);
    }

    public void EnsureDie()
    {
        if (liveDie != null) return;
        if (config != null && config.diePrefab != null)
        {
            liveDie = Instantiate(config.diePrefab, transform.position + Vector3.up * spawnHeight, Quaternion.identity);
            liveDie.name = "LabDie";
            return;
        }
        liveDie = GetComponentInChildren<DiceFaceMap>();
    }

    public void ApplyConfig()
    {
        if (config == null || liveDie == null) return;
        config.ApplyTo(liveDie);
    }

    IEnumerator ThrowRoutine()
    {
        yield return null;
        EnsureDie();
        ApplyConfig();
        if (liveDie == null) yield break;
        Rigidbody rb = liveDie.GetComponent<Rigidbody>();
        if (rb == null) rb = liveDie.gameObject.AddComponent<Rigidbody>();
        liveDie.transform.position = transform.position + Vector3.up * spawnHeight;
        liveDie.transform.rotation = Random.rotation;
        rb.linearVelocity = Random.onUnitSphere * throwForce + Vector3.down * 2f;
        rb.angularVelocity = Random.insideUnitSphere * 10f;
        yield return new WaitForSeconds(3f);
        if (liveDie != null)
        {
            var r = liveDie.ReadUp(sumOnEdge: true);
            Debug.Log("DiceLab read value=" + r.value + " kind=" + r.kind + " secondary=" + r.secondaryValue, liveDie);
        }
    }

    void RebuildLabels()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
                DestroyImmediate(labels[i].gameObject);
        }
        labels.Clear();
        if (config == null || config.sticks == null) return;
        for (int i = 0; i < config.sticks.Count; i++)
        {
            GameObject go = new GameObject("StickLabel_" + i);
            go.transform.SetParent(transform, false);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.text = config.sticks[i].displayValue.ToString();
            labels.Add(tmp);
        }
    }

    void SyncLabels()
    {
        if (config == null || liveDie == null) return;
        if (labels.Count != (config.sticks != null ? config.sticks.Count : 0))
            RebuildLabels();
        Camera cam = Camera.main;
        for (int i = 0; i < labels.Count; i++)
        {
            TextMeshPro tmp = labels[i];
            if (tmp == null || i >= config.sticks.Count) continue;
            var stick = config.sticks[i];
            tmp.gameObject.SetActive(stick.draw);
            if (!stick.draw) continue;
            Vector3 localN = config.ResolveLocalNormal(stick);
            Vector3 worldN = liveDie.transform.TransformDirection(localN);
            Vector3 tip = liveDie.transform.position + worldN * config.stickLength;
            tmp.transform.position = tip;
            tmp.text = stick.displayValue.ToString();
            float s = config.labelScale;
            tmp.transform.localScale = new Vector3(s, s, s);
            if (cam != null)
                tmp.transform.rotation = Quaternion.LookRotation(tmp.transform.position - cam.transform.position);
        }
    }

    void DrawSticks(Transform t, bool gizmo)
    {
        if (config.sticks == null) return;
        for (int i = 0; i < config.sticks.Count; i++)
        {
            var stick = config.sticks[i];
            if (stick == null || !stick.draw) continue;
            Vector3 localN = config.ResolveLocalNormal(stick);
            Vector3 worldN = t.TransformDirection(localN);
            Vector3 origin = t.position;
            Vector3 tip = origin + worldN * config.stickLength;
            Gizmos.color = stick.color;
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawSphere(tip, 0.03f);
        }
    }
}
