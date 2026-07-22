using UnityEngine;

public class SweeperEnemy : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float danioBase = 5f;

    [Header("Empuje")]
    [SerializeField] private float distanciaEmpuje = 3f;
    [SerializeField] private float duracionEmpuje = 0.25f;

    private int playerLayer;

    private RoboMovEnemy enemy;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");

        enemy = GetComponentInParent<RoboMovEnemy>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        PlayerStats stats =
            other.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.RecibirDanio(danioBase);
        }

        RoboMovPlayer player =
            other.GetComponent<RoboMovPlayer>();

        if (player != null)
        {
            Vector3 direccionEmpuje;

            if (enemy != null)
            {
                direccionEmpuje = enemy.transform.forward;
            }
            else
            {
                direccionEmpuje = transform.forward;
            }

            direccionEmpuje.y = 0f;
            direccionEmpuje.Normalize();

            player.Empujar(
                direccionEmpuje,
                distanciaEmpuje,
                duracionEmpuje
            );
        }
    }
}
