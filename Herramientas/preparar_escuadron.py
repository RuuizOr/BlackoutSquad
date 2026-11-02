# -*- coding: utf-8 -*-
"""
Blackout Squad - Hoja y cara de Comando Bravo.

Toma el pack original (Assets/Art/OpenGunner_Master484) y genera en Assets/Art/Generado/Personajes
una hoja de 16 cuadros en el mismo orden que comando_alfa.png:
  0 idle, 1 idle_disparo, 2-5 correr, 6-9 correr_disparo, 10 salto, 11 salto_disparo,
  12 agachado, 13 agachado_disparo, 14 apuntar_arriba, 15 muerte
Dos filas de 8 celdas de 140x150, personaje mirando a la DERECHA y pies en el borde inferior.
El pack no trae cuadros con fogonazo: el juego ya lo dibuja con su propio efecto.

También dibuja su cara para el HUD en Assets/Art/Generado/UI (24x24, como icono_vida.png).

Uso:  python preparar_escuadron.py <carpeta_proyecto>
Requiere Pillow.
"""
import os, sys
from PIL import Image, ImageDraw

PROY = sys.argv[1]
ART = os.path.join(PROY, "Assets", "Art")
SALIDA = os.path.join(ART, "Generado", "Personajes")
CELDA_X, CELDA_Y = 140, 150
COLUMNAS = 8
FILAS_CABEZA_PX = 24  # alto de la cabeza ya escalada, para alinear los cuadros
PPU = 48.0


def sin_fondo(img, color):
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            if px[x, y][:3] == color:
                px[x, y] = (0, 0, 0, 0)
    return img


def recortar(img):
    caja = img.getbbox()
    return img.crop(caja) if caja else img


def centro_pies(img, filas=5):
    px = img.load()
    xs = [x for y in range(max(0, img.height - filas), img.height) for x in range(img.width) if px[x, y][3] > 0]
    return (min(xs) + max(xs)) / 2 if xs else img.width / 2


def centro_cabeza(img, filas):
    px = img.load()
    xs = [x for y in range(min(filas, img.height)) for x in range(img.width) if px[x, y][3] > 0]
    return (min(xs) + max(xs)) / 2 if xs else img.width / 2


def hoja(cuadros, escala, nombre):
    # Dos filas de 8 celdas: la hoja no pasa del máximo de textura de Unity (2048) y no se deforma
    resultado = Image.new("RGBA", (CELDA_X * COLUMNAS, CELDA_Y * (len(cuadros) // COLUMNAS)), (0, 0, 0, 0))
    for i, c in enumerate(cuadros):
        c = recortar(c)
        c = c.resize((int(round(c.width * escala)), int(round(c.height * escala))), Image.NEAREST)
        if c.width > CELDA_X or c.height > CELDA_Y:
            print("  AVISO: cuadro", i, "de", nombre, "no cabe:", c.size)
        # Se alinea por la cabeza para que el cuerpo no salte de lado entre cuadros. Apuntar arriba
        # (el arma sobresale de la cabeza) y morir (acostado) se alinean por los pies.
        cx = centro_pies(c) if i in (14, 15) else centro_cabeza(c, FILAS_CABEZA_PX)
        x = int(round(CELDA_X / 2 - cx))
        x = max(0, min(x, CELDA_X - c.width))
        resultado.paste(c, ((i % COLUMNAS) * CELDA_X + x, (i // COLUMNAS + 1) * CELDA_Y - c.height), c)
    destino = os.path.join(SALIDA, nombre + ".png")
    resultado.save(destino)
    print("  generado:", os.path.relpath(destino, PROY))
    imprimir_bocas(resultado, nombre)


def imprimir_bocas(sheet, nombre):
    """Punta del arma de cada pose, en unidades desde los pies (van en Escuadron, ConstructorPrefabs.cs)."""
    def celda(i):
        x, y = (i % COLUMNAS) * CELDA_X, (i // COLUMNAS) * CELDA_Y
        return sheet.crop((x, y, x + CELDA_X, y + CELDA_Y))

    def punta_frente(i):
        f = celda(i)
        px = f.load()
        caja = f.getbbox()
        ys = [y for y in range(CELDA_Y) if any(px[x, y][3] > 0 for x in range(caja[2] - 3, caja[2]))]
        return (caja[2] - CELDA_X / 2) / PPU - 0.09, (CELDA_Y - sum(ys) / len(ys)) / PPU

    def punta_arriba(i):
        f = celda(i)
        px = f.load()
        caja = f.getbbox()
        xs = [x for x in range(CELDA_X) if any(px[x, y][3] > 0 for y in range(caja[1], caja[1] + 3))]
        return (sum(xs) / len(xs) - CELDA_X / 2) / PPU, (CELDA_Y - caja[1]) / PPU

    print("    bocas %s: frente=%.2f,%.2f agachado=%.2f,%.2f aire=%.2f,%.2f arriba=%.2f,%.2f" % (
        (nombre,) + punta_frente(0) + punta_frente(12) + punta_frente(10) + punta_arriba(14)))


# ------------------------------------------------- Open Gunner (Master484): armadura
def comando_bravo():
    h = sin_fondo(Image.open(os.path.join(ART, "OpenGunner_Master484", "OpenGunnerHeroVer2.png")), (77, 75, 118))
    celda = lambda x, y, w=50, alto=50: h.crop((x, y, x + w, y + alto))
    parado = celda(24, 143)
    agachado = celda(75, 143)
    salto = celda(126, 143)
    correr = [celda(24 + 51 * i, 315) for i in (0, 2, 4, 6)]
    arriba = celda(137, 676, 50, 57)
    muerte = celda(25, 499, 70, 50)
    hoja([parado, parado] + correr + correr + [salto, salto, agachado, agachado, arriba, muerte], 1.75, "comando_bravo")


# ------------------------------------------------- cara para el HUD (24x24, como icono_vida.png)
NEGRO = (0, 0, 0, 255)
FONDO_RETRATO = (64, 88, 128, 128)


def retrato_bravo():
    azul, sombra, claro, rojo, gris = (105, 145, 225, 255), (62, 88, 160, 255), (150, 185, 245, 255), (205, 40, 40, 255), (70, 74, 88, 255)
    img = Image.new("RGBA", (24, 24), FONDO_RETRATO)
    d = ImageDraw.Draw(img)
    d.rectangle((4, 1, 19, 22), fill=NEGRO)
    d.rectangle((2, 3, 21, 20), fill=NEGRO)
    d.rectangle((3, 2, 20, 21), fill=NEGRO)
    d.rectangle((5, 2, 18, 20), fill=azul)
    d.rectangle((3, 4, 20, 19), fill=azul)
    d.rectangle((17, 4, 20, 19), fill=sombra)
    d.rectangle((6, 3, 12, 5), fill=claro)
    d.rectangle((4, 8, 19, 14), fill=NEGRO)
    d.rectangle((6, 10, 9, 11), fill=(120, 225, 245, 255))
    d.rectangle((14, 10, 17, 11), fill=(120, 225, 245, 255))
    d.rectangle((2, 9, 3, 14), fill=rojo)
    d.rectangle((20, 9, 21, 14), fill=rojo)
    d.rectangle((8, 16, 15, 19), fill=gris)
    d.line((9, 17, 14, 17), fill=NEGRO)
    destino = os.path.join(ART, "Generado", "UI", "retrato_bravo.png")
    img.save(destino)
    print("  generado:", os.path.relpath(destino, PROY))


comando_bravo()
retrato_bravo()
