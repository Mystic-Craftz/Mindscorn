using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

public class BossTriggerAnimator : MonoBehaviour
{
    public static BossTriggerAnimator Instance { get; private set; }
    public Animator bossAnimator;
    public ParticleSystem shockwaveParticle;
    public StudioEventEmitter shockwaveEmitter;
    [SerializeField] private CinemachineCamera cam;
    public EventReference liftSound;
    public NavMeshAgent agent;
    public Transform bossHeadLookAt;
    public Transform bossWalkToPoint;
    public Transform bossTransform;
    public string liftParamName = "Lift";
    public string stayParamName = "Stay";
    public string liftStateName = "Lifting Hands";
    public string stayStateName = "Lifting Hands Idle";
    public bool liftActive = true;
    public bool autoPlayStayAfterLift = true;
    public bool singleUse = true;
    public string playerTag = "Player";
    public float animationTimeout = 8f;
    bool _isRunning = false;

    public bool isPlayingMoveToPlayerSection = false;
    private Camera mainCam;
    private UniversalAdditionalCameraData cameraData;
    private List<Camera> originalCameraStack = new List<Camera>();
    Coroutine endCoroutine = null;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        shockwaveEmitter.gameObject.SetActive(true);
        shockwaveEmitter.Stop();
        shockwaveParticle.Stop();
        mainCam = Camera.main;
        cameraData = mainCam.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.ForEach(overlayCam => originalCameraStack.Add(overlayCam));
    }

    private void Update()
    {
        if (isPlayingMoveToPlayerSection)
        {
            agent.SetDestination(bossWalkToPoint.position);
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (endCoroutine == null)
                    Debug.Log("Arrived");
                Vector3 direction = (PlayerController.Instance.transform.position - bossTransform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                bossTransform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);

                if (endCoroutine == null)
                    endCoroutine = StartCoroutine(PlayEndSequence());
            }
            else
            {
                shockwaveEmitter.Stop();
                shockwaveParticle.Stop();
                if (endCoroutine != null)
                {
                    StopCoroutine(endCoroutine);
                    endCoroutine = null;
                }
                bossHeadLookAt.transform.position = mainCam.transform.position;
                bossAnimator.SetBool("IsMoving", true);
            }
        }
    }

    private IEnumerator PlayEndSequence()
    {
        bossAnimator.SetBool("IsMoving", false);
        yield return new WaitForSeconds(1f);


        bossAnimator.SetBool(liftParamName, true);
        yield return new WaitForSeconds(.8f);

        shockwaveParticle.Play();
        shockwaveEmitter.Play();
    }

    public void PlayMovingToPlayerSection()
    {
        isPlayingMoveToPlayerSection = true;
        agent.SetDestination(bossWalkToPoint.position);
        cam.Priority = 100;
        cameraData.cameraStack.Clear();
        // Debug.Log(isPlayingMoveToPlayerSection);
        AudioManager.Instance.PlayOneShot(liftSound, transform.position);
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isRunning) return;
        if (!other.CompareTag(playerTag)) return;

        if (liftActive)
        {
            StartCoroutine(PlayLiftSequence());
        }
        else
        {
            StartCoroutine(PlayStaySequence());
        }
    }

    IEnumerator PlayLiftSequence()
    {
        if (bossAnimator == null) yield break;

        _isRunning = true;
        // bossAnimator.SetBool(liftParamName, true);
        // yield return StartCoroutine(WaitForStateToEnter(liftStateName, animationTimeout));
        // yield return StartCoroutine(WaitForStateToFinish(liftStateName, animationTimeout));
        // bossAnimator.SetBool(liftParamName, false);
        liftActive = false;
        AudioManager.Instance.PlayOneShot(liftSound, transform.position);

        if (autoPlayStayAfterLift)
        {
            StartCoroutine(PlayStaySequence());
        }
    }

    IEnumerator PlayStaySequence()
    {
        if (bossAnimator == null) yield break;

        _isRunning = true;
        // bossAnimator.SetBool(stayParamName, true);

        if (shockwaveParticle != null)
        {
            shockwaveParticle.Play();
            shockwaveEmitter.Play();
        }

        // yield return StartCoroutine(WaitForStateToEnter(stayStateName, animationTimeout));

        _isRunning = false;
        if (singleUse) enabled = false;
    }

    IEnumerator WaitForStateToEnter(string stateName, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            var info = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName)) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator WaitForStateToFinish(string stateName, float timeout)
    {
        float t = 0f;
        var info = bossAnimator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(stateName))
        {
            yield return StartCoroutine(WaitForStateToEnter(stateName, timeout));
            info = bossAnimator.GetCurrentAnimatorStateInfo(0);
        }

        if (!info.IsName(stateName)) yield break;

        while (t < timeout)
        {
            info = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName)) yield break;
            if (info.normalizedTime >= 1f) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

}
