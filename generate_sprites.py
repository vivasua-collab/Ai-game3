#!/usr/bin/env python3
"""
Генератор PNG-спрайтов для Cultivation World Simulator
Эквивалент TileSpriteGenerator.cs, но работает вне Unity.
Создано: 2026-04-14 14:11:00 UTC
"""

import os
import math
import random
from PIL import Image, ImageDraw, ImageFilter

TILE_SIZE = 64
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "UnityProject", "Assets", "Sprites", "Tiles")

# Воспроизведение PerlinNoise-подобной вариации
def perlin_variation(x, y, scale=0.1):
    """Простая псевдо-Perlin вариация для цветового сдвига"""
    val = math.sin(x * scale * 7.3 + y * scale * 13.7) * math.cos(y * scale * 11.1 + x * scale * 5.3)
    return val * 0.05

def clamp(v, lo=0, hi=255):
    return max(lo, min(hi, int(v)))

def color_with_variation(base_color, x, y):
    """Добавить PerlinNoise вариацию к цвету"""
    variation = perlin_variation(x, y)
    r = clamp((base_color[0] / 255.0 + variation) * 255)
    g = clamp((base_color[1] / 255.0 + variation) * 255)
    b = clamp((base_color[2] / 255.0 + variation) * 255)
    a = base_color[3] if len(base_color) > 3 else 255
    return (r, g, b, a)

def generate_terrain_sprite(name, color_rgb, alpha=255):
    """Генерация terrain спрайта с PerlinNoise вариацией"""
    img = Image.new('RGBA', (TILE_SIZE, TILE_SIZE), (0, 0, 0, 0))
    base = (color_rgb[0], color_rgb[1], color_rgb[2], alpha)
    
    pixels = img.load()
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            variation = perlin_variation(x, y)
            r = clamp((base[0] / 255.0 + variation) * 255)
            g = clamp((base[1] / 255.0 + variation) * 255)
            b = clamp((base[2] / 255.0 + variation) * 255)
            pixels[x, y] = (r, g, b, alpha)
    
    # Спецэффекты
    if name == "terrain_lava":
        # Яркие прожилки
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                crack = math.sin(x * 0.3 + 100) * math.cos(y * 0.3 + 100) * 0.5 + 0.5
                if crack > 0.55:
                    pixels[x, y] = (255, 179, 26, alpha)
    
    elif name == "terrain_ice":
        # Блики
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                shine = math.sin(x * 0.15 + 50) * math.cos(y * 0.15 + 50) * 0.5 + 0.5
                if shine > 0.6:
                    pixels[x, y] = (230, 242, 255, alpha)
    
    elif name == "terrain_snow":
        # Лёгкая текстура снежинок
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                sparkle = math.sin(x * 0.8 + y * 0.6) * math.cos(x * 0.4 - y * 0.9) * 0.5 + 0.5
                if sparkle > 0.85:
                    pixels[x, y] = (255, 255, 255, alpha)
    
    elif name == "terrain_water_shallow":
        # Волнистая текстура
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                wave = math.sin(x * 0.2 + y * 0.15) * 10
                r = clamp(77 + wave)
                g = clamp(128 + wave)
                b = clamp(204 + wave * 0.5)
                pixels[x, y] = (r, g, b, alpha)
    
    elif name == "terrain_water_deep":
        # Глубокие волны
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                wave = math.sin(x * 0.15 + y * 0.2) * 15
                r = clamp(51 + wave * 0.5)
                g = clamp(77 + wave)
                b = clamp(179 + wave * 0.3)
                pixels[x, y] = (r, g, b, alpha)
    
    elif name == "terrain_grass":
        # Травяная текстура с точками
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                grass_detail = math.sin(x * 0.5 + y * 0.3) * math.cos(x * 0.3 - y * 0.7) * 0.5 + 0.5
                if grass_detail > 0.7 and (x + y) % 3 == 0:
                    r = clamp(80 + grass_detail * 30)
                    g = clamp(160 + grass_detail * 20)
                    b = clamp(60 + grass_detail * 15)
                    pixels[x, y] = (r, g, b, alpha)
    
    elif name == "terrain_stone":
        # Каменная текстура
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                stone = math.sin(x * 0.4 + y * 0.5) * math.cos(x * 0.6 - y * 0.3)
                if stone > 0.3:
                    pixels[x, y] = (140, 140, 148, alpha)
                elif stone < -0.2:
                    pixels[x, y] = (115, 115, 125, alpha)
    
    elif name == "terrain_dirt":
        # Земляная текстура
        for y in range(TILE_SIZE):
            for x in range(TILE_SIZE):
                dirt = math.sin(x * 0.3 + y * 0.4) * math.cos(x * 0.5 - y * 0.6)
                if dirt > 0.3:
                    pixels[x, y] = (165, 110, 55, alpha)
    
    save_png(img, name)

