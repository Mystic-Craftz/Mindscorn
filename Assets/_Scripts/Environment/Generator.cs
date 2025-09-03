using System;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour, ISaveable
{
    public static Generator Instance { get; private set; }

    private bool isOn = false;
    private bool isFirstTime = true;

    [Serializable]
    public class RendererMaterial
    {
        public Renderer renderer;
        public int materialIndex;
    }

    private List<Light> registeredLights = new List<Light>();
    private List<RendererMaterial> registeredMaterials = new List<RendererMaterial>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!isOn && isFirstTime)
            TurnOffRegisteredLights();
    }

    public void RegisterLight(Light light)
    {
        registeredLights.Add(light);
    }

    public void RegisterRenderer(Renderer renderer, int materialIndex)
    {
        registeredMaterials.Add(new RendererMaterial { renderer = renderer, materialIndex = materialIndex });
    }

    public bool IsTurnedOn()
    {
        return isOn;
    }

    public void TurnOnRegisteredLights()
    {
        foreach (var light in registeredLights)
        {
            light.gameObject.SetActive(true);
        }
        foreach (var material in registeredMaterials)
        {
            material.renderer.materials[material.materialIndex].EnableKeyword("_EMISSION");
        }
        isOn = true;
        isFirstTime = false;
    }

    public void TurnOffRegisteredLights()
    {
        foreach (var light in registeredLights)
        {
            light.gameObject.SetActive(false);
        }
        foreach (var material in registeredMaterials)
        {
            material.renderer.materials[material.materialIndex].DisableKeyword("_EMISSION");
        }
        isOn = false;
    }


    public string GetUniqueIdentifier()
    {
        return "Generator Data";
    }

    public object CaptureState()
    {
        return new SaveData { isOn = isOn, isFirstTime = isFirstTime };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isOn = data.isOn;
        isFirstTime = data.isFirstTime;
        if (isOn) { TurnOnRegisteredLights(); }
        else { TurnOffRegisteredLights(); }
    }

    public class SaveData
    {
        public bool isOn;
        public bool isFirstTime;
    }
}
