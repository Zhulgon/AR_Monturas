import bpy

obj = bpy.context.active_object

# medidas actuales
current_lens = 62
current_bridge = 17
current_temple = 135

# objetivo
target_lens = 55
target_bridge = 17
target_temple = 145

# escala horizontal (lente + puente)
scale_x = (target_lens + target_bridge + target_lens) / (current_lens + current_bridge + current_lens)

# escala profundidad (patilla)
scale_y = target_temple / current_temple

# aplicar escala
obj.scale.x *= scale_x
obj.scale.y *= scale_y

print("Escala aplicada")