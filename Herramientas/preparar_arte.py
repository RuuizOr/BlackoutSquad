# -*- coding: utf-8 -*-
"""
Blackout Squad - Preparación de arte (se ejecutó una vez durante el desarrollo).

Toma los packs descargados (carpeta de descargas) y genera hojas de sprites
ordenadas en Assets/Art/Generado, todas con:
  - celdas del mismo tamaño
  - personaje mirando a la DERECHA
  - pies tocando el borde inferior de la celda (pivote abajo al centro en Unity)

Uso:  python preparar_arte.py <carpeta_descargas> <carpeta_proyecto>
Requiere Pillow.
"""
import os, sys, shutil, random
from collections import deque
from PIL import Image, ImageDraw, ImageOps, ImageEnhance

DL = sys.argv[1]
PROY = sys.argv[2]
ART = os.path.join(PROY, "Assets", "Art")
GEN = os.path.join(ART, "Generado")
random.seed(7)


def ruta(*p):
    return os.path.join(*p)


def asegurar(d):
    os.makedirs(d, exist_ok=True)
    return d


def guardar(img, *partes):
    p = ruta(GEN, *partes)
    asegurar(os.path.dirname(p))
    img.save(p)
    print("  generado:", os.path.relpath(p, PROY))


# ---------------------------------------------------------------- utilidades
def recortar_opaco(img):
    caja = img.getbbox()
    return img.crop(caja) if caja else img


def centro_pies(img, filas=10):
    """Centro X de los píxeles opacos en las últimas filas (los pies)."""
    px = img.load()
    W, H = img.size
    xs = [x for y in range(max(0, H - filas), H) for x in range(W) if px[x, y][3] > 0]
    return (min(xs) + max(xs)) / 2 if xs else W / 2


