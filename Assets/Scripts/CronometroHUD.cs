using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class CronometroHUD : MonoBehaviour
{
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorUrgencia = Color.red;
    [SerializeField] private float tiempoUrgencia = 10f;

    [Header("Tiempo")]
    [SerializeField] private float tiempoInicial = 300f; // 5 minutos
    [SerializeField] private bool iniciarAutomaticamente = true;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoCronometro;

    [Header("Eventos")]
    public UnityEvent alTerminarTiempo;

    private float tiempoRestante;
    private bool cronometroActivo;

    public float TiempoRestante => tiempoRestante;
    public bool CronometroActivo => cronometroActivo;

    void Start()
    {
        tiempoRestante = tiempoInicial;
        cronometroActivo = iniciarAutomaticamente;

        ActualizarTexto();
    }

    void Update()
    {
        if (!cronometroActivo)
            return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            cronometroActivo = false;

            ActualizarTexto();

            alTerminarTiempo?.Invoke();
            return;
        }

        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60);

        textoCronometro.text = $"{minutos:00}:{segundos:00}";
        textoCronometro.color = tiempoRestante <= tiempoUrgencia
            ? colorUrgencia
            : colorNormal;
    }

    public void IniciarCronometro()
    {
        cronometroActivo = true;
    }

    public void PausarCronometro()
    {
        cronometroActivo = false;
    }

    public void ReiniciarCronometro()
    {
        tiempoRestante = tiempoInicial;
        cronometroActivo = true;
        ActualizarTexto();
    }

    public void AgregarTiempo(float segundos)
    {
        tiempoRestante += segundos;
        ActualizarTexto();
    }

    public void RestarTiempo(float segundos)
    {
        tiempoRestante -= segundos;

        if (tiempoRestante < 0)
            tiempoRestante = 0;

        ActualizarTexto();
    }
}