using UnityEngine;

// Datos de un comando jugable (se crea como asset). Todos juegan igual: solo cambia la skin.
// Las hojas deben tener los mismos cuadros en el mismo orden que comando_alfa.png.
[CreateAssetMenu(menuName = "Blackout Squad/Personaje", fileName = "NuevoPersonaje")]
public class PersonajeDatos : ScriptableObject
{
    public string nombre = "COMANDO ALFA";
    [TextArea(1, 3)] public string descripcion = "";
    public Sprite[] frames; // 0 idle, 2-5 correr (mismo orden que la hoja)
    public Sprite retrato;  // cara del HUD, junto a las vidas

    [Header("Punta del arma de cada pose, relativa a los pies")]
    public Vector2 bocaFrente = new Vector2(0.72f, 1.07f);
    public Vector2 bocaAgachado = new Vector2(0.72f, 0.72f);
    public Vector2 bocaAire = new Vector2(0.65f, 1f);
    public Vector2 bocaArriba = new Vector2(-0.35f, 2.45f);
}
