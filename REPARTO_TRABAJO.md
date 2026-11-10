# Blackout Squad — Reparto del trabajo

**Blackout Squad** es un *Run & Gun* 2D de desplazamiento lateral hecho en **Unity 6** (URP 2D, nuevo Input System, PC Windows,
teclado y mando de Xbox/PlayStation). El jugador elige entre dos comandos (Alfa o Bravo) y atraviesa tres misiones,
cada una con su propio jefe:

| Misión | Escena | Tema | Objetivo | Jefe |
|---|---|---|---|---|
| 1 | `Nivel1` | Selva hostil | Llegar al campamento enemigo | Jefe del Campamento |
| 2 | `Nivel2` | Base militar | Destruir el punto de control | Tanque (mini-jefe) |
| 3 | `Nivel3` | La fortaleza | Derrotar al Comandante Kruger | Kruger (2 fases) |

El trabajo se dividió en **cuatro áreas**, una por integrante. Cada quien programó los scripts de su área, preparó sus
recursos (prefabs, sprites, audio) y la parte de la herramienta de construcción (`Assets/Editor`) que genera su contenido.

| Integrante | Área | Scripts del juego | Herramientas / contenido |
|---|---|---|---|
| **Orlando** | Jugador, armas y sistemas base | 24 | `ConstructorJuego`, `CompilarBuild`, repositorio y MCP |
| **Astrid** | Enemigos y jefes | 9 + 8 prefabs | `ConstructorPrefabs` |
| **Ángel** | Niveles, escenario y arte | 9 + 3 mapas | `ConstructorNiveles`, `ConstructorImportacion`, `preparar_arte.py`, `generar_mapas.py` |
| **Abril** | Interfaz, música y sonido | 11 + 42 audios | `ConstructorUI`, `preparar_escuadron.py`, `CREDITOS.md` |

---

## 1. Orlando — Jugador, armas y sistemas base

Se encargó del "esqueleto" del juego: todo lo que hace que el jugador se mueva, dispare y que las escenas funcionen entre sí.

**Control del jugador** (`Scripts/Jugador`)
- `JugadorMovimiento`: máquina de estados (quieto, correr, saltar, agacharse, rodar), *coyote time*, *buffer* de salto,
  salto variable (mantener = más alto) y rodar con 0.2 s de invulnerabilidad.
- `JugadorDisparo`: disparo en 8 direcciones, cambio de arma, munición y granadas.
- `VidaJugador`: daño, invulnerabilidad con parpadeo y muerte.
- `AnimacionJugador` y `PersonajeDatos`: animaciones por estado y datos de cada comando.

**Armas** (`Scripts/Armas` y `Datos/Armas`)
- `ArmaDatos` como *ScriptableObject* editable (cadencia, daño, munición, patrón de disparo).
- Cuatro armas configuradas: **Pistola, Ametralladora, Escopeta y Lanzallamas**.
- `Bala`, `ProyectilExplosivo` (granadas y morteros), `DañoArea` e `ItemRecogible`.
- Prefabs de proyectiles: bala del jugador, perdigón, llama, granada, bala pesada y onda de choque.

**Sistemas base** (`Scripts/Core`)
- `ControlesJuego`: único punto donde se lee teclado, mouse y gamepad con el **Input System**.
- `GestorMando`: conexión y desconexión de mandos en caliente (USB/Bluetooth) y pausa automática si se desconecta.
- `GameManager`: vidas, puntos, nivel actual, arma conservada entre niveles y cambio de escena.
- `Arranque` y `Resources/SistemasJuego.prefab`: crean los sistemas globales antes de cualquier escena.
- `CamaraSeguimiento`: cámara con suavizado, adelanto, límites, bloqueo en la arena del jefe y sacudida.
- `PoolObjetos`, `AutoDevolver`, `Efectos`, `EfectoAnimado`, `PopupTexto`, `AnimadorSprites`, `Transicion`, `Capas`, `IDañable`.
- Prefabs de efectos: explosiones, chispas, humo, polvo, escombros, casquillos, fogonazo y texto flotante.

**Integración y entrega**
- `Editor/ConstructorJuego.cs`: menú **Blackout Squad > Construir juego**, que crea capas y matriz de colisiones y
  manda llamar a los constructores de las demás áreas.
- `Editor/CompilarBuild.cs`: genera el ejecutable `Build/Windows/BlackoutSquad.exe`.
- Repositorio en GitHub, `.gitignore` de Unity e integración de **MCP for Unity** con Claude Code.
- `LEEME_BlackoutSquad.md`: guía del proyecto y controles.

---

## 2. Astrid — Enemigos y jefes

Diseñó y programó a todos los rivales: su inteligencia artificial, su vida y lo que sueltan al morir.

**Enemigos comunes** (`Scripts/Enemigos`)
- `EnemigoBase`: detección del jugador, volteo y disparo.
- `EnemigoSoldado`: IA por estados **Patrulla → Persecución → Disparo → Regreso**, reutilizada para tres variantes:
  - **Soldado raso**: el enemigo básico.
  - **Soldado con escudo**: bloquea balas de frente; hay que pegarle por detrás o con granada.
  - **Soldado élite**: más vida y más agresivo.
