using DG.Tweening;
using UnityEngine;

public class Drawer : MonoBehaviour, IAmInteractable
{
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private float moveAmount = 1f;
    [SerializeField] private SpriteRenderer searchImg;
    [SerializeField] private float distanceToShowSearchImg;
    [SerializeField] private float fadingSpeed = 5;

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
        if (Vector3.Distance(transform.position, player.position) <= distanceToShowSearchImg && !isOpen)
        {
            searchImg.color = new Color(255, 255, 255, Mathf.MoveTowards(searchImg.color.a, 255, Time.deltaTime * fadingSpeed));
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
            transform.DOLocalMoveZ(moveAmount, 0.5f).SetEase(Ease.OutQuad);
            isOpen = true;
        }
        else
        {
            transform.DOLocalMoveZ(0, 0.5f).SetEase(Ease.OutQuad);
            isOpen = false;
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return isInteractable;
    }
}
