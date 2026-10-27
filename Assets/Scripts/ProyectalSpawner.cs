
using UnityEngine;
using System.Collections;

public class ProyectalSpawner : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float tiempoEntreDisparos = 5f;

    // Referencia a los chips del jugador
    private EquiparChips equiparChips;

    private void Start()
    {
        equiparChips = GetComponentInParent<EquiparChips>();

        // Si no se asigna un punto de disparo,
        // utilizar el propio Empty
        if (puntoDisparo == null)
        {
            puntoDisparo = transform;
        }

        StartCoroutine(DispararAutomaticamente());
    }

    private IEnumerator DispararAutomaticamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreDisparos);

            Disparar();
        }
    }


    private void Disparar()
    {
        if (prefabProyectil == null)
            return;

        // Crear el proyectil con la posición
        // y rotación del Empty
        GameObject proyectil = Instantiate(
            prefabProyectil,
            puntoDisparo.position,
            puntoDisparo.rotation
        );

        // Si es una flecha, asignar los chips
        CrossbowPlayer crossbow =
            proyectil.GetComponent<CrossbowPlayer>();

        if (crossbow != null)
        {
            crossbow.AsignarEquiparChips(equiparChips);
        }

        // Si es una esfera metálica, asignar los chips
        MetalballPlayer metalball =
            proyectil.GetComponent<MetalballPlayer>();

        if (metalball != null)
        {
            metalball.AsignarEquiparChips(equiparChips);
        }
    }

}
