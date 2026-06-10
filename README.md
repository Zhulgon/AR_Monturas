# Visualizador AR de Monturas

Aplicación móvil de realidad aumentada para Android orientada al apoyo en la atención y venta de monturas en una óptica. El sistema permite visualizar modelos 3D de gafas mediante reconocimiento de imagen objetivo, navegación por catálogo, selección de talla y una escena experimental de Face Tracking.

## Descripción del proyecto

El proyecto consiste en una aplicación desarrollada en Unity que utiliza realidad aumentada para mejorar la experiencia de visualización de monturas ópticas. Al enfocar una imagen objetivo impresa con la cámara del celular, la aplicación reconoce el marcador y muestra una montura 3D superpuesta sobre él.

La solución busca ofrecer una alternativa más interactiva frente a la presentación tradicional de monturas mediante fotografías, inventario físico o vitrinas. Con la realidad aumentada, el usuario puede explorar referencias del catálogo, observar el modelo desde diferentes ángulos y seleccionar tallas de forma dinámica.

Este proyecto fue desarrollado como producto final para el curso de Realidad Aumentada, consolidando un Producto Mínimamente Viable funcional en Android.

## Características principales

- Aplicación ejecutable en dispositivos Android.
- Reconocimiento de imagen objetivo mediante Vuforia Engine.
- Visualización de monturas 3D sobre un marcador físico.
- Navegación entre referencias del catálogo.
- Selección de talla mediante botones X, S, M, L y XL.
- Manipulación visual del modelo al mover o girar el marcador físico.
- Persistencia temporal de la selección del usuario.
- Transición hacia una escena experimental de Face Tracking.
- Activación de cámara frontal para prueba inicial de montura sobre rostro.
- Flujo de escenas: SplashScene, InfoScene, ARScene y FaceTracking.

## Tecnologías utilizadas

- Unity 6
- Universal Render Pipeline, URP
- C#
- Vuforia Engine
- AR Foundation
- ARCore XR Plugin
- Android Build Support
- SDK, NDK, Gradle y OpenJDK
- Modelos 3D en formato compatible con Unity

## Flujo de funcionamiento

El usuario abre la aplicación en Android.
Se muestra la pantalla inicial SplashScene.
La escena InfoScene presenta información básica sobre la experiencia.
El usuario ingresa a ARScene.
La cámara se activa y Vuforia busca la imagen objetivo.
Cuando el marcador es reconocido, aparece la montura 3D sobre el target.
El usuario puede cambiar entre modelos del catálogo.
El usuario puede seleccionar talla: X, S, M, L o XL.
La selección se guarda temporalmente.
El usuario puede pasar a la escena FaceTracking.
La cámara frontal se activa e instancia la montura seleccionada sobre el rostro.
