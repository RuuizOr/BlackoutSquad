# Genera las viñetas de la intro tipo cómic (historia de la Operación Apagón) con el arte del juego.
# Uso: python Herramientas/generar_intro.py <carpeta del proyecto>
# Crea Assets/Resources/Intro/vineta1.png ... vineta3.png, vineta4_alfa.png y vineta4_bravo.png.
# Cada escena se arma en baja resolución (320x200) y se escala x3 sin suavizado para conservar el pixel art.
import os, sys, math, random
from PIL import Image, ImageDraw, ImageEnhance

PROY = sys.argv[1]
ART = os.path.join(PROY, 'Assets', 'Art')
SALIDA = os.path.join(PROY, 'Assets', 'Resources', 'Intro')
W, H, ESC = 320, 200, 3
random.seed(7)


def img(ruta):
    return Image.open(os.path.join(ART, ruta)).convert('RGBA')


def cuadro(hoja, ancho, alto, col, fila=0):
    c = hoja.crop((col * ancho, fila * alto, (col + 1) * ancho, (fila + 1) * alto))
    caja = c.getbbox()
    return c.crop(caja) if caja else c


def escalar(im, factor):
    return im.resize((max(1, round(im.width * factor)), max(1, round(im.height * factor))), Image.NEAREST)


def espejo(im):
    return im.transpose(Image.FLIP_LEFT_RIGHT)


def tintar(im, color, fuerza):
    capa = Image.new('RGBA', im.size, color + (255,))
    mezcla = Image.blend(im.convert('RGBA'), capa, fuerza)
    mezcla.putalpha(im.getchannel('A'))
    return mezcla


def silueta(im, color):
    s = Image.new('RGBA', im.size, color + (255,))
    s.putalpha(im.getchannel('A'))
    return s


def contorno(im, color=(16, 8, 12)):
    # Contorno de 1 px alrededor del sprite (estilo cómic)
    a = im.getchannel('A').point(lambda v: 255 if v > 40 else 0)
    base = Image.new('RGBA', (im.width + 2, im.height + 2), (0, 0, 0, 0))
    for dx, dy in ((0, 1), (2, 1), (1, 0), (1, 2)):
        s = Image.new('RGBA', im.size, color + (255,)); s.putalpha(a)
        base.alpha_composite(s, (dx, dy))
    base.alpha_composite(im, (1, 1))
    return base


def pegar_pie(lienzo, sprite, x_centro, y_pie):
    lienzo.alpha_composite(sprite, (int(x_centro - sprite.width / 2), int(y_pie - sprite.height)))


def fondo_repetido(lienzo, capa, y, factor=1.0, desplazar=0):
    capa = escalar(capa, factor)
    x = -desplazar
    while x < W:
        lienzo.alpha_composite(capa, (int(x), int(y)))
        x += capa.width


def degradado(c1, c2):
    g = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(g)
    for y in range(H):
        t = y / (H - 1)
        d.line([(0, y), (W, y)], fill=tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(3)) + (255,))
    return g


def lineas_velocidad(lienzo, centro, color, n=40, alpha=70):
    capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    for k in range(n):
        a = random.uniform(0, math.tau)
        r0 = random.uniform(70, 110)
        p0 = (centro[0] + r0 * math.cos(a), centro[1] + r0 * math.sin(a))
        p1 = (centro[0] + 400 * math.cos(a), centro[1] + 400 * math.sin(a))
        d.line([p0, p1], fill=color + (alpha,), width=random.choice((1, 1, 2)))
    lienzo.alpha_composite(capa)