def draw_rect(img, x, y, w, h, color):
    """Нарисовать прямоугольник"""
    draw = ImageDraw.Draw(img)
    for px in range(x, min(x + w, TILE_SIZE)):
        for py in range(y, min(y + h, TILE_SIZE)):
            if px >= 0 and py >= 0:
                img.putpixel((px, py), color)

def draw_ellipse(img, cx, cy, rx, ry, color):
    """Нарисовать эллипс (залитый)"""
    for x in range(cx - rx, cx + rx + 1):
        for y in range(cy - ry, cy + ry + 1):
            dx = (x - cx) / max(rx, 1)
            dy = (y - cy) / max(ry, 1)
            if dx * dx + dy * dy <= 1.0 and 0 <= x < TILE_SIZE and 0 <= y < TILE_SIZE:
                img.putpixel((x, y), color)

def generate_object_sprite(name, color_rgb, shape):
    """Генерация object спрайта с формой"""
    img = Image.new('RGBA', (TILE_SIZE, TILE_SIZE), (0, 0, 0, 0))
    cx = TILE_SIZE // 2
    cy = TILE_SIZE // 2
    color = (color_rgb[0], color_rgb[1], color_rgb[2], 255)
    
    if shape == "tree":
        # Ствол
        draw_rect(img, cx - 4, 0, 8, 20, (102, 64, 38, 255))
        # Крона (треугольник)
        for y in range(15, TILE_SIZE):
            width = (TILE_SIZE - y) // 2
            for px in range(cx - width, cx + width):
                if 0 <= px < TILE_SIZE:
                    variation = perlin_variation(px, y) * 20
                    c = (clamp(color_rgb[0] + variation), clamp(color_rgb[1] + variation), clamp(color_rgb[2] + variation), 255)
                    img.putpixel((px, y), c)
    
    elif shape == "small_rock":
        draw_ellipse(img, cx, cy - 5, 12, 8, color)
        lighter = (clamp(color_rgb[0] * 1.1), clamp(color_rgb[1] * 1.1), clamp(color_rgb[2] * 1.1), 255)
        draw_ellipse(img, cx, cy - 5, 10, 6, lighter)
    
    elif shape == "medium_rock":
        draw_ellipse(img, cx, cy - 8, 18, 14, color)
        lighter = (clamp(color_rgb[0] * 1.2), clamp(color_rgb[1] * 1.2), clamp(color_rgb[2] * 1.2), 255)
        darker = (clamp(color_rgb[0] * 0.9), clamp(color_rgb[1] * 0.9), clamp(color_rgb[2] * 0.9), 255)
        draw_ellipse(img, cx - 5, cy - 5, 8, 6, lighter)
        draw_ellipse(img, cx + 6, cy - 3, 6, 5, darker)
    
    elif shape == "bush":
        darker = (clamp(color_rgb[0] * 0.9), clamp(color_rgb[1] * 0.9), clamp(color_rgb[2] * 0.9), 255)
        lighter = (clamp(color_rgb[0] * 1.1), clamp(color_rgb[1] * 1.1), clamp(color_rgb[2] * 1.1), 255)
        draw_ellipse(img, cx - 8, cy, 10, 12, color)
        draw_ellipse(img, cx + 8, cy, 10, 12, darker)
        draw_ellipse(img, cx, cy + 5, 14, 10, lighter)
    
    elif shape == "chest":
        # Основа
        draw_rect(img, 10, 10, 44, 30, color)
        # Крышка
        lighter = (clamp(color_rgb[0] * 1.2), clamp(color_rgb[1] * 1.2), clamp(color_rgb[2] * 1.2), 255)
        draw_rect(img, 8, 35, 48, 12, lighter)
        # Замок
        draw_rect(img, cx - 4, 20, 8, 15, (204, 179, 77, 255))
    
    elif shape == "ore_vein":
        # Каменная основа
        draw_ellipse(img, cx, cy - 5, 16, 12, (115, 102, 89, 255))
        # Рудные вкрапления
        draw_ellipse(img, cx - 5, cy - 3, 6, 4, (204, 153, 51, 255))
        draw_ellipse(img, cx + 4, cy - 7, 5, 3, (179, 128, 38, 255))
        draw_ellipse(img, cx + 2, cy + 2, 4, 3, (217, 166, 64, 255))
    
    elif shape == "herb":
        # Стебель
        draw_rect(img, cx - 1, 5, 2, 25, (51, 102, 38, 255))
        # Листья
        draw_ellipse(img, cx - 6, cy + 5, 5, 4, color)
        draw_ellipse(img, cx + 6, cy + 8, 5, 4, color)
        lighter = (clamp(color_rgb[0] * 1.15), clamp(color_rgb[1] * 1.15), clamp(color_rgb[2] * 1.15), 255)
        draw_ellipse(img, cx, cy + 12, 6, 5, lighter)
        # Цветок
        draw_ellipse(img, cx, cy + 20, 4, 4, (230, 230, 77, 255))
    
    save_png(img, name)

