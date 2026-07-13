# Walkthrough - Desarrollo del Personaje y Mapa Inicial

Este documento detalla las últimas implementaciones realizadas y los objetivos pendientes estructurados para el proyecto **Feed the Night**.

---

## 1. Fase 2.0: Creación Básica del Personaje
Se ha completado la creación básica del personaje principal (Ghoul):
- **Modelo Base**: Configurado para deformación y enlazado con la máquina de estados.
- **Movimiento**: Implementación de las animaciones de movimiento básicas (`Idle`, `Walk`, `Run`).
- **Ataques y Armas**: Implementados los combos de ataque básicos y posturas preliminares con armas.
- **Avatar Masks**: Configuración para independizar el torso de las piernas (`Upper Body` / `Lower Body`).

### 🛠️ Pendientes para la Fase 3.1 (Tech Art & Pulido):
- **Pulir animaciones**: Ajustar detalles finos en la animación de comer (`Feed`) y en el uso de armas.
- **Arreglar animación**: Corregir y ajustar el comportamiento visual en la animación de **camuflaje**.
- **Ampliación de combate**: Incorporar más sistemas de ataque y esquive (`Block` / `Dash`).

---

## 2. Fase 2.0 y 2.3: Mapa y Colisionadores
Se ha estructurado la base del escenario (un mapa de ciudad con callejones debido al gran volumen de edificios):
- **Geometría y Colisiones**: Calles, callejones, obstáculos menores y bordes del mapa cuentan con **Box Colliders** funcionales.
- **Nivel de Prueba (Fase 2.3)**: El escenario de prueba se establece como una **ciudad con callejones y edificios** en lugar de una sala de pruebas vacía (gym).

### 🛠️ Pendientes para la Fase 3 (Acabar el Mapa):
- **Finalización del Entorno**: Completar el detalle y la geometría de la ciudad.
- **Edificios (Pendiente)**: Los Box Colliders definitivos para los edificios se colocarán al final del desarrollo del nivel para optimizar las iteraciones en el gameplay.

---

## 3. Estado de la Fase 2.1 y 2.4
- **Gameplay "El Cazador" (2.1)**: Se han marcado como **completadas** todas las mecánicas, incluyendo la lógica de hambre, la salud, la energía, el trigger de alimentación (`CanFeed?`) y la acción básica de comer.
- **Shader de Lluvia (Fase 2.4)**: Permanece **desmarcado** (pendiente de validación/implementación final).
- **Excepción (VFX de Partículas)**: Las partículas temporales de alimentación (sangre/energía) han sido movidas a la **Fase 3 (Sistemas Avanzados / Pulido)**.

---

## 4. Actualización del Roadmap
Todas las tareas han sido actualizadas y organizadas en el archivo [ROADMAP.md](file:///e:/Proyectos%20Unity/Feed%20the%20night/ROADMAP.md).
