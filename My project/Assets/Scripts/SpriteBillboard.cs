using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    [Header("Sprites for Directions")]
    [Tooltip("Order: 0:N (Up), 1:NE (Up-Right), 2:E (Right), 3:SE (Down-Right), 4:S (Down), 5:SW (Down-Left), 6:W (Left), 7:NW (Up-Left)")]
    [SerializeField] private Sprite[] directionalSprites;

    private SpriteRenderer spriteRenderer;
    private PlayerBase playerBase;
    private Camera mainCamera;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerBase = GetComponentInParent<PlayerBase>();
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Align the billboard's rotation to look in the same direction as the camera
        transform.rotation = mainCamera.transform.rotation;

        // If sprites are assigned, select the appropriate frame based on player direction
        if (playerBase != null && spriteRenderer != null && directionalSprites != null && directionalSprites.Length == 8)
        {
            Vector3 facing = playerBase.FacingDirection;
            int spriteIndex = GetDirectionIndex(facing);
            if (spriteIndex >= 0 && spriteIndex < directionalSprites.Length && directionalSprites[spriteIndex] != null)
            {
                spriteRenderer.sprite = directionalSprites[spriteIndex];
            }
        }
    }

    private int GetDirectionIndex(Vector3 dir)
    {
        // Calculate angle on XZ plane relative to Vector3.forward (North)
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (angle < 0)
        {
            angle += 360f;
        }

        // 360 degrees / 8 sectors = 45 degrees per sector.
        // Rounding divides the sectors centered around: 0, 45, 90, 135, 180, 225, 270, 315
        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return index;
    }
}
