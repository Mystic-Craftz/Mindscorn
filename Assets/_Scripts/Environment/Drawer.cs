using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class Drawer : MonoBehaviour, IAmInteractable
{
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private bool isCupboard = false;
    [SerializeField] private float moveAmount = 1f;
    [SerializeField] private SpriteRenderer searchImg;
    [SerializeField] private float distanceToShowSearchImg;
    [SerializeField] private float fadingSpeed = 5;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;

    private bool isOpen = false;

    private Transform player;

    private void Start()
    {
        player = PlayerController.Instance.transform;
        if (!isInteractable)
        {
            searchImg.color = new Color(255, 255, 255, 0);
        }
    }

    private void Update()
    {
        if (!isInteractable) return;
        bool isPlayerClose = Vector3.Distance(transform.position, player.position) <= distanceToShowSearchImg;
        if (isPlayerClose && !isOpen)
        {
            searchImg.color = new Color(255, 255, 255, Mathf.MoveTowards(searchImg.color.a, 255, Time.deltaTime * fadingSpeed));
        }
        else if (isPlayerClose && isOpen)
        {
            searchImg.color = new Color(255, 255, 255, Mathf.MoveTowards(searchImg.color.a, 0, Time.deltaTime * fadingSpeed));
        }
        else
        {
            searchImg.color = new Color(255, 255, 255, Mathf.MoveTowards(searchImg.color.a, 0, Time.deltaTime * fadingSpeed));
        }
    }

    public void Interact()
    {
        if (!isInteractable) return;

        if (!isOpen)
        {
            if (isCupboard)
            {
                transform.DOLocalRotate(new Vector3(transform.localEulerAngles.x, 0, moveAmount), 0.5f).SetEase(Ease.OutQuad);
            }
            else
            {
                transform.DOLocalMoveZ(moveAmount, 0.5f).SetEase(Ease.OutQuad);
            }
            AudioManager.Instance.PlayOneShot(openSound, transform.position);
            isOpen = true;
        }
        else
        {
            if (isCupboard)
            {
                transform.DOLocalRotate(new Vector3(transform.localEulerAngles.x, 0, 0), 0.5f).SetEase(Ease.OutQuad);
            }
            else
            {
                transform.DOLocalMoveZ(0, 0.5f).SetEase(Ease.OutQuad);
            }
            AudioManager.Instance.PlayOneShot(closeSound, transform.position);
            isOpen = false;
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return isInteractable;
    }
}
