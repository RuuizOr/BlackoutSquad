using UnityEngine;

// Capa de fondo con parallax. factor 0 = se mueve junto a la cámara (muy lejos),
// factor 1 = queda fija en el mundo (cerca). El sprite usa modo "Tiled" para repetirse.
// Se ejecuta después de la cámara para que no tiemble.
[DefaultExecutionOrder(100)]
public class ParallaxCapa : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float factorX = 0.3f;
    [SerializeField, Range(0f, 1f)] float factorY = 0.1f;
    [SerializeField] float alturaBase = 0f;   // posición Y cuando la cámara está abajo
    [SerializeField] bool desplazamientoAutomatico;
    [SerializeField] float velocidadAutomatica = 0.3f;

    Transform cam;
    float desplazamiento;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        if (desplazamientoAutomatico) desplazamiento += velocidadAutomatica * Time.deltaTime;
        float x = cam.position.x * (1f - factorX) - desplazamiento;
        float y = alturaBase + cam.position.y * (1f - factorY);
        transform.position = new Vector3(x, y, transform.position.z);
    }
}
