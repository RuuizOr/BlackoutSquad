using UnityEngine;
using UnityEngine.UI;

// Capa de fondo del menú que se desplaza sola (parallax en la interfaz).
[RequireComponent(typeof(RawImage))]
public class FondoAnimadoUI : MonoBehaviour
{
    [SerializeField] float velocidad = 0.02f;
    [SerializeField] float repeticionesX = 2f;

    RawImage imagen;
    float desplazamiento;

    void Awake()
    {
        imagen = GetComponent<RawImage>();
    }

    void Update()
    {
        desplazamiento += velocidad * Time.unscaledDeltaTime;
        imagen.uvRect = new Rect(desplazamiento, 0f, repeticionesX, 1f);
    }
}
