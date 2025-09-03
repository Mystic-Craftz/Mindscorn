using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId = "";

    public string UniqueId => uniqueId;

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = Guid.NewGuid().ToString();
        }
    }
}
