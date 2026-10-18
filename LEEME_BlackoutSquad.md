# Blackout Squad — Guía del proyecto

Run & Gun 2D de desplazamiento lateral (Unity 6, URP 2D, nuevo Input System, PC Windows).

## 1. Cómo generar y probar el juego

1. Abre el proyecto en Unity 6 y espera a que compile.
2. Menú **Blackout Squad > Construir juego**. Esto crea capas, matriz de colisiones, importación de arte y audio,
   fuentes TMP, prefabs, tiles, las 5 escenas, toda la interfaz y Build Profiles. Puedes ejecutarlo las veces que quieras.
3. Abre `Assets/Scenes/Splash.unity` y presiona **Play**.
   También puedes abrir directamente `Nivel1`, `Nivel2` o `Nivel3`: los sistemas globales se crean solos.
4. Para generar el ejecutable: **Blackout Squad > Compilar build Windows** (queda en `Build/Windows/BlackoutSquad.exe`).

## 2. Controles

| Acción | Teclado y mouse | Gamepad |
|---|---|---|
| Moverse | A / D (o flechas ← →) | Stick izquierdo / D-pad |
| Saltar (mantener = más alto) | W | A |
| Agacharse | S (o flecha ↓) | Stick abajo |
| Disparar | Espacio | Gatillo derecho |
| Apuntar arriba / diagonal | Flecha ↑ (+ A/D) | Stick arriba (+ lado) |
| Apuntar abajo (en el aire) | S (+ A/D) | Stick abajo |
| Granada | E | B |
| Rodar (esquivar) | Shift | X |
| Pausa | ESC | Start |

Consejos: agáchate para esquivar las balas rectas de los soldados; rueda para atravesar disparos (0.2 s invulnerable);
al soldado con escudo dale por detrás o con granada; al tanque, por detrás o desde arriba.

## 3. Estructura de carpetas

```
Assets/
  Art/                 Packs descargados (cada uno con su LICENCIA.txt)
    Generado/          Sprites ya cortados/recoloreados que usa el juego (Personajes, Proyectiles, Efectos, Objetos, Tiles, Fondos, UI)
  Audio/Musica, Audio/SFX, Audio/Licencias
  Fuentes/             Black Ops One y Press Start 2P (+ sus TMP Font Assets)
  Datos/Armas/         Pistola, Ametralladora, Escopeta, Lanzallamas (ScriptableObjects editables)
  Niveles/Mapas/       Nivel1.txt, Nivel2.txt, Nivel3.txt  (mapas de texto)
  Niveles/Tiles/       Tiles generados
  Prefabs/             Jugador, Enemigos, Proyectiles, Objetos, Escenario, Efectos
  Resources/           SistemasJuego.prefab (GameManager + AudioManager + Efectos + Transición)
  Scenes/              Splash, MainMenu, Nivel1, Nivel2, Nivel3
  Scripts/Core, Jugador, Enemigos, Armas, Niveles, UI, Audio
  Editor/              Constructor del juego y build
  TextMesh Pro/        Recursos esenciales de TMP (ya extraídos)
Herramientas/          Scripts de Python que se usaron para preparar el arte y la primera versión de los mapas
CREDITOS.md            Todos los recursos con autor, enlace y licencia
```

## 4. Qué hace cada script importante

**Core**
- `ControlesJuego` — único lugar donde se leen teclado, mouse y gamepad (Input System). Los demás scripts usan sus propiedades.
- `GameManager` — vidas, puntos, nivel actual, arma conservada, cambio de escena y "hit stop". Persiste entre escenas.
- `Arranque` — crea `Resources/SistemasJuego` antes de cargar cualquier escena.
- `Transicion` — fundido a negro entre escenas.
- `CamaraSeguimiento` — cámara lateral con suavizado, adelanto, límites, bloqueo en la arena y sacudida.
- `PoolObjetos` — reutiliza balas, efectos y popups.
- `Efectos` — explosiones, chispas, polvo, casquillos, fogonazos y textos flotantes con una línea.
- `AnimadorSprites` — animaciones por listas de sprites (las "skins").

**Jugador**: `JugadorMovimiento` (estados, coyote time, buffer, salto variable, rodar), `JugadorDisparo`
(8 direcciones, armas, munición, granadas), `VidaJugador` (daño, invulnerabilidad, parpadeo, muerte), `AnimacionJugador`.

