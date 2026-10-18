using System.Collections;
using UnityEngine;

// Al entrar el jugador: cierra la arena, bloquea la cámara, cambia la música,
// muestra la barra de vida del jefe y lo activa.
public class ArenaJefe : MonoBehaviour
{
    [SerializeField] JefeBase jefe;
    [SerializeField] GameObject[] barreras;   // se activan para cerrar la arena
    [SerializeField] float minX;
    [SerializeField] float maxX;
    [SerializeField] float retrasoActivacion = 1.5f;

    bool iniciada;

    public Vector3 PuntoReaparicion => new Vector3(minX + 2f, transform.position.y + 1f, 0f);

    void Start()
    {
        foreach (GameObject b in barreras) if (b != null) b.SetActive(false);
        if (jefe != null) jefe.ConfigurarArena(minX, maxX);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (iniciada || otro.GetComponentInParent<JugadorMovimiento>() == null) return;
        iniciada = true;
        StartCoroutine(Iniciar());
    }

    IEnumerator Iniciar()
    {
        foreach (GameObject b in barreras) if (b != null) b.SetActive(true);
        if (CamaraSeguimiento.Instancia != null) CamaraSeguimiento.Instancia.Bloquear(minX, maxX);
        AudioManager.Sonar(Sfx.AlarmaJefe);
        CamaraSeguimiento.Sacudir(0.2f, 0.6f);
        if (ControladorNivel.Instancia != null) ControladorNivel.Instancia.IniciarJefe(this);
        if (HUD.Instancia != null && jefe != null)
        {
            HUD.Instancia.MostrarAviso("¡PELIGRO!  " + jefe.NombreJefe, 1.8f);
            HUD.Instancia.MostrarJefe(jefe.Salud, jefe.NombreJefe);
        }
        yield return new WaitForSeconds(retrasoActivacion);
        if (jefe != null) jefe.Activar();
    }
}
