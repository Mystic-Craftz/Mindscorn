using UnityEngine;
using UnityEngine.Pool;

public class DeathScreenTextEffect : MonoBehaviour
{

    [SerializeField] private DeathScreenTextEffectTemplate insanityText;

    [SerializeField] private int spawnAmount = 50;

    [SerializeField] private float spawnAfter = 1f;

    private float timeSinceLastSpawn = 0f;

    [HideInInspector]
    public ObjectPool<DeathScreenTextEffectTemplate> pool;

    private bool isActivated = false;

    private void Start()
    {
        pool = new ObjectPool<DeathScreenTextEffectTemplate>(
            () => Instantiate(insanityText, transform),
            obj => obj.gameObject.SetActive(true),
            obj => obj.gameObject.SetActive(false),
            obj => Destroy(obj.gameObject),
            false,
            spawnAmount
        );
    }

    private void Update()
    {
        if (!isActivated) return;

        if (timeSinceLastSpawn >= spawnAfter)
        {
            timeSinceLastSpawn = 0f;
            DeathScreenTextEffectTemplate text = pool.Get();
            text.Init(DestroyAction);
        }
        else
        {
            timeSinceLastSpawn += Time.deltaTime;
        }
    }

    private void DestroyAction(DeathScreenTextEffectTemplate insanityText)
    {
        pool.Release(insanityText);
    }

    public void StartEffect()
    {
        isActivated = true;
    }

    public void StopEffect() => isActivated = false;
}
