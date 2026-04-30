#!/usr/bin/env python3
"""
Generate 6 NPC character sprite PNGs for a 2D xianxia cultivation game.
64x64 pixels, RGBA, transparent background, top-down chibi pixel-art style.
"""

from PIL import Image, ImageDraw
import os

OUT_DIR = "/home/z/my-project/UnityProject/Assets/Sprites/Characters/NPC"
os.makedirs(OUT_DIR, exist_ok=True)


def new_sprite():
    """Create a new 64x64 RGBA image with transparent background."""
    return Image.new("RGBA", (64, 64), (0, 0, 0, 0))


def put_pixel(img, x, y, color):
    """Safely put a pixel on the image."""
    if 0 <= x < 64 and 0 <= y < 64:
        img.putpixel((x, y), color)


def fill_rect(img, x1, y1, x2, y2, color):
    """Fill a rectangle with a color."""
    for y in range(max(0, y1), min(64, y2 + 1)):
        for x in range(max(0, x1), min(64, x2 + 1)):
            img.putpixel((x, y), color)


def fill_circle(img, cx, cy, r, color):
    """Fill a circle (approximate)."""
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                put_pixel(img, x, y, color)


def draw_oval(img, cx, cy, rx, ry, color):
    """Fill an ellipse."""
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if rx > 0 and ry > 0:
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                    put_pixel(img, x, y, color)


def blend_color(c1, c2, t):
    """Linearly interpolate between two RGBA colors."""
    return tuple(int(a + (b - a) * t) for a, b in zip(c1, c2))


# ────────────────────────────────────────────────────────────────
# 1. npc_monster_wild  — Red/orange wild beast, glowing eyes, aggressive
# ────────────────────────────────────────────────────────────────
def draw_monster_wild():
    img = new_sprite()
    # Colors
    body_dark   = (139, 30, 10, 255)
    body_mid    = (200, 60, 20, 255)
    body_light  = (240, 100, 30, 255)
    belly_color = (255, 160, 60, 255)
    eye_glow    = (255, 255, 100, 255)
    eye_core    = (255, 50, 0, 255)
    claw_color  = (220, 200, 160, 255)
    horn_color  = (80, 20, 10, 255)
    fang_color  = (240, 240, 220, 255)
    tail_tip    = (255, 120, 20, 255)

    # === BODY (hunched quadruped) - main torso ===
    # Large body mass centered
    fill_rect(img, 18, 28, 46, 48, body_mid)
    fill_rect(img, 16, 30, 48, 46, body_mid)
    fill_rect(img, 14, 32, 50, 44, body_mid)
    # Darker top/back
    fill_rect(img, 16, 28, 48, 34, body_dark)
    fill_rect(img, 14, 30, 50, 34, body_dark)
    # Lighter belly
    fill_rect(img, 22, 40, 42, 46, belly_color)
    fill_rect(img, 20, 38, 44, 42, belly_color)

    # === HEAD (large, aggressive, slightly left-facing) ===
    fill_rect(img, 8, 16, 30, 34, body_mid)
    fill_rect(img, 6, 18, 32, 32, body_mid)
    fill_rect(img, 10, 14, 28, 18, body_dark)  # top of head
    # Snout/jaw
    fill_rect(img, 4, 22, 14, 30, body_light)
    fill_rect(img, 2, 24, 10, 28, body_light)

    # === HORNS ===
    # Left horn
    fill_rect(img, 10, 8, 13, 16, horn_color)
    put_pixel(img, 9, 10, horn_color)
    put_pixel(img, 14, 12, horn_color)
    # Right horn
    fill_rect(img, 22, 8, 25, 16, horn_color)
    put_pixel(img, 26, 10, horn_color)
    put_pixel(img, 21, 12, horn_color)
    # Horn tips
    put_pixel(img, 11, 7, (60, 10, 5, 255))
    put_pixel(img, 23, 7, (60, 10, 5, 255))

    # === EYES (glowing) ===
    # Left eye
    fill_rect(img, 12, 20, 16, 24, eye_glow)
    put_pixel(img, 13, 21, eye_core)
    put_pixel(img, 14, 22, eye_core)
    # Right eye
    fill_rect(img, 20, 20, 24, 24, eye_glow)
    put_pixel(img, 21, 21, eye_core)
    put_pixel(img, 22, 22, eye_core)
    # Eye glow aura
    for dx, dy in [(-1,0),(1,0),(0,-1),(0,1)]:
        put_pixel(img, 14+dx, 22+dy, (255, 255, 100, 120))
        put_pixel(img, 22+dx, 22+dy, (255, 255, 100, 120))

    # === FANGS ===
    put_pixel(img, 6, 26, fang_color)
    put_pixel(img, 8, 28, fang_color)
    put_pixel(img, 10, 26, fang_color)

    # === FRONT LEGS ===
    fill_rect(img, 10, 46, 14, 56, body_dark)
    fill_rect(img, 8, 54, 14, 58, claw_color)
    fill_rect(img, 24, 46, 28, 56, body_dark)
    fill_rect(img, 22, 54, 28, 58, claw_color)

    # === HIND LEGS ===
    fill_rect(img, 38, 46, 42, 56, body_dark)
    fill_rect(img, 36, 54, 42, 58, claw_color)
    fill_rect(img, 46, 44, 50, 54, body_dark)
    fill_rect(img, 44, 52, 50, 56, claw_color)

    # === TAIL ===
    put_pixel(img, 50, 38, body_mid)
    fill_rect(img, 51, 36, 54, 40, body_dark)
    fill_rect(img, 54, 34, 57, 38, tail_tip)
    fill_rect(img, 56, 32, 59, 36, tail_tip)
    put_pixel(img, 58, 31, (255, 180, 40, 255))

    # === SPIKES on back ===
    for sx in [20, 26, 32, 38]:
        put_pixel(img, sx, 27, horn_color)
        put_pixel(img, sx, 28, horn_color)

    return img


