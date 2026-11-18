
using UnityEngine;
using TMPro;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] public float vidaMax = 100f;
    [SerializeField] public float vida = 100f;

    [SerializeField] private TMP_Text textoVida;

    private void Awake()
    {
        BuscarTextoVida();
    }

    private void Start()
    {
        ActualizarTexto();
    }

    private void BuscarTextoVida()
    {
        TMP_Text[] textos =
            Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (TMP_Text texto in textos)
        {
            if (texto.gameObject.name == "VidaEnemigo" &&
                texto.gameObject.scene.IsValid() &&
                texto.gameObject.scene.isLoaded)
            {
                textoVida = texto;
                return;
            }
        }

        textoVida = null;

        Debug.LogWarning(
            "EnemyStats: No se encontró el objeto " +
            "'VidaEnemigo' en la escena.",
            this
        );
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

    public void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text =
                $"Enemy Vida: {Mathf.CeilToInt(vida)}/{Mathf.CeilToInt(vidaMax)}";
        }
    }
}
