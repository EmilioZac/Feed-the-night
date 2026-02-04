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
*Definir las matemáticas detrás del miedo.*

*   **Sistema de Hambre (Matemáticas)**
    *   [ ] Definir en hoja de cálculo: `Valor_Maximo_Hambre`, `Tasa_Decaimiento_Pasiva` (por segundo), `Costo_Habilidad_Correr`, `Costo_Habilidad_Camuflaje`.
    *   [ ] Definir `Umbral_Frenesi` (ej: al llegar al 0% o 5% de hambre).
    *   [ ] Diseñar la "Tabla de Alimentación": Cuánta hambre recupera un Civil vs un Policía vs un NPC Crítico.
*   **Sistema de Sigilo (Reglas)**
    *   [ ] Definir distancias de visión en metros para estados de alerta (Blanco, Amarillo, Rojo).
    *   [ ] Definir tiempos de reacción (cuántos segundos tarda un guardia en pasar de "Te veo" a "Disparo").
*   **Diseño de Nivel (Papel)**
    *   [ ] Dibujar plano top-down del "Distrito Residencial" identificando: Rutas principales (calle), Rutas de sigilo (callejones/tejados), Puntos de alimentación seguros.

### 1.2 Subfase: Ingeniería Core
*Setup del proyecto y herramientas base.*

*   **Arquitectura del Proyecto**
    *   [ ] Configurar Unity 2022 LTS o superior con URP (Universal Render Pipeline) para iluminación performante.
    *   [ ] Estructurar carpetas: `_Project`, `_Project/Art`, `_Project/Code/Systems`, `_Project/Code/Controllers`.
    *   [ ] Instalar Paquetes: `Input System` (nuevo), `Cinemachine` (cámara), `ProBuilder` (greyboxing rápido).
*   **Controller del Jugador (Prototipo)**
    *   [ ] Crear Script `PlayerController.cs`.
    *   [ ] Implementar Máquina de Estados Finitos (FSM): `Idle`, `Walk`, `Run` (con ruido), `Crouch` (sigilo), `Feed` (bloqueo de movimiento).
    *   [ ] Implementar movimiento físico básico (CharacterController o Rigidbody) ajustando la sensación de peso.

### 1.3 Subfase: Tech Art & Estética
*Validar el "Look & Feel" Neo-noir.*

*   **Atmósfera Visual**
    *   [ ] **Shader de Lluvia**: Crear shader graph que simule ondas de agua en el suelo y efecto de "mojado" en superficies (Smoothness up, Normal map noise).
    *   [ ] **Iluminación Volumétrica**: Configurar niebla global y luces de área para simular la contaminación lumínica de neones.
    *   [ ] **Post-Processing Inicial**: Crear perfil con Color Grading (tonos fríos/azules), Bloom (neones) y Vignette (claustrofobia).

---

## Fase 2: Prototipo Jugable (Validation of Core Loop)
**Objetivo:** Construir una "Caja Gris" (Greybox) fea pero divertida. Si no es divertido cazar cubos, no será divertido cazar modelos 3D.
**Duración Estimada:** 5 Semanas

### 2.1 Subfase: Gameplay "El Cazador"
*Implementar las mecánicas del jugador.*

*   **Gestión de Hambre**
    *   [ ] Script `HungerSystem`: Decremento por `Time.deltaTime`.
    *   [ ] Conexión UI: Barra simple (Slider) que cambia de color al bajar del 20%.
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

### 2.3 Subfase: Integración del Loop
*Cerrar el círculo jugable.*

*   **Nivel de Prueba (Gym)**
    *   [ ] Construir nivel con ProBuilder: Un callejón en forma de T, un patio abierto, cajas para cobertura alta y baja.
    *   [ ] Colocar 3 Civiles y 1 Policía patrullando.
*   **Game Cycle**
    *   [ ] Condición de Victoria: Llenar la barra de hambre al 100% y llegar a la "Zona Segura".
    *   [ ] Condición de Derrota: Barra de Hambre llega a 0 (Muerte por inanición) o Salud llega a 0 (Disparado por policía).

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