# ────────────────────────────────────────────────────────────────
# 2. npc_passerby  — Grey/brown simple robes, calm traveler
# ────────────────────────────────────────────────────────────────
def draw_passerby():
    img = new_sprite()
    # Colors
    robe_dark   = (90, 75, 60, 255)
    robe_mid    = (120, 100, 80, 255)
    robe_light  = (150, 130, 110, 255)
    skin        = (230, 195, 160, 255)
    skin_shadow = (200, 165, 130, 255)
    hair_color  = (60, 45, 30, 255)
    hat_color   = (100, 80, 55, 255)
    hat_band    = (140, 110, 70, 255)
    sash_color  = (130, 100, 60, 255)
    boot_color  = (70, 50, 35, 255)
    staff_color = (110, 85, 50, 255)

    # === ROBE BODY ===
    fill_rect(img, 22, 30, 42, 54, robe_mid)
    fill_rect(img, 20, 32, 44, 52, robe_mid)
    fill_rect(img, 18, 36, 46, 50, robe_mid)
    # Darker edges
    fill_rect(img, 18, 36, 20, 50, robe_dark)
    fill_rect(img, 44, 36, 46, 50, robe_dark)
    # Lighter center
    fill_rect(img, 26, 34, 38, 48, robe_light)
    # Sash/belt
    fill_rect(img, 20, 40, 44, 42, sash_color)

    # === HEAD ===
    fill_rect(img, 24, 16, 40, 30, skin)
    fill_rect(img, 22, 18, 42, 28, skin)
    # Face shadow
    fill_rect(img, 24, 24, 40, 28, skin_shadow)

    # === EYES (small, calm) ===
    put_pixel(img, 28, 23, (40, 30, 20, 255))
    put_pixel(img, 35, 23, (40, 30, 20, 255))

    # === CONICAL HAT ===
    fill_rect(img, 20, 10, 44, 18, hat_color)
    fill_rect(img, 22, 8, 42, 12, hat_color)
    fill_rect(img, 26, 6, 38, 10, hat_color)
    fill_rect(img, 30, 4, 34, 8, hat_color)
    put_pixel(img, 32, 3, hat_color)
    # Hat band
    fill_rect(img, 20, 14, 44, 16, hat_band)

    # === HAIR (peeking below hat) ===
    fill_rect(img, 22, 16, 24, 22, hair_color)
    fill_rect(img, 40, 16, 42, 22, hair_color)

    # === SLEEVES ===
    fill_rect(img, 14, 32, 22, 40, robe_mid)
    fill_rect(img, 14, 34, 20, 38, robe_light)
    fill_rect(img, 42, 32, 50, 40, robe_mid)
    fill_rect(img, 44, 34, 50, 38, robe_light)

    # === HANDS ===
    fill_rect(img, 12, 38, 16, 42, skin)
    fill_rect(img, 48, 38, 52, 42, skin)

    # === FEET/BOOTS ===
    fill_rect(img, 24, 54, 30, 58, boot_color)
    fill_rect(img, 34, 54, 40, 58, boot_color)

    # === WALKING STAFF (held in right hand) ===
    fill_rect(img, 50, 28, 52, 58, staff_color)
    put_pixel(img, 49, 27, staff_color)
    put_pixel(img, 53, 27, staff_color)
    put_pixel(img, 51, 26, (130, 100, 60, 255))

    # === SMALL BUNDLE on back ===
    fill_rect(img, 42, 26, 48, 34, robe_dark)
    put_pixel(img, 43, 25, robe_dark)
    put_pixel(img, 47, 25, robe_dark)

    return img


