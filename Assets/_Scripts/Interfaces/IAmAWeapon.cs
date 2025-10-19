using UnityEngine;

public interface IAmAWeapon
{
    public void Fire(PlayerWeapons playerWeapons);
    public void Reload(PlayerWeapons playerWeapons);
    public void FinishReload(PlayerWeapons playerWeapons);
    public bool IsReloadInProgress();
}