**Enemigos**: `EnemigoBase` (ver, voltear, disparar), `EnemigoSoldado` (Patrulla → Persecución → Disparo → Regreso; sirve
para raso, escudo y élite), `TorretaEnemiga`, `SaludEnemigo` (vida, escudo frontal, flash, puntos, drop, muerte sin sangre),
`SoltarObjeto`, `JefeBase`, `JefeCampamento` (nivel 1), `VehiculoMiniJefe` (nivel 2), `JefeKruger` (nivel 3, 2 fases).

**Armas**: `ArmaDatos` (asset con cadencia, daño, munición, patrón), `Bala`, `ProyectilExplosivo` (granada y morteros),
`DañoArea`, `ItemRecogible`.

**Niveles**: `ControladorNivel` (briefing, tarjeta de título, muerte/reaparición, checkpoints, jefe, bonos, fin),
`GenerarColisionTilemap`, `Checkpoint`, `ZonaMuerte`, `AlambrePuas`, `PlataformaDestructible`, `Cofre`, `ArenaJefe`, `ParallaxCapa`.

**UI**: `HUD`, `MenuPausa`, `PantallaFinal`, `PanelOpciones`, `MenuPrincipal`, `Splash`, `BotonAnimado`,
`FondoAnimadoUI`, `AnimadorImagenUI`.

**Audio**: `AudioManager` (música con fundido cruzado, jingles, efectos con variación de tono, volúmenes en PlayerPrefs).

## 5. Cambiar skins (sprites)

- **Personajes/enemigos**: abre el prefab (por ejemplo `Prefabs/Jugador/ComandoAlfa`), componente **AnimadorSprites**,
  y arrastra tus sprites a cada clip (`Idle`, `Correr`, `Salto`, `Agachado`, `Arriba`, `Muerte`, ...).
  El sprite de cada personaje está en el hijo `Sprite`.
- **Ítems/cofres/escenario**: cambia el sprite del `SpriteRenderer` del prefab.
- **Íconos de armas**: en `Assets/Datos/Armas/*.asset`, campo `icono`.
- Importa tus PNG como *Sprite (2D and UI)*, *Filter Mode: Point*, sin compresión. Los personajes usan 48 px por unidad
  (el soldado mide ~1.75 unidades) y los tiles 32 px por unidad.
- Si cambias los PNG de `Assets/Art/Generado` conservando el nombre y el tamaño de celda, basta con volver a correr
  **Construir juego**.

## 6. Reemplazar audio

- Música: reemplaza los archivos en `Assets/Audio/Musica` (mismo nombre) o asigna otros clips en
  `Resources/SistemasJuego` → **AudioManager** (menú, jefe, final, jingles) y en cada escena → **ControladorNivel → Musica Nivel**.
- Efectos: en `Resources/SistemasJuego` → **AudioManager → Sonidos** hay una lista por evento (puedes poner varios clips
  y se elige uno al azar). Los disparos del jugador están en cada asset de `Assets/Datos/Armas`.
- Sonidos **temporales** (sintetizados): `Assets/Audio/SFX/llama_SINTETIZADO.wav` y `rodar_SINTETIZADO.wav`.

## 7. Editar niveles

Cada nivel es un archivo de texto en `Assets/Niveles/Mapas/NivelN.txt`. Cada carácter es una celda de 1×1; la leyenda
completa está al inicio del archivo. Edita, guarda y ejecuta **Blackout Squad > Construir juego**.

| Símbolo | Significado | Símbolo | Significado |
|---|---|---|---|
| `#` | suelo | `=` | plataforma (se atraviesa desde abajo) |
| `D` | bloque destructible | `~` | agua/foso líquido (mortal) |
| `^` | alambre de púas | `O` | contenedor 3×2 |
| `P` | inicio del jugador | `C` | checkpoint |
| `s` `e` `l` `t` | soldado raso, escudo, élite, torreta | `a` `h` `f` | ametralladora, escopeta, lanzallamas |
| `+` `1` `g` | botiquín, vida extra, granadas | `K` `M` `G` | cofre con botiquín / ametralladora / granadas |
| `[` | entrada de la arena del jefe | `]` | pared derecha de la arena |
| `J` | jefe | `Z` | punto de control (nivel 2) |
| `p q b T L x S Y` | decoración | `.` | vacío |

Huecos sin `#` en las filas de abajo son fosos (caer cuesta una vida). La dificultad por nivel (vida de enemigos,
agresividad, frecuencia de botiquines) se ajusta en la escena → **ControladorNivel**.

## 8. Build

**Blackout Squad > Compilar build Windows** usa `BuildPipeline` con las escenas de Build Profiles en este orden:
Splash (0), MainMenu (1), Nivel1 (2), Nivel2 (3), Nivel3 (4).
