using FMODUnity;
using UnityEngine;

public class BossEndsDemo : MonoBehaviour
{
    public ParticleSystem shockwaveParticle;
    public StudioEventEmitter shockwaveEmitter;
    private int counter = 0;


    public void EndDemo()
    {
        counter++;
        if (counter == 3)
        {
            shockwaveEmitter.Stop();
            shockwaveParticle.Stop();
            DemoEndScreenUI.Instance.Show();
        }
    }
}
