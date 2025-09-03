using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public static Crosshair Instance { get; private set; }
    [SerializeField] private Vector3 movingScale;
    [SerializeField] private Vector3 aimingScale;
    [SerializeField] private Vector3 movingWhileAimingScale;
    [SerializeField] private Image crosshairImg;
    [SerializeField] private Sprite aimedSprite;
    [SerializeField] private Sprite unAimedSprite;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private float lerpSpeed = 10f;

    PlayerController playerController;
    PlayerWeapons playerWeapons;

    private Vector3 idleScale;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerController = PlayerController.Instance;
        playerWeapons = PlayerWeapons.Instance;
        idleScale = transform.localScale;
    }

    private void Update()
    {
        if (!playerWeapons.IsHoldingWeapon())
        {
            crosshairImg.sprite = emptySprite;
            return;
        }
        if (playerController.isSprinting)
        {
            crosshairImg.sprite = emptySprite;
        }
        else if (playerController.isMoving && playerWeapons.IsAiming())
        {
            crosshairImg.sprite = unAimedSprite;
            transform.localScale = Vector3.Lerp(transform.localScale, movingWhileAimingScale, Time.deltaTime * lerpSpeed);
        }
        else if (playerController.isMoving && !playerWeapons.IsAiming())
        {
            crosshairImg.sprite = unAimedSprite;
            transform.localScale = Vector3.Lerp(transform.localScale, movingScale, Time.deltaTime * lerpSpeed);

        }
        else if (playerWeapons.IsAiming())
        {
            crosshairImg.sprite = unAimedSprite;
            transform.localScale = Vector3.Lerp(transform.localScale, aimingScale, Time.deltaTime * lerpSpeed);
        }
        else
        {
            crosshairImg.sprite = unAimedSprite;
            transform.localScale = Vector3.Lerp(transform.localScale, idleScale, Time.deltaTime * lerpSpeed);
        }

        if ((transform.localScale - aimingScale).magnitude <= 0.1f)
        {
            crosshairImg.sprite = aimedSprite;
        }
    }

    public void SetVisibility(bool value)
    {
        crosshairImg.enabled = value;
    }
}
