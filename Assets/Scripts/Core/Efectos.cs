using UnityEngine;

// Biblioteca de efectos visuales. Vive en "SistemasJuego" y cualquier script puede pedir
// una explosión, chispas, polvo o un texto flotante con una sola línea.
public class Efectos : MonoBehaviour
{
    public static Efectos Instancia { get; private set; }

    [Header("Explosiones y humo (EfectoAnimado)")]
    [SerializeField] GameObject explosionGrande;
    [SerializeField] GameObject explosionChica;
    [SerializeField] GameObject humo;
    [SerializeField] GameObject fogonazo;

    [Header("Partículas")]
    [SerializeField] GameObject chispas;
    [SerializeField] GameObject escombros;
    [SerializeField] GameObject polvo;
    [SerializeField] GameObject casquillo;

    [Header("Texto flotante")]
    [SerializeField] GameObject popupTexto;

    void Awake()
    {
        Instancia = this;
    }

    // Saca un efecto del pool con la escala del prefab multiplicada por "escala"
    static GameObject Crear(GameObject prefab, Vector3 pos, Quaternion rot, float escala = 1f)
    {
        if (Instancia == null || prefab == null) return null;
        GameObject obj = PoolObjetos.Sacar(prefab, pos, rot);
        obj.transform.localScale = prefab.transform.localScale * escala;
        return obj;
    }

    // Explosión con sonido y sacudida de cámara
    public static void Explosion(Vector3 pos, bool grande)
    {
        if (Instancia == null) return;
        Crear(grande ? Instancia.explosionGrande : Instancia.explosionChica, pos, Quaternion.identity);
        Crear(Instancia.escombros, pos, Quaternion.identity);
        Crear(Instancia.humo, pos + Vector3.up * 0.3f, Quaternion.identity);
        AudioManager.Sonar(grande ? Sfx.ExplosionGrande : Sfx.Explosion);
        CamaraSeguimiento.Sacudir(grande ? 0.35f : 0.18f, grande ? 0.4f : 0.22f);
    }

    public static void ExplosionChica(Vector3 pos)
    {
        if (Instancia == null) return;
        Crear(Instancia.explosionChica, pos, Quaternion.identity);
        Crear(Instancia.chispas, pos, Quaternion.identity);
        AudioManager.Sonar(Sfx.ExplosionChica);
        CamaraSeguimiento.Sacudir(0.1f, 0.15f);
    }

    public static void Impacto(Vector3 pos)
    {
        if (Instancia == null) return;
        Crear(Instancia.chispas, pos, Quaternion.identity);
    }

    public static void Polvo(Vector3 pos)
    {
        if (Instancia == null) return;
        Crear(Instancia.polvo, pos, Quaternion.identity);
    }

    public static void Humo(Vector3 pos)
    {
        if (Instancia == null) return;
        Crear(Instancia.humo, pos, Quaternion.identity, 0.6f);
    }

    public static void Fogonazo(Vector3 pos, Vector2 direccion)
    {
        if (Instancia == null) return;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        Crear(Instancia.fogonazo, pos, Quaternion.Euler(0f, 0f, angulo));
    }

    public static void Casquillo(Vector3 pos, float lado)
    {
        if (Instancia == null) return;
        Crear(Instancia.casquillo, pos + new Vector3(-0.1f * lado, 0f, 0f), Quaternion.identity);
    }

    public static void Popup(Vector3 pos, string texto, Color color)
    {
        if (Instancia == null) return;
        GameObject p = Crear(Instancia.popupTexto, pos, Quaternion.identity);
        if (p != null) p.GetComponent<PopupTexto>().Mostrar(texto, color);
    }
}
