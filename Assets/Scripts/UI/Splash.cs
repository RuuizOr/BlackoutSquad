using System.Collections;
using UnityEngine;

// Pantalla de inicio: título "Blackout Squad" y nombres del equipo con fade in/out.
public class Splash : MonoBehaviour
{
    [SerializeField] CanvasGroup grupoTitulo;
    [SerializeField] CanvasGroup grupoEquipo;
    [SerializeField] string escenaSiguiente = "MainMenu";
    [SerializeField] float duracionFundido = 0.8f;
    [SerializeField] float tiempoVisible = 1.6f;

    bool saltado;

    void Start()
    {
        grupoTitulo.alpha = 0f;
        grupoEquipo.alpha = 0f;
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        // Cualquier botón de aceptar salta el splash
        if (!saltado && ControlesJuego.AceptarPresionado)
        {
            saltado = true;
            StopAllCoroutines();
            Transicion.Cargar(escenaSiguiente);
        }
    }

    IEnumerator Secuencia()
    {
        yield return Fundir(grupoTitulo, 0f, 1f);
        yield return new WaitForSeconds(tiempoVisible);
        yield return Fundir(grupoTitulo, 1f, 0f);
        yield return Fundir(grupoEquipo, 0f, 1f);
        yield return new WaitForSeconds(tiempoVisible);
        yield return Fundir(grupoEquipo, 1f, 0f);
        saltado = true;
        Transicion.Cargar(escenaSiguiente);
    }

    IEnumerator Fundir(CanvasGroup g, float desde, float hasta)
    {
        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(desde, hasta, t / duracionFundido);
            yield return null;
        }
        g.alpha = hasta;
    }
}
