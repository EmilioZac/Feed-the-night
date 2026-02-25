# Roadmap de Desarrollo: Feed the Night (Detallado)
**Versión:** 2.0
**Rol:** Lead Game Designer / Product Manager
**Objetivo:** Guía de implementación granular desde concepto hasta Vertical Slice.

> [!NOTE]
> Este documento profundiza en las tareas específicas requeridas para cada entregable. Úsalo para crear tickets en tu gestor de tareas (Trello/Jira/HacknPlan).

---

## Fase 1: Pre-producción (Cimientos y Definición)
**Objetivo:** Eliminar la incertidumbre. Asegurar que la tecnología soporta la visión y que las reglas del juego están escritas antes de codificar.
**Duración Estimada:** 3 Semanas

### 1.1 Subfase: Game Design & Documentación
*Definir las matemáticas detrás del miedo y reglas de zonas.*

*   **Sistema de Hambre (Reglas Numéricas Finales)**
    *   [x] **Decaimiento**:
        *   Pasivo: -1% cada 20 segundos.
        *   Correr: -1% cada 10 segundos (2x velocidad).
        *   Habilidad de Combate: -1% (Costo instantáneo).
        *   Regeneración de Vida: +1 HP/seg a cambio de -0.2% Hambre/seg.
    *   [x] **Alimentación (Ganancia Base)**:
        *   Civil: +20%
        *   Investigador Rango Bajo: +30%
        *   Investigador Rango Alto: +40%
        *   *Diminishing Returns*: Cada vez que comes, la ganancia futura se reduce en un 0.1% acumulativo.
    *   [x] **Tipos de NPC**:
        *   Civiles (Pasivos)
        *   Policías (Atacan según Matrix)
        *   Investigadores Bajo Rango (Atacan según Matrix)
        *   Investigadores Alto Rango (Atacan según Matrix)

*   **Matriz de Sigilo y Zonas (Fuente: `StealthMatrix.csv`)**
    *   [x] **Integrar Reglas del CSV**:
        *   🟢 **Zona Verde (Pública)**: Camuflaje = Invisible. Agacharse = Sospecha (Investigan posición).
        *   🟠 **Zona Naranja (Callejones)**: Camuflaje = Advertencia (Te siguen a 4m). Agacharse = Detección Lenta.
        *   🔴 **Zona Roja (Asesinato)**: Camuflaje = Ataque inmediato. Sigilo Puro = Única opción.
    *   [x] **Validación**: Usar el archivo `StealthMatrix.csv` en la raíz del proyecto como tabla de verdad para la IA.

*   **Diseño de Nivel (Papel)**
    *   [ ] Dibujar plano top-down aplicando códigos de color (Verde/Naranja/Rojo) a las calles y callejones.

### 1.2 Subfase: Ingeniería Core
*Setup del proyecto y herramientas base.*

*   **Arquitectura del Proyecto**
    *   [x] Configurar Unity 2022 LTS o superior con URP (Universal Render Pipeline) para iluminación performante.
    *   [x] Estructurar carpetas: `Assets/_Project`, `Assets/_Project/Art`, `Assets/_Project/Code/Systems`, `Assets/_Project/Code/Controllers`.
    *   [x] Instalar Paquetes: `Input System` (nuevo), `Cinemachine` (cámara), `ProBuilder` (greyboxing rápido).
*   **Controller del Jugador (Prototipo)**
    *   [x] Crear Script `PlayerController.cs`.
    *   [x] Implementar Máquina de Estados Finitos (FSM): `Idle`, `Walk`, `Run` (con ruido), `Crouch` (sigilo), `Feed` (bloqueo de movimiento).
    *   [x] Implementar movimiento físico básico (CharacterController o Rigidbody) ajustando la sensación de peso.

### 1.3 Subfase: Tech Art & Estética
*→ Movida a **Fase 2.4** para no bloquear el prototipo jugable.*

---

## Fase 2: Prototipo Jugable (Validation of Core Loop)
**Objetivo:** Construir una "Caja Gris" (Greybox) fea pero divertida. Si no es divertido cazar cubos, no será divertido cazar modelos 3D.
**Duración Estimada:** 5 Semanas

### 2.1 Subfase: Gameplay "El Cazador"
*Implementar las mecánicas del jugador.*

*   **Gestión de Hambre**
    *   [x] Script `HungerSystem`: Decremento por `Time.deltaTime`.
    *   [x] Script `HealthSystem`: Regeneración pasiva consumiendo hambre.
    *   [x] Script `EnergySystem`: Consumo por carrera/salto y regeneración.
    *   [x] Conexión UI: Barras de Vida, Hambre y Energía con porcentajes.
    *   [ ] Estado `Frenzy`: Si hambre == 0, forzar movimiento hacia el NPC más cercano (override de controles).
*   **Mecánica de Alimentación**
    *   [ ] Trigger de detección: `CanFeed?` (bool) cuando está detrás de un NPC y en rango.
    *   [ ] Acción de Comer: Mantener botón 'E' durante 3 segundos.
    *   [ ] Feedback: Partículas temporales (sangre/energía) y recuperación de la variable `Hunger`.

