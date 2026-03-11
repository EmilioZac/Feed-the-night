# Walkthrough - Cinemachine Camera System

I have switched the camera system to **Cinemachine**, which is much more stable and professional than the custom script.

## Changes Made

### 1. New Player Link Script
Created `Assets/_Project/Code/Controllers/CinemachinePlayerLink.cs`.
- **Purpose**: Sincroniza la rotación horizontal del jugador con la cámara de Cinemachine.
- **Handling**: Se encarga de bloquear el cursor y asegurar que el jugador siempre mire "hacia adelante" según la cámara.

### 2. Old Script Removal
He eliminado `CameraController.cs` para evitar conflictos y errores de compilación con el Input System.

## Setup Instructions (Pasos en Unity)

Sigue estos pasos para configurar la cámara profesionalmente:

1. **Main Camera**:
   - Asegúrate de que tu `Main Camera` tenga el componente **Cinemachine Brain**. Si no lo tiene, añádelo.

2. **Crear Virtual Camera**:
   - Ve a `GameObject > Cinemachine > 2D Camera` (o `Virtual Camera`).
   - Renómbrala a `PlayerVirtualCamera`.
   - En el Inspector:
     - **Follow**: Arrastra tu objeto `Player`.
     - **Look At**: Arrastra tu objeto `Player`.
     - **Body**: Cambia a `3rd Person Follow`.
     - **Aim**: Cambia a `Composer` o `Same As Follow Target`.

3. **Sincronizar Rotación**:
   - Arrastra el nuevo script `CinemachinePlayerLink.cs` a tu objeto **Player**.
   - En el campo `Player Body`, arrastra tu objeto `Player`.
   - En el campo `Virtual Camera`, arrastra la cámara que creaste en el paso 2.

4. **Input System**:
   - Si la cámara no se mueve con el ratón, añade el componente **Cinemachine Input Provider** a la cámara virtual y asígnale las acciones del `InputSystem_Actions`.

## Verification
- [ ] La cámara debe seguir al jugador suavemente.
- [ ] Al mover el ratón, la vista debe rotar y el cuerpo del jugador debe girar horizontalmente.
- [ ] W/A/S/D deben mover al jugador relativo a la vista de la cámara.
