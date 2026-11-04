using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] public float vidaMax = 100f;
    [SerializeField] public float vida = 100f;

    private TMP_Text textoVida;

    private void Start()
    {
        GameObject objetoVida = GameObject.Find("VidaJugador");

        if (objetoVida != null)
        {
            textoVida = objetoVida.GetComponent<TMP_Text>();
        }

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

    public void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text = $"Player Vida: {Mathf.CeilToInt(vida)}/{Mathf.CeilToInt(vidaMax)}";
        }
    }
}