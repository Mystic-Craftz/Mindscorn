using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class LabButtonMechanism : MonoBehaviour, IAmInteractable
{
    [SerializeField] private Transform button;
    [SerializeField] private Material buttonMaterial;

    [SerializeField] private Color buttonEmissionColor = Color.white;
    [SerializeField] private float buttonEmissionIntensity = 1f;

    [SerializeField] private List<Renderer> lights;
    [SerializeField] private List<ParticleSystem> sparays;
    [SerializeField] private ParticleSystem fog;

    [Header("Settings")]
    [SerializeField] private float delayBetweenLightsTurningOn = 1f;
    [SerializeField] private float delayBetweenLightsTurningOff = 1.5f;
    [SerializeField] private float durationForGasEmission = 3f;

    [Header("Events")]
    [SerializeField] private UnityEvent onMechanismStart;
    [SerializeField] private UnityEvent onMechanismEnd;

    [Header("FMOD Stuff")]
    [SerializeField] private EventReference buttonPressSound;
    [SerializeField] private EventReference spraySound;
    [SerializeField] private EventReference warningSound;
    [SerializeField] private EventReference lightOnSound;

    [Header("Sound Sources")]
    [SerializeField] private Transform spraySoundSource;
    [SerializeField] private Transform warningSoundSource;

    private bool isStartingUp = false;
    private bool isOnCooldown = false;
    private bool canShowDeclineDialog = true;

    // runtime cloned material for the button
    private Material runtimeButtonMaterial;
    private Renderer buttonRendererCached;
    private Color originalButtonEmissionColor = Color.red;

    private void Start()
    {
        lights.ForEach((light) =>
        {
            light.material.SetVector("_BaseColor", Color.black);
            light.material.DisableKeyword("_EMISSION");
        });

        sparays.ForEach((s) => s.Stop());
        fog.Stop();

        // Clone button material
        if (buttonMaterial != null)
        {
            runtimeButtonMaterial = new Material(buttonMaterial);

            if (button != null)
            {
                buttonRendererCached = button.GetComponent<Renderer>();
                if (buttonRendererCached != null)
                {
                    buttonRendererCached.material = runtimeButtonMaterial;
                }
            }

            if (runtimeButtonMaterial.HasProperty("_EmissionColor"))
            {
                originalButtonEmissionColor = runtimeButtonMaterial.GetColor("_EmissionColor");
            }
        }
        else
        {
            Debug.LogWarning($"[{nameof(LabButtonMechanism)}] buttonMaterial not assigned on '{name}'.");
        }

        // Set initial emission state should be on
        SetButtonEmission(true);
    }

    public void Interact()
    {
        if (!isStartingUp && !isOnCooldown)
        {
            FMODUnity.RuntimeManager.PlayOneShot(buttonPressSound, transform.position);
            isStartingUp = true;

            // during the start up process, button emission should be off
            SetButtonEmission(false);

            StartCoroutine(TurnLightsOn());
        }
        else
        {
            if (canShowDeclineDialog)
            {
                if (isStartingUp && !isOnCooldown)
                    DialogUI.Instance.ShowDialog("It’s already started.", 2f);

                if (isOnCooldown && !isStartingUp)
                    DialogUI.Instance.ShowDialog("It’s on cooldown.", 2f);

                canShowDeclineDialog = false;
                StartCoroutine(ResetDeclineDialog());
            }
        }
    }

    private IEnumerator ResetDeclineDialog()
    {
        yield return new WaitForSeconds(2.5f);
        canShowDeclineDialog = true;
    }

    private IEnumerator TurnLightsOn()
    {
        float originalY = button.localPosition.y;
        button.DOLocalMoveY(-0.015f, .2f).OnComplete(() =>
        {
            button.DOLocalMoveY(originalY, .2f);
        });

        yield return new WaitForSeconds(.2f);

        for (int i = 0; i < lights.Count; i++)
        {
            lights[i].material.EnableKeyword("_EMISSION");

            // play beep sound
            FMODUnity.RuntimeManager.PlayOneShot(lightOnSound, transform.position);

            if (i != lights.Count - 1)
                yield return new WaitForSeconds(delayBetweenLightsTurningOn);
        }

        // Play warning sound 
        Vector3 warningPos = (warningSoundSource != null) ? warningSoundSource.position : transform.position;
        FMODUnity.RuntimeManager.PlayOneShot(warningSound, warningPos);

        // Start sprays sound
        sparays.ForEach((s) => s.Play());
        Vector3 sprayPos = (spraySoundSource != null) ? spraySoundSource.position : transform.position;
        FMODUnity.RuntimeManager.PlayOneShot(spraySound, sprayPos);

        yield return new WaitForSeconds(.5f);

        fog.Play();

        onMechanismStart?.Invoke();

        yield return new WaitForSeconds(durationForGasEmission);

        onMechanismEnd?.Invoke();

        isStartingUp = false;
        isOnCooldown = true;

        // Turn off button emission when we enter cooldown 
        SetButtonEmission(false);

        StartCoroutine(CooldownSection());
    }

    private IEnumerator CooldownSection()
    {
        sparays.ForEach((s) => s.Stop());

        for (int i = lights.Count - 1; i >= 0; i--)
        {
            lights[i].material.DisableKeyword("_EMISSION");

            if (i == lights.Count - 2) fog.Stop();

            if (i != 0)
                yield return new WaitForSeconds(delayBetweenLightsTurningOff);
        }

        isStartingUp = false;
        isOnCooldown = false;

        // Play light-on /ready sound when cooldown is finished
        FMODUnity.RuntimeManager.PlayOneShot(lightOnSound, transform.position);

        // Re-enable button emission when cooldown finishes 
        SetButtonEmission(true);
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }

    // helper to toggle button emission
    private void SetButtonEmission(bool on)
    {
        Material mat = runtimeButtonMaterial != null ? runtimeButtonMaterial : buttonMaterial;
        if (mat == null) return;

        if (!mat.HasProperty("_EmissionColor"))
        {
            if (on) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
            return;
        }

        if (on)
        {
            mat.EnableKeyword("_EMISSION");
            Color emit = buttonEmissionColor * buttonEmissionIntensity;
            mat.SetColor("_EmissionColor", emit);

            if (buttonRendererCached != null)
                DynamicGI.SetEmissive(buttonRendererCached, emit);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);

            if (buttonRendererCached != null)
                DynamicGI.SetEmissive(buttonRendererCached, Color.black);
        }
    }
}
