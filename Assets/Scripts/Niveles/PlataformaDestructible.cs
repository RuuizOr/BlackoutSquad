using UnityEngine;

// Bloque/plataforma que se rompe a balazos o con granadas.
public class PlataformaDestructible : MonoBehaviour, IDañable
{
    [SerializeField] int vida = 4;
    [SerializeField] SpriteRenderer render;
    [SerializeField] int puntos = 20;

    float tiempoFlash;

    public bool RecibirDaño(int cantidad, Vector2 direccion)
    {
        vida -= cantidad;
        tiempoFlash = 0.06f;
        render.color = new Color(1f, 0.7f, 0.6f);
        if (vida <= 0)
        {
            Efectos.ExplosionChica(transform.position + new Vector3(0.5f, 0.5f, 0f));
            if (GameManager.Instancia != null) GameManager.Instancia.SumarPuntos(puntos);
            Destroy(gameObject);
        }
        return true;
    }

    void Update()
    {
        if (tiempoFlash <= 0f) return;
        tiempoFlash -= Time.deltaTime;
        if (tiempoFlash <= 0f) render.color = Color.white;
    }
}