### 2.2 Subfase: Gameplay "La Presa" (IA)
*Crear el desafío.*

*   **Sensores de IA**
    *   [ ] Script `VisionCone`: Mesh procedural o trigger cónico que detecta el Layer 'Player'.
    *   [ ] Sistema de Raycasts: Verificar si hay obstáculos (muros) entre el Ojo del NPC y el Jugador.
*   **Comportamientos (Behavior Tree o FSM)**
    *   [ ] **Civil**: `Wander` (puntos aleatorios) -> `DetectPlayer` -> `Flee` (correr opuesto al jugador).
    *   [ ] **Policía**: `Patrol` (lista de waypoints) -> `Investigate` (ir a la posición del último ruido) -> `Attack` (Shooting/Arrest).
*   **Coherencia Visual (Color Scripting)**
    *   [ ] **Siluetas y Legibilidad**:
        *   *Civiles*: Siluetas redondeadas, colores desaturados (grises, marrones), postura encorvada (miedo).
        *   *Investigadores*: Siluetas angulares (hombreras, gabardinas rígidas), acentos de color rojo/blanco puro, postura erguida (autoridad).
    *   [ ] **Jerarquía de Amenaza**: Usar emisivos en los enemigos (ojos o equipo) que cambien de Amarillo (Búsqueda) a Rojo (Combate).

### 2.3 Subfase: Integración del Loop
*Cerrar el círculo jugable.*

*   **Nivel de Prueba (Gym)**
    *   [ ] Construir nivel con ProBuilder: Un callejón en forma de T, un patio abierto, cajas para cobertura alta y baja.
    *   [ ] Colocar 3 Civiles y 1 Policía patrullando.
*   **Game Cycle**
    *   [ ] Condición de Victoria: Llenar la barra de hambre al 100% y llegar a la "Zona Segura".
    *   [ ] Condición de Derrota: Barra de Hambre llega a 0 (Muerte por inanición) o Salud llega a 0 (Disparado por policía).

### 2.4 Subfase: Tech Art & Estética (Neo-noir)
*Validar el "Look & Feel" Neo-noir. Movida desde Fase 1.3.*

*   **Atmósfera Visual**
    *   [x] **Shader de Lluvia**: Shader URP (`RainSurface.shader`) con ripples animados, wet-look y VFX de partículas (`RainVFX.cs`).
    *   [ ] **Iluminación Volumétrica**: Configurar niebla global y luces de área para simular la contaminación lumínica de neones.
    *   [ ] **Post-Processing Inicial**: Crear perfil con Color Grading (tonos fríos/azules), Bloom (neones) y Vignette (claustrofobia).
    *   [ ] **Guía de Estilo Técnica (Performance & Look)**:
        *   **Trim Sheets**: Diseñar atlas de texturas de 2048x2048 para elementos arquitectónicos repetitivos (cornisas, marcos de ventanas, bordillos) para reducir draw calls.
        *   **Weighted Normals**: Aplicar en modelos hard-surface para suavizar bordes sin aumentar el polycount (baking innecesario).
    *   [ ] **Sistema de Materiales (Master Shader)**:
        *   **Shader Graph "Uber"**: Crear shader maestro con switches para:
            *   *Wetness/Rain*: Parámetro float (0-1) que ajusta Smoothness y oscurece el Albedo en tiempo real.
            *   *Neon Pulse*: Parámetro de emisivo controlable por script para carteles que parpadean o reaccionan al audio.

---

---

## Fase 2.5: Sistema de Combate (Expansión)
**Objetivo:** Implementar la respuesta ofensiva del jugador y la IA cuando el sigilo falla.
**Duración Estimada:** 3-4 Semanas

### 2.5.1 Estadísticas y Balanceo
*Definir las reglas de daño y resistencia.*

*   **Stats Base**
    *   [ ] **Vida Estándar**: 100 HP (Jugador, Civiles, Policías, Investigador Bajo).
    *   [ ] **Vida Boss**: 150 HP (Investigador Alto Rango).
*   **Jugador (Ghoul)**
    *   [ ] **Ataque Básico (Puños)**: 0.5 Daño.
    *   [ ] **Kagune (Arma Biológica)**: 2 Daño.
        *   *Desbloqueo*: Tras comer 10 NPCs.
        *   *Tipo*: Aleatorio al desbloquear.
    *   [ ] **Ataque Aéreo**: 3 Daño (Requiere estar en aire).
    *   [ ] **Bloqueo**:
        *   Resistencia: 3 Golpes.
        *   Fatiga: Si dura > 8 segundos, se debilita (se rompe con 2 golpes).
    *   [ ] **Dash**: Esquiva rápida.
*   **IA Enemiga**
    *   [ ] **Policía**:
        *   Porra: 2 Daño.
        *   Pistola: 6 Daño.
        *   *Comportamiento*: Huye con < 30% Vida.
    *   [ ] **Investigador Rango Bajo**:
        *   Espada (Quinque): 5 Daño.
        *   Distancia: 7 Daño.
        *   *Comportamiento*: Huye con < 15% Vida.
    *   [ ] **Investigador Rango Alto (Élite)**:
        *   Espada (Quinque): 7 Daño.
        *   Distancia: 10 Daño.
        *   *Comportamiento*: No huye (Lucha a muerte).

