using System.Collections;
using UnityEngine;

public partial class PlayerController
{
    public void FlashWhenDamaged()
    {
        if (hasDied)
        {
            return;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(DamageFlash());
        ShakeWhenDamaged();
    }

    public void ShakeWhenDamaged()
    {
        if (hasDied || playerCamera == null || damageShakeDuration <= 0f || damageShakeMagnitude <= 0f)
        {
            return;
        }

        if (damageShakeCoroutine != null)
        {
            StopCoroutine(damageShakeCoroutine);
        }

        damageShakeCoroutine = StartCoroutine(DamageShake());
    }

    private IEnumerator DamageFlash()
    {
        SetFlashAmount(1f);
        yield return new WaitForSeconds(damageFlashDuration);
        SetFlashAmount(0f);
        damageFlashCoroutine = null;
    }

    private IEnumerator DamageShake()
    {
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            float strength = Mathf.Lerp(
                damageShakeMagnitude,
                0f,
                elapsed / damageShakeDuration
            );
            damageShakeOffset = (Vector3)(Random.insideUnitCircle * strength);
            elapsed += Time.deltaTime;
            yield return null;
        }

        damageShakeOffset = Vector3.zero;
        damageShakeCoroutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        if (damageFlashMaterials == null)
        {
            return;
        }

        foreach (Material material in damageFlashMaterials)
        {
            material.SetFloat(FlashAmount, amount);
        }
    }

    private void SetupDamageFlashMaterials()
    {
        if (damageFlashMaterial == null)
        {
            return;
        }

        damageFlashMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Material material = new Material(damageFlashMaterial);
            spriteRenderers[i].material = material;
            damageFlashMaterials[i] = material;
        }
    }
}
