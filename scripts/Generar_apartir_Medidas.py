import bpy

base = bpy.context.active_object

# medidas actuales de tu base
current_lens = 56
current_bridge = 15
current_temple = 135

models = [
    ("metal_52_18_140", 52, 18, 140),
    ("metal_55_17_145", 55, 17, 145),
    ("metal_58_16_145", 58, 16, 145),
    ("metal_60_15_150", 60, 15, 150),
]

for name, lens, bridge, temple in models:

    obj = base.copy()
    obj.data = base.data.copy()
    bpy.context.collection.objects.link(obj)

    obj.name = name

    # escala frontal
    scale_x = (lens + bridge + lens) / (current_lens + current_bridge + current_lens)

    # escala patilla
    scale_y = temple / current_temple

    obj.scale.x *= scale_x
    obj.scale.y *= scale_y

print("4 monturas generadas")