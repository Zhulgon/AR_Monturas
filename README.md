# Catalogo Virtual Unity (AR)

Proyecto Unity para catalogo de monturas en AR con Vuforia.

## Requisitos

- Unity `6000.0.69f1`
- Modulo Android instalado en Unity Hub (si vas a compilar APK)

## Abrir en otra laptop

1. Clona el repositorio.
2. En Unity Hub, `Add > Add project from disk`.
3. Selecciona la carpeta del proyecto (`Catalogo_Virtual`).
4. Abre con Unity `6000.0.69f1`.

## Escena principal

- `Assets/Scenes/ARScene.unity`

## Configuracion rapida del catalogo

- Menu de Unity: `Tools > Setup AR Catalog`
- Corrige la ruta de origen de FBX en:
  - `Assets/Editor/ARCatalogAutoSetup.cs`
  - Constante: `SourceFbxFolderAbsolute`

## Notas

- No se suben carpetas generadas (`Library`, `Temp`, `Logs`, etc.).
- Si vas a publicar el repo, revisa credenciales/licencias embebidas (ejemplo: Vuforia key) antes de hacerlo publico.
