using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SistemaDialogos : MonoBehaviour
{
    [System.Serializable]
    public class LineaDialogo
    {
        public string nombrePersonaje;

        [TextArea(3, 6)]
        public string texto;

        public Sprite imagenPersonaje;
    }

    [Header("UI")]
    [SerializeField] private GameObject uiDialogo;
    [SerializeField] private TMP_Text textoNombre;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private Image imagenPersonajeUI;

    [Header("Dialogos")]
    [SerializeField] private LineaDialogo[] dialogos;

    [Header("Interaccion")]
    [SerializeField] private KeyCode teclaInteractuar = KeyCode.E;
    [SerializeField] private string tagJugador = "Player";

    [Header("Texto progresivo")]
    [SerializeField] private float velocidadTexto = 0.04f;

    [Header("Opciones")]
    [SerializeField] private bool iniciarAlComenzarEscena = false;
    [SerializeField] private bool pausarJuegoDuranteDialogo = true;

    private bool jugadorCerca = false;
    private bool dialogoActivo = false;
    private bool escribiendoTexto = false;

    private int indiceDialogo = 0;

    private Coroutine corrutinaTexto;

    private float escalaTiempoAnterior = 1f;

    private void Start()
    {
        if (uiDialogo != null)
            uiDialogo.SetActive(false);

        if (iniciarAlComenzarEscena)
        {
            IniciarDialogo();
        }
    }

    private void Update()
    {
        if (dialogoActivo)
        {
            if (TeclaInteractuarPresionada())
            {
                if (escribiendoTexto)
                {
                    MostrarTextoCompleto();
                }
                else
                {
                    AvanzarDialogo();
                }
            }

            return;
        }

        if (jugadorCerca && TeclaInteractuarPresionada())
        {
            IniciarDialogo();
        }
    }

    private bool TeclaInteractuarPresionada()
    {
        if (Keyboard.current == null)
            return false;

        switch (teclaInteractuar)
        {
            case KeyCode.E:
                return Keyboard.current.eKey.wasPressedThisFrame;

            case KeyCode.F:
                return Keyboard.current.fKey.wasPressedThisFrame;

            case KeyCode.Q:
                return Keyboard.current.qKey.wasPressedThisFrame;

            case KeyCode.Space:
                return Keyboard.current.spaceKey.wasPressedThisFrame;

            case KeyCode.Return:
                return Keyboard.current.enterKey.wasPressedThisFrame;

            case KeyCode.Y:
                return Keyboard.current.yKey.wasPressedThisFrame;

            case KeyCode.N:
                return Keyboard.current.nKey.wasPressedThisFrame;

            default:
                return false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            jugadorCerca = false;
        }
    }

    public void IniciarDialogo()
    {
        if (dialogoActivo || dialogos == null || dialogos.Length == 0)
            return;

        dialogoActivo = true;
        indiceDialogo = 0;

        if (uiDialogo != null)
            uiDialogo.SetActive(true);

        if (pausarJuegoDuranteDialogo)
        {
            escalaTiempoAnterior = Time.timeScale;
            Time.timeScale = 0f;
        }

        MostrarDialogo();
    }

    private void MostrarDialogo()
    {
        if (indiceDialogo >= dialogos.Length)
        {
            TerminarDialogo();
            return;
        }

        LineaDialogo linea = dialogos[indiceDialogo];

        // Mostrar nombre
        if (textoNombre != null)
            textoNombre.text = linea.nombrePersonaje;

        // Mostrar imagen del personaje
        if (imagenPersonajeUI != null)
        {
            imagenPersonajeUI.sprite = linea.imagenPersonaje;

            // Ocultar la imagen si no se asignó una
            imagenPersonajeUI.enabled =
                linea.imagenPersonaje != null;
        }

        // Mostrar texto progresivamente
        if (corrutinaTexto != null)
            StopCoroutine(corrutinaTexto);

        corrutinaTexto = StartCoroutine(
            EscribirTexto(linea.texto)
        );
    }

    private IEnumerator EscribirTexto(string texto)
    {
        escribiendoTexto = true;
        textoDialogo.text = "";

        foreach (char letra in texto)
        {
            textoDialogo.text += letra;

            yield return new WaitForSecondsRealtime(
                velocidadTexto
            );
        }

        escribiendoTexto = false;
        corrutinaTexto = null;
    }

    private void MostrarTextoCompleto()
    {
        if (corrutinaTexto != null)
        {
            StopCoroutine(corrutinaTexto);
            corrutinaTexto = null;
        }

        textoDialogo.text = dialogos[indiceDialogo].texto;
        escribiendoTexto = false;
    }

    private void AvanzarDialogo()
    {
        indiceDialogo++;
        MostrarDialogo();
    }

    private void TerminarDialogo()
    {
        dialogoActivo = false;
        escribiendoTexto = false;

        if (corrutinaTexto != null)
        {
            StopCoroutine(corrutinaTexto);
            corrutinaTexto = null;
        }

        if (uiDialogo != null)
            uiDialogo.SetActive(false);

        if (pausarJuegoDuranteDialogo)
        {
            Time.timeScale = escalaTiempoAnterior;
        }
    }

    private void OnDisable()
    {
        if (corrutinaTexto != null)
        {
            StopCoroutine(corrutinaTexto);
            corrutinaTexto = null;
        }

        if (dialogoActivo && pausarJuegoDuranteDialogo)
        {
            Time.timeScale = escalaTiempoAnterior;
        }

        dialogoActivo = false;
        escribiendoTexto = false;
    }
}