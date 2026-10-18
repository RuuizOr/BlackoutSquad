# -*- coding: utf-8 -*-
"""
Blackout Squad - genera la primera versión de los mapas de texto de los 3 niveles.
Después de generarlos, los mapas se editan A MANO en Assets/Niveles/Mapas/NivelN.txt
(este script ya no es necesario; se deja como referencia).

Uso: python generar_mapas.py <carpeta_proyecto>
"""
import os, sys

H = 16          # filas (la fila 0 es la de arriba)
SUELO = 12      # fila donde empieza el suelo normal

LEYENDA = """# ================= BLACKOUT SQUAD - MAPA DE NIVEL =================
# Cada carácter es una celda de 1x1 unidad. La fila de arriba es la más alta.
# Las líneas que empiezan con '#' y un espacio son comentarios.
# Después de editar, ejecuta el menú  Blackout Squad > Construir juego.
#
#  #  suelo sólido              =  plataforma (se atraviesa desde abajo)
#  D  bloque destructible       ~  agua / foso líquido (mortal)
#  ^  alambre de púas           O  contenedor sólido (3x2, esquina inferior izquierda)
#  P  inicio del jugador        C  checkpoint (bandera)
#  s  soldado raso              e  soldado con escudo
#  l  soldado élite             t  torreta fija
#  a  ametralladora             h  escopeta          f  lanzallamas
#  +  botiquín                  1  vida extra        g  granadas extra
#  K  cofre con botiquín        M  cofre con ametralladora   G  cofre con granadas
#  [  inicio de la arena del jefe (aquí se cierra)   ]  pared derecha de la arena
#  J  jefe del nivel            Z  punto de control (objetivo nivel 2)
#  p  palmera grande  q  palmera chica  b  arbusto  T  carpa  L  antorcha
#  x  troncos  S  sacos de arena  Y  máquina de la fortaleza (decoración)
#  .  vacío
# ==================================================================
"""


class Mapa:
    def __init__(self, ancho):
        self.w = ancho
        self.g = [["." for _ in range(ancho)] for _ in range(H)]
        self.suelo(0, ancho, SUELO)

    def suelo(self, x0, x1, tope):
        for x in range(x0, x1):
            for r in range(H):
                if self.g[r][x] in "#~":
                    self.g[r][x] = "."
            for r in range(tope, H):
                self.g[r][x] = "#"

    def foso(self, x0, x1):
        for x in range(x0, x1):
            for r in range(H):
                if self.g[r][x] in "#~":
                    self.g[r][x] = "."

    def agua(self, x0, x1, tope=SUELO + 1):
        self.foso(x0, x1)
        for x in range(x0, x1):
            for r in range(tope, H):
                self.g[r][x] = "~"

    def plat(self, x0, x1, fila, c="="):
        for x in range(x0, x1):
            self.g[fila][x] = c

    def tope(self, x):
        for r in range(H):
            if self.g[r][x] in "#=D":
                return r
        return H

    def sobre(self, x, c):
        """Coloca c justo encima del suelo/plataforma en la columna x."""
        r = self.tope(x)
        if r == H:
            raise ValueError("No hay suelo en x=%d para '%s'" % (x, c))
        self.g[r - 1][x] = c

    def en(self, x, fila, c):
        self.g[fila][x] = c

    def texto(self, titulo):
        lineas = [LEYENDA + "# " + titulo]
        lineas += ["".join(f) for f in self.g]
        return "\n".join(lineas) + "\n"


