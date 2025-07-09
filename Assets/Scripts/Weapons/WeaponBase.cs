using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    public WeaponData weaponData;
    [Header("References")]
    public ParticleSystem[] muzzleFlashes;
    public ParticleSystem hitEffect;
    public TrailRenderer tracerEffect;
    public Transform raycastOrigin;
    public Transform crosshairTarget;

    public WeaponData GetData()
    {
        return weaponData;
    }

}
