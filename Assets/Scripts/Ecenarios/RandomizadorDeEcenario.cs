using UnityEngine;
using Unity.AI.Navigation;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefabs disponibles")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Lugar de aparición")]
    [SerializeField] private Transform puntoDeSpawn;

    [Header("Opciones")]
    [SerializeField] private bool aparecerAlIniciar = true;
    [SerializeField] private bool usarRotacionDelPunto = true;

    [Header("Navegación del escenario")]
    [SerializeField] private NavMeshSurface superficieNavegacion;

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

        int indice = Random.Range(0, prefabs.Length);
        GameObject prefabElegido = prefabs[indice];

        if (prefabElegido == null)
        {
            Debug.LogWarning(
                "El prefab seleccionado está vacío en la lista.", this);
            return;
        }

        Quaternion rotacion = usarRotacionDelPunto
            ? puntoDeSpawn.rotation
            : prefabElegido.transform.rotation;

        Instantiate(prefabElegido, puntoDeSpawn.position, rotacion);
        Debug.Log("Prefab generado: " + prefabElegido.name, this);

        if (superficieNavegacion == null)
        {
            Debug.LogWarning(
                "El escenario se generó, pero falta asignar la superficie de navegación.",
                this);
            return;
        }

        superficieNavegacion.BuildNavMesh();
    }
}