def nivel1():
    m = Mapa(262)
    # inicio
    m.sobre(3, "P"); m.sobre(1, "p"); m.sobre(9, "q"); m.sobre(6, "b"); m.sobre(13, "x")
    for x in (20, 27): m.sobre(x, "s")
    m.sobre(24, "K"); m.sobre(17, "b")
    # foso con plataforma
    m.foso(31, 35); m.plat(32, 34, 10)
    # escalón y torreta
    m.suelo(40, 48, 10)
    m.sobre(38, "s"); m.sobre(45, "s"); m.sobre(49, "t"); m.sobre(42, "p")
    # río con puente roto
    m.agua(52, 59); m.plat(52, 55, SUELO); m.plat(56, 59, SUELO)
    # alambre y plataformas con ametralladora
    m.sobre(63, "^"); m.sobre(64, "^")
    m.plat(66, 71, 9); m.en(68, 8, "a")
    m.sobre(70, "s"); m.sobre(75, "s"); m.sobre(72, "q")
    # muro destructible y torreta en alto
    m.en(80, 11, "D"); m.en(80, 10, "D"); m.en(80, 9, "D")
    m.suelo(84, 91, 11); m.sobre(86, "t"); m.sobre(89, "s")
    # foso con plataforma pequeña
    m.foso(92, 96); m.plat(93, 95, 11)
    # trinchera
    m.sobre(97, "S")
    m.suelo(99, 106, 13); m.sobre(101, "s"); m.sobre(104, "s"); m.sobre(98, "^")
    # checkpoint
    m.sobre(113, "C"); m.sobre(110, "p"); m.sobre(116, "K"); m.sobre(118, "b")
    # plataformas en capas con vida extra escondida
    m.plat(123, 129, 9); m.plat(131, 135, 6); m.en(133, 5, "1")
    m.sobre(126, "s"); m.sobre(137, "s"); m.sobre(140, "s")
    # río con piedras para saltar
    m.agua(143, 149); m.plat(144, 146, SUELO - 1); m.plat(147, 148, SUELO)
    # campamento enemigo
    m.sobre(151, "T"); m.sobre(161, "T"); m.sobre(155, "L")
    for x in (153, 157, 164, 169): m.sobre(x, "s")
    m.suelo(158, 161, 11); m.sobre(159, "t")
    m.sobre(166, "M"); m.sobre(172, "^"); m.sobre(173, "^")
    # foso sin ayuda
    m.foso(176, 179)
    # bloques destructibles y botiquín
    m.en(183, 11, "D"); m.en(183, 10, "D")
    m.sobre(187, "K"); m.sobre(185, "s"); m.sobre(191, "s"); m.sobre(196, "s"); m.sobre(199, "t")
    m.plat(192, 195, 9); m.en(193, 8, "+")
    # última subida antes del jefe
    m.suelo(206, 216, 11)
    for x in (208, 213, 219, 225): m.sobre(x, "s")
    m.sobre(221, "^"); m.sobre(228, "+"); m.sobre(203, "p")
    # arena del jefe
    m.sobre(233, "["); m.sobre(237, "L"); m.sobre(251, "L"); m.sobre(244, "T")
    m.sobre(256, "J")
    for r in range(H): m.en(260, r, "]")
    m.foso(261, 262); m.suelo(261, 262, SUELO)
    return m.texto("NIVEL 1 - SELVA: llega al campamento enemigo y vence a su jefe")


def nivel2():
    m = Mapa(286)
    m.sobre(3, "P"); m.sobre(8, "S")
    for x in (16, 22): m.sobre(x, "s")
    m.sobre(28, "e"); m.sobre(19, "K")
    m.sobre(12, "O")
    # alambre y contenedores
    m.sobre(33, "^"); m.sobre(34, "^"); m.sobre(35, "^")
    m.sobre(39, "O"); m.sobre(44, "e"); m.sobre(48, "O"); m.en(49, 9, "s")
    m.sobre(53, "t")
    # foso con pasarela
    m.foso(56, 61); m.plat(57, 60, 10)
    m.sobre(64, "e"); m.sobre(69, "s"); m.sobre(72, "S")
    # estructura metálica de dos pisos
    m.plat(75, 88, 8); m.sobre(78, "s"); m.en(83, 7, "t"); m.en(86, 7, "h")
    m.sobre(80, "e"); m.sobre(86, "e")
    m.en(91, 11, "D"); m.en(91, 10, "D"); m.en(91, 9, "D")
    m.sobre(95, "s"); m.sobre(99, "O"); m.sobre(104, "e")
    # foso
    m.foso(107, 111)
    m.sobre(114, "^"); m.sobre(115, "^")
    m.sobre(120, "s"); m.sobre(124, "e")
    # checkpoint
    m.sobre(130, "C"); m.sobre(133, "K"); m.sobre(127, "S")
    # zona del lanzallamas (detrás de un muro destructible)
    m.suelo(138, 152, 10)
    m.en(140, 9, "D"); m.en(140, 8, "D"); m.sobre(142, "f"); m.sobre(146, "e"); m.sobre(150, "t")
    m.sobre(156, "s"); m.sobre(160, "e"); m.sobre(163, "O"); m.sobre(168, "s")
    # agua aceitosa (foso líquido)
    m.agua(171, 177); m.plat(172, 174, SUELO - 1); m.plat(175, 177, SUELO - 2)
    m.sobre(181, "e"); m.sobre(185, "s"); m.sobre(188, "t")
    # plataformas altas con vida extra
    m.plat(192, 197, 9); m.plat(199, 203, 6); m.en(201, 5, "1")
    m.sobre(195, "s"); m.sobre(205, "e"); m.sobre(210, "O"); m.sobre(216, "s")
    m.sobre(219, "^"); m.sobre(220, "^"); m.sobre(224, "e"); m.sobre(228, "t")
    m.sobre(232, "G"); m.sobre(236, "e"); m.sobre(240, "s"); m.sobre(244, "+")
    # arena: tanque y punto de control
    m.sobre(250, "["); m.sobre(254, "S"); m.sobre(278, "Z")
    m.sobre(266, "J")
    for r in range(H): m.en(284, r, "]")
    m.foso(285, 286); m.suelo(285, 286, SUELO)
    return m.texto("NIVEL 2 - BASE MILITAR: destruye el punto de control")


