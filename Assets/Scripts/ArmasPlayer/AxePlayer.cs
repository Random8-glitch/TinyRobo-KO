using UnityEngine;
using System.Collections;

public class AxePlayer : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float tiempoEspera = 1f;
    [SerializeField] private float danioNormal = 10f;
    [SerializeField] private float multiplicadorCritico = 3f;

    [Header("Estado Crítico")]
    [SerializeField] private float tiempoMinCritico = 5f;
    [SerializeField] private float tiempoMaxCritico = 10f;
    [SerializeField] private float duracionCritico = 3f;

    private int enemyLayer;
    private GameObject enemigoDentro;

    private bool criticoActivo = false;

    private Renderer rend;
    private Color colorOriginal;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            colorOriginal = rend.material.color;
        }

        StartCoroutine(CicloCritico());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer)
            return;

        enemigoDentro = other.gameObject;

        StartCoroutine(EsperarHit(other.gameObject));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == enemigoDentro)
        {
            enemigoDentro = null;
        }
    }

    private IEnumerator EsperarHit(GameObject enemigo)
    {
        yield return new WaitForSeconds(tiempoEspera);

        if (enemigoDentro == enemigo)
        {
            float danio = danioNormal;

            if (criticoActivo)
            {
                danio *= multiplicadorCritico;
                Debug.Log("¡GOLPE CRÍTICO!");
            }
            else
            {
                Debug.Log("Enemigo golpeado");
            }

            EnemyStats stats = enemigo.GetComponent<EnemyStats>();

            if (stats != null)
            {
                stats.RecibirDanio(danio);
            }
        }
    }

    private IEnumerator CicloCritico()
    {
        while (true)
        {
            float espera = Random.Range(
                tiempoMinCritico,
                tiempoMaxCritico
            );

            yield return new WaitForSeconds(espera);

            criticoActivo = true;

            if (rend != null)
            {
                rend.material.color = Color.red;
            }

            Debug.Log("CRÍTICO ACTIVADO");

            yield return new WaitForSeconds(duracionCritico);

            criticoActivo = false;

            if (rend != null)
            {
                rend.material.color = colorOriginal;
            }

            Debug.Log("CRÍTICO TERMINADO");
        }
    }
}