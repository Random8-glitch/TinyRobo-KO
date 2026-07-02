using UnityEngine;

public class WeaponUIData : MonoBehaviour
{
    [SerializeField] private GameObject weaponPrefab;

    public GameObject WeaponPrefab => weaponPrefab;
}