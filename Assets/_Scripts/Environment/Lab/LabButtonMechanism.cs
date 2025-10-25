using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class LabButtonMechanism : MonoBehaviour, IAmInteractable
{
    [SerializeField] private Transform button;
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

    private bool isStartingUp = false;
    private bool isOnCooldown = false;
    private bool canShowDeclineDialog = true;


    private void Start()
    {
        lights.ForEach((light) =>
        {
            light.material.SetVector("_BaseColor", Color.black);
            light.material.DisableKeyword("_EMISSION");
        });

        sparays.ForEach((s) => s.Stop());
        fog.Stop();
    }

    public void Interact()
    {
        if (!isStartingUp && !isOnCooldown)
        {
            isStartingUp = true;
            StartCoroutine(TurnLightsOn());
        }
        else
        {
            if (canShowDeclineDialog)
            {
                if (isStartingUp && !isOnCooldown)
                    DialogUI.Instance.ShowDialog("It is starting up", 2f);

                if (isOnCooldown && !isStartingUp)
                    DialogUI.Instance.ShowDialog("It is on cooldown", 2f);

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
            if (i != lights.Count - 1)
                yield return new WaitForSeconds(delayBetweenLightsTurningOn);
        }

        sparays.ForEach((s) => s.Play());

        yield return new WaitForSeconds(.5f);

        fog.Play();

        onMechanismStart?.Invoke();

        yield return new WaitForSeconds(durationForGasEmission);

        onMechanismEnd?.Invoke();

        isStartingUp = false;
        isOnCooldown = true;
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
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }
}
