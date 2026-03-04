import bpy
import csv
import os
from mathutils import Vector

MM = 0.001

# Paths (edit if needed)
CSV_PATH = r"C:\Users\Js\Desktop\Proyectos\AR_Monturas\data\realistic_catalog.csv"
EXPORT_DIR = r"C:\Users\Js\Desktop\Proyectos\AR_Monturas\exports\glb"

# -------------------------
# Utils
# -------------------------
def ensure_dir(path: str):
    os.makedirs(path, exist_ok=True)

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def set_units_mm_visual():
    s = bpy.context.scene
    s.unit_settings.system = 'METRIC'
    s.unit_settings.scale_length = 0.001  # makes viewport feel like mm

def new_material(name, rgba=(0.2,0.2,0.2,1.0), metallic=0.0, rough=0.35, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (rgba[0], rgba[1], rgba[2], alpha)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    if alpha < 1.0:
        mat.blend_method = 'BLEND'
    return mat

def material_for_row(color_name: str, material_name: str):
    palette = {
        "black": (0.03,0.03,0.03,1.0),
        "gray": (0.25,0.25,0.25,1.0),
        "silver": (0.75,0.75,0.78,1.0),
        "gold": (0.85,0.72,0.25,1.0),
        "blue": (0.10,0.25,0.70,1.0),
        "green": (0.10,0.55,0.25,1.0),
        "red": (0.70,0.12,0.12,1.0),
        "purple": (0.45,0.20,0.65,1.0),
        "transparent": (0.9,0.9,0.95,0.25),
        "tortoise": (0.25,0.16,0.08,1.0),
    }
    color_name = color_name.strip().lower()
    material_name = material_name.strip().lower()
    c = palette.get(color_name, (0.2,0.2,0.2,1.0))
    is_metal = material_name == "metal"

    if color_name == "transparent":
        return new_material(f"MAT_{color_name}_{material_name}", c, metallic=0.0, rough=0.05, alpha=0.25)
    if is_metal:
        return new_material(f"MAT_{color_name}_{material_name}", c, metallic=1.0, rough=0.25, alpha=1.0)
    return new_material(f"MAT_{color_name}_{material_name}", c, metallic=0.0, rough=0.35, alpha=1.0)

def make_curve_from_points(name, points_2d):
    curve_data = bpy.data.curves.new(name=name, type='CURVE')
    curve_data.dimensions = '3D'
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(len(points_2d) - 1)

    for i, (x, y) in enumerate(points_2d):
        bp = spline.bezier_points[i]
        bp.co = (x, y, 0.0)
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'

    spline.use_cyclic_u = True

    obj = bpy.data.objects.new(name, curve_data)
    bpy.context.collection.objects.link(obj)
    return obj

def set_bevel(curve_obj, radius_m, resolution=8):
    curve_obj.data.bevel_depth = radius_m
    curve_obj.data.bevel_resolution = resolution
    curve_obj.data.fill_mode = 'FULL'

def convert_to_mesh(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    obj.select_set(False)
    return obj

def add_solidify(obj, thickness_m):
    mod = obj.modifiers.new("Solidify", "SOLIDIFY")
    mod.thickness = thickness_m
    mod.offset = 0.0
    return mod

# -------------------------
# 5 styles (good enough for MVP)
# -------------------------
def shape_points(style, w_mm, h_mm):
    import math
    w = w_mm * MM
    h = h_mm * MM
    style = style.strip().lower()

    def ellipse(n=18):
        pts=[]
        for i in range(n):
            t=(i/n)*2*math.pi
            pts.append(((w/2)*math.cos(t), (h/2)*math.sin(t)))
        return pts

    if style == "round":
        r = (min(w_mm, h_mm)/2.0) * MM
        n=18
        return [(r*math.cos((i/n)*2*math.pi), r*math.sin((i/n)*2*math.pi)) for i in range(n)]

    if style == "oval":
        return ellipse(18)

    if style == "square":
        n=20
        p=0.55
        pts=[]
        for i in range(n):
            t=(i/n)*2*math.pi
            cx=math.cos(t); sy=math.sin(t)
            x=(w/2)*(abs(cx)**p)*(1 if cx>=0 else -1)
            y=(h/2)*(abs(sy)**p)*(1 if sy>=0 else -1)
            pts.append((x,y))
        return pts

    if style == "aviator":
        n=22
        pts=[]
        for i in range(n):
            t=(i/n)*2*math.pi
            cx=math.cos(t); sy=math.sin(t)
            scale = 1.0 - 0.22*max(0.0, -sy)
            x=(w/2)*cx*scale
            y=(h/2)*sy*1.05
            if sy > 0.75: y *= 0.92
            pts.append((x,y))
        return pts

    if style == "cat_eye":
        n=22
        pts=[]
        for i in range(n):
            t=(i/n)*2*math.pi
            cx=math.cos(t); sy=math.sin(t)
            x=(w/2)*cx
            y=(h/2)*sy
            if sy > 0.0:
                y += (abs(cx)**1.5) * (0.10*h)
            if sy < -0.5:
                y *= 0.92
            pts.append((x,y))
        return pts

    return ellipse(18)

# -------------------------
# Build frame
# -------------------------
def build_frame(row):
    model     = row["model"].strip()
    lens      = float(row["lens"])
    bridge    = float(row["bridge"])
    temple    = float(row["temple"])
    height    = float(row["height"])
    style     = row["style"].strip().lower()
    color     = row.get("color","black").strip().lower()
    material  = row.get("material","acetate").strip().lower()
    thick_mm  = float(row.get("thickness", 3.0))
    depth_mm  = float(row.get("depth", 6.0))

    mat = material_for_row(color, material)

    x_offset = (bridge/2.0 + lens/2.0) * MM
    left_center  = Vector((-x_offset, 0.0, 0.0))
    right_center = Vector(( x_offset, 0.0, 0.0))

    pts = shape_points(style, lens, height)

    rimL = make_curve_from_points(f"{model}_RimL_curve", pts)
    rimL.location = left_center
    set_bevel(rimL, thick_mm*MM, resolution=8)

    rimR = make_curve_from_points(f"{model}_RimR_curve", pts)
    rimR.location = right_center
    set_bevel(rimR, thick_mm*MM, resolution=8)

    rimL = convert_to_mesh(rimL)
    rimR = convert_to_mesh(rimR)
    add_solidify(rimL, depth_mm*MM)
    add_solidify(rimR, depth_mm*MM)

    # Bridge
    bpy.ops.mesh.primitive_cube_add(location=(0.0,0.0,0.0))
    bridge_obj = bpy.context.active_object
    bridge_obj.name = f"{model}_Bridge"
    bridge_obj.scale = ((bridge*0.50)*MM, (thick_mm*0.60)*MM, (thick_mm*0.60)*MM)
    bev = bridge_obj.modifiers.new("Bevel", "BEVEL")
    bev.width = (thick_mm*0.55)*MM
    bev.segments = 3

    # Temples (simple)
    hinge_x = (x_offset + (lens*0.45)*MM)
    y_back  = -(thick_mm*0.35)*MM

    bpy.ops.mesh.primitive_cube_add(location=( hinge_x, y_back, 0.0))
    tR = bpy.context.active_object
    tR.name = f"{model}_TempleR"
    tR.scale = ((temple*0.50)*MM, (thick_mm*0.35)*MM, (thick_mm*0.30)*MM)

    bpy.ops.mesh.primitive_cube_add(location=(-hinge_x, y_back, 0.0))
    tL = bpy.context.active_object
    tL.name = f"{model}_TempleL"
    tL.scale = ((temple*0.50)*MM, (thick_mm*0.35)*MM, (thick_mm*0.30)*MM)

    for o in [rimL, rimR, bridge_obj, tR, tL]:
        o.data.materials.append(mat)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.shade_smooth()

    return model

def export_glb(model_name: str):
    ensure_dir(EXPORT_DIR)
    filepath = os.path.join(EXPORT_DIR, f"{model_name}.glb")
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=filepath, export_format='GLB', export_apply=True)

def main():
    set_units_mm_visual()
    ensure_dir(EXPORT_DIR)

    with open(CSV_PATH, newline='', encoding='utf-8') as f:
        rows = list(csv.DictReader(f))

    for row in rows:
        clean_scene()
        model = build_frame(row)
        export_glb(model)
        print(f"✅ Exported {model}.glb")

    print("🎉 Done. Check exports/glb")

main()