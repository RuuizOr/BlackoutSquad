using UnityEngine;

public enum TipoItem { Arma, Botiquin, VidaExtra, Granadas }

// Objeto que flota y se recoge al tocarlo: armas, botiquín, vida extra o granadas.
public class ItemRecogible : MonoBehaviour
{
    [SerializeField] TipoItem tipo = TipoItem.Botiquin;
    [SerializeField] ArmaDatos arma;          // solo si tipo = Arma
    [SerializeField] int cantidad = 40;        // vida que cura o granadas que da
    [SerializeField] string textoPopup = "+BOTIQUÍN";
    [SerializeField] Color colorPopup = new Color(0.6f, 1f, 0.5f);

    [Header("Animación de flotar")]
    [SerializeField] Transform visual;
    [SerializeField] float alturaBase = 0.65f;
    [SerializeField] float alturaFlotar = 0.12f;
    [SerializeField] float velocidadFlotar = 3f;

    float fase;
    bool recogido;

    public TipoItem Tipo => tipo;

    void OnEnable()
    {
        recogido = false;
        fase = Random.value * 6f;
    }

    void Update()
    {
        fase += Time.deltaTime * velocidadFlotar;
        if (visual != null) visual.localPosition = new Vector3(0f, alturaBase + Mathf.Sin(fase) * alturaFlotar, 0f);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (recogido) return;
        JugadorMovimiento jugador = otro.GetComponentInParent<JugadorMovimiento>();
        if (jugador == null || jugador.EstaMuerto) return;
        recogido = true;
        Aplicar(jugador);
        Efectos.Popup(transform.position + Vector3.up * 1.2f, textoPopup, colorPopup);
        Destroy(gameObject);
    }

    void Aplicar(JugadorMovimiento jugador)
    {
        switch (tipo)
        {
            case TipoItem.Arma:
                jugador.GetComponent<JugadorDisparo>().EquiparArma(arma);
                AudioManager.Sonar(Sfx.RecogerArma);
                break;
            case TipoItem.Botiquin:
                jugador.GetComponent<VidaJugador>().Curar(cantidad);
                AudioManager.Sonar(Sfx.RecogerObjeto);
                break;
            case TipoItem.VidaExtra:
                if (GameManager.Instancia != null) GameManager.Instancia.GanarVida();
                AudioManager.Sonar(Sfx.VidaExtra);
                break;
            case TipoItem.Granadas:
                jugador.GetComponent<JugadorDisparo>().SumarGranadas(cantidad);
                AudioManager.Sonar(Sfx.RecogerObjeto);
                break;
        }
    }
}
