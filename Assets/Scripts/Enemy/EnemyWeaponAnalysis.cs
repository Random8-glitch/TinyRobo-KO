
using UnityEngine;

[RequireComponent(typeof(ManagerWeaponEnemy))]
public class EnemyWeaponAnalysis : MonoBehaviour
{
    [Header("Posiciones de armas")]
    [SerializeField] private Transform armaFrontal;
    [SerializeField] private Transform armaLateralIzquierda;
    [SerializeField] private Transform armaLateralDerecha;

    [Header("Orientación")]
    [SerializeField, Range(0f, 1f)]
    private float pesoOrientacionArma = 0.7f;

    private ManagerWeaponEnemy manager;

    private void Awake()
    {
        manager = GetComponent<ManagerWeaponEnemy>();
    }

    public int CantidadArmas
    {
        get
        {
            return manager != null
                ? manager.EquippedWeaponCount
                : 0;
        }
    }

    public bool TieneArmaFrontal()
    {
        return TieneArmaEnHolder(armaFrontal);
    }

    public bool TieneArmasLaterales()
    {
        return TieneArmaEnHolder(armaLateralIzquierda) ||
               TieneArmaEnHolder(armaLateralDerecha);
    }

    public bool TieneDosArmasLaterales()
    {
        return TieneArmaEnHolder(armaLateralIzquierda) &&
               TieneArmaEnHolder(armaLateralDerecha);
    }

    private bool TieneArmaEnHolder(Transform holder)
    {
        if (holder == null || manager == null)
            return false;

        for (int i = 0; i < manager.WeaponSlotCount; i++)
        {
            Transform holderManager = manager.GetWeaponHolder(i);

            if (holderManager == holder)
            {
                return manager.GetWeaponInstance(i) != null;
            }
        }

        return false;
    }

    public Vector3 ObtenerRumboDeAtaque(
        Transform jugador,
        Vector3 rumboBase)
    {
        if (jugador == null || manager == null)
            return rumboBase;

        Vector3 direccionJugador =
            jugador.position - transform.position;

        direccionJugador.y = 0f;

        if (direccionJugador.sqrMagnitude < 0.001f)
            return rumboBase;

        direccionJugador.Normalize();

        Transform[] holders =
        {
            armaFrontal,
            armaLateralIzquierda,
            armaLateralDerecha
        };

        Vector3 mejorRumbo = rumboBase.normalized;
        float menorGiro = Mathf.Infinity;

        foreach (Transform holder in holders)
        {
            if (holder == null)
                continue;

            // Comprueba si este holder tiene un arma equipada.
            if (!TieneArmaEnHolder(holder))
                continue;

            // Obtiene el arma correspondiente.
            GameObject arma = ObtenerArmaDelHolder(holder);

            if (arma == null)
                continue;

            // Comprueba si el arma está atacando.
            IEnemyWeaponState estadoArma = null;

            MonoBehaviour[] componentes =
                arma.GetComponentsInChildren<MonoBehaviour>();

            foreach (MonoBehaviour componente in componentes)
            {
                if (componente is IEnemyWeaponState estado)
                {
                    estadoArma = estado;
                    break;
                }
            }

            if (estadoArma != null && estadoArma.EstaAtacando)
                continue;

            // Obtiene la orientación local del holder.
            Vector3 direccionLocal =
                transform.InverseTransformDirection(holder.forward);

            direccionLocal.y = 0f;

            if (direccionLocal.sqrMagnitude < 0.001f)
                continue;

            direccionLocal.Normalize();

            float anguloLocal =
                Mathf.Atan2(direccionLocal.x, direccionLocal.z)
                * Mathf.Rad2Deg;

            // Calcula cuánto debe girar el robot para orientar
            // el arma hacia el jugador.
            Vector3 rumboNecesario =
                Quaternion.Euler(0f, -anguloLocal, 0f)
                * direccionJugador;

            rumboNecesario.y = 0f;
            rumboNecesario.Normalize();

            float giroNecesario =
                Vector3.Angle(transform.forward, rumboNecesario);

            if (giroNecesario < menorGiro)
            {
                menorGiro = giroNecesario;
                mejorRumbo = rumboNecesario;
            }
        }

        if (menorGiro == Mathf.Infinity)
            return rumboBase;

        return Vector3.Slerp(
            rumboBase.normalized,
            mejorRumbo,
            pesoOrientacionArma
        ).normalized;
    }

    private GameObject ObtenerArmaDelHolder(Transform holder)
    {
        if (manager == null || holder == null)
            return null;

        for (int i = 0; i < manager.WeaponSlotCount; i++)
        {
            if (manager.GetWeaponHolder(i) == holder)
            {
                return manager.GetWeaponInstance(i);
            }
        }

        return null;
    }
}

