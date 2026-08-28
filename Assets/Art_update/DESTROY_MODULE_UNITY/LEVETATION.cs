using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Плавно вращает дочерние объекты по всем осям с плавно меняющейся случайной угловой скоростью.
/// </summary>
public class RandomRotation : MonoBehaviour
{
    [Header("Настройки вращения")]
    [Tooltip("Максимальная угловая скорость (градусов в секунду) по каждой оси")]
    public float maxAngularSpeed = 90f;

    [Tooltip("Интервал (в секундах) между сменой целевой скорости")]
    public float changeInterval = 3f;

    [Tooltip("Время плавного перехода к новой скорости (чем больше, тем плавнее)")]
    public float smoothTime = 1f;

    // Данные для каждого дочернего объекта
    private List<ChildData> children = new List<ChildData>();

    private void Start()
    {
        // Собираем все прямые дочерние трансформы
        foreach (Transform child in transform)
        {
            var data = new ChildData
            {
                transform = child,
                // начальная случайная скорость
                currentVelocity = Random.insideUnitSphere * maxAngularSpeed,
                targetVelocity = Vector3.zero,
                velocityRef = Vector3.zero,
                timer = Random.Range(0f, changeInterval) // случайное смещение, чтобы не синхронизироваться
            };
            data.targetVelocity = data.currentVelocity; // сразу задаём цель
            children.Add(data);
        }
    }

    private void Update()
    {
        foreach (var data in children)
        {
            if (data.transform == null) continue; // защита от удаления

            // Обновляем таймер и при необходимости меняем цель
            data.timer += Time.deltaTime;
            if (data.timer >= changeInterval)
            {
                data.timer = 0f;
                // Новая случайная скорость в пределах сферы радиуса maxAngularSpeed
                data.targetVelocity = Random.insideUnitSphere * maxAngularSpeed;
            }

            // Плавно подводим текущую скорость к целевой по каждой оси
            data.currentVelocity.x = Mathf.SmoothDamp(
                data.currentVelocity.x,
                data.targetVelocity.x,
                ref data.velocityRef.x,
                smoothTime
            );
            data.currentVelocity.y = Mathf.SmoothDamp(
                data.currentVelocity.y,
                data.targetVelocity.y,
                ref data.velocityRef.y,
                smoothTime
            );
            data.currentVelocity.z = Mathf.SmoothDamp(
                data.currentVelocity.z,
                data.targetVelocity.z,
                ref data.velocityRef.z,
                smoothTime
            );

            // Поворачиваем объект вокруг его локальных осей
            data.transform.Rotate(data.currentVelocity * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Вспомогательный класс для хранения состояния каждого ребёнка.
    /// </summary>
    [System.Serializable]
    private class ChildData
    {
        public Transform transform;
        public Vector3 currentVelocity; // текущая угловая скорость
        public Vector3 targetVelocity;  // целевая скорость (к которой стремимся)
        public Vector3 velocityRef;     // вспомогательная переменная для SmoothDamp
        public float timer;             // таймер до следующей смены цели
    }
}