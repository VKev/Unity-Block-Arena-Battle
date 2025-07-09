using System.Collections.Generic;
using UnityEngine;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance;
    public List<WeaponData> allWeaponData;

    private Dictionary<string, WeaponData> lookup;

    private void Awake()
    {
        Instance = this;
        lookup = new Dictionary<string, WeaponData>();
        foreach (var data in allWeaponData)
        {
            lookup[data.name.Replace("Data", "")] = data; // "AssaultRifle"
        }
    }

    public WeaponData GetByPrefabName(string prefabName)
    {
        return lookup.TryGetValue(prefabName, out var data) ? data : null;
    }
}