def terminar(lienzo, nombre):
    # Trama de puntos sutil (semitono) y marco grueso de viñeta
    trama = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(trama)
    for y in range(0, H, 4):
        for x in range((y // 4) % 2 * 2, W, 4):
            d.point((x, y), fill=(0, 0, 0, 28))
    lienzo.alpha_composite(trama)
    final = escalar(lienzo, ESC)
    d = ImageDraw.Draw(final)
    d.rectangle([0, 0, final.width - 1, final.height - 1], outline=(16, 8, 12, 255), width=12)
    d.rectangle([12, 12, final.width - 13, final.height - 13], outline=(240, 232, 205, 255), width=4)
    final.convert('RGB').save(os.path.join(SALIDA, nombre + '.png'))
    print('ok', nombre)


# ---------- Sprites ----------
kruger = cuadro(img('Generado/Personajes/kruger.png'), 80, 100, 1)
raso = cuadro(img('Generado/Personajes/soldado_raso.png'), 72, 96, 0)
escudo = cuadro(img('Generado/Personajes/soldado_escudo.png'), 72, 96, 0)
tanque = cuadro(img('Generado/Personajes/tanque.png'), 144, 64, 0)
alfa_hoja = img('Generado/Personajes/comando_alfa.png')
alfa_quieto = cuadro(alfa_hoja, 96, 120, 1)
alfa_corre = cuadro(alfa_hoja, 96, 120, 6)
bravo_hoja = img('Generado/Personajes/comando_bravo.png')
bravo_quieto = cuadro(bravo_hoja, 140, 150, 0)
bravo_corre = cuadro(bravo_hoja, 140, 150, 3)


def vineta1():
    # La fortaleza de Kruger al anochecer, con su ejército
    l = Image.new('RGBA', (W, H))
    l.alpha_composite(tintar(escalar(img('ParallaxIndustrial_ansimuz/industrial_0003_bg.png'), 1.25), (200, 40, 30), 0.45), (0, 0))
    fondo_repetido(l, tintar(img('ParallaxIndustrial_ansimuz/industrial_0002_far-buildings.png'), (120, 20, 30), 0.5), 40, 1.0)
    fondo_repetido(l, tintar(img('ParallaxIndustrial_ansimuz/industrial_0001_buildings.png'), (90, 10, 20), 0.45), 50, 1.0, 40)
    ImageDraw.Draw(l).rectangle([0, 172, W, H], fill=(30, 12, 16, 255))
    for i, x in enumerate((205, 240, 275, 305)):
        s = silueta(escalar(espejo(raso), 0.62), (40, 14, 20))
        pegar_pie(l, s, x, 176 - (i % 2) * 3)
    k = contorno(escalar(kruger, 1.55))
    pegar_pie(l, k, 95, 198)
    terminar(l, 'vineta1')


def vineta2():
    # Operación Apagón: la región se queda a oscuras y avanzan los tanques
    l = degradado((10, 14, 40), (40, 30, 70))
    d = ImageDraw.Draw(l)
    for _ in range(40):
        d.point((random.randint(0, W), random.randint(0, 90)), fill=(220, 220, 255, random.randint(80, 200)))
    fondo_repetido(l, silueta(img('ParallaxMountain_ansimuz/parallax-mountain-montain-far.png'), (26, 24, 60)), 30, 1.0)
    edificios = silueta(img('ParallaxIndustrial_ansimuz/industrial_0001_buildings.png'), (14, 14, 30))
    fondo_repetido(l, edificios, 62, 0.85)
    # Ventanas: unas cuantas encendidas y el resto apagándose
    for _ in range(26):
        x, y = random.randint(5, W - 5), random.randint(95, 150)
        if edificios.getpixel((min(int(x / 0.85) % edificios.width, edificios.width - 1), min(int((y - 62) / 0.85), edificios.height - 1)))[3] > 0:
            d.rectangle([x, y, x + 2, y + 3], fill=(255, 210, 90, 255) if random.random() < 0.3 else (40, 40, 70, 255))
    d.rectangle([0, 168, W, H], fill=(12, 10, 22, 255))
    t = contorno(escalar(tanque, 1.2))
    pegar_pie(l, t, 215, 186)
    for x in (70, 105, 140):
        pegar_pie(l, contorno(tintar(escalar(raso, 0.75), (20, 30, 80), 0.35)), x, 188)
    terminar(l, 'vineta2')


def vineta3():
    # El Blackout Squad: Alfa y Bravo listos para la misión
    l = Image.new('RGBA', (W, H))
    cx, cy = W / 2, H * 0.45
    px = l.load()
    for y in range(H):
        for x in range(W):
            dd = min(1.0, math.hypot(x - cx, y - cy) / (W * 0.62))
            a, b = ((255, 200, 90), (232, 104, 36)) if dd < 0.45 else ((232, 104, 36), (50, 16, 22))
            t = dd / 0.45 if dd < 0.45 else (dd - 0.45) / 0.55
            px[x, y] = tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)
    lineas_velocidad(l, (cx, cy), (255, 240, 200), 46, 60)
    pal = escalar(img('Generado/Objetos/palmera_grande.png'), 0.75)
    pegar_pie(l, silueta(pal, (30, 10, 14)), 20, 190)
    pegar_pie(l, silueta(espejo(pal), (30, 10, 14)), 300, 190)
    ImageDraw.Draw(l).rectangle([0, 182, W, H], fill=(30, 10, 14, 255))
    pegar_pie(l, contorno(escalar(alfa_quieto, 1.35)), 205, 196)
    pegar_pie(l, contorno(escalar(bravo_quieto, 0.95)), 110, 196)
    terminar(l, 'vineta3')


def vineta4(nombre, heroe):
    # Primera misión: cruzar la selva
    l = Image.new('RGBA', (W, H))
    l.alpha_composite(escalar(img('Generado/Fondos/selva_cielo.png'), 1.12), (0, 0))
    fondo_repetido(l, img('Generado/Fondos/selva_lejos.png'), 30, 1.0)
    fondo_repetido(l, img('Generado/Fondos/selva_montes.png'), 40, 1.0, 60)
    fondo_repetido(l, img('Generado/Fondos/selva_palmeras.png'), 20, 1.0, 30)
    d = ImageDraw.Draw(l)
    d.rectangle([0, 176, W, H], fill=(40, 26, 30, 255))
    d.rectangle([0, 172, W, 177], fill=(110, 200, 60, 255))
    lineas_velocidad(l, (90, 130), (255, 255, 255), 30, 45)
    pegar_pie(l, contorno(heroe), 95, 196)
    for x, s in ((235, raso), (285, escudo)):
        pegar_pie(l, contorno(escalar(espejo(s), 0.8)), x, 178)
    terminar(l, nombre)


os.makedirs(SALIDA, exist_ok=True)
vineta1()
vineta2()
vineta3()
vineta4('vineta4_alfa', escalar(alfa_corre, 1.2))
vineta4('vineta4_bravo', escalar(bravo_corre, 0.85))
