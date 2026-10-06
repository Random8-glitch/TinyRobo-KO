
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EfectoModificador))]
public class EfectoModificadorDrawer : PropertyDrawer
{
    private static readonly string[] stats =
    {
        "VidaMax",
        "Vida"
    };

    private static readonly string[] roboMov =
    {
        "Velocidad",
        "TiempoDeGiro",
        "VelocidadGiroManual",
        "VelocidadGiroMinima",
        "VelocidadGiroMaxima",
        "DistanciaRebote",
        "DistanciaReboteObjetivo",
        "TiempoAturdido"
    };

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var script = property.FindPropertyRelative("scriptObjetivo");
        var atributo = property.FindPropertyRelative("atributo");
        var cantidad = property.FindPropertyRelative("cantidad");

        float linea = EditorGUIUtility.singleLineHeight;
        float espacio = EditorGUIUtility.standardVerticalSpacing;

        Rect r1 = new Rect(position.x, position.y, position.width, linea);
        Rect r2 = new Rect(position.x, position.y + linea + espacio, position.width, linea);
        Rect r3 = new Rect(position.x, position.y + (linea + espacio) * 2, position.width, linea);

        EditorGUI.PropertyField(r1, script, new GUIContent("Script"));

        string[] opciones = script.enumValueIndex == 0 ? stats : roboMov;

        string actual = ((AtributoModificable)atributo.enumValueIndex).ToString();
        int indice = System.Array.IndexOf(opciones, actual);
        if (indice < 0) indice = 0;

        indice = EditorGUI.Popup(r2, "Stat", indice, opciones);

        atributo.enumValueIndex =
            (int)System.Enum.Parse(typeof(AtributoModificable), opciones[indice]);

        EditorGUI.PropertyField(r3, cantidad, new GUIContent("Cantidad"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight +
                EditorGUIUtility.standardVerticalSpacing) * 3;
    }
}
