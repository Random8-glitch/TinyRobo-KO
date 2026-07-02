using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private PlayerArmasRobo playerArmas;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Arma Equipada");

        GameObject droppedObject = eventData.pointerDrag;

        if (droppedObject == null)
            return;

        WeaponUIData weaponData = droppedObject.GetComponent<WeaponUIData>();

        if (weaponData == null)
            return;

        playerArmas.EquiparArma(weaponData.WeaponPrefab);
    }
}