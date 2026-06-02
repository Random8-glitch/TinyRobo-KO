using UnityEngine;
using System.Collections;

public class AxePlayer : MonoBehaviour
{
    [SerializeField] private float tiempoEspera = 1f;

    private int enemyLayer;
    private GameObject enemigoDentro;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
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
            Debug.Log("Hit");
        }
    }
}