using UnityEngine;

// Cofre: se rompe al dispararle y suelta su objeto.
public class Cofre : MonoBehaviour, IDañable
{
    [SerializeField] int vida = 3;
    [SerializeField] GameObject contenido;
    [SerializeField] SpriteRenderer render;
    [SerializeField] Sprite spriteAbierto;
    [SerializeField] int puntos = 50;

    bool abierto;

    public bool RecibirDaño(int cantidad, Vector2 direccion)
    {
        if (abierto) return false;
        vida -= cantidad;
        transform.position += new Vector3(Random.Range(-0.03f, 0.03f), 0f, 0f);
        if (vida > 0) { AudioManager.Sonar(Sfx.ImpactoBala, 0.5f); return true; }

        abierto = true;
        render.sprite = spriteAbierto;
        AudioManager.Sonar(Sfx.RomperCofre);
        Efectos.Humo(transform.position + Vector3.up * 0.3f);
        Efectos.Impacto(transform.position + Vector3.up * 0.3f);
        if (GameManager.Instancia != null) GameManager.Instancia.SumarPuntos(puntos);
        if (contenido != null) Instantiate(contenido, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        return true;
    }
}
