import bpy
from mathutils import Vector

MM = 0.001

# ====== CONFIG ======
# Si el objeto activo es tu montura, déjalo así:
USE_ACTIVE_OBJECT = True
OBJECT_NAME = "montura"  # solo se usa si USE_ACTIVE_OBJECT = False

# Si quieres medir usando el eje X como "ancho" frontal
# (en algunos modelos puede ser Y; abajo te doy cómo detectar)
FRONT_AXIS = "X"  # "X" o "Y"
UP_AXIS = "Z"     # normalmente Z

# ====== HELPERS ======
def world_bbox(obj):
    return [obj.matrix_world @ Vector(c) for c in obj.bound_box]

def bbox_min_max(bb):
    xs = [p.x for p in bb]
    ys = [p.y for p in bb]
    zs = [p.z for p in bb]
    mn = Vector((min(xs), min(ys), min(zs)))
    mx = Vector((max(xs), max(ys), max(zs)))
    return mn, mx

def m_to_mm(x): 
    return x / MM

def get_obj():
    if USE_ACTIVE_OBJECT:
        o = bpy.context.active_object
        if not o:
            raise ValueError("No hay objeto activo. Selecciona tu montura en el viewport/Outliner.")
        return o
    o = bpy.data.objects.get(OBJECT_NAME)
    if not o:
        raise ValueError(f"No existe un objeto llamado '{OBJECT_NAME}'.")
    return o

def axis_val(v, axis):
    return getattr(v, axis.lower())

# ====== MAIN ======
obj = get_obj()
bb = world_bbox(obj)
mn, mx = bbox_min_max(bb)
dims = mx - mn
center = (mn + mx) * 0.5

# Medidas globales
width_total_mm  = m_to_mm(dims.x)
depth_total_mm  = m_to_mm(dims.y)
height_total_mm = m_to_mm(dims.z)

# Medidas según ejes configurados
front_mm = m_to_mm(axis_val(dims, FRONT_AXIS))
up_mm    = m_to_mm(axis_val(dims, UP_AXIS))

# Estimaciones ópticas (aprox, porque es 1 solo objeto)
# Asumimos: frontal ≈ 2*lens + bridge
# Estimamos lens_w ≈ 0.45 * frontal (típico en gafas) y puente ≈ 0.10-0.18 frontal.
# Mejor: tú le das bridge esperado y resolvemos lens, o viceversa.
lens_est_mm = front_mm * 0.44
bridge_est_mm = max(10.0, min(22.0, front_mm - 2*lens_est_mm))

# Estimación patilla:
# Si las patillas están extendidas hacia atrás, el "largo" se mezcla con profundidad.
# Aquí damos una aproximación: max(front, depth) y lo acotamos.
temple_est_mm = max(135.0, min(155.0, m_to_mm(max(dims.x, dims.y)) * 0.65))

print("\n==============================")
print("MEASURE: SINGLE FRAME OBJECT")
print("==============================")
print(f"Objeto: {obj.name}")
print(f"Centro (mm): ({m_to_mm(center.x):.1f}, {m_to_mm(center.y):.1f}, {m_to_mm(center.z):.1f})")
print("\n--- BBox global (mm) ---")
print(f"Ancho total X: {width_total_mm:.1f}")
print(f"Profundidad Y: {depth_total_mm:.1f}")
print(f"Alto total Z: {height_total_mm:.1f}")

print("\n--- Medidas por ejes (mm) ---")
print(f"Frontal ({FRONT_AXIS}): {front_mm:.1f}")
print(f"Vertical ({UP_AXIS}): {up_mm:.1f}")

print("\n--- Estimaciones ópticas (aprox) ---")
print(f"Lens width (estimado): {lens_est_mm:.1f} mm")
print(f"Bridge (estimado): {bridge_est_mm:.1f} mm")
print(f"Temple length (estimado): {temple_est_mm:.1f} mm")

print("\nTIP:")
print("- Si el 'Frontal' te sale raro, cambia FRONT_AXIS a 'Y' y vuelve a correr.")
print("- Para medir puente/patilla exacto, hay que separar partes (o marcar vertices).")
print("==============================\n")