- `TorretaEnemiga`: torreta fija que apunta al jugador.
- `SaludEnemigo`: vida, escudo frontal, destello al recibir daño, puntos y muerte sin sangre.
- `SoltarObjeto`: probabilidad de soltar botiquines, munición o armas.

**Jefes**
- `JefeBase`: base común (barra de vida, activación en la arena, derrota).
- `JefeCampamento`: jefe de la misión 1.
- `VehiculoMiniJefe`: el **tanque** de la misión 2 (vulnerable por detrás o desde arriba).
- `JefeKruger`: **Comandante Kruger**, jefe final con **dos fases** y cambio de patrón de ataque.

**Prefabs y configuración**
- 8 prefabs: `SoldadoRaso`, `SoldadoEscudo`, `SoldadoElite`, `Torreta`, `JefeCampamento`, `TanqueMiniJefe`,
  `PuntoControl` y `ComandanteKruger`, más la `BalaEnemiga` y el `Mortero`.
- `Editor/ConstructorPrefabs.cs`: genera automáticamente los prefabs de enemigos, jugador, objetos y proyectiles
  con sus colisionadores, animaciones y valores.
- Recoloreo de los sprites del pack *Fierce Soldier* para distinguir raso, escudo, élite y Kruger.

---

## 3. Ángel — Niveles, escenario y arte

Construyó los tres mundos del juego: cómo se ven, cómo están armados y qué peligros tienen.

**Diseño de las tres misiones**
- Mapas en texto `Niveles/Mapas/Nivel1.txt`, `Nivel2.txt` y `Nivel3.txt`, donde cada carácter es una celda
  (suelo, plataformas, fosos, enemigos, objetos, checkpoints y arena del jefe).
- Acomodo de enemigos, cofres, botiquines y armas, y curva de dificultad entre misiones.
- Textos de cada misión (título, subtítulo y briefing).

**Mecánicas del escenario** (`Scripts/Niveles`)
- `ControladorNivel`: briefing, tarjeta de título, muerte y reaparición, checkpoints, activación del jefe, bonos y fin del nivel.
- `Checkpoint`, `ZonaMuerte` (fosos y agua), `AlambrePuas`, `PlataformaDestructible`, `Cofre` y `ArenaJefe`.
- `GenerarColisionTilemap`: colisiones automáticas a partir de los tiles.
- `ParallaxCapa`: fondos con profundidad.

**Arte y escenario**
- `Herramientas/preparar_arte.py`: corta, escala y recolorea los packs de sprites y genera el arte propio
  (escudo, alambre de púas, contenedores, cajas, bandera, botiquín, granada, escopeta, lanzallamas, tiles de la base militar).
- `Herramientas/generar_mapas.py`: primera versión de los mapas.
- `Editor/ConstructorNiveles.cs`: convierte los mapas de texto en escenas con tilemaps, prefabs y fondos.
- `Editor/ConstructorImportacion.cs`: configuración de importación de sprites (pixel art, *Point*, sin compresión).
- Fondos parallax de cada tema (selva, base militar, fortaleza industrial) y prefabs de escenario
  (bloques, contenedores, alambre de púas, checkpoint).

---

## 4. Abril — Interfaz, música y sonido

Hizo todo lo que el jugador ve y escucha fuera de la acción: menús, HUD, música y efectos.

**Menús y pantallas** (`Scripts/UI`)
- `Splash`: pantalla de inicio.
- `MenuPrincipal`: menú con fondo animado en capas (`FondoAnimadoUI`) y soldados animados (`AnimadorImagenUI`).
- `SeleccionPersonaje`: pantalla **ELIGE TU COMANDO** (Alfa o Bravo).
- `PanelOpciones`: volúmenes y ajustes guardados en *PlayerPrefs*.
- `MenuPausa`, `PantallaFinal` (victoria y game over) y `BotonAnimado`.
- `HUD`: vida, vidas extra, puntos, arma, munición y granadas, con íconos que cambian según el control que se use.
- `Editor/ConstructorUI.cs`: genera toda la interfaz de las escenas.
- Fuentes **Black Ops One** (títulos) y **Press Start 2P** (HUD) como TMP Font Assets.

**Comando Bravo**
- `Herramientas/preparar_escuadron.py`: selección de cuadros del pack *Open Gunner*, hoja de sprites del segundo
  comando y su retrato para el HUD.

**Música y sonido** (`Scripts/Audio` y `Assets/Audio`)
- `AudioManager`: música con fundido cruzado, jingles, efectos con variación de tono y volúmenes persistentes.
- Selección de **9 pistas**: menú, nivel 1, nivel 2, nivel 3, jefe, final y jingles de inicio de nivel, victoria y game over.
- **33 efectos de sonido**: disparos de cada arma, explosiones, pasos, salto, aterrizaje, daño, objetos, cofres,
  checkpoint, alarma del jefe, cambio de fase y UI. Dos de ellos (lanzallamas y rodar) se sintetizaron.
- `CREDITOS.md`: registro de todos los recursos con autor, enlace y licencia (CC0, CC-BY y SIL OFL).

---

## Trabajo en conjunto

- **Pruebas de juego** de las tres misiones y ajuste de dificultad.
- **Revisión con MCP**: inspección de las escenas y la consola de Unity con Claude Code (0 errores y 0 advertencias en
  los tres niveles).
- **Control de versiones** en GitHub con commits descriptivos por funcionalidad.
