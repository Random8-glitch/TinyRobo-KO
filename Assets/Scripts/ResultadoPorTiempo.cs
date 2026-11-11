using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoPorTiempo : MonoBehaviour
{
    private bool resultadoComprobado;

    public void ComprobarResultado()
    {
        if (resultadoComprobado)
            return;

        resultadoComprobado = true;

        PlayerStats jugador = FindFirstObjectByType<PlayerStats>();
        EnemyStats enemigo = FindFirstObjectByType<EnemyStats>();

        if (jugador == null)
        {
            Derrota derrota = FindFirstObjectByType<Derrota>();

            if (derrota != null)
                derrota.DerrotaPorTiempo();

            return;
        }

        if (enemigo == null || jugador.vida > enemigo.vida)
        {
            Victoria victoria = FindFirstObjectByType<Victoria>();

            if (victoria != null)
                victoria.VictoriaPorTiempo();

            return;
        }

        Derrota derrotaPorVida = FindFirstObjectByType<Derrota>();

        if (derrotaPorVida != null)
            derrotaPorVida.DerrotaPorTiempo();
    }
}