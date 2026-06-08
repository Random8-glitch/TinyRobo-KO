using UnityEngine;
using TMPro;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] public float vidaMax = 100f;
    [SerializeField] public float vida = 100f;

    [SerializeField] private TMP_Text textoVida;

    private void Start()
    {
        ActualizarTexto();
    }

    public void RecibirDanio(float cantidad)
    {
        vida = Mathf.Max(0f, vida - cantidad);

        ActualizarTexto();

        if (vida <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text = $"Enemy Vida: {Mathf.CeilToInt(vida)}/{Mathf.CeilToInt(vidaMax)}";
        }
    }
}