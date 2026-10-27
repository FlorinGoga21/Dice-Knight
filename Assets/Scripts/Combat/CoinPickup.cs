using System.Collections;
using UnityEngine;

/// <summary>
/// A coin that pops out upon an enemy's defeat, bounces in an arc,
/// and then flies towards the player's currency UI element.
/// </summary>
public class CoinPickup : MonoBehaviour
{
    private ICurrencyReceiver target;
    private int value;
    private float delay;
    private float jumpDuration;
    private Vector2 jumpOffset;
    private float jumpHeight;
    private float flightDuration;
    private Camera mainCamera;

    /// <summary>
    /// Initializes and starts the coin jump and flight sequence.
    /// </summary>
    public void Initialize(
        ICurrencyReceiver currencyReceiver,
        int currencyValue,
        float pickupDelay,
        float launchDuration,
        Vector2 launchOffset,
        float launchHeight,
        float duration)
    {
        target = currencyReceiver;
        value = currencyValue;
        delay = pickupDelay;
        jumpDuration = Mathf.Max(0.05f, launchDuration);
        jumpOffset = launchOffset;
        jumpHeight = Mathf.Max(0f, launchHeight);
        flightDuration = Mathf.Max(0.05f, duration);
        mainCamera = Camera.main;

        StartCoroutine(FlyToCurrency());
    }

    private IEnumerator FlyToCurrency()
    {
        yield return new WaitForSeconds(delay);

        if (IsTargetInvalid())
        {
            Destroy(gameObject);
            yield break;
        }

        // 1. Initial jump arc
        Vector3 startPosition = transform.position;
        Vector3 jumpEndPosition = GetJumpEndPosition(startPosition);
        float jumpElapsed = 0f;

        while (jumpElapsed < jumpDuration)
        {
            jumpElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(jumpElapsed / jumpDuration);
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = Vector3.Lerp(startPosition, jumpEndPosition, progress) + Vector3.up * arcHeight;
            yield return null;
        }
        transform.position = jumpEndPosition;

        // 2. Flight to currency counter
        startPosition = transform.position;
        float flightElapsed = 0f;

        while (flightElapsed < flightDuration)
        {
            if (IsTargetInvalid())
            {
                Destroy(gameObject);
                yield break;
            }

            flightElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(flightElapsed / flightDuration);
            Vector3 targetPosition = GetCurrencyWorldPosition();
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress * progress);
            yield return null;
        }

        // 3. Grant currency to target
        if (target != null && target.CanReceiveCurrency)
        {
            target.AddCurrency(value);
            GameAudio.Play(GameAudio.Cue.CoinPickup);
        }

        Destroy(gameObject);
    }

    private Vector3 GetJumpEndPosition(Vector3 startPosition)
    {
        Vector3 endPosition = startPosition + (Vector3)jumpOffset;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        // Measure the visible bottom so off-center sprite pivots also rest on the ground.
        float bottomOffset = spriteRenderer != null
            ? transform.position.y - spriteRenderer.bounds.min.y
            : 0f;
        Vector2 origin = new Vector2(endPosition.x,
            Mathf.Max(startPosition.y, endPosition.y) + Mathf.Max(0f, bottomOffset));
        RaycastHit2D ground = Physics2D.Raycast(origin, Vector2.down, Mathf.Infinity, LayerMask.GetMask("Ground"));
        if (ground.collider != null)
            endPosition.y = ground.point.y + bottomOffset;
        return endPosition;
    }

    private bool IsTargetInvalid()
    {
        return target == null || !target.CanReceiveCurrency || target.CurrencyTarget == null;
    }

    private Vector3 GetCurrencyWorldPosition()
    {
        Transform currencyTarget = target.CurrencyTarget;
        RectTransform rectTransform = currencyTarget as RectTransform;
        Canvas canvas = currencyTarget.GetComponentInParent<Canvas>();

        if (mainCamera == null || rectTransform == null || canvas == null)
        {
            return currencyTarget.position;
        }

        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        return mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera)
        );
    }
}