---

## Fase 3: Vertical Slice (The Residential District)
**Objetivo:** Una porción vertical del juego final. Calidad de lanzamiento en un área limitada.
**Duración Estimada:** 8-10 Semanas

### 3.1 Subfase: Arte y Entorno (World Building)
*Reemplazar las cajas grises con arte inmersivo.*

*   **Activos 3D**
    *   [ ] Modelar/Adquirir Kit Modular Urbano: Pared ladrillo, Ventana iluminada, Farola, Contenedor de basura, Tuberías (para trepar).
    *   [ ] Modelar Personajes: 1 Ghoul (Jugador), 1 Modelo Civil (con variantes de color), 1 Modelo Policía.
*   **Level Dressing**
    *   [ ] Vestir el nivel Greybox: Añadir detalles, cables colgando, charcos específicos, basura dinámica.
    *   [ ] Iluminación Final: "Bake" de luces estáticas + Luces dinámicas para patrullas (linternas).
*   **Pipeline de Personajes (Tech Art)**
    *   [ ] **Modelado para Animación**:
        *   Modelar en A-Pose (mejor deformación de hombros que T-Pose).
        *   Separar malla de cabeza y manos si se planea desmembramiento o personalización futura.
    *   [ ] **Avatar Masks en Unity**:
        *   Configurar máscaras para "Upper Body" y "Lower Body".
        *   *Objetivo*: Permitir que el Ghoul ejecute la animación de ataque (Kagune) con el torso mientras las piernas siguen corriendo o caminando.
    *   [ ] **Animaciones por Implementar (Checklist)**:
        *   [ ] Idle (Base)
        *   [ ] Walk (Frente, Lados, Atrás)
        *   [ ] Run
        *   [ ] Crouch Idle & Walk
        *   [ ] Attack (Combo 3 golpes)
        *   [ ] Block (Loop)
        *   [ ] Dash (Roll o Slide)
        *   [ ] Feed (Comer)
*   **Optimización de Assets**
    *   [ ] **LODs (Level of Detail)**:
        *   LOD0 (Close): 10k tris.
        *   LOD1 (10m): 5k tris.
        *   LOD2 (Far): Billboard o Low Poly (<500 tris).
    *   [ ] **Colisionadores**: Usar primitivas (Box/Capsule) para el 90% del entorno. Mesh Colliders solo para geometría compleja navegable.

### 3.2 Subfase: Sistemas Avanzados (Polishing)
*Profundidad y sensaciones.*

*   **Audio Inmersivo (Wwise/FMOD o Unity Audio)**
    *   [ ] **Audio Manager**: Sistema para priorizar sonidos.
    *   [ ] **SFX**: Pasos (diferentes según superficie: agua vs concreto), Latido de corazón (aumenta velocidad con bajo Hambre o detección).
    *   [ ] **Música Dinámica**: Layers que entran/salen según el estado de alerta de la IA.
*   **UI Diegética y Feedback**
    *   [ ] Reemplazar barras placeholder con diseño estilizado (Minimalista, blanco/rojo).
    *   [ ] **Indicadores de Daño**: Viñeta roja direccional.
    *   [ ] **Indicadores de Ruido**: Visualizar ondas de sonido en el suelo cuando el jugador corre (feedback visual del ruido generado).

### 3.3 Subfase: Progresión Lite
*Una pequeña muestra de la evolución.*

*   **El Refugio (Safe House)**
    *   [ ] Crear micro-nivel interior (apartamento abandonado).
    *   [ ] Sistema de Interacción: "Dormir" (Guardar partida), "Evolucionar" (UI de mejoras).
*   **Upgrade: Visión Cazadora**
    *   [ ] Implementar Post-Process effect que resalta siluetas de enemigos en Naranja a través de paredes.
    *   [ ] Programar consumo de recurso (Hambre o Energía) al usarla.

### 3.4 Subfase: Empaquetado y QA
*Preparar para mostrar.*

*   **Menús de Flujo**
    *   [ ] Pantalla de Título, Pausa, Game Over, Créditos.
    *   [ ] Transiciones (Fade in/out) entre escenas.
*   **Optimización**
    *   [ ] Configurar Occlusion Culling.
    *   [ ] Ajustar calidad de sombras y luces para mantener 60 FPS estables en la máquina objetivo.

---

## Checklist de Validación Final (Antes de considerar terminada la fase)
- [ ] **El "Game Feel"**: ¿El movimiento se siente fluido y responsivo, no flotante?
- [ ] **Claridad**: ¿Entiende el jugador por qué fue detectado el 100% de las veces? (Crucial para juegos de sigilo).
- [ ] **Estabilidad**: ¿Se puede jugar 30 minutos sin crasheos o bugs bloqueantes?
- [ ] **Atmósfera**: ¿Siente el tester "soledad" o "tensión" solo por el ambiente?
