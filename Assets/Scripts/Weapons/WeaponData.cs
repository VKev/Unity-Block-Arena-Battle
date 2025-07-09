using UnityEngine;

public enum FireMode
{
    Tap,
    Hold
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "New Weapon";
    public float fireRate = 0.2f; // thời gian giữa 2 phát bắn
    public float fireDelay = 0f; // delay giữa lúc nhấn và bắn
    public FireMode fireMode = FireMode.Tap;
    public float damage = 2f; // sát thương mỗi phát

}
