using UnityEngine;

public class SawPlayer : MonoBehaviour
{
    [SerializeField] private float danioPorTick = 1f;

    private int enemyLayer;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != enemyLayer)
            return;

        EnemyStats stats = other.GetComponent<EnemyStats>();

        if (stats != null)
        {
            stats.RecibirDanio(danioPorTick);
        }
    }
}