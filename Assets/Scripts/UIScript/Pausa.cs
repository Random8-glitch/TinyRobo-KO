using UnityEngine;

public class Pausa : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelConfiguracion;

    [Header("Configuración de escena")]
    [SerializeField] private bool esMenuPrincipal;

    [Header("Tecla")]
    [SerializeField] private KeyCode teclaPausa = KeyCode.Escape;

    private bool juegoPausado;
    private float escalaTiempoAnterior = 1f;

    public bool JuegoPausado => juegoPausado;

    private void Start()
    {
        Time.timeScale = 1f;
        juegoPausado = false;

        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }

        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(false);
        }

        if (esMenuPrincipal)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(teclaPausa))
        {
            return;
        }

        if (panelConfiguracion != null &&
            panelConfiguracion.activeSelf)
        {
            CerrarConfiguracion();
            return;
        }

        if (!esMenuPrincipal)
        {
            AlternarPausa();
        }
    }

    public void AlternarPausa()
    {
        if (juegoPausado)
        {
            ReanudarJuego();
        }
        else
        {
            PausarJuego();
        }
    }

    public void PausarJuego()
    {
        if (juegoPausado || esMenuPrincipal)
        {
            return;
        }

        juegoPausado = true;
        escalaTiempoAnterior = Time.timeScale;
        Time.timeScale = 0f;

        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
        }

        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReanudarJuego()
    {
        if (!juegoPausado)
        {
            return;
        }

        juegoPausado = false;
        Time.timeScale = escalaTiempoAnterior;

        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }

        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void AbrirConfiguracion()
    {
        if (!esMenuPrincipal && !juegoPausado)
        {
            return;
        }

        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarConfiguracion()
    {
        if (panelConfiguracion != null)
        {
            panelConfiguracion.SetActive(false);
        }

        if (esMenuPrincipal)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (juegoPausado)
        {
            if (panelPausa != null)
            {
                panelPausa.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDisable()
    {
        if (juegoPausado)
        {
            Time.timeScale = escalaTiempoAnterior;
            juegoPausado = false;
        }
    }
}
