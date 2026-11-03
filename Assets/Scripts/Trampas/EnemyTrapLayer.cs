using UnityEngine;

public class EnemyTrapLayer : MonoBehaviour
{
    private void Awake()
    {
        int layerTrampa = LayerMask.NameToLayer("Trap");

        if (layerTrampa == -1)
        {
            Debug.LogError(
                "No existe la Layer 'Trap'. Créala desde el Inspector.",
                this
            );

            return;
        }

        AsignarLayerRecursiva(transform, layerTrampa);
    }

    private void AsignarLayerRecursiva(Transform objeto, int layer)
    {
        objeto.gameObject.layer = layer;

        foreach (Transform hijo in objeto)
        {
            AsignarLayerRecursiva(hijo, layer);
        }
    }
}