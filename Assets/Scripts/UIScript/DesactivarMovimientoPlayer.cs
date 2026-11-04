using UnityEngine;

public class DesactivarMovimientoPlayer : MonoBehaviour
{
    private void Start()
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return;

        RoboMovPlayer[] movimientos = FindObjectsByType<RoboMovPlayer>(
            FindObjectsSortMode.None
        );

        foreach (RoboMovPlayer movimiento in movimientos)
        {
            if (movimiento.gameObject.layer == playerLayer)
            {
                movimiento.enabled = false;
            }
        }
    }
}