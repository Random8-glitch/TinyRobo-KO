using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Configuracion : MonoBehaviour
{
    [System.Serializable]
    private class OpcionResolucion
    {
        public string nombre = "1920 x 1080";
        public int ancho = 1920;
        public int alto = 1080;
    }

    [Header("Controles de configuracion")]
    [SerializeField] private TMP_Dropdown desplegableResolucion;

    [Header("Modo de pantalla")]
    [SerializeField] private Toggle togglePantallaCompleta;
    [SerializeField] private Toggle toggleModoVentana;

    [Header("Resoluciones disponibles")]
    [SerializeField] private List<OpcionResolucion> resoluciones =
        new List<OpcionResolucion>
        {
            new OpcionResolucion
            {
                nombre = "1280 x 720",
                ancho = 1280,
                alto = 720
            },
            new OpcionResolucion
            {
                nombre = "1600 x 900",
                ancho = 1600,
                alto = 900
            },
            new OpcionResolucion
            {
                nombre = "1920 x 1080",
                ancho = 1920,
                alto = 1080
            },
            new OpcionResolucion
            {
                nombre = "2560 x 1440 - 2K",
                ancho = 2560,
                alto = 1440
            },
            new OpcionResolucion
            {
                nombre = "3840 x 2160 - 4K",
                ancho = 3840,
                alto = 2160
            }
        };

    [Header("Rendimiento")]
    [SerializeField] private TMP_Dropdown desplegableLimiteFPS;
    [SerializeField] private Toggle toggleVSync;

    [SerializeField] private int[] limitesFPS =
    {
        30,
        60,
        120,
        144,
        240,
        -1
    };

    private const string AnchoGuardado =
        "ResolucionAncho";

    private const string AltoGuardado =
        "ResolucionAlto";

    private const string ModoVentanaGuardado =
        "ModoVentana";

    private const string LimiteFPSGuardado =
        "LimiteFPS";

    private const string VSyncGuardado =
        "VSync";

    private bool actualizandoToggles;

    private void Start()
    {
        AgregarResolucionNativa();
        InicializarResoluciones();
        InicializarLimitesFPS();
        CargarConfiguracion();
    }

    private void AgregarResolucionNativa()
    {
        Resolution resolucionNativa =
            Screen.currentResolution;

        int indiceNativo = BuscarResolucion(
            resolucionNativa.width,
            resolucionNativa.height
        );

        if (indiceNativo >= 0)
        {
            return;
        }

        resoluciones.Insert(
            0,
            new OpcionResolucion
            {
                nombre =
                    resolucionNativa.width +
                    " x " +
                    resolucionNativa.height +
                    " - Nativa",

                ancho = resolucionNativa.width,
                alto = resolucionNativa.height
            }
        );
    }

    private void InicializarResoluciones()
    {
        if (desplegableResolucion == null)
        {
            Debug.LogWarning(
                "Configuracion: No se asignó el desplegable de resolución.",
                this
            );

            return;
        }

        desplegableResolucion.ClearOptions();

        List<string> opciones =
            new List<string>();

        foreach (OpcionResolucion resolucion in resoluciones)
        {
            opciones.Add(resolucion.nombre);
        }

        desplegableResolucion.AddOptions(opciones);
    }

    private void InicializarLimitesFPS()
    {
        if (desplegableLimiteFPS == null ||
            limitesFPS == null)
        {
            return;
        }

        desplegableLimiteFPS.ClearOptions();

        List<string> opciones =
            new List<string>();

        foreach (int limite in limitesFPS)
        {
            if (limite < 0)
            {
                opciones.Add("Ilimitado");
            }
            else
            {
                opciones.Add(limite + " FPS");
            }
        }

        desplegableLimiteFPS.AddOptions(opciones);
    }

    private void CargarConfiguracion()
    {
        Resolution resolucionNativa =
            Screen.currentResolution;

        int ancho = PlayerPrefs.GetInt(
            AnchoGuardado,
            resolucionNativa.width
        );

        int alto = PlayerPrefs.GetInt(
            AltoGuardado,
            resolucionNativa.height
        );

        bool modoVentana = PlayerPrefs.GetInt(
            ModoVentanaGuardado,
            0
        ) == 1;

        int indiceResolucion =
            BuscarResolucion(ancho, alto);

        if (indiceResolucion < 0)
        {
            indiceResolucion = 0;
            ancho = resoluciones[0].ancho;
            alto = resoluciones[0].alto;
        }

        if (desplegableResolucion != null)
        {
            desplegableResolucion.SetValueWithoutNotify(
                indiceResolucion
            );
        }

        ActualizarTogglesModoPantalla(
            modoVentana
        );

        AplicarResolucion(
            ancho,
            alto,
            !modoVentana
        );

        int limiteFPS = PlayerPrefs.GetInt(
            LimiteFPSGuardado,
            60
        );

        bool vSyncActivado = PlayerPrefs.GetInt(
            VSyncGuardado,
            1
        ) == 1;

        int indiceFPS =
            BuscarIndiceFPS(limiteFPS);

        if (desplegableLimiteFPS != null &&
            indiceFPS >= 0)
        {
            desplegableLimiteFPS.SetValueWithoutNotify(
                indiceFPS
            );
        }

        if (toggleVSync != null)
        {
            toggleVSync.SetIsOnWithoutNotify(
                vSyncActivado
            );
        }

        AplicarRendimiento(
            limiteFPS,
            vSyncActivado
        );
    }

    public void AplicarConfiguracion()
    {
        if (resoluciones == null ||
            resoluciones.Count == 0)
        {
            return;
        }

        int indice = 0;

        if (desplegableResolucion != null)
        {
            indice = desplegableResolucion.value;
        }

        indice = Mathf.Clamp(
            indice,
            0,
            resoluciones.Count - 1
        );

        OpcionResolucion resolucion =
            resoluciones[indice];

        bool modoVentana =
            toggleModoVentana != null &&
            toggleModoVentana.isOn;

        AplicarResolucion(
            resolucion.ancho,
            resolucion.alto,
            !modoVentana
        );

        PlayerPrefs.SetInt(
            AnchoGuardado,
            resolucion.ancho
        );

        PlayerPrefs.SetInt(
            AltoGuardado,
            resolucion.alto
        );

        PlayerPrefs.SetInt(
            ModoVentanaGuardado,
            modoVentana ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void CambiarPantallaCompleta(bool activado)
    {
        if (!activado || actualizandoToggles)
        {
            return;
        }

        actualizandoToggles = true;

        ActualizarTogglesModoPantalla(false);

        actualizandoToggles = false;

        AplicarConfiguracion();
    }

    public void CambiarModoVentana(bool activado)
    {
        if (!activado || actualizandoToggles)
        {
            return;
        }

        actualizandoToggles = true;

        ActualizarTogglesModoPantalla(true);

        actualizandoToggles = false;

        AplicarConfiguracion();
    }

    private void ActualizarTogglesModoPantalla(
        bool modoVentana)
    {
        if (togglePantallaCompleta != null)
        {
            togglePantallaCompleta.SetIsOnWithoutNotify(
                !modoVentana
            );
        }

        if (toggleModoVentana != null)
        {
            toggleModoVentana.SetIsOnWithoutNotify(
                modoVentana
            );
        }
    }

    public void CambiarLimiteFPS(int indice)
    {
        if (limitesFPS == null ||
            limitesFPS.Length == 0)
        {
            return;
        }

        indice = Mathf.Clamp(
            indice,
            0,
            limitesFPS.Length - 1
        );

        int limiteFPS =
            limitesFPS[indice];

        bool vSyncActivado =
            toggleVSync != null &&
            toggleVSync.isOn;

        AplicarRendimiento(
            limiteFPS,
            vSyncActivado
        );
    }

    public void CambiarVSync(bool activado)
    {
        if (limitesFPS == null ||
            limitesFPS.Length == 0)
        {
            return;
        }

        int indice = 0;

        if (desplegableLimiteFPS != null)
        {
            indice = desplegableLimiteFPS.value;
        }

        indice = Mathf.Clamp(
            indice,
            0,
            limitesFPS.Length - 1
        );

        int limiteFPS =
            limitesFPS[indice];

        AplicarRendimiento(
            limiteFPS,
            activado
        );
    }

    private void AplicarRendimiento(
        int limiteFPS,
        bool vSyncActivado)
    {
        QualitySettings.vSyncCount =
            vSyncActivado ? 1 : 0;

        Application.targetFrameRate =
            vSyncActivado
                ? -1
                : limiteFPS;

        PlayerPrefs.SetInt(
            LimiteFPSGuardado,
            limiteFPS
        );

        PlayerPrefs.SetInt(
            VSyncGuardado,
            vSyncActivado ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    private void AplicarResolucion(
        int ancho,
        int alto,
        bool pantallaCompleta)
    {
        FullScreenMode modo =
            pantallaCompleta
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

        Screen.SetResolution(
            ancho,
            alto,
            modo
        );
    }

    private int BuscarResolucion(
        int ancho,
        int alto)
    {
        if (resoluciones == null)
        {
            return -1;
        }

        for (int i = 0; i < resoluciones.Count; i++)
        {
            if (resoluciones[i].ancho == ancho &&
                resoluciones[i].alto == alto)
            {
                return i;
            }
        }

        return -1;
    }

    private int BuscarIndiceFPS(int limiteFPS)
    {
        if (limitesFPS == null)
        {
            return -1;
        }

        for (int i = 0; i < limitesFPS.Length; i++)
        {
            if (limitesFPS[i] == limiteFPS)
            {
                return i;
            }
        }

        return 0;
    }
}