def nivel3():
    m = Mapa(302)
    m.sobre(3, "P")
    for x in (15, 21): m.sobre(x, "l")
    m.sobre(26, "e"); m.sobre(18, "K")
    # lava
    m.agua(30, 36); m.plat(31, 33, SUELO - 1); m.plat(34, 36, SUELO - 2)
    m.sobre(39, "t"); m.sobre(43, "l"); m.sobre(47, "S")
    # muro destructible y pasillo industrial
    m.en(51, 11, "D"); m.en(51, 10, "D"); m.en(51, 9, "D")
    m.plat(55, 70, 8); m.en(58, 7, "l"); m.en(66, 7, "t")
    m.sobre(57, "e"); m.sobre(63, "l"); m.sobre(69, "l")
    m.sobre(73, "^"); m.sobre(74, "^")
    m.foso(78, 82); m.plat(79, 81, 10)
    m.suelo(85, 96, 10); m.sobre(87, "l"); m.sobre(92, "t"); m.sobre(95, "e")
    m.sobre(100, "l"); m.sobre(104, "+")
    # zona del lanzallamas
    m.plat(108, 114, 9); m.en(111, 8, "f")
    m.sobre(110, "e"); m.sobre(116, "l")
    m.agua(119, 125); m.plat(120, 122, SUELO - 1); m.plat(123, 125, SUELO)
    m.sobre(128, "l"); m.sobre(133, "t")
    # checkpoint
    m.sobre(140, "C"); m.sobre(143, "K")
    m.sobre(148, "l"); m.sobre(152, "e"); m.sobre(156, "l"); m.sobre(159, "^"); m.sobre(160, "^")
    m.suelo(163, 175, 11); m.sobre(165, "t"); m.sobre(170, "l"); m.sobre(174, "e")
    m.foso(178, 183); m.plat(179, 182, 10)
    m.sobre(186, "l"); m.sobre(190, "l"); m.en(194, 11, "D"); m.en(194, 10, "D")
    # torre con vida extra
    m.plat(198, 203, 9); m.plat(205, 209, 6); m.en(207, 5, "1")
    m.sobre(200, "l"); m.sobre(211, "t"); m.sobre(215, "e")
    m.agua(219, 224); m.plat(220, 223, SUELO - 1)
    m.sobre(227, "l"); m.sobre(231, "l"); m.sobre(235, "t"); m.sobre(238, "G")
    m.sobre(242, "e"); m.sobre(246, "l"); m.sobre(250, "^"); m.sobre(255, "+")
    m.sobre(258, "l"); m.sobre(262, "t")
    # arena de Kruger
    m.sobre(268, "["); m.sobre(272, "L"); m.sobre(296, "L")
    m.sobre(283, "Y")
    m.sobre(290, "J")
    for r in range(H): m.en(300, r, "]")
    m.foso(301, 302); m.suelo(301, 302, SUELO)
    return m.texto("NIVEL 3 - FORTALEZA: derrota al Comandante Kruger")


def main():
    d = os.path.join(sys.argv[1], "Assets", "Niveles", "Mapas")
    os.makedirs(d, exist_ok=True)
    for i, f in enumerate((nivel1, nivel2, nivel3), 1):
        with open(os.path.join(d, "Nivel%d.txt" % i), "w", encoding="utf-8") as fh:
            fh.write(f())
        print("Mapa generado: Nivel%d.txt" % i)


if __name__ == "__main__":
    main()