# ────────────────────────────────────────────────────────────────
# 3. npc_merchant_female  — Gold/green robes, money pouch, friendly
# ────────────────────────────────────────────────────────────────
def draw_merchant_female():
    img = new_sprite()
    # Colors
    robe_main   = (50, 140, 70, 255)    # green
    robe_dark   = (35, 110, 55, 255)
    robe_light  = (70, 170, 90, 255)
    gold_trim   = (230, 190, 50, 255)
    gold_bright = (255, 220, 80, 255)
    skin        = (240, 200, 165, 255)
    skin_shadow = (210, 170, 135, 255)
    hair_color  = (30, 20, 40, 255)     # dark hair
    pouch_color = (160, 120, 40, 255)
    eye_color   = (50, 35, 20, 255)
    lip_color   = (200, 100, 90, 255)

    # === ROBE BODY ===
    fill_rect(img, 20, 30, 44, 56, robe_main)
    fill_rect(img, 18, 34, 46, 54, robe_main)
    fill_rect(img, 16, 38, 48, 52, robe_main)
    # Darker edges
    fill_rect(img, 16, 38, 18, 52, robe_dark)
    fill_rect(img, 46, 38, 48, 52, robe_dark)
    # Gold trim on robe
    fill_rect(img, 20, 30, 44, 32, gold_trim)
    fill_rect(img, 18, 44, 46, 46, gold_trim)
    # Light center
    fill_rect(img, 26, 34, 38, 44, robe_light)
    # Collar V-shape
    for i in range(6):
        fill_rect(img, 30-i, 30+i, 34+i, 30+i, gold_trim)

    # === HEAD ===
    fill_rect(img, 24, 14, 40, 30, skin)
    fill_rect(img, 22, 16, 42, 28, skin)
    # Face shadow
    fill_rect(img, 24, 24, 40, 28, skin_shadow)

    # === EYES (friendly, slightly larger) ===
    put_pixel(img, 28, 22, (255, 255, 255, 255))
    put_pixel(img, 29, 22, eye_color)
    put_pixel(img, 35, 22, (255, 255, 255, 255))
    put_pixel(img, 36, 22, eye_color)

    # === SMILE ===
    put_pixel(img, 30, 26, lip_color)
    put_pixel(img, 31, 27, lip_color)
    put_pixel(img, 32, 27, lip_color)
    put_pixel(img, 33, 26, lip_color)

    # === LONG HAIR ===
    fill_rect(img, 20, 14, 24, 30, hair_color)
    fill_rect(img, 40, 14, 44, 30, hair_color)
    fill_rect(img, 22, 12, 42, 16, hair_color)
    # Hair flowing down sides
    fill_rect(img, 18, 16, 22, 38, hair_color)
    fill_rect(img, 42, 16, 46, 38, hair_color)
    # Hair ornament (gold)
    put_pixel(img, 42, 18, gold_bright)
    put_pixel(img, 43, 17, gold_bright)
    put_pixel(img, 44, 16, gold_bright)

    # === SLEEVES (wide) ===
    fill_rect(img, 10, 32, 20, 42, robe_main)
    fill_rect(img, 10, 34, 18, 40, robe_light)
    fill_rect(img, 44, 32, 54, 42, robe_main)
    fill_rect(img, 46, 34, 54, 40, robe_light)
    # Gold trim on sleeves
    fill_rect(img, 10, 40, 20, 42, gold_trim)
    fill_rect(img, 44, 40, 54, 42, gold_trim)

    # === HANDS ===
    fill_rect(img, 8, 40, 12, 44, skin)
    fill_rect(img, 52, 40, 56, 44, skin)

    # === MONEY POUCH (at belt) ===
    fill_rect(img, 38, 42, 44, 48, pouch_color)
    fill_rect(img, 37, 43, 45, 47, pouch_color)
    put_pixel(img, 40, 41, gold_bright)  # tie
    put_pixel(img, 41, 41, gold_bright)
    # Coin symbol on pouch
    put_pixel(img, 40, 44, gold_trim)
    put_pixel(img, 41, 44, gold_trim)
    put_pixel(img, 40, 45, gold_trim)
    put_pixel(img, 41, 45, gold_trim)

    # === FEET ===
    fill_rect(img, 24, 54, 30, 58, (80, 60, 40, 255))
    fill_rect(img, 34, 54, 40, 58, (80, 60, 40, 255))

    return img


