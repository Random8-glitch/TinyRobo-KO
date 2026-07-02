using UnityEngine;

public class PlayerArmasRobo : MonoBehaviour
{
    [Header("Prefabs de referencia")]
    [SerializeField] private GameObject hachaPrefab;
    [SerializeField] private GameObject martilloPrefab;
    [SerializeField] private GameObject sierraPrefab;
    [SerializeField] private GameObject barredoraPrefab;

    [SerializeField] private GameObject armaActual;

    public void EquiparArma(GameObject prefab)
    {
        if (prefab == null)
            return;

        // Eliminar arma anterior
        if (armaActual != null)
        {
            Destroy(armaActual);
        }

        // Crear nueva arma
        armaActual = Instantiate(prefab, transform);

        // Aplicar configuración según el arma
        if (prefab == hachaPrefab)
        {
            armaActual.transform.localPosition = new Vector3(0f, 1.5f, 0.333333343f);
            armaActual.transform.localEulerAngles = new Vector3(90f, 180.000061f, 0f);
            armaActual.transform.localScale = new Vector3(0.5f, 0.333333343f, 2.00010014f);
        }
        else if (prefab == martilloPrefab)
        {
            armaActual.transform.localPosition = new Vector3(1.67634071e-14f, 1.49999988f, 0.333333343f);
            armaActual.transform.localEulerAngles = new Vector3(-1.70754754e-06f, 180.000061f, -6.37209673e-14f);
            armaActual.transform.localScale = new Vector3(0.5f, 1f, 0.333333343f);
        }
        else if (prefab == sierraPrefab)
        {
            armaActual.transform.localPosition = new Vector3(0f, -0.0700000972f, 0.5f);
            armaActual.transform.localEulerAngles = new Vector3(-1.70754754e-06f, 180.000061f, -6.37209673e-14f);
            armaActual.transform.localScale = new Vector3(1f, 0.364069998f, 0.666666687f);
        }
        else if (prefab == barredoraPrefab)
        {
            armaActual.transform.localPosition = new Vector3(0f, 0f, 0.666666687f);
            armaActual.transform.localEulerAngles = new Vector3(-1.70754754e-06f, 180.000061f, -6.37209673e-14f);
            armaActual.transform.localScale = new Vector3(0.5f, 1f, 0.333333343f);
        }
    }
}