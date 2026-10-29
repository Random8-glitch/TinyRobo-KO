using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class MovimientoPrimeraPersona : MonoBehaviour
{
    [Header("Escena donde funciona el movimiento")]
    [SerializeField] private string escenaPermitida = "Nivel1";

    [Header("Referencias")]
    [SerializeField] private Transform camaraJugador;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float velocidadCorrer = 9f;
    [SerializeField] private float fuerzaSalto = 1.5f;
    [SerializeField] private float gravedad = -20f;

    [Header("Camara y raton")]
    [SerializeField] private float sensibilidadMouse = 2f;
    [SerializeField] private float limiteVertical = 85f;

    private CharacterController controlador;
    private float rotacionVertical = 0f;
    private float velocidadVertical = 0f;
    private bool movimientoHabilitado = false;

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();
        ComprobarEscena();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += AlCambiarEscena;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= AlCambiarEscena;
    }

    private void OnDestroy()
    {
        LiberarCursor();
    }

    private void ComprobarEscena()
    {
        movimientoHabilitado =
            SceneManager.GetActiveScene().name == escenaPermitida;

        if (movimientoHabilitado)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            LiberarCursor();
        }
    }

    private void AlCambiarEscena(Scene escenaAnterior, Scene escenaNueva)
    {
        ComprobarEscena();
    }

    private void Update()
    {
        if (!movimientoHabilitado)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LiberarCursor();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        MoverJugador();
        MirarConMouse();
    }

    private void MoverJugador()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Correr mientras se mantiene presionada la tecla Shift.
        float velocidadActual =
            (Input.GetKey(KeyCode.LeftShift) ||
             Input.GetKey(KeyCode.RightShift))
            ? velocidadCorrer
            : velocidad;

        Vector3 movimiento =
            transform.right * horizontal +
            transform.forward * vertical;

        movimiento = Vector3.ClampMagnitude(movimiento, 1f);
        movimiento *= velocidadActual;

        if (controlador.isGrounded && velocidadVertical < 0f)
        {
            velocidadVertical = -2f;
        }

        if (controlador.isGrounded &&
            Input.GetKeyDown(KeyCode.Space))
        {
            velocidadVertical =
                Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
        }

        velocidadVertical += gravedad * Time.deltaTime;
        movimiento.y = velocidadVertical;

        controlador.Move(movimiento * Time.deltaTime);
    }

    private void MirarConMouse()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (camaraJugador == null)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * sensibilidadMouse;

        float mouseY =
            Input.GetAxis("Mouse Y") * sensibilidadMouse;

        // Girar al jugador horizontalmente.
        transform.Rotate(Vector3.up * mouseX);

        // Girar la camara verticalmente.
        rotacionVertical -= mouseY;

        rotacionVertical = Mathf.Clamp(
            rotacionVertical,
            -limiteVertical,
            limiteVertical
        );

        camaraJugador.localRotation =
            Quaternion.Euler(rotacionVertical, 0f, 0f);
    }

    private void LiberarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