# ────────────────────────────────────────────────────────────────
# 4. npc_guard_female  — Blue/steel armor, spear, ponytail
# ────────────────────────────────────────────────────────────────
def draw_guard_female():
    img = new_sprite()
    # Colors
    armor_main  = (70, 110, 170, 255)   # steel blue
    armor_dark  = (50, 80, 140, 255)
    armor_light = (110, 150, 200, 255)
    armor_shine = (160, 190, 220, 255)
    cloth_color = (40, 50, 80, 255)      # dark blue under-armor
    skin        = (235, 200, 165, 255)
    skin_shadow = (205, 170, 135, 255)
    hair_color  = (40, 25, 20, 255)      # dark brown
    spear_wood  = (120, 90, 50, 255)
    spear_tip   = (190, 200, 210, 255)
    eye_color   = (30, 40, 60, 255)
    red_accent  = (180, 40, 40, 255)     # red trim/cord

    # === ARMOR BODY ===
    fill_rect(img, 22, 28, 42, 52, armor_main)
    fill_rect(img, 20, 30, 44, 50, armor_main)
    fill_rect(img, 18, 34, 46, 48, armor_main)
    # Shoulder plates
    fill_rect(img, 14, 28, 22, 34, armor_light)
    fill_rect(img, 14, 28, 18, 32, armor_shine)
    fill_rect(img, 42, 28, 50, 34, armor_light)
    fill_rect(img, 46, 28, 50, 32, armor_shine)
    # Chest plate detail
    fill_rect(img, 24, 30, 40, 38, armor_light)
    fill_rect(img, 28, 30, 36, 34, armor_shine)
    # Dark edges
    fill_rect(img, 18, 34, 20, 48, armor_dark)
    fill_rect(img, 44, 34, 46, 48, armor_dark)
    # Belt
    fill_rect(img, 20, 44, 44, 46, armor_dark)
    put_pixel(img, 31, 44, red_accent)
    put_pixel(img, 32, 44, red_accent)
    # Skirt/leg armor plates
    fill_rect(img, 20, 46, 28, 54, armor_dark)
    fill_rect(img, 36, 46, 44, 54, armor_dark)
    fill_rect(img, 22, 48, 26, 52, cloth_color)
    fill_rect(img, 38, 48, 42, 52, cloth_color)

    # === HEAD ===
    fill_rect(img, 24, 14, 40, 30, skin)
    fill_rect(img, 22, 16, 42, 28, skin)
    # Face shadow
    fill_rect(img, 24, 24, 40, 28, skin_shadow)

    # === EYES (alert) ===
    put_pixel(img, 28, 22, eye_color)
    put_pixel(img, 36, 22, eye_color)
    # Determined brows
    put_pixel(img, 27, 21, (60, 40, 30, 255))
    put_pixel(img, 37, 21, (60, 40, 30, 255))

    # === HAIR + PONYTAIL ===
    fill_rect(img, 22, 12, 42, 18, hair_color)
    fill_rect(img, 20, 14, 24, 24, hair_color)
    fill_rect(img, 40, 14, 44, 24, hair_color)
    # Ponytail flowing right and down
    fill_rect(img, 44, 16, 48, 36, hair_color)
    fill_rect(img, 46, 18, 50, 34, hair_color)
    fill_rect(img, 48, 22, 52, 30, hair_color)
    # Red hair cord
    put_pixel(img, 44, 16, red_accent)
    put_pixel(img, 45, 16, red_accent)
    put_pixel(img, 44, 17, red_accent)

    # === ARM GUARDS + HANDS ===
    fill_rect(img, 12, 34, 18, 46, armor_light)
    fill_rect(img, 46, 34, 52, 46, armor_light)
    fill_rect(img, 10, 44, 14, 48, skin)
    fill_rect(img, 50, 44, 54, 48, skin)

    # === SPEAR (held in right hand) ===
    fill_rect(img, 52, 8, 54, 58, spear_wood)
    # Spear tip
    fill_rect(img, 52, 2, 54, 8, spear_tip)
    fill_rect(img, 51, 4, 55, 6, spear_tip)
    put_pixel(img, 53, 1, spear_tip)
    # Spear butt
    put_pixel(img, 53, 58, (80, 60, 30, 255))

    # === BOOTS ===
    fill_rect(img, 22, 54, 28, 58, (50, 50, 60, 255))
    fill_rect(img, 36, 54, 42, 58, (50, 50, 60, 255))

    return img


