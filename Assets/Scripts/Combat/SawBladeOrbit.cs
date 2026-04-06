using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 차량 주변을 회전하는 톱날 하나.
/// AutoAttack에서 생성/관리.
/// </summary>
public class SawBladeOrbit : MonoBehaviour
{
    public Transform center;
    public float orbitRadius = 2f;
    public float rotateSpeed = 200f;
    public float damage = 10f;
    public float damageInterval = 0.3f;
    public int bladeIndex;
    public int totalBlades = 1;

    private float angle;
    private Dictionary<int, float> hitTimers = new Dictionary<int, float>();

    private void Start()
    {
        angle = 360f / totalBlades * bladeIndex;
    }

    private void Update()
    {
        if (center == null) return;

        angle += rotateSpeed * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * orbitRadius;
        transform.position = center.position + offset;

        transform.Rotate(0, 0, -rotateSpeed * 2f * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        int id = other.GetEntityId();
        if (hitTimers.ContainsKey(id) && Time.time < hitTimers[id]) return;
        hitTimers[id] = Time.time + damageInterval;

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null)
            eh.TakeDamage(damage);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        hitTimers.Remove(other.GetEntityId());
    }
}
