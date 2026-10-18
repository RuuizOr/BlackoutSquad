using UnityEngine;

// Datos de un arma (se crea como asset). Cambiar números aquí cambia el arma sin tocar código.
[CreateAssetMenu(menuName = "Blackout Squad/Arma", fileName = "NuevaArma")]
public class ArmaDatos : ScriptableObject
{
    [Header("Presentación")]
    public string nombre = "PISTOLA";
    public Sprite icono;
    public AudioClip sonidoDisparo;
    [Range(0f, 1f)] public float volumenSonido = 0.7f;
    public float intervaloSonido = 0f; // >0 limita qué tan seguido suena (lanzallamas)

    [Header("Disparo")]
    public GameObject prefabBala;
    public float cadencia = 0.2f;      // segundos entre disparos
    public int daño = 1;
    public float velocidadBala = 18f;
    public float vidaBala = 1.2f;      // segundos (define el alcance)
    public int proyectilesPorDisparo = 1;
    public float dispersionGrados = 0f;
    public bool expulsaCasquillo = true;
    public bool muestraFogonazo = true;

    [Header("Munición (0 = infinita)")]
    public int municion = 0;

    public bool EsInfinita => municion <= 0;
}