# ────────────────────────────────────────────────────────────────
# 5. npc_cultivator_master  — Purple/cyan robes, glowing aura, ribbons
# ────────────────────────────────────────────────────────────────
def draw_cultivator_master():
    img = new_sprite()
    # Colors
    robe_main   = (90, 50, 150, 255)    # purple
    robe_dark   = (65, 35, 120, 255)
    robe_light  = (120, 75, 180, 255)
    cyan_glow   = (80, 220, 240, 255)
    cyan_light  = (140, 240, 255, 255)
    aura_color1 = (120, 200, 255, 50)
    aura_color2 = (160, 100, 255, 40)
    aura_color3 = (200, 220, 255, 30)
    skin        = (230, 200, 170, 255)
    skin_shadow = (200, 170, 140, 255)
    hair_color  = (200, 210, 230, 255)   # white/silver hair
    ribbon_color = (100, 230, 250, 255)
    eye_color   = (120, 200, 255, 255)   # glowing cyan eyes
    gold_detail = (220, 190, 60, 255)

    # === GLOWING AURA (drawn first, behind character) ===
    for r in range(30, 8, -2):
        alpha = max(5, 30 - r)
        aura_c = (140, 180, 255, alpha)
        draw_oval(img, 32, 36, r, r + 4, aura_c)
    # Inner glow
    for r in range(20, 6, -2):
        alpha = max(10, 40 - r * 2)
        draw_oval(img, 32, 36, r, r + 2, (180, 200, 255, alpha))

    # === ROBE BODY ===
    fill_rect(img, 20, 28, 44, 58, robe_main)
    fill_rect(img, 18, 32, 46, 56, robe_main)
    fill_rect(img, 16, 36, 48, 54, robe_main)
    # Darker edges
    fill_rect(img, 16, 36, 18, 54, robe_dark)
    fill_rect(img, 46, 36, 48, 54, robe_dark)
    # Inner light
    fill_rect(img, 26, 32, 38, 50, robe_light)
    # Cyan mystical symbols/trim
    fill_rect(img, 20, 28, 44, 30, cyan_glow)
    fill_rect(img, 18, 42, 46, 44, cyan_glow)
    # Gold spiritual symbols
    put_pixel(img, 30, 34, gold_detail)
    put_pixel(img, 34, 34, gold_detail)
    put_pixel(img, 32, 36, gold_detail)
    put_pixel(img, 30, 38, gold_detail)
    put_pixel(img, 34, 38, gold_detail)

    # === HEAD ===
    fill_rect(img, 24, 12, 40, 28, skin)
    fill_rect(img, 22, 14, 42, 26, skin)
    fill_rect(img, 24, 22, 40, 26, skin_shadow)

    # === EYES (glowing cyan) ===
    put_pixel(img, 28, 20, eye_color)
    put_pixel(img, 36, 20, eye_color)
    # Eye glow
    put_pixel(img, 27, 19, (80, 180, 230, 120))
    put_pixel(img, 29, 19, (80, 180, 230, 120))
    put_pixel(img, 35, 19, (80, 180, 230, 120))
    put_pixel(img, 37, 19, (80, 180, 230, 120))

    # === SILVER/WHITE HAIR (long, flowing upward - cultivation energy) ===
    fill_rect(img, 20, 8, 44, 16, hair_color)
    fill_rect(img, 18, 10, 24, 22, hair_color)
    fill_rect(img, 40, 10, 46, 22, hair_color)
    # Hair floating upward (cultivation energy)
    fill_rect(img, 22, 4, 28, 10, (210, 220, 240, 200))
    fill_rect(img, 36, 4, 42, 10, (210, 220, 240, 200))
    fill_rect(img, 26, 2, 32, 8, (220, 230, 245, 180))
    fill_rect(img, 32, 2, 38, 8, (220, 230, 245, 180))
    put_pixel(img, 29, 0, (230, 240, 255, 150))
    put_pixel(img, 35, 0, (230, 240, 255, 150))

    # === FLOATING RIBBONS ===
    # Left ribbon
    for i in range(10):
        rx = 14 - i
        ry = 24 + i * 2
        put_pixel(img, rx, ry, ribbon_color)
        put_pixel(img, rx + 1, ry, cyan_light)
    # Right ribbon
    for i in range(10):
        rx = 48 + i
        ry = 24 + i * 2
        put_pixel(img, rx, ry, ribbon_color)
        put_pixel(img, rx - 1, ry, cyan_light)
    # Ribbon glow
    for i in range(0, 10, 2):
        rx_l = 14 - i
        ry = 24 + i * 2
        put_pixel(img, rx_l, ry - 1, (100, 220, 240, 80))
        rx_r = 48 + i
        put_pixel(img, rx_r, ry - 1, (100, 220, 240, 80))

    # === WIDE SLEEVES ===
    fill_rect(img, 8, 30, 20, 42, robe_main)
    fill_rect(img, 8, 32, 18, 40, robe_light)
    fill_rect(img, 44, 30, 56, 42, robe_main)
    fill_rect(img, 46, 32, 56, 40, robe_light)
    # Cyan trim on sleeves
    fill_rect(img, 8, 40, 20, 42, cyan_glow)
    fill_rect(img, 44, 40, 56, 42, cyan_glow)

    # === HANDS (palms together, meditative) ===
    fill_rect(img, 28, 42, 36, 46, skin)
    fill_rect(img, 30, 40, 34, 44, skin)

    # === FLOATING FEET ===
    fill_rect(img, 24, 56, 30, 60, robe_dark)
    fill_rect(img, 34, 56, 40, 60, robe_dark)
    # Glow beneath feet
    for x in range(22, 42):
        put_pixel(img, x, 61, (140, 200, 255, 40))
    for x in range(24, 40):
        put_pixel(img, x, 62, (140, 200, 255, 25))

    # === EXTRA SPARKLE PARTICLES ===
    sparkle_positions = [
        (10, 20), (54, 18), (8, 44), (56, 42),
        (6, 12), (58, 10), (12, 56), (52, 54),
        (4, 32), (60, 30),
    ]
    for sx, sy in sparkle_positions:
        put_pixel(img, sx, sy, cyan_light)

    return img