def hoja_celdas(frames, cw, ch, alinear_pies=True, ancla_x=None):
    """Coloca cada frame en una celda cw x ch con los pies abajo y centrados."""
    hoja = Image.new("RGBA", (cw * len(frames), ch), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        f = recortar_opaco(f)
        cx = ancla_x(f) if ancla_x else (centro_pies(f) if alinear_pies else f.width / 2)
        x = int(round(i * cw + cw / 2 - cx))
        y = ch - f.height
        hoja.alpha_composite(f, (max(i * cw, x), max(0, y)))
    return hoja


def recolorear(img, mapa, tolerancia=40):
    """Cambia colores aproximados según mapa {(r,g,b): (r,g,b)} conservando luz."""
    img = img.copy()
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            for (cr, cg, cb), (nr, ng, nb) in mapa.items():
                if abs(r - cr) + abs(g - cg) + abs(b - cb) < tolerancia:
                    px[x, y] = (nr, ng, nb, a)
                    break
    return img


def tonos(img):
    """Lista de colores más usados (para depurar recoloreos)."""
    from collections import Counter
    c = Counter(img.getpixel((x, y))[:3] for y in range(img.height) for x in range(img.width) if img.getpixel((x, y))[3] > 0)
    return c.most_common(24)


def matiz_hacia(img, color, fuerza=0.65):
    """Tiñe una imagen hacia un color conservando su luminosidad."""
    img = img.copy()
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            l = (r * 0.3 + g * 0.59 + b * 0.11) / 255
            tr, tg, tb = [int(c * (0.35 + l * 0.9)) for c in color]
            px[x, y] = (
                min(255, int(r * (1 - fuerza) + tr * fuerza)),
                min(255, int(g * (1 - fuerza) + tg * fuerza)),
                min(255, int(b * (1 - fuerza) + tb * fuerza)),
                a,
            )
    return img


def contorno(img, color=(20, 20, 20, 255)):
    """Agrega un contorno de 1 px alrededor de los píxeles opacos."""
    W, H = img.size
    base = Image.new("RGBA", (W + 2, H + 2), (0, 0, 0, 0))
    base.alpha_composite(img, (1, 1))
    px = base.load()
    out = base.copy()
    po = out.load()
    for y in range(H + 2):
        for x in range(W + 2):
            if px[x, y][3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W + 2 and 0 <= ny < H + 2 and px[nx, ny][3] > 0:
                        po[x, y] = color
                        break
    return out


def bob(img, dy):
    """Desplaza el torso (mitad superior) dy píxeles: simula paso al caminar."""
    W, H = img.size
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    mitad = int(H * 0.55)
    out.alpha_composite(img.crop((0, mitad, W, H)), (0, mitad))
    sup = img.crop((0, 0, W, mitad))
    out.alpha_composite(sup, (0, dy))
    return out


# ---------------------------------------------------------------- packs
FIERCE = ruta(DL, "fierce")
JUNGLE = ruta(DL, "jungle", "opp1_jungle_tiles")
MOUNTAIN = ruta(DL, "bgmtn", "parallax_mountain_pack", "layers")
INDUST = ruta(DL, "bgind", "parallax-industrial-pack", "layers")


def copiar_packs():
    """Copia los packs originales a Assets/Art/<pack> con su licencia."""
    print("Copiando packs originales...")
    d = asegurar(ruta(ART, "FierceSoldier_Vircon32"))
    for f in os.listdir(FIERCE):
        shutil.copy(ruta(FIERCE, f), ruta(d, f))
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("Fierce Soldier sprites - Vircon32 (Carra)\nLicencia: CC-BY 4.0 https://creativecommons.org/licenses/by/4.0/\n"
                 "Origen: https://opengameart.org/content/fierce-soldier-sprites\n")

    d = asegurar(ruta(ART, "OPP2017_Jungle_Hapiel"))
    for sub in ["environment/tiles/jungle", "environment/background"]:
        dst = asegurar(ruta(d, *sub.split("/")))
        src = ruta(JUNGLE, *sub.split("/"))
        for root, _, files in os.walk(src):
            for f in files:
                if f.endswith(".png"):
                    shutil.copy(ruta(root, f), ruta(dst, f))
    obj = asegurar(ruta(d, "environment", "objects"))
    shutil.copy(ruta(JUNGLE, "environment", "objects", "obj_carniplant.png"), obj)
    shutil.copy(ruta(JUNGLE, "license cc0 - public domain.txt"), ruta(d, "LICENCIA_CC0.txt"))
    shutil.copy(ruta(JUNGLE, "readme.txt"), ruta(d, "readme.txt"))

    d = asegurar(ruta(ART, "ParallaxMountain_ansimuz"))
    for f in os.listdir(MOUNTAIN):
        shutil.copy(ruta(MOUNTAIN, f), d)
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("Mountain at Dusk Background - ansimuz\nLicencia: CC0\nOrigen: https://opengameart.org/content/mountain-at-dusk-background\n")

    d = asegurar(ruta(ART, "ParallaxIndustrial_ansimuz"))
    for f in os.listdir(INDUST):
        shutil.copy(ruta(INDUST, f), ruta(d, f.replace("skill-desc_", "industrial_")))
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("Industrial Parallax Background - ansimuz\nLicencia: CC0\nOrigen: https://opengameart.org/content/industrial-parallax-background\n")

    d = asegurar(ruta(ART, "Tank_2DTyrant"))
    shutil.copy(ruta(DL, "tank", "tank_right_strip3.png"), d)
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("2D Side scrolling tank - 2D Tyrant\nLicencia: CC0\nOrigen: https://opengameart.org/content/2d-side-scrolling-tank\n")

    d = asegurar(ruta(ART, "Explosions_Jetrel"))
    shutil.copy(ruta(DL, "expl", "explosion3.png"), d)
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("explosion animations - Jetrel\nLicencia: CC0\nOrigen: https://opengameart.org/content/explosion-animations\n")

    d = asegurar(ruta(ART, "TreasureChests_Blarumyrran"))
    shutil.copy(ruta(DL, "chests", "chests_32x32.png"), d)
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("Treasure chests 32x32 - Blarumyrran\nLicencia: CC0\nOrigen: https://opengameart.org/content/treasure-chests-32x32-and-16x16\n")

    d = asegurar(ruta(ART, "FirearmIcons_AntumDeluge"))
    for f in os.listdir(ruta(DL, "guns", "32x32")):
        shutil.copy(ruta(DL, "guns", "32x32", f), d)
    for f in ["firearm-turacept.txt", "firearm-7Soul1.txt"]:
        shutil.copy(ruta(DL, "guns", f), d)
    with open(ruta(d, "LICENCIA.txt"), "w", encoding="utf-8") as fh:
        fh.write("CC0 Firearm Icons - AntumDeluge (arte de turacept)\nLicencia: CC0\nOrigen: https://opengameart.org/content/cc0-firearm-icons\n")


# ---------------------------------------------------------------- personajes
def cortar(img, caja):
    return img.crop(caja[:4]).convert("RGBA")


HERO = [
    (5, 5, 61, 82), (66, 5, 122, 82), (127, 18, 187, 82), (192, 18, 252, 82),
    (267, 5, 324, 89), (329, 5, 386, 89),
    (5, 87, 62, 171), (67, 89, 124, 171), (129, 87, 186, 171), (191, 89, 248, 171),
    (271, 94, 355, 137), (292, 143, 337, 260),
    (5, 176, 62, 260), (67, 178, 124, 260), (129, 176, 186, 260), (191, 178, 248, 260),
    (260, 189, 269, 198), (260, 202, 269, 211),
]
ENEM = {
    "soldado": (6, 5, 63, 89), "soldado_disp": (68, 5, 125, 89), "torreta": (142, 14, 216, 89),
    "misil1": (230, 15, 294, 42), "misil2": (230, 45, 298, 72), "disco": (307, 15, 367, 75),
    "cañon_doble": (376, 21, 425, 47), "bala_amarilla": (431, 23, 440, 32),
    "cañon": (378, 56, 417, 93), "cañon_fuego": (378, 96, 417, 133),
    "rueda_roja": (272, 83, 319, 130), "rueda_azul": (324, 83, 371, 130), "bola": (425, 104, 446, 125),
    "jefe_rojo": (6, 102, 133, 241), "jefe_azul": (138, 102, 265, 241), "jefe_ancho": (270, 146, 447, 241),
}


def generar_personajes():
    print("Personajes...")
    h = Image.open(ruta(FIERCE, "main_character.png")).convert("RGBA")
    f = [cortar(h, c) for c in HERO]
    # orden: 0 idle, 1 idle_disparo, 2-5 correr, 6-9 correr_disparo, 10 salto, 11 salto_disparo,
    #        12 agachado, 13 agachado_disparo, 14 apuntar_arriba, 15 muerte
    orden = [f[4], f[5], f[6], f[7], f[8], f[9], f[12], f[13], f[14], f[15],
             f[0], f[1], f[2], f[3], f[11], f[10]]
    guardar(hoja_celdas(orden, 96, 120), "Personajes", "comando_alfa.png")

    e = Image.open(ruta(FIERCE, "enemies_and_bosses.png")).convert("RGBA")
    sold = ImageOps.mirror(cortar(e, ENEM["soldado"]))
    sold_d = ImageOps.mirror(cortar(e, ENEM["soldado_disp"]))

    def set_soldado(base, disp, nombre):
        frames = [base, disp, bob(base, 1), bob(base, 2)]
        guardar(hoja_celdas(frames, 72, 96), "Personajes", nombre)

    set_soldado(sold, sold_d, "soldado_raso.png")

    # élite: camisa roja oscura y pantalón gris
    paleta = [c for c, _ in tonos(sold)]
    elite_map = construir_mapa(paleta, verde=(140, 40, 34), azul=(70, 70, 80))
    set_soldado(recolorear(sold, elite_map, 6), recolorear(sold_d, elite_map, 6), "soldado_elite.png")

    # escudo: soldado + escudo antidisturbios dibujado al frente
    def con_escudo(img):
        W, H = img.size
        lienzo = Image.new("RGBA", (W + 10, H), (0, 0, 0, 0))
        lienzo.alpha_composite(img, (0, 0))
        d = ImageDraw.Draw(lienzo)
        x0, y0, x1, y1 = W - 26, 18, W + 6, H - 8
        d.rectangle((x0, y0, x1, y1), fill=(30, 30, 34, 255))
        d.rectangle((x0 + 2, y0 + 2, x1 - 2, y1 - 2), fill=(110, 118, 108, 255))
        d.rectangle((x0 + 4, y0 + 4, x1 - 4, y1 - 4), fill=(138, 146, 128, 255))
        d.rectangle((x0 + 7, y0 + 8, x1 - 7, y0 + 16), fill=(40, 60, 70, 255))  # visor
        d.line((x0 + 8, y0 + 9, x1 - 9, y0 + 9), fill=(140, 200, 220, 255))
        for yy in range(y0 + 22, y1 - 4, 10):
            d.line((x0 + 5, yy, x1 - 5, yy), fill=(96, 104, 92, 255))
        d.line((x0 + 3, y0 + 3, x0 + 3, y1 - 3), fill=(180, 186, 166, 255))
        return lienzo

    set_soldado(con_escudo(sold), con_escudo(sold_d), "soldado_escudo.png")

    # Kruger: armadura gris acero, pantalón negro, tamaño grande se aplica en Unity (escala)
    kr_map = construir_mapa(paleta, verde=(120, 126, 134), azul=(46, 44, 52))
    k1 = contorno(recolorear(sold, kr_map, 6), (10, 10, 12, 255))
    k2 = contorno(recolorear(sold_d, kr_map, 6), (10, 10, 12, 255))
    guardar(hoja_celdas([k1, k2, bob(k1, 1), bob(k1, 2)], 80, 100), "Personajes", "kruger.png")

    # torreta (mira a la derecha) + variante disparando
    t = ImageOps.mirror(cortar(e, ENEM["torreta"]))
    t2 = t.copy()
    d = ImageDraw.Draw(t2)
    # fogonazo en la boca del cañón (lado derecho, zona superior)
    bx, by = t.width - 2, 22
    d.polygon([(bx - 4, by - 8), (bx + 0, by), (bx - 4, by + 8)], fill=(255, 230, 90, 255))
    guardar(hoja_celdas([t, t2], 96, 96), "Personajes", "torreta.png")

    # tanque: aclarado para que contraste con fondos oscuros
    tk = Image.open(ruta(DL, "tank", "tank_right_strip3.png")).convert("RGBA")
    cajas = [(38, 82, 174, 134), (238, 82, 374, 134), (438, 82, 574, 134)]
    tframes = []
    for c in cajas:
        fr = tk.crop(c)
        fr = ImageEnhance.Brightness(fr).enhance(2.1)
        fr = matiz_hacia(fr, (150, 160, 90), 0.35)
        tframes.append(contorno(fr, (14, 18, 10, 255)))
    guardar(hoja_celdas(tframes, 144, 64, alinear_pies=False), "Personajes", "tanque.png")

    # jefes / estructuras
    guardar(cortar(e, ENEM["jefe_rojo"]), "Personajes", "jefe_campamento.png")
    guardar(cortar(e, ENEM["jefe_azul"]), "Personajes", "punto_control.png")
    guardar(cortar(e, ENEM["jefe_ancho"]), "Personajes", "maquina_fortaleza.png")
    c1 = ImageOps.mirror(cortar(e, ENEM["cañon"]))
    c2 = ImageOps.mirror(cortar(e, ENEM["cañon_fuego"]))
    guardar(hoja_celdas([c1, c2], 48, 48, alinear_pies=False), "Personajes", "cañon_jefe.png")


def construir_mapa(paleta, verde, azul):
    """Mapea tonos verdes de la camisa/casco y azules del pantalón a nuevos colores."""
    mapa = {}
    for (r, g, b) in paleta:
        l = (r + g + b) / 3
        if g > r + 8 and g > b + 8:  # verde
            k = 0.45 + l / 160
            mapa[(r, g, b)] = tuple(min(255, int(c * k)) for c in verde)
        elif b > r + 20 and b > g:  # azul
            k = 0.45 + l / 160
            mapa[(r, g, b)] = tuple(min(255, int(c * k)) for c in azul)
    return mapa


# ---------------------------------------------------------------- proyectiles y efectos
def generar_proyectiles():
    print("Proyectiles...")
    h = Image.open(ruta(FIERCE, "main_character.png")).convert("RGBA")
    e = Image.open(ruta(FIERCE, "enemies_and_bosses.png")).convert("RGBA")
    guardar(cortar(h, HERO[16]), "Proyectiles", "bala_jugador.png")
    guardar(cortar(h, HERO[17]), "Proyectiles", "bala_perdigon.png")
    guardar(cortar(e, ENEM["bala_amarilla"]), "Proyectiles", "bala_enemiga.png")
    m1 = ImageOps.mirror(cortar(e, ENEM["misil1"]))
    m2 = ImageOps.mirror(cortar(e, ENEM["misil2"]))
    guardar(hoja_celdas([m1, m2], 72, 32, alinear_pies=False), "Proyectiles", "misil.png")
    guardar(cortar(e, ENEM["bola"]), "Proyectiles", "obus.png")

    # granada (14x16) verde oliva
    g = Image.new("RGBA", (14, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    d.ellipse((1, 3, 12, 15), fill=(20, 24, 16, 255))
    d.ellipse((2, 4, 11, 14), fill=(84, 98, 52, 255))
    d.line((4, 6, 4, 11), fill=(128, 146, 80, 255))
    d.line((2, 9, 11, 9), fill=(56, 66, 34, 255))
    d.rectangle((5, 0, 8, 3), fill=(150, 150, 150, 255))
    d.point((9, 1), fill=(200, 200, 200, 255))
    guardar(g, "Proyectiles", "granada.png")

    # llama del lanzallamas: 4 frames
    frames = []
    for i in range(4):
        im = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        r = 6 + i * 2
        d.ellipse((12 - r, 12 - r, 12 + r, 12 + r), fill=(230, 70, 20, 230 - i * 40))
        r2 = max(2, r - 3)
        d.ellipse((12 - r2, 12 - r2, 12 + r2, 12 + r2), fill=(255, 170, 40, 240 - i * 40))
        r3 = max(1, r - 6)
        d.ellipse((12 - r3, 12 - r3, 12 + r3, 12 + r3), fill=(255, 245, 170, 255 - i * 50))
        frames.append(im)
    hoja = Image.new("RGBA", (96, 24), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        hoja.alpha_composite(fr, (i * 24, 0))
    guardar(hoja, "Proyectiles", "llama.png")


def generar_efectos():
    print("Efectos...")
    ex = Image.open(ruta(DL, "expl", "explosion3.png")).convert("RGBA")
    fondo = (111, 109, 81)

    def celdas(xs, y0, y1):
        out = []
        for i in range(len(xs) - 1):
            c = ex.crop((xs[i] + 3, y0 + 3, xs[i + 1] - 3, y1 - 3)).copy()
            px = c.load()
            for y in range(c.height):
                for x in range(c.width):
                    if px[x, y][:3] == fondo:
                        px[x, y] = (0, 0, 0, 0)
            out.append(c)
        return out

    def tira(frames, celda):
        hoja = Image.new("RGBA", (celda * len(frames), celda), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            hoja.alpha_composite(f, (i * celda + (celda - f.width) // 2, (celda - f.height) // 2))
        return hoja

    big = celdas([0, 92, 184, 276, 368, 460], 0, 91) + celdas([0, 92, 184, 276, 368, 460], 91, 182)
    big = big[1:]  # el primer frame es casi vacío
    guardar(tira(big, 92), "Efectos", "explosion_grande.png")
    med = celdas([0, 60, 119, 179, 239, 299], 184, 243) + celdas([0, 60, 119, 179, 239, 299], 243, 302)
    guardar(tira(med[1:], 60), "Efectos", "explosion_chica.png")
    humo = celdas([0, 49, 99, 149, 199, 249, 299], 302, 355)
    guardar(tira(humo, 50), "Efectos", "humo.png")

    # fogonazo (muzzle flash) 3 frames 16x16
    hoja = Image.new("RGBA", (48, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(hoja)
    for i in range(3):
        ox = i * 16
        s = 7 - i * 2
        d.polygon([(ox + 1, 8 - s // 2), (ox + 15 - i * 2, 8), (ox + 1, 8 + s // 2)], fill=(255, 200, 60, 255))
        d.polygon([(ox + 4, 8 - s), (ox + 8, 8), (ox + 4, 8 + s)], fill=(255, 140, 30, 255))
        d.ellipse((ox + 1, 8 - s // 2, ox + 1 + s, 8 + s // 2), fill=(255, 250, 200, 255))
    guardar(hoja, "Efectos", "fogonazo.png")

    # casquillo, chispa, píxel blanco (partículas)
    c = Image.new("RGBA", (4, 2), (214, 170, 60, 255))
    c.putpixel((3, 0), (120, 90, 30, 255)); c.putpixel((3, 1), (120, 90, 30, 255))
    guardar(c, "Efectos", "casquillo.png")
    p = Image.new("RGBA", (4, 4), (255, 255, 255, 255))
    guardar(p, "Efectos", "particula.png")
    ch = Image.new("RGBA", (6, 6), (0, 0, 0, 0))
    d = ImageDraw.Draw(ch)
    d.line((0, 3, 5, 3), fill=(255, 255, 255, 255)); d.line((3, 0, 3, 5), fill=(255, 255, 255, 255))
    guardar(ch, "Efectos", "chispa.png")


# ---------------------------------------------------------------- objetos
def generar_objetos():
    print("Objetos e íconos...")
    G = ruta(DL, "guns", "32x32")
    pist = Image.open(ruta(G, "pistol.png")).convert("RGBA")
    thomp = Image.open(ruta(G, "thompson.png")).convert("RGBA")
    rifle = Image.open(ruta(G, "rifle.png")).convert("RGBA")
    guardar(pist, "Objetos", "icono_pistola.png")
    guardar(thomp, "Objetos", "icono_ametralladora.png")

    # escopeta: dibujada (doble cañón, culata de madera) 32x32
    esc = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(esc)
    d.rectangle((10, 12, 31, 14), fill=(30, 30, 34, 255))
    d.rectangle((10, 15, 30, 16), fill=(60, 60, 66, 255))
    d.rectangle((14, 17, 22, 18), fill=(110, 70, 40, 255))
    d.polygon([(1, 16), (11, 12), (13, 17), (9, 21), (2, 22)], fill=(130, 80, 40, 255))
    d.line((2, 21, 10, 18), fill=(90, 54, 26, 255))
    d.rectangle((11, 17, 13, 21), fill=(40, 40, 40, 255))
    guardar(contorno(esc, (12, 12, 12, 255)).crop((1, 1, 33, 33)), "Objetos", "icono_escopeta.png")

    # lanzallamas: tanque + boquilla 32x32
    ll = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(ll)
    d.rounded_rectangle((2, 10, 11, 25), 3, fill=(170, 40, 30, 255))
    d.line((4, 12, 4, 23), fill=(220, 90, 70, 255))
    d.rectangle((11, 14, 27, 17), fill=(60, 60, 64, 255))
    d.rectangle((27, 13, 30, 18), fill=(40, 40, 44, 255))
    d.rectangle((14, 18, 17, 23), fill=(50, 50, 50, 255))
    d.line((6, 10, 14, 14), fill=(30, 30, 30, 255))
    d.point((31, 15), fill=(255, 170, 40, 255))
    guardar(contorno(ll, (12, 12, 12, 255)).crop((1, 1, 33, 33)), "Objetos", "icono_lanzallamas.png")

    # botiquín verde oliva con cruz blanca (sin usar el emblema de la Cruz Roja)
    b = Image.new("RGBA", (24, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(b)
    d.rectangle((0, 3, 23, 19), fill=(20, 24, 16, 255))
    d.rectangle((1, 4, 22, 18), fill=(96, 112, 60, 255))
    d.rectangle((1, 4, 22, 6), fill=(126, 146, 80, 255))
    d.rectangle((8, 0, 15, 3), fill=(20, 24, 16, 255))
    d.rectangle((9, 1, 14, 3), fill=(70, 80, 44, 255))
    d.rectangle((10, 7, 13, 16), fill=(245, 245, 235, 255))
    d.rectangle((7, 10, 16, 13), fill=(245, 245, 235, 255))
    guardar(b, "Objetos", "botiquin.png")

    # vida extra: retrato del héroe (del GUI de Fierce Soldier)
    sg = Image.open(ruta(FIERCE, "scenery_and_gui.png")).convert("RGBA")
    marco = sg.crop((236, 75, 310, 115))
    cara = marco.crop((8, 8, 32, 32))
    guardar(cara, "Objetos", "vida_extra.png")
    guardar(cara, "UI", "icono_vida.png")

    # cofres (Blarumyrran): madera cerrado/abierto y metal cerrado/abierto
    ch = Image.open(ruta(DL, "chests", "chests_32x32.png")).convert("RGBA")
    guardar(ch.crop((32, 0, 64, 32)), "Objetos", "cofre_cerrado.png")
    guardar(ch.crop((0, 0, 32, 32)), "Objetos", "cofre_abierto.png")
    guardar(ch.crop((32, 64, 64, 96)), "Objetos", "cofre_metal_cerrado.png")
    guardar(ch.crop((0, 64, 32, 96)), "Objetos", "cofre_metal_abierto.png")

    # granada como ícono de HUD
    gr = Image.open(ruta(GEN, "Proyectiles", "granada.png"))
    guardar(gr.resize((28, 32), Image.NEAREST), "UI", "icono_granada.png")

    # bandera de checkpoint: 2 frames (gris = inactiva, naranja = activa) 32x64
    hoja = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(hoja)
    for i, col in enumerate([(120, 120, 110), (236, 120, 30)]):
        ox = i * 32
        d.rectangle((ox + 6, 6, ox + 8, 63), fill=(40, 40, 40, 255))
        d.rectangle((ox + 7, 6, ox + 7, 63), fill=(170, 170, 170, 255))
        d.rectangle((ox + 3, 58, ox + 12, 63), fill=(60, 60, 60, 255))
        d.polygon([(ox + 9, 8), (ox + 29, 14), (ox + 9, 22)], fill=col + (255,))
        d.line((ox + 10, 12, ox + 20, 14), fill=tuple(min(255, c + 50) for c in col) + (255,))
        d.ellipse((ox + 5, 3, ox + 9, 7), fill=(230, 200, 60, 255))
    guardar(hoja, "Objetos", "bandera.png")

    # alambre de púas 32x32 (sobre el suelo)
    a = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(a)
    for x in (2, 29):
        d.rectangle((x, 10, x + 1, 31), fill=(80, 60, 40, 255))
    for k in range(3):
        y = 14 + k * 6
        for x in range(0, 32, 2):
            yy = y + (1 if (x // 2 + k) % 2 else 0)
            d.point((x, yy), fill=(170, 170, 176, 255)); d.point((x + 1, yy), fill=(120, 120, 126, 255))
        for x in range(3, 32, 7):
            d.line((x - 2, y - 2, x + 2, y + 2), fill=(200, 200, 206, 255))
            d.line((x - 2, y + 2, x + 2, y - 2), fill=(200, 200, 206, 255))
    guardar(a, "Objetos", "alambre_puas.png")

    # contenedores 96x64 (oliva y óxido)
    for nombre, col in (("contenedor_verde", (78, 96, 52)), ("contenedor_oxido", (140, 70, 40))):
        c = Image.new("RGBA", (96, 64), (0, 0, 0, 255))
        d = ImageDraw.Draw(c)
        d.rectangle((1, 1, 94, 62), fill=col + (255,))
        claro = tuple(min(255, v + 30) for v in col) + (255,)
        oscuro = tuple(max(0, v - 30) for v in col) + (255,)
        for x in range(4, 92, 6):
            d.line((x, 4, x, 59), fill=oscuro)
            d.line((x + 1, 4, x + 1, 59), fill=claro)
        d.rectangle((1, 1, 94, 4), fill=claro); d.rectangle((1, 59, 94, 62), fill=oscuro)
        d.rectangle((70, 10, 72, 54), fill=(30, 30, 30, 255)); d.rectangle((80, 10, 82, 54), fill=(30, 30, 30, 255))
        d.text((12, 24), "BS-07", fill=(230, 220, 190, 255))
        guardar(c, "Objetos", nombre + ".png")

    # caja de madera destructible 32x32
    cj = Image.new("RGBA", (32, 32), (40, 26, 14, 255))
    d = ImageDraw.Draw(cj)
    d.rectangle((1, 1, 30, 30), fill=(150, 100, 52, 255))
    for y in (8, 16, 24):
        d.line((2, y, 29, y), fill=(110, 70, 34, 255))
    d.rectangle((1, 1, 30, 3), fill=(178, 124, 66, 255)); d.rectangle((1, 28, 30, 30), fill=(96, 60, 30, 255))
    d.line((3, 4, 28, 27), fill=(96, 60, 30, 255)); d.line((3, 5, 28, 28), fill=(178, 124, 66, 255))
    guardar(cj, "Objetos", "caja_madera.png")

    # bloque de concreto agrietado 32x32 (destructible fortaleza/base)
    bc = Image.new("RGBA", (32, 32), (30, 30, 34, 255))
    d = ImageDraw.Draw(bc)
    d.rectangle((1, 1, 30, 30), fill=(128, 126, 118, 255))
    d.rectangle((1, 1, 30, 3), fill=(160, 158, 148, 255))
    d.line((6, 4, 12, 14), fill=(70, 70, 66, 255)); d.line((12, 14, 9, 22), fill=(70, 70, 66, 255))
    d.line((12, 14, 22, 18), fill=(70, 70, 66, 255)); d.line((22, 18, 26, 27), fill=(70, 70, 66, 255))
    guardar(bc, "Objetos", "bloque_grieta.png")

    # sacos de arena 64x32
    s = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(s)
    for fila, (y, n, off) in enumerate([(18, 4, 0), (6, 3, 8)]):
        for i in range(n):
            x = off + i * 16
            d.rounded_rectangle((x, y, x + 16, y + 13), 5, fill=(60, 50, 30, 255))
            d.rounded_rectangle((x + 1, y + 1, x + 15, y + 12), 4, fill=(176, 152, 102, 255))
            d.line((x + 3, y + 3, x + 12, y + 3), fill=(206, 186, 136, 255))
    guardar(s, "Objetos", "sacos_arena.png")

    # barrera de la arena del jefe 32x96 (reja metálica con franjas)
    br = Image.new("RGBA", (32, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(br)
    d.rectangle((2, 0, 29, 95), fill=(24, 24, 28, 255))
    d.rectangle((4, 0, 27, 95), fill=(90, 94, 100, 255))
    for y in range(0, 96, 12):
        d.polygon([(4, y), (14, y), (4, y + 10)], fill=(230, 180, 30, 255))
        d.polygon([(27, y + 2), (27, y + 12), (17, y + 12)], fill=(30, 30, 30, 255))
    for x in (9, 16, 23):
        d.line((x, 0, x, 95), fill=(60, 62, 68, 255))
    guardar(br, "Objetos", "barrera_arena.png")

    # decoración de la selva (de OPP / Fierce Soldier)
    p = Image.open(ruta(JUNGLE, "environment", "tiles", "jungle", "tile_jungle_plants_objects.png")).convert("RGBA")
    guardar(recortar_opaco(p.crop((0, 336, 96, 416))), "Objetos", "carpa.png")
    guardar(recortar_opaco(p.crop((160, 320, 192, 384))), "Objetos", "antorcha.png")
    guardar(recortar_opaco(p.crop((64, 64, 128, 128))), "Objetos", "arbusto_1.png")
    guardar(recortar_opaco(p.crop((192, 64, 256, 128))), "Objetos", "arbusto_2.png")
    guardar(recortar_opaco(p.crop((256, 96, 320, 128))), "Objetos", "flor_roja.png")
    guardar(recortar_opaco(p.crop((448, 224, 512, 256))), "Objetos", "troncos.png")
    guardar(recortar_opaco(sg.crop((5, 5, 118, 165))), "Objetos", "palmera_grande.png")
    guardar(recortar_opaco(sg.crop((125, 5, 226, 113))), "Objetos", "palmera_chica.png")
    guardar(recortar_opaco(sg.crop((233, 5, 313, 65))), "Objetos", "arbusto_fierce.png")
    guardar(recortar_opaco(sg.crop((136, 126, 219, 165))), "Objetos", "helecho.png")
    guardar(recortar_opaco(sg.crop((254, 125, 294, 165))), "Objetos", "puas_metal.png")


# ---------------------------------------------------------------- tiles
def tile_relleno(base, roca, n=10, seed=1):
    rnd = random.Random(seed)
    t = Image.new("RGBA", (32, 32), base + (255,))
    d = ImageDraw.Draw(t)
    for _ in range(n):
        x, y = rnd.randrange(0, 28), rnd.randrange(0, 28)
        w, h = rnd.randrange(3, 7), rnd.randrange(2, 5)
        d.rectangle((x, y, x + w, y + h), fill=roca + (255,))
        d.line((x, y, x + w, y), fill=tuple(min(255, c + 22) for c in roca) + (255,))
    return t


def generar_tiles():
    print("Tiles...")
    J = ruta(JUNGLE, "environment", "tiles", "jungle")
    gb = Image.open(ruta(J, "tile_jungle_ground_brown.png")).convert("RGBA")
    gg = Image.open(ruta(J, "tile_jungle_ground_grey.png")).convert("RGBA")
    bb = Image.open(ruta(J, "tile_jungle_bottom_brown.png")).convert("RGBA")
    wt = Image.open(ruta(J, "tile_jungle_water.png")).convert("RGBA")
    br = Image.open(ruta(J, "tile_jungle_bridge.png")).convert("RGBA")

    def fila(tiles):
        hoja = Image.new("RGBA", (32 * len(tiles), 32), (0, 0, 0, 0))
        for i, t in enumerate(tiles):
            hoja.alpha_composite(t.convert("RGBA"), (i * 32, 0))
        return hoja

    def superficie_agua(color_sup, color_cuerpo):
        top = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        d = ImageDraw.Draw(top)
        d.rectangle((0, 8, 31, 31), fill=color_cuerpo + (210,))
        for x in range(0, 32, 8):
            d.line((x, 8, x + 4, 8), fill=color_sup + (255,))
            d.line((x + 4, 9, x + 7, 9), fill=color_sup + (255,))
        cuerpo = Image.new("RGBA", (32, 32), color_cuerpo + (210,))
        d = ImageDraw.Draw(cuerpo)
        for k in range(3):
            d.point((5 + k * 11, 6 + k * 9), fill=color_sup + (180,))
        return top, cuerpo

    # Orden de cada hoja: 0 suelo_sup, 1 suelo_relleno, 2 plataforma, 3 agua_sup, 4 agua
    relleno_selva = bb.crop((96, 0, 128, 32))
    sup_selva = gb.crop((32, 26, 64, 58))
    plataforma_selva = br.crop((64, 54, 96, 86))
    agua_sup, agua = superficie_agua((150, 220, 230), (46, 110, 120))
    guardar(fila([sup_selva, relleno_selva, plataforma_selva, agua_sup, agua]), "Tiles", "tiles_selva.png")

    # base militar: placas de acero con franja de peligro
    sup = Image.new("RGBA", (32, 32), (52, 56, 60, 255))
    d = ImageDraw.Draw(sup)
    d.rectangle((0, 0, 31, 5), fill=(30, 30, 30, 255))
    for x in range(-6, 32, 8):
        d.polygon([(x, 5), (x + 4, 5), (x + 8, 0), (x + 4, 0)], fill=(226, 176, 32, 255))
    d.rectangle((0, 6, 31, 7), fill=(132, 138, 142, 255))
    d.rectangle((0, 8, 31, 31), fill=(88, 94, 98, 255))
    d.line((0, 19, 31, 19), fill=(60, 64, 68, 255)); d.line((15, 8, 15, 31), fill=(60, 64, 68, 255))
    for (x, y) in ((3, 11), (12, 11), (19, 11), (28, 11), (3, 23), (12, 23), (19, 23), (28, 23)):
        d.point((x, y), fill=(170, 176, 180, 255))
    rel = Image.new("RGBA", (32, 32), (60, 64, 68, 255))
    d = ImageDraw.Draw(rel)
    d.rectangle((1, 1, 30, 30), fill=(72, 76, 80, 255))
    d.line((0, 15, 31, 15), fill=(44, 46, 50, 255)); d.line((15, 0, 15, 31), fill=(44, 46, 50, 255))
    for (x, y) in ((3, 3), (12, 3), (19, 3), (28, 3), (3, 27), (12, 27), (19, 27), (28, 27)):
        d.point((x, y), fill=(120, 124, 128, 255))
    pla = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(pla)
    d.rectangle((0, 0, 31, 9), fill=(24, 24, 26, 255))
    d.rectangle((0, 1, 31, 7), fill=(116, 122, 128, 255))
    for x in range(0, 32, 4):
        d.line((x, 2, x + 2, 6), fill=(70, 74, 78, 255))
    d.line((0, 1, 31, 1), fill=(170, 176, 180, 255))
    agua_sup_b, agua_b = superficie_agua((140, 150, 120), (52, 62, 40))
    guardar(fila([sup, rel, pla, agua_sup_b, agua_b]), "Tiles", "tiles_base.png")

    # fortaleza: piedra gris (OPP) + muro de ladrillos de concreto
    sup_f = gg.crop((32, 155, 64, 187))
    rel_f = Image.new("RGBA", (32, 32), (36, 36, 44, 255))
    d = ImageDraw.Draw(rel_f)
    for fila_i, y in enumerate(range(0, 32, 8)):
        off = 0 if fila_i % 2 == 0 else 8
        for x in range(-16 + off, 32, 16):
            d.rectangle((x + 1, y + 1, x + 15, y + 7), fill=(76, 74, 84, 255))
            d.line((x + 1, y + 1, x + 15, y + 1), fill=(98, 96, 106, 255))
    pla_f = pla.copy()
    pla_f = matiz_hacia(pla_f, (150, 110, 80), 0.3)
    agua_sup_f, agua_f = superficie_agua((240, 160, 60), (150, 50, 20))  # foso de lava/aceite
    guardar(fila([sup_f, rel_f, pla_f, agua_sup_f, agua_f]), "Tiles", "tiles_fortaleza.png")


# ---------------------------------------------------------------- fondos
def generar_fondos():
    print("Fondos parallax...")
    # Selva: cielo + montañas lejanas (ansimuz re-coloreadas) + palmeras + arbustos
    cielo = Image.new("RGBA", (320, 180), (0, 0, 0, 255))
    d = ImageDraw.Draw(cielo)
    for y in range(180):
        t = y / 179
        c = (int(96 + 110 * t), int(170 + 50 * t), int(200 + 20 * t))
        d.line((0, y, 319, y), fill=c + (255,))
    nubes = ruta(JUNGLE, "environment", "background", "bg objects")
    for nombre, pos in (("bg_cloud2.png", (10, 20)), ("bg_cloud5.png", (180, 40)), ("bg_cloud3.png", (120, 10))):
        n = Image.open(ruta(nubes, nombre)).convert("RGBA")
        cielo.alpha_composite(n.resize((n.width // 2 * 2 // 2, n.height // 2), Image.NEAREST) if n.width > 200 else n, pos)
    guardar(cielo, "Fondos", "selva_cielo.png")

    far = Image.open(ruta(MOUNTAIN, "parallax-mountain-montain-far.png")).convert("RGBA")
    guardar(matiz_hacia(far, (70, 130, 110), 0.8), "Fondos", "selva_lejos.png")
    mts = Image.open(ruta(MOUNTAIN, "parallax-mountain-mountains.png")).convert("RGBA")
    guardar(matiz_hacia(mts, (50, 100, 70), 0.8), "Fondos", "selva_montes.png")

    sg = Image.open(ruta(FIERCE, "scenery_and_gui.png")).convert("RGBA")
    palma_g = recortar_opaco(sg.crop((5, 5, 118, 165)))
    palma_c = recortar_opaco(sg.crop((125, 5, 226, 113)))
    medio = Image.new("RGBA", (480, 180), (0, 0, 0, 0))
    for x, p in ((0, palma_g), (110, palma_c), (190, palma_g), (300, palma_c), (370, palma_g)):
        sil = ImageEnhance.Brightness(p).enhance(0.45)
        sil = matiz_hacia(sil, (30, 80, 60), 0.6)
        medio.alpha_composite(sil, (x, 180 - sil.height))
    d = ImageDraw.Draw(medio)
    d.rectangle((0, 172, 479, 179), fill=(26, 52, 40, 255))
    guardar(medio, "Fondos", "selva_palmeras.png")


# ---------------------------------------------------------------- UI
def generar_ui():
    print("UI...")
    # panel 9-slice 48x48 (borde 12 px) verde oliva oscuro con remaches
    def panel(fondo, borde, luz, tam=48):
        im = Image.new("RGBA", (tam, tam), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle((0, 0, tam - 1, tam - 1), fill=(12, 14, 10, 255))
        d.rectangle((2, 2, tam - 3, tam - 3), fill=borde + (255,))
        d.rectangle((5, 5, tam - 6, tam - 6), fill=(12, 14, 10, 255))
        d.rectangle((6, 6, tam - 7, tam - 7), fill=fondo + (255,))
        d.line((3, 3, tam - 4, 3), fill=luz + (255,))
        for (x, y) in ((3, 3), (tam - 5, 3), (3, tam - 5), (tam - 5, tam - 5)):
            d.rectangle((x, y, x + 1, y + 1), fill=(200, 190, 140, 255))
        return im

    guardar(panel((38, 44, 30), (92, 104, 60), (140, 152, 96)), "UI", "panel.png")
    guardar(panel((70, 82, 46), (120, 134, 76), (176, 186, 120)), "UI", "boton.png")
    guardar(panel((30, 30, 30), (70, 70, 70), (120, 120, 120), 24), "UI", "barra_marco.png")
    blanco = Image.new("RGBA", (8, 8), (255, 255, 255, 255))
    guardar(blanco, "UI", "blanco.png")
    # perilla del slider
    k = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    d = ImageDraw.Draw(k)
    d.rectangle((4, 0, 19, 23), fill=(12, 14, 10, 255))
    d.rectangle((6, 2, 17, 21), fill=(220, 150, 40, 255))
    d.line((7, 3, 16, 3), fill=(255, 210, 120, 255))
    guardar(k, "UI", "perilla.png")


def main():
    copiar_packs()
    generar_personajes()
    generar_proyectiles()
    generar_efectos()
    generar_objetos()
    generar_tiles()
    generar_fondos()
    generar_ui()
    print("Listo.")


if __name__ == "__main__":
    main()
