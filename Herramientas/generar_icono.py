# Genera el ícono de la app (Android, PC y web) con el arte del juego.
# Uso: python Herramientas/generar_icono.py <carpeta del proyecto> Assets/Art/Icono
# Crea icono_app.png (completo), icono_fondo.png y icono_frente.png (ícono adaptable de Android).
import sys, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter
proyecto, salida = sys.argv[1], sys.argv[2]
N = 1024

def fondo():
    # Atardecer de la selva: brillo naranja al centro, rojo oscuro y casi negro en las orillas
    im = Image.new('RGBA', (N, N))
    px = im.load()
    c = (N/2, N*0.46)
    for y in range(N):
        for x in range(N):
            d = min(1.0, math.hypot(x-c[0], y-c[1]) / (N*0.72))
            if d < 0.45:
                t = d/0.45; a, b = (255, 196, 80), (232, 104, 36)
            else:
                t = (d-0.45)/0.55; a, b = (232, 104, 36), (38, 14, 20)
            px[x, y] = tuple(int(a[i] + (b[i]-a[i])*t) for i in range(3)) + (255,)
    # Rayos de luz tenues
    rayos = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    dr = ImageDraw.Draw(rayos)
    for k in range(16):
        a0 = k * (2*math.pi/16)
        pts = [c, (c[0]+1500*math.cos(a0), c[1]+1500*math.sin(a0)), (c[0]+1500*math.cos(a0+0.11), c[1]+1500*math.sin(a0+0.11))]
        dr.polygon(pts, fill=(255, 230, 160, 28))
    im.alpha_composite(rayos)
    # Silueta de palmeras y suelo abajo
    pal = Image.open(proyecto + '/Assets/Art/Generado/Objetos/palmera_grande.png').convert('RGBA')
    esc = 560 / pal.height
    pal = pal.resize((int(pal.width*esc), 560), Image.NEAREST)
    sil = Image.new('RGBA', pal.size, (20, 8, 12, 255)); sil.putalpha(pal.getchannel('A'))
    im.alpha_composite(sil, (-int(pal.width*0.35), N-560-60))
    sil2 = sil.transpose(Image.FLIP_LEFT_RIGHT)
    im.alpha_composite(sil2, (N-int(pal.width*0.65), N-560-40))
    ImageDraw.Draw(im).rectangle([0, N-110, N, N], fill=(20, 8, 12, 255))
    return im

def soldado(alto):
    hoja = Image.open(proyecto + '/Assets/Art/Generado/Personajes/comando_alfa.png').convert('RGBA')
    cuadro = hoja.crop((96, 0, 192, 120))           # pose disparando con fogonazo
    cuadro = cuadro.crop(cuadro.getbbox())
    esc = alto / cuadro.height
    cuadro = cuadro.resize((round(cuadro.width*esc), alto), Image.NEAREST)
    # Contorno oscuro grueso para que se lea en pantallas chicas
    a = cuadro.getchannel('A').point(lambda v: 255 if v > 0 else 0)
    borde = a.filter(ImageFilter.MaxFilter(max(3, (alto//40)|1)))
    capa = Image.new('RGBA', cuadro.size, (20, 8, 12, 255)); capa.putalpha(borde)
    capa.alpha_composite(cuadro)
    return capa

def texto(im, centro_y, tam):
    f = ImageFont.truetype(proyecto + '/Assets/Fuentes/BlackOpsOne-Regular.ttf', tam)
    d = ImageDraw.Draw(im)
    t = 'BLACKOUT SQUAD'
    w = d.textlength(t, font=f)
    d.text(((N-w)/2, centro_y - tam*0.55), t, font=f, fill=(255, 174, 46, 255), stroke_width=max(4, tam//9), stroke_fill=(20, 8, 12, 255))

def pegar_centrado(base, img, cy):
    base.alpha_composite(img, ((N-img.width)//2 + int(img.width*0.04), int(cy - img.height/2)))

# Ícono completo (Android "legacy"/redondo, PC y web)
completo = fondo()
pegar_centrado(completo, soldado(720), N*0.5)
completo.save(salida + '/icono_app.png')

# Ícono adaptable de Android: fondo y frente por separado (el sistema recorta en círculo o en "squircle",
# así que el soldado va dentro de la zona segura central del 66%)
fondo().save(salida + '/icono_fondo.png')
frente = Image.new('RGBA', (N, N), (0, 0, 0, 0))
pegar_centrado(frente, soldado(560), N*0.5)
frente.save(salida + '/icono_frente.png')

# Vista previa opcional (tercer argumento = carpeta donde guardarla): completo, adaptable en círculo y tamaño real
if len(sys.argv) > 3:
    salida = sys.argv[3]
    prev = Image.new('RGBA', (N*2 + 340, N), (245, 245, 245, 255))
    prev.alpha_composite(completo, (0, 0))
    ad = fondo(); ad.alpha_composite(frente)
    mask = Image.new('L', (N, N), 0); ImageDraw.Draw(mask).ellipse([0, 0, N, N], fill=255)
    circ = Image.new('RGBA', (N, N), (0, 0, 0, 0)); circ.paste(ad, (0, 0), mask)
    prev.alpha_composite(circ, (N + 20, 0))
    chico = completo.resize((144, 144), Image.LANCZOS); prev.alpha_composite(chico, (N*2 + 80, 200))
    chico2 = circ.resize((144, 144), Image.LANCZOS); prev.alpha_composite(chico2, (N*2 + 80, 420))
    prev.convert('RGB').save(salida + '/vista_previa.png')
print('ok')
