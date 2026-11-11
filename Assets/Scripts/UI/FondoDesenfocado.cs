using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Fondo desenfocado para pantallas que van encima del juego (selección de comando, intro tipo cómic):
// toma una foto de lo que se ve en pantalla, la reduce y la desenfoca, y la pone de fondo en un RawImage.
// Mientras se toma la foto, la pantalla que la pide debe estar invisible (alpha 0) para no salir en ella.
public static class FondoDesenfocado
{
    const int Reduccion = 8;        // la foto se reduce a 1/8: más rápido y ya de por sí borroso
    const int PasadasBlur = 3;      // pasadas de desenfoque de caja
    const int RadioBlur = 2;

    // Captura la pantalla al final del cuadro y devuelve la textura desenfocada (o null si no se pudo)
    public static IEnumerator Capturar(Action<Texture2D> listo)
    {
        yield return new WaitForEndOfFrame();
        Texture2D resultado = null;
        try { resultado = CapturarAhora(); }
        catch (Exception e) { Debug.LogWarning("No se pudo crear el fondo desenfocado: " + e.Message); }
        listo?.Invoke(resultado);
    }

    static Texture2D CapturarAhora()
    {
        int w = Screen.width, h = Screen.height;
        if (w < 16 || h < 16) return null;

        RenderTexture completa = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(completa);

        // Reducción en pasos (cada paso promedia píxeles: empieza el desenfoque)
        RenderTexture actual = completa;
        int cw = w, ch = h;
        for (int f = 2; f <= Reduccion; f *= 2)
        {
            cw = Mathf.Max(8, w / f);
            ch = Mathf.Max(8, h / f);
            RenderTexture menor = RenderTexture.GetTemporary(cw, ch, 0, RenderTextureFormat.ARGB32);
            actual.filterMode = FilterMode.Bilinear;
            Graphics.Blit(actual, menor);
            RenderTexture.ReleaseTemporary(actual);
            actual = menor;
        }

        RenderTexture previa = RenderTexture.active;
        RenderTexture.active = actual;
        Texture2D tex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, cw, ch), 0, 0);
        RenderTexture.active = previa;
        RenderTexture.ReleaseTemporary(actual);

        Color32[] px = tex.GetPixels32();
        Color32[] tmp = new Color32[px.Length];
        for (int i = 0; i < PasadasBlur; i++)
        {
            Caja(px, tmp, cw, ch, true);
            Caja(tmp, px, cw, ch, false);
        }
        // En algunas APIs gráficas (DirectX, Vulkan, Metal) la captura sale de cabeza
        if (SystemInfo.graphicsUVStartsAtTop) Voltear(px, cw, ch);
        for (int i = 0; i < px.Length; i++) px[i].a = 255;

        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false, true); // ya no se necesita leer en CPU
        return tex;
    }

    // Desenfoque de caja en una dirección
    static void Caja(Color32[] origen, Color32[] destino, int w, int h, bool horizontal)
    {
        int largo = horizontal ? w : h, lineas = horizontal ? h : w;
        int n = RadioBlur * 2 + 1;
        for (int l = 0; l < lineas; l++)
        {
            int r = 0, g = 0, b = 0;
            for (int k = -RadioBlur; k <= RadioBlur; k++)
            {
                Color32 c = origen[Indice(Mathf.Clamp(k, 0, largo - 1), l, w, horizontal)];
                r += c.r; g += c.g; b += c.b;
            }
            for (int p = 0; p < largo; p++)
            {
                destino[Indice(p, l, w, horizontal)] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                Color32 sale = origen[Indice(Mathf.Clamp(p - RadioBlur, 0, largo - 1), l, w, horizontal)];
                Color32 entra = origen[Indice(Mathf.Clamp(p + RadioBlur + 1, 0, largo - 1), l, w, horizontal)];
                r += entra.r - sale.r; g += entra.g - sale.g; b += entra.b - sale.b;
            }
        }
    }

    static int Indice(int p, int l, int w, bool horizontal) => horizontal ? l * w + p : p * w + l;

    static void Voltear(Color32[] px, int w, int h)
    {
        for (int y = 0; y < h / 2; y++)
        for (int x = 0; x < w; x++)
        {
            int a = y * w + x, b = (h - 1 - y) * w + x;
            Color32 t = px[a]; px[a] = px[b]; px[b] = t;
        }
    }

    // Crea (o reutiliza) el RawImage del fondo como primer hijo de "padre", con una capa oscura encima
    public static RawImage Aplicar(Transform padre, Texture2D tex, float oscuridad)
    {
        Transform existente = padre.Find("FondoDesenfocado");
        RawImage img;
        if (existente != null) img = existente.GetComponent<RawImage>();
        else
        {
            GameObject go = new GameObject("FondoDesenfocado", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            go.transform.SetSiblingIndex(0);
            Estirar((RectTransform)go.transform);
            img = go.AddComponent<RawImage>();
            img.raycastTarget = false;

            GameObject capa = new GameObject("Oscurecer", typeof(RectTransform));
            capa.transform.SetParent(go.transform, false);
            Estirar((RectTransform)capa.transform);
            Image oscuro = capa.AddComponent<Image>();
            oscuro.raycastTarget = false;
            oscuro.color = new Color(0.02f, 0.02f, 0.03f, oscuridad);
        }
        if (img.texture is Texture2D vieja && vieja != tex) UnityEngine.Object.Destroy(vieja);
        img.texture = tex;
        return img;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
