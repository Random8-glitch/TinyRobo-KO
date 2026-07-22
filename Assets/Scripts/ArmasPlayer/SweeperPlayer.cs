using UnityEngine;

public class SweeperPlayer : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float danioBase = 5f;

    [Header("Empuje")]
    [SerializeField] private float distanciaEmpuje = 3f;
    [SerializeField] private float duracionEmpuje = 0.25f;

    private int enemyLayer;

    private RoboMovPlayer player;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        player = GetComponentInParent<RoboMovPlayer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer)
            return;

        EnemyStats stats =
            other.GetComponent<EnemyStats>();

        if (stats != null)
        {
            stats.RecibirDanio(danioBase);
        }

        RoboMovEnemy enemigo =
            other.GetComponent<RoboMovEnemy>();

        if (enemigo != null)
        {
            Vector3 direccionEmpuje;

            if (player != null)
            {
                direccionEmpuje = player.transform.forward;
            }
            else
            {
                direccionEmpuje = transform.forward;
            }

            direccionEmpuje.y = 0f;
            direccionEmpuje.Normalize();

            enemigo.Empujar(
                direccionEmpuje,
                distanciaEmpuje,
                duracionEmpuje
            );
        }
    }
}