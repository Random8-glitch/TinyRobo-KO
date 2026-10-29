using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BotonSigPelea : MonoBehaviour
{
    [Header("Escenas candidatas")]
    [Tooltip("Añade nombres de escenas aquí. Se elegirá una al azar al pulsar.")]
    [SerializeField] private List<string> escenas = new List<string>();

    [Tooltip("Nombre de escena por defecto si la lista está vacía.")]
    [SerializeField] private string escenaPorDefecto = "RoboTeam";

    public void Starter()
    {
        string escenaSeleccionada = escenaPorDefecto;

        if (escenas != null && escenas.Count > 0)
        {
            int idx = Random.Range(0, escenas.Count);
            escenaSeleccionada = escenas[idx];
        }

        if (string.IsNullOrEmpty(escenaSeleccionada))
        {
            Debug.LogWarning("BotonSigPelea: no hay una escena válida para cargar.");
            return;
        }

        SceneManager.LoadScene(escenaSeleccionada);
    }
}