def save_png(img, name):
    """Сохранить PNG файл"""
    path = os.path.join(OUTPUT_DIR, f"{name}.png")
    img.save(path, "PNG")
    print(f"  ✓ {name}.png")

def main():
    print(f"Генерация спрайтов в {OUTPUT_DIR}")
    
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    print("\n=== Terrain Sprites ===")
    # Terrain sprites (цвета из TileSpriteGenerator.cs)
    generate_terrain_sprite("terrain_grass", (102, 179, 77))
    generate_terrain_sprite("terrain_dirt", (153, 102, 51))
    generate_terrain_sprite("terrain_stone", (128, 128, 140))
    generate_terrain_sprite("terrain_water_shallow", (77, 128, 204, 204))  # alpha=0.8 → 204
    generate_terrain_sprite("terrain_water_deep", (51, 77, 179, 230))  # alpha=0.9 → 230
    generate_terrain_sprite("terrain_sand", (230, 217, 153))
    generate_terrain_sprite("terrain_snow", (242, 242, 255))
    generate_terrain_sprite("terrain_ice", (179, 217, 242))
    generate_terrain_sprite("terrain_lava", (230, 77, 13))
    generate_terrain_sprite("terrain_void", (26, 26, 26))
    
    print("\n=== Object Sprites ===")
    generate_object_sprite("obj_tree", (77, 128, 51), "tree")
    generate_object_sprite("obj_rock_small", (153, 140, 128), "small_rock")
    generate_object_sprite("obj_rock_medium", (128, 115, 102), "medium_rock")
    generate_object_sprite("obj_bush", (89, 140, 64), "bush")
    generate_object_sprite("obj_chest", (153, 102, 51), "chest")
    generate_object_sprite("obj_ore_vein", (179, 128, 51), "ore_vein")
    generate_object_sprite("obj_herb", (51, 153, 77), "herb")
    
    print(f"\n✅ Все спрайты сгенерированы ({len(os.listdir(OUTPUT_DIR))} файлов)")

if __name__ == "__main__":
    main()
