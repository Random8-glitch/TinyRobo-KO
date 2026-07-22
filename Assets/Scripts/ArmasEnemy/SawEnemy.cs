using UnityEngine;

public class SawEnemy : MonoBehaviour
{
    [SerializeField] private float danioPorTick = 1f;

    private int playerLayer;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        PlayerStats stats = other.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.RecibirDanio(danioPorTick);
        }
    }
}