# ────────────────────────────────────────────────────────────────
# 6. npc_villager_female  — Brown/green dress, basket, gentle
# ────────────────────────────────────────────────────────────────
def draw_villager_female():
    img = new_sprite()
    # Colors
    dress_main  = (140, 110, 70, 255)    # brown
    dress_dark  = (110, 85, 55, 255)
    dress_light = (170, 140, 100, 255)
    apron_color = (100, 140, 70, 255)    # green apron
    apron_light = (130, 170, 90, 255)
    skin        = (240, 205, 170, 255)
    skin_shadow = (210, 175, 140, 255)
    hair_color  = (70, 45, 25, 255)      # brown hair
    eye_color   = (50, 35, 20, 255)
    basket_color = (160, 120, 50, 255)
    basket_rim  = (130, 90, 30, 255)
    herb_color  = (60, 160, 60, 255)
    flower_color = (220, 120, 160, 255)

    # === DRESS BODY ===
    fill_rect(img, 22, 30, 42, 56, dress_main)
    fill_rect(img, 20, 34, 44, 54, dress_main)
    fill_rect(img, 18, 38, 46, 52, dress_main)
    fill_rect(img, 16, 44, 48, 52, dress_main)
    # Darker edges
    fill_rect(img, 16, 44, 18, 52, dress_dark)
    fill_rect(img, 46, 44, 48, 52, dress_dark)
    # Light center
    fill_rect(img, 26, 34, 38, 48, dress_light)
    # Green apron
    fill_rect(img, 24, 36, 40, 52, apron_color)
    fill_rect(img, 26, 38, 38, 50, apron_light)
    # Apron strings
    put_pixel(img, 24, 36, apron_color)
    put_pixel(img, 40, 36, apron_color)

    # === HEAD ===
    fill_rect(img, 24, 14, 40, 30, skin)
    fill_rect(img, 22, 16, 42, 28, skin)
    fill_rect(img, 24, 24, 40, 28, skin_shadow)

    # === EYES (gentle) ===
    put_pixel(img, 28, 22, eye_color)
    put_pixel(img, 36, 22, eye_color)
    # Gentle eyebrows
    put_pixel(img, 27, 20, (100, 70, 40, 255))
    put_pixel(img, 37, 20, (100, 70, 40, 255))

    # === HAIR (brown, with simple bun) ===
    fill_rect(img, 22, 12, 42, 18, hair_color)
    fill_rect(img, 20, 14, 24, 26, hair_color)
    fill_rect(img, 40, 14, 44, 26, hair_color)
    # Hair bun on top
    fill_rect(img, 28, 6, 36, 14, hair_color)
    fill_rect(img, 26, 8, 38, 12, hair_color)
    # Simple hair pin
    put_pixel(img, 36, 8, flower_color)
    put_pixel(img, 37, 7, (200, 100, 140, 255))
    put_pixel(img, 35, 7, (200, 100, 140, 255))

    # === SLEEVES ===
    fill_rect(img, 14, 32, 22, 40, dress_main)
    fill_rect(img, 14, 34, 20, 38, dress_light)
    fill_rect(img, 42, 32, 50, 40, dress_main)
    fill_rect(img, 44, 34, 50, 38, dress_light)

    # === HANDS ===
    fill_rect(img, 12, 38, 16, 42, skin)
    fill_rect(img, 48, 38, 52, 42, skin)

    # === BASKET (held in left hand) ===
    fill_rect(img, 4, 36, 14, 44, basket_color)
    fill_rect(img, 6, 38, 12, 42, basket_rim)
    # Basket rim
    fill_rect(img, 3, 35, 15, 37, basket_rim)
    # Herbs/flowers in basket
    put_pixel(img, 7, 34, herb_color)
    put_pixel(img, 9, 33, herb_color)
    put_pixel(img, 11, 34, herb_color)
    put_pixel(img, 8, 32, flower_color)
    put_pixel(img, 10, 32, (255, 200, 50, 255))
    put_pixel(img, 12, 33, herb_color)
    # Basket handle
    put_pixel(img, 6, 33, basket_rim)
    put_pixel(img, 12, 33, basket_rim)
    put_pixel(img, 7, 32, basket_rim)
    put_pixel(img, 11, 32, basket_rim)

    # === FEET ===
    fill_rect(img, 24, 54, 30, 58, (90, 65, 40, 255))
    fill_rect(img, 34, 54, 40, 58, (90, 65, 40, 255))

    # === DRESS HEM DETAIL ===
    fill_rect(img, 18, 52, 48, 54, dress_dark)

    return img


# ────────────────────────────────────────────────────────────────
# Generate all sprites
# ────────────────────────────────────────────────────────────────
sprites = {
    "npc_monster_wild.png": draw_monster_wild,
    "npc_passerby.png": draw_passerby,
    "npc_merchant_female.png": draw_merchant_female,
    "npc_guard_female.png": draw_guard_female,
    "npc_cultivator_master.png": draw_cultivator_master,
    "npc_villager_female.png": draw_villager_female,
}

for filename, draw_func in sprites.items():
    path = os.path.join(OUT_DIR, filename)
    img = draw_func()
    img.save(path, "PNG")
    print(f"  Saved: {path}  ({img.size[0]}x{img.size[1]}, {img.mode})")

print("\nAll 6 NPC sprites generated successfully!")
