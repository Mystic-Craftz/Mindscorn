using System.Collections.Generic;
using UnityEngine;

public class MannequinManager : MonoBehaviour
{
    public static MannequinManager Instance { get; private set; }
    [SerializeField] private bool debug = false;
    private List<Mannequin> mannequins;

    private void Awake()
    {
        Instance = this;
        mannequins = new List<Mannequin>();
    }

    public void RegisterMannequin(Mannequin mannequin)
    {
        if (!mannequins.Contains(mannequin))
        {
            mannequins.Add(mannequin);
        }
    }

    public void UnregisterMannequin(Mannequin mannequin)
    {
        if (mannequins.Contains(mannequin))
        {
            mannequins.Remove(mannequin);
        }
    }

    public void StopAllMannequins()
    {
        foreach (var mannequin in mannequins)
        {
            if (debug)
                Debug.Log("Stopping mannequin: " + mannequin.gameObject.name);
            mannequin.StopMovement();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (debug)
                Debug.Log("Player entered the stop zone. Stopping all mannequins.");
            StopAllMannequins();
        }
    }
}
