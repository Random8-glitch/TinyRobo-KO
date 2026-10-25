
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "NuevoModificador",
    menuName = "Modificadores/Nuevo Modificador"
)]
public class ModificadorStats : ScriptableObject
{
    public List<EfectoModificador> efectos = new List<EfectoModificador>();

    [System.NonSerialized]
    public ModificadorStats Origen;
}

public enum TipoScript
{
    Stats,
    RoboMov,
    Weapon
}

public enum AtributoModificable
{
    

    VidaMax,
    Vida,

    Velocidad,
    TiempoDeGiro,
    VelocidadGiroManual,
    VelocidadGiroMinima,
    VelocidadGiroMaxima,
    DistanciaRebote,
    DistanciaReboteObjetivo,
    TiempoAturdido,
    ModificadorMovimiento,

    DañoPlus
}

[System.Serializable]
public class EfectoModificador
{
    public TipoScript scriptObjetivo;
    public AtributoModificable atributo;
    public float cantidad;
}
