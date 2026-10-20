using System.Collections;
using UnityEngine;

public partial class EnemyController
{
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

    private IEnumerator DamageShake()
    {
        if (playerCamera == null)
        {
            damageShakeCoroutine = null;
            yield break;
        }

        // Cache the original camera position.
        Vector3 originalCamPos = playerCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            float strength = Mathf.Lerp(
                damageShakeMagnitude,
                0f,
                elapsed / damageShakeDuration
            );
            // Apply a random offset to the camera's position.
            Vector2 offset2D = Random.insideUnitCircle * strength;
            playerCamera.transform.position = originalCamPos + new Vector3(offset2D.x, offset2D.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore the original camera position.
        playerCamera.transform.position = originalCamPos;
        damageShakeCoroutine = null;
    }

    public void PlayDamageAnimation()
    {
        if (hasDied)
        {
            return;
        }

        animator.ResetTrigger("TakeDamage");
        animator.SetTrigger("TakeDamage");
    }

    public void FlashWhenDamaged()
    {
        if (hasDied || damageFlashMaterials == null)
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

    private IEnumerator DamageFlash()
    {
        SetFlashAmount(1f);
        yield return new WaitForSeconds(damageFlashDuration);
        SetFlashAmount(0f);
        damageFlashCoroutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        if (damageFlashMaterials == null)
        {
            return;
        }

        foreach (Material material in damageFlashMaterials)
        {
            if (material != null)
            {
                material.SetFloat(FlashAmount, amount);
            }
        }
    }

    private void SetupDamageFlashMaterials()
    {
        if (damageFlashMaterial == null || spriteRenderers == null)
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

    private void CleanupDamageFlashMaterials()
    {
        if (damageFlashMaterials == null)
        {
            return;
        }

        foreach (Material material in damageFlashMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
