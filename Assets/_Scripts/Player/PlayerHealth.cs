using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using UnityEngine.Events;
using Unity.Cinemachine;
using FMODUnity;

public class PlayerHealth : MonoBehaviour, ISaveable
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Components")]
    [SerializeField] private GameObject eyes;
    [SerializeField] private GameObject torch;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private EventReference gruntSound;
    [SerializeField] private EventReference deathSound;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Effects")]
    [SerializeField] private Volume globalVolume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;

    private UnityAction Effect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (globalVolume == null)
        {
            Debug.LogError("No Volume assigned to PlayerHealth on " + name);
            enabled = false;
            return;
        }

        if (globalVolume.profile.TryGet(out Vignette vignette))
        {
            this.vignette = vignette;
        }
        if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            this.colorAdjustments = colorAdjustments;
        }
        if (globalVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
        {
            this.chromaticAberration = chromaticAberration;
        }
    }

    private void Update()
    {
        Effect?.Invoke();
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;
        SetHealth(currentHealth - amount);
        AudioManager.Instance.PlayOneShot(gruntSound, transform.position);
    }

    public void Heal(float amount)
    {
        SetHealth(currentHealth + amount);
    }

    private void SetHealth(float newHealth)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);

        float healthPct = currentHealth / maxHealth;

        if (AudioManager.Instance != null)
        {
            if (healthPct <= 0.4f && currentHealth > 0f)
                AudioManager.Instance.StartHeartbeat();
            else
                AudioManager.Instance.StopHeartbeat();
        }

        if (healthPct < 0.4f)
        {
            if (Mathf.Approximately(healthPct, 0f))
            {
                Death();
                return;
            }

            Effect = () =>
            {
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, 0.5f, Time.deltaTime * 2f);
                }
                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, -25f, Time.deltaTime * 2f);
                }
            };
        }
        else
        {
            Effect = () =>
            {
                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, 0f, Time.deltaTime * 2f);
                }
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, 0f, Time.deltaTime * 2f);
                }
            };
        }
    }


    private void Death()
    {
        Rigidbody eyesRB = eyes.GetComponent<Rigidbody>();
        Rigidbody torchRB = torch.GetComponent<Rigidbody>();
        eyesRB.isKinematic = false;
        eyesRB.AddForce(eyes.transform.forward, ForceMode.Impulse);
        torchRB.isKinematic = false;
        torchRB.AddForce(eyes.transform.forward, ForceMode.Impulse);

        if (chromaticAberration != null)
            DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, .5f, .8f);
        if (colorAdjustments != null)
            colorAdjustments.saturation.value = -25f;

        Effect = null;
        Effect = () =>
        {
            cam.Lens.Dutch = Mathf.Lerp(cam.Lens.Dutch, 3f, Time.deltaTime * 5f);
            panTilt.TiltAxis.Value = Mathf.Lerp(panTilt.TiltAxis.Value, -15f, Time.deltaTime * 5f);
        };
        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.HideHands();
        PlayerWeapons.Instance.DisableWeaponFunctions(true);
        DeathScreenUI.Instance.Show();
        AudioManager.Instance.StopAllPossibleInstances();
    }

    public void DropPlayerEyeObject()
    {
        AudioManager.Instance.PlayOneShot(gruntSound, transform.position);
        // Rigidbody eyesRB = eyes.GetComponent<Rigidbody>();
        // Rigidbody torchRB = torch.GetComponent<Rigidbody>();
        // eyesRB.isKinematic = false;
        // eyesRB.AddForce(eyes.transform.forward, ForceMode.Impulse);
        // torchRB.isKinematic = false;
        // torchRB.AddForce(eyes.transform.forward, ForceMode.Impulse);
        // Effect = null;
        // Effect = () =>
        // {
        //     cam.Lens.Dutch = Mathf.Lerp(cam.Lens.Dutch, 3f, Time.deltaTime * 5f);
        //     panTilt.TiltAxis.Value = Mathf.Lerp(panTilt.TiltAxis.Value, -15f, Time.deltaTime * 5f);
        // };
        PlayerController.Instance.SetCanMove(false);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // <-- ADD THIS so other scripts can compute percentage
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public string GetUniqueIdentifier()
    {
        return "PlayerHealth" + GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData { currentHealth = currentHealth };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        SetHealth(data.currentHealth);
    }

    public class SaveData
    {
        public float currentHealth;
    }
}
