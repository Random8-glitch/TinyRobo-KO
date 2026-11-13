using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogoNPC : MonoBehaviour
{
    [System.Serializable]
    public class LineaDialogo
    {
        public string nombrePersonaje;

        [TextArea(3, 6)]
        public string texto;

        public Sprite imagenPersonaje;

        [Header("Expresion del NPC")]
        public GameObject quadExpresion;
    }

    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string tagJugador = "Player";

    [Header("Radio de interaccion")]
    [SerializeField] private float radioInteraccion = 3f;

    [SerializeField] private bool mostrarRadio = true;

    [SerializeField] private Color colorRadio = Color.yellow;

    [Header("Tecla de interaccion")]
    [SerializeField] private KeyCode teclaInteractuar = KeyCode.E;

    [Header("UI")]
    [SerializeField] private GameObject uiDialogo;
    [SerializeField] private TMP_Text textoNombre;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private Image imagenPersonajeUI;

    [Header("Dialogo")]
    [SerializeField] private LineaDialogo[] dialogos;
    [SerializeField] private float velocidadTexto = 0.04f;

    [Header("Opciones")]
    [SerializeField] private bool pausarJuegoDuranteDialogo = true;

    [Header("NPC en el escenario")]
    [SerializeField] private GameObject quadBaseNPC;

    private bool jugadorDentroDelRadio;
    private bool dialogoActivo;
    private bool escribiendoTexto;

    private int indiceDialogo;
    private Coroutine corrutinaTexto;
    private float escalaTiempoAnterior = 1f;

    private GameObject quadActivo;

    private void Start()
    {
        if (uiDialogo != null)
        {
            uiDialogo.SetActive(false);
        }

        if (jugador == null)
        {
            GameObject objetoJugador = GameObject.FindGameObjectWithTag(
                tagJugador
            );

            if (objetoJugador != null)
            {
                jugador = objetoJugador.transform;
            }
            else
            {
                Debug.LogWarning(
                    "DialogoNPC: No se encontró un jugador con el tag '" +
                    tagJugador +
                    "'.",
                    this
                );
            }
        }
    }

    private void Update()
    {
        if (jugador == null)
        {
            return;
        }

        float distancia = Vector3.Distance(
            transform.position,
            jugador.position
        );

        jugadorDentroDelRadio =
            distancia <= Mathf.Max(0f, radioInteraccion);

        if (!TeclaInteractuarPresionada())
        {
            return;
        }

        if (dialogoActivo)
        {
            if (escribiendoTexto)
            {
                MostrarTextoCompleto();
            }
            else
            {
                AvanzarDialogo();
            }

            return;
        }

        if (jugadorDentroDelRadio)
        {
            IniciarDialogo();
        }
    }

    private bool TeclaInteractuarPresionada()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

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

            default:
                return false;
        }
    }

    public void IniciarDialogo()
    {
        if (dialogoActivo ||
            dialogos == null ||
            dialogos.Length == 0)
        {
            return;
        }

        dialogoActivo = true;
        indiceDialogo = 0;

        if (uiDialogo != null)
        {
            uiDialogo.SetActive(true);
        }

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

        if (textoNombre != null)
        {
            textoNombre.text = linea.nombrePersonaje;
        }

        if (imagenPersonajeUI != null)
        {
            imagenPersonajeUI.sprite = linea.imagenPersonaje;
            imagenPersonajeUI.enabled =
                linea.imagenPersonaje != null;
        }

        ActualizarQuadNPC(linea.quadExpresion);

        if (corrutinaTexto != null)
        {
            StopCoroutine(corrutinaTexto);
        }

        corrutinaTexto = StartCoroutine(
            EscribirTexto(linea.texto)
        );
    }

    private IEnumerator EscribirTexto(string texto)
    {
        escribiendoTexto = true;

        if (textoDialogo != null)
        {
            textoDialogo.text = string.Empty;
        }

        foreach (char letra in texto)
        {
            if (textoDialogo != null)
            {
                textoDialogo.text += letra;
            }

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

        if (textoDialogo != null)
        {
            textoDialogo.text =
                dialogos[indiceDialogo].texto;
        }

        escribiendoTexto = false;
    }

    private void AvanzarDialogo()
    {
        indiceDialogo++;
        MostrarDialogo();
    }

    private void TerminarDialogo()
    {
        RestaurarQuadBase();

        dialogoActivo = false;
        escribiendoTexto = false;

        if (corrutinaTexto != null)
        {
            StopCoroutine(corrutinaTexto);
            corrutinaTexto = null;
        }

        if (uiDialogo != null)
        {
            uiDialogo.SetActive(false);
        }

        if (pausarJuegoDuranteDialogo)
        {
            Time.timeScale = escalaTiempoAnterior;
        }
    }

    private void OnDisable()
    {
        RestaurarQuadBase();

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

    private void OnDrawGizmos()
    {
        if (!mostrarRadio)
        {
            return;
        }

        Gizmos.color = colorRadio;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(0f, radioInteraccion)
        );
    }

    private void ActualizarQuadNPC(GameObject nuevoQuad)
    {
        if (quadActivo != null)
        {
            quadActivo.SetActive(false);
        }

        if (quadBaseNPC != null)
        {
            quadBaseNPC.SetActive(false);
        }

        if (nuevoQuad != null)
        {
            nuevoQuad.SetActive(true);
            quadActivo = nuevoQuad;
        }
        else
        {
            if (quadBaseNPC != null)
            {
                quadBaseNPC.SetActive(true);
                quadActivo = quadBaseNPC;
            }
            else
            {
                quadActivo = null;
            }
        }
    }

    private void RestaurarQuadBase()
    {
        if (quadActivo != null)
        {
            quadActivo.SetActive(false);
            quadActivo = null;
        }

        if (quadBaseNPC != null)
        {
            quadBaseNPC.SetActive(true);
        }
    }
}