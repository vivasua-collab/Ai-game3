#!/usr/bin/env python3
"""
process_ai_sprites.py — Предобработка AI-спрайтов для Cultivation World Simulator

Обрабатывает AI-спрайты из UnityProject/Assets/Sprites/Tiles_AI/:
1. Уменьшает 1024×1024 → 64×64 (Lanczos resize)
2. Для obj_* спрайтов: конвертирует RGB→RGBA, flood-fill фон → прозрачный
3. Сохраняет результат в UnityProject/Assets/Sprites/Tiles/

Запуск: python3 tools/process_ai_sprites.py

Использовать ПЕРЕД Build All в Unity если C# обработка не сработала.
Создано: 2026-04-15 16:53:48 UTC
"""

import os
import sys
from collections import deque

try:
    from PIL import Image
except ImportError:
    print("ОШИБКА: Pillow не установлен. Установите: pip install Pillow")
    sys.exit(1)


# === НАСТРОЙКИ ===
AI_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "UnityProject", "Assets", "Sprites", "Tiles_AI")
OUTPUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                          "UnityProject", "Assets", "Sprites", "Tiles")
TARGET_SIZE = 64
BG_TOLERANCE = 30  # Допуск цвета фона (0-255) для flood-fill


def remove_background(img, tolerance=BG_TOLERANCE):
    """
    Удалить фон через flood-fill от углов.
    Все подключённые к углам пиксели с цветом ≈ угловому → alpha=0.
    """
    rgba = img.convert('RGBA')
    pixels = rgba.load()
    w, h = rgba.size
    visited = [[False] * w for _ in range(h)]
    queue = deque()

    # Стартовые точки — 4 угла + пиксели по краям
    for x, y in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
        queue.append((x, y))

    # Также добавляем пиксели по краям (для сложных фонов)
    step = max(1, w // 16)
    for x in range(0, w, step):
        queue.append((x, 0))
        queue.append((x, h - 1))
    for y in range(0, h, step):
        queue.append((0, y))
        queue.append((w - 1, y))

    # Цвет фона = средний цвет угловых пикселей
    corners = [
        pixels[0, 0][:3],
        pixels[w - 1, 0][:3],
        pixels[0, h - 1][:3],
        pixels[w - 1, h - 1][:3]
    ]
    bg_r = sum(c[0] for c in corners) / 4
    bg_g = sum(c[1] for c in corners) / 4
    bg_b = sum(c[2] for c in corners) / 4

    filled = 0
    while queue:
        x, y = queue.popleft()
        if x < 0 or x >= w or y < 0 or y >= h:
            continue
        if visited[y][x]:
            continue
        visited[y][x] = True

        r, g, b, a = pixels[x, y]
        if abs(r - bg_r) <= tolerance and abs(g - bg_g) <= tolerance and abs(b - bg_b) <= tolerance:
            pixels[x, y] = (r, g, b, 0)  # Прозрачный
            filled += 1
            queue.append((x + 1, y))
            queue.append((x - 1, y))
            queue.append((x, y + 1))
            queue.append((x, y - 1))

    print(f"    Flood-fill: удалено {filled} пикселей фона")
    return rgba


def process_sprite(filename, ai_dir, output_dir):
    """Обработать один AI-спрайт."""
    name = os.path.splitext(filename)[0]
    is_object = name.startswith("obj_")

    input_path = os.path.join(ai_dir, filename)
    output_path = os.path.join(output_dir, filename)

    print(f"  Обработка: {filename} (object={is_object})")

    # Загрузка
    img = Image.open(input_path)
    original_mode = img.mode
    original_size = img.size
    print(f"    Исходный: {original_size[0]}×{original_size[1]}, mode={original_mode}")

    # Для obj_*: удалить фон (конвертируем в RGBA, flood-fill)
    if is_object:
        img = remove_background(img)

    # Уменьшение до TARGET_SIZE×TARGET_SIZE
    if img.size[0] != TARGET_SIZE or img.size[1] != TARGET_SIZE:
        # Конвертируем в RGBA если ещё нет (для obj_* уже RGBA, для terrain — RGB)
        if img.mode != 'RGBA':
            img = img.convert('RGBA')

        img = img.resize((TARGET_SIZE, TARGET_SIZE), Image.LANCZOS)
        print(f"    Уменьшено: {TARGET_SIZE}×{TARGET_SIZE}")

    # Сохранение
    img.save(output_path, 'PNG')
    print(f"    Сохранено: {output_path}")

    return True


def main():
    print("=" * 60)
    print("process_ai_sprites.py — Обработка AI-спрайтов")
    print("=" * 60)

    if not os.path.exists(AI_DIR):
        print(f"ОШИБКА: Папка AI-спрайтов не найдена: {AI_DIR}")
        sys.exit(1)

    if not os.path.exists(OUTPUT_DIR):
        os.makedirs(OUTPUT_DIR)
        print(f"Создана папка вывода: {OUTPUT_DIR}")

    # Получаем список PNG файлов
    ai_files = [f for f in os.listdir(AI_DIR) if f.lower().endswith('.png')]
    ai_files.sort()

    if not ai_files:
        print("AI-спрайты не найдены в Tiles_AI/")
        sys.exit(0)

    print(f"Найдено {len(ai_files)} AI-спрайтов\n")

    processed = 0
    failed = 0

    for filename in ai_files:
        try:
            if process_sprite(filename, AI_DIR, OUTPUT_DIR):
                processed += 1
            else:
                failed += 1
        except Exception as e:
            print(f"  ОШИБКА: {filename}: {e}")
            failed += 1

    print(f"\n{'=' * 60}")
    print(f"Готово! Обработано: {processed}, Ошибок: {failed}")
    print(f"Результат: {OUTPUT_DIR}")
    print(f"{'=' * 60}")

    if failed > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
