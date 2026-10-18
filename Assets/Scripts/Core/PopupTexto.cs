using TMPro;
using UnityEngine;

// Texto flotante en el mundo ("+100", "+AMETRALLADORA"). Sube, se desvanece y vuelve al pool.
public class PopupTexto : MonoBehaviour
{
    [SerializeField] TextMeshPro texto;
    [SerializeField] float duracion = 0.9f;
    [SerializeField] float velocidadSubida = 1.6f;

    float tiempo;
    Color colorBase;

    public void Mostrar(string contenido, Color color)
    {
        texto.text = contenido;
        colorBase = color;
        texto.color = color;
        tiempo = 0f;
        transform.localScale = Vector3.one * 0.6f;
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        float t = tiempo / duracion;
        transform.position += Vector3.up * velocidadSubida * (1f - t) * Time.deltaTime;
        // pequeño "pop" de escala al aparecer
        float escala = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) * 3f);
        transform.localScale = Vector3.one * escala;
        Color c = colorBase;
        c.a = t > 0.6f ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f;
        texto.color = c;
        if (tiempo >= duracion) PoolObjetos.Devolver(gameObject);
    }
}
