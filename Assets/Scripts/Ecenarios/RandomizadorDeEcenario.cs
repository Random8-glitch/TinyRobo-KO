
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefabs disponibles")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Lugar de aparición")]
    [SerializeField] private Transform puntoDeSpawn;

    [Header("Opciones")]
    [SerializeField] private bool aparecerAlIniciar = true;
    [SerializeField] private bool usarRotacionDelPunto = true;

    private void Start()
    {
        if (aparecerAlIniciar)
        {
            GenerarPrefabAleatorio();
        }
    }

    public void GenerarPrefabAleatorio()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("No hay prefabs asignados.", this);
            return;
        }

        if (puntoDeSpawn == null)
        {
            Debug.LogWarning("No has asignado el punto de aparición.", this);
            return;
        }

        // Escoger un prefab aleatorio
        int indice = Random.Range(0, prefabs.Length);
        GameObject prefabElegido = prefabs[indice];

        if (prefabElegido == null)
        {
            Debug.LogWarning(
                "El prefab seleccionado está vacío en la lista.", this);
            return;
        }

        // Determinar la rotación
        Quaternion rotacion = usarRotacionDelPunto
            ? puntoDeSpawn.rotation
            : prefabElegido.transform.rotation;

        // Crear el prefab en la posición indicada
        Instantiate(
            prefabElegido,
            puntoDeSpawn.position,
            rotacion
        );

        Debug.Log("Prefab generado: " + prefabElegido.name);
    }
}
