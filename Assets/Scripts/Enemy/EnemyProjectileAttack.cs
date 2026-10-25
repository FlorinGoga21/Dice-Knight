using System.Collections;
using UnityEngine;

/// <summary>Optional projectile attack used by DiceDemon; other enemies retain their melee attack.</summary>
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyProjectileAttack : MonoBehaviour
{
    [SerializeField] private EnemyFireball projectilePrefab;
    [SerializeField] private Transform mouth;
    [SerializeField, Min(0f)] private float launchDelay = 0.18f;

    private EnemyController owner;
    private Coroutine windup;
    private EnemyFireball projectile;

    public bool IsAttacking => windup != null || projectile != null;

    private void Awake() => owner = GetComponent<EnemyController>();

    public void Fire(Transform target, IDamageable damageable, int damage, bool applyDamage)
    {
        if (IsAttacking || projectilePrefab == null || target == null || damageable == null) return;
        windup = StartCoroutine(LaunchAfterWindup(target, damageable, damage, applyDamage));
    }

    private IEnumerator LaunchAfterWindup(Transform target, IDamageable damageable, int damage, bool applyDamage)
    {
        yield return new WaitForSeconds(launchDelay);
        if (!owner.IsDead && target != null && !damageable.IsDead)
        {
            Vector3 position = mouth != null ? mouth.position : transform.position;
            projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
            owner.PlayProjectileLaunchSound();
            projectile.Launch(owner, target, damageable, damage, applyDamage);
        }
        windup = null;
    }

    private void OnDisable()
    {
        if (windup != null) StopCoroutine(windup);
        windup = null;
        if (projectile != null) Destroy(projectile.gameObject);
    }
}
