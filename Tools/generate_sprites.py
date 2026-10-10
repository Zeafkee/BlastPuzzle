import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

REPO = Path(__file__).resolve().parents[1]
ROOT = REPO / "Assets" / "_Project" / "Art" / "Sprites"
SS = 4
TILE = 128

COLORS = [
    ("Red",    (235, 64, 60)),
    ("Yellow", (255, 188, 28)),
    ("Blue",   (44, 146, 255)),
    ("Green",  (76, 196, 64)),
    ("Purple", (160, 84, 232)),
    ("Pink",   (255, 104, 178)),
]
TIER_NAMES = ["Default", "Rocket", "Bomb", "Disco"]
WHITE = (255, 255, 255)


def shade(rgb, k):
    if k <= 1:
        return tuple(max(0, int(c * k)) for c in rgb)
    t = min(1.0, k - 1)
    return tuple(int(c + (255 - c) * t) for c in rgb)


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def canvas(w, h=None):
    h = h or w
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def finish(img, w, h=None):
    return img.resize((w, h or w), Image.LANCZOS)


def mask_rrect(size, box, r):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle([int(v) for v in box], radius=int(r), fill=255)
    return m


def mask_ellipse(size, box):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).ellipse([int(v) for v in box], fill=255)
    return m


def vgradient(size, stops):
    w, h = size
    col = Image.new("RGB", (1, h))
    px = col.load()
    for y in range(h):
        t = y / max(1, h - 1)
        for i in range(len(stops) - 1):
            t0, c0 = stops[i]
            t1, c1 = stops[i + 1]
            if t0 <= t <= t1:
                px[0, y] = mix(c0, c1, (t - t0) / max(1e-6, t1 - t0))
                break
        else:
            px[0, y] = stops[-1][1]
    return col.resize((w, h)).convert("RGBA")


def paste_masked(dst, src, mask):
    dst.paste(src, (0, 0), mask)


def fill(dst, mask, rgb, alpha=255):
    layer = Image.new("RGBA", dst.size, rgb + (alpha,))
    blank = Image.new("RGBA", dst.size, (0, 0, 0, 0))
    dst.alpha_composite(Image.composite(layer, blank, mask))


def soft_shadow(dst, mask, offset=(0, 0), blur=8, alpha=110, rgb=(0, 0, 0)):
    sh = Image.new("RGBA", dst.size, (0, 0, 0, 0))
    a = mask.point(lambda v: int(v * alpha / 255))
    sh.paste(Image.new("RGBA", dst.size, rgb + (255,)), (int(offset[0]), int(offset[1])), a)
    dst.alpha_composite(sh.filter(ImageFilter.GaussianBlur(blur)))


def gloss(dst, box, r, alpha=70, blur=4, clip=None):
    g = Image.new("RGBA", dst.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).rounded_rectangle([int(v) for v in box], radius=int(r), fill=(255, 255, 255, alpha))
    g = g.filter(ImageFilter.GaussianBlur(blur))
    if clip is not None:
        g.putalpha(ImageChops.multiply(g.split()[3], clip))
    dst.alpha_composite(g)


def star_points(cx, cy, r_out, r_in, points=5, rot=-90):
    pts = []
    for i in range(points * 2):
        rad = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180 / points)
        pts.append((cx + rad * math.cos(a), cy + rad * math.sin(a)))
    return pts


def rounded_poly_mask(size, pts, round_px):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    if round_px > 0:
        m = m.filter(ImageFilter.GaussianBlur(round_px)).point(lambda v: 255 if v > 110 else 0)
        m = m.filter(ImageFilter.GaussianBlur(SS * 0.6))
    return m


def glyph_mask(name, size, cx, cy, s):
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    if name == "heart":
        r = s * 0.54
        d.ellipse([cx - s, cy - s * 0.78, cx - s + 2 * r, cy - s * 0.78 + 2 * r], fill=255)
        d.ellipse([cx + s - 2 * r, cy - s * 0.78, cx + s, cy - s * 0.78 + 2 * r], fill=255)
        d.polygon([(cx - s * 0.96, cy - s * 0.12), (cx + s * 0.96, cy - s * 0.12), (cx, cy + s * 0.98)], fill=255)
    elif name == "star":
        return rounded_poly_mask(size, star_points(cx, cy + s * 0.05, s * 1.08, s * 0.5), s * 0.07)
    elif name == "diamond":
        return rounded_poly_mask(size, [(cx, cy - s * 1.05), (cx + s * 0.8, cy), (cx, cy + s * 1.05), (cx - s * 0.8, cy)], s * 0.08)
    elif name == "clover":
        r = s * 0.46
        for ox, oy in [(0, -1), (1, 0), (0, 1), (-1, 0)]:
            x, y = cx + ox * s * 0.5, cy + oy * s * 0.5
            d.ellipse([x - r, y - r, x + r, y + r], fill=255)
        d.ellipse([cx - r * 0.6, cy - r * 0.6, cx + r * 0.6, cy + r * 0.6], fill=255)
    elif name == "moon":
        d.ellipse([cx - s, cy - s, cx + s, cy + s], fill=255)
        d.ellipse([cx - s * 0.3, cy - s * 1.1, cx + s * 1.3, cy + s * 0.6], fill=0)
    elif name == "triangle":
        return rounded_poly_mask(size, [(cx, cy - s), (cx + s, cy + s * 0.78), (cx - s, cy + s * 0.78)], s * 0.1)
    elif name == "rocket":
        bar = s * 0.24
        d.rounded_rectangle([cx - s * 0.5, cy - bar, cx + s * 0.5, cy + bar], radius=bar * 0.4, fill=255)
        d.polygon([(cx - s * 1.08, cy), (cx - s * 0.42, cy - s * 0.6), (cx - s * 0.42, cy + s * 0.6)], fill=255)
        d.polygon([(cx + s * 1.08, cy), (cx + s * 0.42, cy - s * 0.6), (cx + s * 0.42, cy + s * 0.6)], fill=255)
    elif name == "bomb":
        r = s * 0.74
        by = cy + s * 0.22
        d.ellipse([cx - r, by - r, cx + r, by + r], fill=255)
        d.line([(cx + r * 0.45, by - r * 0.75), (cx + s * 0.7, cy - s * 0.85)], fill=255, width=int(s * 0.2))
        d.polygon(star_points(cx + s * 0.78, cy - s * 0.9, s * 0.34, s * 0.12, points=4, rot=0), fill=255)
    elif name == "disco":
        d.polygon(star_points(cx - s * 0.1, cy + s * 0.1, s * 1.0, s * 0.27, points=4), fill=255)
        d.polygon(star_points(cx + s * 0.66, cy - s * 0.66, s * 0.4, s * 0.12, points=4), fill=255)
    return m


COLOR_GLYPHS = ["heart", "star", "diamond", "clover", "moon", "triangle"]
TIER_GLYPHS = [None, "rocket", "bomb", "disco"]


def cube_base(rgb, size_px=TILE, lip_ratio=0.085, radius_ratio=0.22):
    s = size_px * SS
    img = canvas(size_px)
    m = s * 0.035
    r = s * radius_ratio
    lip = s * lip_ratio

    body = mask_rrect(img.size, [m, m, s - m, s - m], r)
    soft_shadow(img, body, offset=(0, s * 0.02), blur=s * 0.022, alpha=90)

    fill(img, body, shade(rgb, 0.56))
    top_box = [m, m, s - m, s - m - lip]
    top = mask_rrect(img.size, top_box, r)
    paste_masked(img, vgradient(img.size, [(0.0, shade(rgb, 1.22)), (0.45, rgb), (1.0, shade(rgb, 0.86))]), top)

    inner = mask_rrect(img.size, [m + s * 0.03, m + s * 0.035, s - m - s * 0.03, s - m - lip - s * 0.02], r * 0.86)
    rim = ImageChops.subtract(top, inner)
    rim_top = ImageChops.multiply(rim, vgradient(img.size, [(0, WHITE), (0.45, (0, 0, 0)), (1, (0, 0, 0))]).convert("L"))
    fill(img, rim_top, WHITE, 120)
    rim_bottom = ImageChops.multiply(rim, vgradient(img.size, [(0, (0, 0, 0)), (0.6, (0, 0, 0)), (1, WHITE)]).convert("L"))
    fill(img, rim_bottom, shade(rgb, 0.6), 110)

    gloss(img, [s * 0.17, s * 0.085, s * 0.83, s * 0.27], s * 0.09, alpha=62, blur=s * 0.012, clip=top)
    return img, top, top_box


def draw_block(rgb, color_index, tier):
    img, _, top_box = cube_base(rgb)
    s = img.size[0]
    cx = s / 2
    cy = (top_box[1] + top_box[3]) / 2 + s * 0.015
    gs = s * 0.235
    name = TIER_GLYPHS[tier] if tier > 0 else COLOR_GLYPHS[color_index]
    if tier > 0:
        gs *= 1.06

    shadow = glyph_mask(name, img.size, cx, cy + s * 0.022, gs)
    fill(img, shadow, shade(rgb, 0.5), 150)
    face = glyph_mask(name, img.size, cx, cy, gs)
    paste_masked(img, vgradient(img.size, [(0, WHITE), (0.35, WHITE), (0.75, shade(rgb, 1.78))]), face)
    return finish(img, TILE)


def sphere(img, cx, cy, r, rgb, hi=0.5):
    s = img.size[0]
    m = mask_ellipse(img.size, [cx - r, cy - r, cx + r, cy + r])
    soft_shadow(img, m, offset=(0, s * 0.03), blur=s * 0.025, alpha=100)
    fill(img, m, shade(rgb, 0.62))
    lit = mask_ellipse(img.size, [cx - r * 0.98, cy - r * 1.02, cx + r * 0.98, cy + r * 0.86])
    lit = ImageChops.multiply(lit, m)
    paste_masked(img, vgradient(img.size, [(0, shade(rgb, 1.0 + hi)), (0.5, rgb), (1, shade(rgb, 0.8))]), lit)
    return m


def draw_bomb():
    img = canvas(TILE)
    s = img.size[0]
    cx, cy, r = s * 0.48, s * 0.56, s * 0.34
    d = ImageDraw.Draw(img)
    d.line([(cx + r * 0.55, cy - r * 0.8), (cx + r * 0.95, cy - r * 1.25)], fill=(214, 168, 96, 255), width=int(s * 0.045))
    body = sphere(img, cx, cy, r, (58, 62, 92), hi=0.45)
    cap = mask_rrect(img.size, [cx + r * 0.28, cy - r * 1.02, cx + r * 0.82, cy - r * 0.62], s * 0.03)
    cap = cap.rotate(-38, center=(cx + r * 0.55, cy - r * 0.82), resample=Image.BICUBIC)
    fill(img, cap, (120, 126, 160))
    gl = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(gl).ellipse([cx - r * 0.62, cy - r * 0.72, cx - r * 0.05, cy - r * 0.2], fill=(255, 255, 255, 120))
    img.alpha_composite(gl.filter(ImageFilter.GaussianBlur(s * 0.018)))
    sx, sy = cx + r * 1.0, cy - r * 1.32
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse([sx - s * 0.1, sy - s * 0.1, sx + s * 0.1, sy + s * 0.1], fill=(255, 170, 40, 200))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(s * 0.03)))
    d = ImageDraw.Draw(img)
    d.polygon(star_points(sx, sy, s * 0.105, s * 0.04, points=6, rot=0), fill=(255, 226, 90, 255))
    d.polygon(star_points(sx, sy, s * 0.055, s * 0.025, points=6, rot=30), fill=(255, 255, 235, 255))
    star = rounded_poly_mask(img.size, star_points(cx + r * 0.05, cy + r * 0.12, r * 0.42, r * 0.2), s * 0.006)
    fill(img, star, (255, 255, 255), 215)
    return finish(img, TILE)


def draw_rocket():
    img = canvas(TILE)
    s = img.size[0]
    cy = s * 0.5
    half_h = s * 0.19
    x0, x1 = s * 0.2, s * 0.8

    body = mask_rrect(img.size, [x0, cy - half_h, x1, cy + half_h], s * 0.04)
    soft_shadow(img, body, offset=(0, s * 0.03), blur=s * 0.022, alpha=100)

    red = (236, 62, 68)
    for sign in (-1, 1):
        tip_x = s * 0.5 + sign * s * 0.47
        base_x = s * 0.5 + sign * s * 0.27
        cone = rounded_poly_mask(img.size, [(tip_x, cy), (base_x, cy - half_h * 1.12), (base_x, cy + half_h * 1.12)], s * 0.018)
        soft_shadow(img, cone, offset=(0, s * 0.03), blur=s * 0.02, alpha=80)
        paste_masked(img, vgradient(img.size, [(0, shade(red, 1.3)), (0.5, red), (1, shade(red, 0.62))]), cone)

    paste_masked(img, vgradient(img.size, [(0.25, (255, 255, 255)), (0.5, (232, 236, 248)), (0.72, (150, 160, 196))]), body)
    for fx in (0.36, 0.5, 0.64):
        stripe = mask_rrect(img.size, [s * fx - s * 0.03, cy - half_h, s * fx + s * 0.03, cy + half_h], 0)
        paste_masked(img, vgradient(img.size, [(0.25, (90, 190, 255)), (0.5, (44, 146, 255)), (0.72, (24, 86, 190))]), stripe)
    gloss(img, [x0 + s * 0.02, cy - half_h + s * 0.02, x1 - s * 0.02, cy - half_h * 0.35], s * 0.03, alpha=120, blur=s * 0.008, clip=body)
    return finish(img, TILE)


def draw_disco(rgb):
    img = canvas(TILE)
    s = img.size[0]
    cx, cy, r = s * 0.5, s * 0.52, s * 0.37
    ball = sphere(img, cx, cy, r, shade(rgb, 0.92), hi=0.35)

    rng = random.Random(hash(rgb) & 0xFFFF)
    facets = Image.new("RGBA", img.size, (0, 0, 0, 0))
    fd = ImageDraw.Draw(facets)
    rows = 7
    for i in range(rows):
        v0 = -1 + 2 * i / rows
        v1 = -1 + 2 * (i + 1) / rows
        y0, y1 = cy + v0 * r, cy + v1 * r
        half0 = math.sqrt(max(0, 1 - v0 * v0)) * r
        half1 = math.sqrt(max(0, 1 - v1 * v1)) * r
        cols = max(3, int(8 * math.sqrt(max(0.05, 1 - ((v0 + v1) / 2) ** 2))))
        for j in range(cols):
            u0 = -1 + 2 * j / cols
            u1 = -1 + 2 * (j + 1) / cols
            def bx(u, half):
                return cx + math.sin(u * math.pi / 2) * half
            poly = [(bx(u0, half0), y0), (bx(u1, half0), y0), (bx(u1, half1), y1), (bx(u0, half1), y1)]
            light = 1.0 - 0.5 * ((v0 + v1) / 2 + 1) / 2 - 0.25 * abs((u0 + u1) / 2)
            k = light * rng.uniform(0.75, 1.5) + 0.2
            c = shade(rgb, min(1.9, max(0.45, k)))
            fd.polygon(poly, fill=c + (255,), outline=shade(rgb, 0.42) + (255,))
    facets.putalpha(ImageChops.multiply(facets.split()[3], ball))
    img.alpha_composite(facets)

    gl = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(gl).ellipse([cx - r * 0.7, cy - r * 0.8, cx + r * 0.05, cy - r * 0.1], fill=(255, 255, 255, 150))
    gl = gl.filter(ImageFilter.GaussianBlur(s * 0.03))
    gl.putalpha(ImageChops.multiply(gl.split()[3], ball))
    img.alpha_composite(gl)
    d = ImageDraw.Draw(img)
    d.polygon(star_points(cx - r * 0.42, cy - r * 0.5, s * 0.1, s * 0.028, points=4), fill=(255, 255, 255, 255))
    d.polygon(star_points(cx + r * 0.62, cy + r * 0.2, s * 0.055, s * 0.016, points=4), fill=(255, 255, 255, 235))
    return finish(img, TILE)


def draw_box(reinforced):
    img = canvas(TILE)
    s = img.size[0]
    m = s * 0.045
    r = s * 0.1
    wood = (206, 142, 78)
    dark = (128, 78, 40)

    body = mask_rrect(img.size, [m, m, s - m, s - m], r)
    soft_shadow(img, body, offset=(0, s * 0.02), blur=s * 0.022, alpha=100)
    fill(img, body, shade(dark, 0.8))
    lip = s * 0.07
    top_box = [m, m, s - m, s - m - lip]
    top = mask_rrect(img.size, top_box, r)
    paste_masked(img, vgradient(img.size, [(0, shade(dark, 1.25)), (1, dark)]), top)

    f = s * 0.115
    inner_box = [m + f, m + f, s - m - f, s - m - lip - f]
    inner = mask_rrect(img.size, inner_box, r * 0.3)
    paste_masked(img, vgradient(img.size, [(0, shade(wood, 1.12)), (1, shade(wood, 0.9))]), inner)
    d = ImageDraw.Draw(img)
    planks = 3
    ph = (inner_box[3] - inner_box[1]) / planks
    for i in range(1, planks):
        y = inner_box[1] + i * ph
        d.line([(inner_box[0], y), (inner_box[2], y)], fill=shade(wood, 0.62) + (255,), width=int(s * 0.012))
        d.line([(inner_box[0], y + s * 0.01), (inner_box[2], y + s * 0.01)], fill=shade(wood, 1.25) + (140,), width=int(s * 0.006))
    rng = random.Random(7)
    for _ in range(9):
        gx = rng.uniform(inner_box[0] + s * 0.04, inner_box[2] - s * 0.14)
        gy = rng.uniform(inner_box[1] + s * 0.03, inner_box[3] - s * 0.03)
        d.line([(gx, gy), (gx + rng.uniform(s * 0.05, s * 0.12), gy)], fill=shade(wood, 0.78) + (200,), width=int(s * 0.007))
    edge = ImageChops.subtract(inner, mask_rrect(img.size, [inner_box[0] + s * 0.02, inner_box[1] + s * 0.03, inner_box[2] - s * 0.01, inner_box[3]], r * 0.3))
    fill(img, edge, shade(dark, 0.6), 130)

    gloss(img, [m + s * 0.05, m + s * 0.02, s - m - s * 0.05, m + s * 0.06], s * 0.02, alpha=70, blur=s * 0.008, clip=top)
    for nx, ny in [(0.105, 0.1), (0.895, 0.1), (0.105, 0.82), (0.895, 0.82)]:
        x, y = s * nx, s * ny
        d.ellipse([x - s * 0.022, y - s * 0.022, x + s * 0.022, y + s * 0.022], fill=shade(dark, 0.55) + (255,))
        d.ellipse([x - s * 0.014, y - s * 0.018, x + s * 0.01, y + s * 0.006], fill=(235, 200, 150, 255))

    if reinforced:
        steel = (150, 160, 184)
        for band in ("h", "v"):
            if band == "h":
                box = [m, s * 0.41, s - m, s * 0.55]
            else:
                box = [s * 0.43, m, s * 0.57, s - m - lip * 0.2]
            bm = ImageChops.multiply(mask_rrect(img.size, box, s * 0.012), body)
            soft_shadow(img, bm, offset=(0, s * 0.012), blur=s * 0.01, alpha=110)
            if band == "h":
                paste_masked(img, vgradient(img.size, [(0.41, shade(steel, 1.45)), (0.48, steel), (0.55, shade(steel, 0.6))]), bm)
            else:
                paste_masked(img, vgradient(img.size, [(0, shade(steel, 1.3)), (1, shade(steel, 0.75))]), bm)
        d = ImageDraw.Draw(img)
        for bx, by in [(0.5, 0.48), (0.15, 0.48), (0.85, 0.48), (0.5, 0.14), (0.5, 0.82)]:
            x, y = s * bx, s * by
            d.ellipse([x - s * 0.03, y - s * 0.03, x + s * 0.03, y + s * 0.03], fill=shade(steel, 0.5) + (255,))
            d.ellipse([x - s * 0.022, y - s * 0.026, x + s * 0.018, y + s * 0.012], fill=shade(steel, 1.55) + (255,))
    return finish(img, TILE)


def button(rgb, size=112, radius=34):
    img = canvas(size)
    s = img.size[0]
    m = s * 0.03
    r = radius * SS
    lip = s * 0.085
    body = mask_rrect(img.size, [m, m, s - m, s - m], r)
    soft_shadow(img, body, offset=(0, s * 0.015), blur=s * 0.018, alpha=80)
    fill(img, body, shade(rgb, 0.55))
    top = mask_rrect(img.size, [m, m, s - m, s - m - lip], r)
    paste_masked(img, vgradient(img.size, [(0, shade(rgb, 1.3)), (0.5, rgb), (1, shade(rgb, 0.85))]), top)
    inner = mask_rrect(img.size, [m + s * 0.028, m + s * 0.03, s - m - s * 0.028, s - m - lip - s * 0.02], r * 0.88)
    rim = ImageChops.subtract(top, inner)
    rim_top = ImageChops.multiply(rim, vgradient(img.size, [(0, WHITE), (0.4, (0, 0, 0)), (1, (0, 0, 0))]).convert("L"))
    fill(img, rim_top, WHITE, 130)
    gloss(img, [s * 0.14, s * 0.08, s * 0.86, s * 0.3], s * 0.11, alpha=58, blur=s * 0.012, clip=top)
    return finish(img, size)


def panel(size=160, radius=40, face=(255, 248, 232), border=(120, 86, 210)):
    img = canvas(size)
    s = img.size[0]
    m = s * 0.03
    r = radius * SS
    body = mask_rrect(img.size, [m, m, s - m, s - m], r)
    soft_shadow(img, body, offset=(0, s * 0.02), blur=s * 0.02, alpha=110)
    fill(img, body, shade(border, 0.6))
    top = mask_rrect(img.size, [m, m, s - m, s - m - s * 0.035], r)
    paste_masked(img, vgradient(img.size, [(0, shade(border, 1.3)), (1, shade(border, 0.9))]), top)
    b = s * 0.075
    inner = mask_rrect(img.size, [m + b, m + b, s - m - b, s - m - b - s * 0.035], r * 0.72)
    paste_masked(img, vgradient(img.size, [(0, face), (1, shade(face, 0.94))]), inner)
    edge = ImageChops.subtract(inner, mask_rrect(img.size, [m + b + s * 0.012, m + b + s * 0.02, s - m - b - s * 0.012, s - m - b - s * 0.035], r * 0.7))
    fill(img, edge, shade(border, 0.5), 60)
    gloss(img, [s * 0.14, m + s * 0.015, s * 0.86, m + s * 0.05], s * 0.02, alpha=90, blur=s * 0.006, clip=top)
    return finish(img, size)


def flat_rrect(size, radius, rgb=WHITE, alpha=255):
    img = canvas(size)
    s = img.size[0]
    fill(img, mask_rrect(img.size, [0, 0, s - 1, s - 1], radius * SS), rgb, alpha)
    return finish(img, size)


def board_frame(size=160, radius=36):
    img = canvas(size)
    s = img.size[0]
    m = s * 0.02
    r = radius * SS
    outer = mask_rrect(img.size, [m, m, s - m, s - m], r)
    soft_shadow(img, outer, offset=(0, s * 0.02), blur=s * 0.02, alpha=120)
    paste_masked(img, vgradient(img.size, [(0, (126, 150, 255)), (1, (70, 84, 200))]), outer)
    b = s * 0.05
    inner = mask_rrect(img.size, [m + b, m + b, s - m - b, s - m - b], r * 0.78)
    paste_masked(img, vgradient(img.size, [(0, (26, 30, 78)), (1, (40, 46, 108))]), inner)
    edge = ImageChops.subtract(inner, mask_rrect(img.size, [m + b + s * 0.01, m + b + s * 0.03, s - m - b - s * 0.01, s - m - b], r * 0.76))
    fill(img, edge, (8, 10, 40), 150)
    gloss(img, [s * 0.1, m + s * 0.008, s * 0.9, m + s * 0.03], s * 0.01, alpha=120, blur=s * 0.004, clip=outer)
    return finish(img, size)


def ribbon(w=320, h=96, rgb=(236, 62, 86)):
    img = canvas(w, h)
    W, H = img.size
    d = ImageDraw.Draw(img)
    fold = W * 0.1
    for sign in (0, 1):
        x_out = 0 if sign == 0 else W
        x_in = fold * 1.5 if sign == 0 else W - fold * 1.5
        notch = fold * 0.55 if sign == 0 else W - fold * 0.55
        d.polygon([(x_out, H * 0.3), (x_in, H * 0.3), (x_in, H * 0.96), (x_out, H * 0.96), (notch, H * 0.63)], fill=shade(rgb, 0.62) + (255,))
    body = mask_rrect(img.size, [fold, H * 0.06, W - fold, H * 0.8], H * 0.12)
    soft_shadow(img, body, offset=(0, H * 0.04), blur=H * 0.04, alpha=90)
    fill(img, body, shade(rgb, 0.6))
    top = mask_rrect(img.size, [fold, H * 0.06, W - fold, H * 0.72], H * 0.12)
    paste_masked(img, vgradient(img.size, [(0.06, shade(rgb, 1.3)), (0.4, rgb), (0.72, shade(rgb, 0.86))]), top)
    gloss(img, [fold + W * 0.03, H * 0.1, W - fold - W * 0.03, H * 0.26], H * 0.08, alpha=70, blur=H * 0.02, clip=top)
    return finish(img, w, h)


def star(full, size=128):
    img = canvas(size)
    s = img.size[0]
    cx, cy = s / 2, s * 0.53
    outer = rounded_poly_mask(img.size, star_points(cx, cy, s * 0.47, s * 0.235), s * 0.03)
    soft_shadow(img, outer, offset=(0, s * 0.02), blur=s * 0.02, alpha=100)
    if full:
        gold = (255, 196, 30)
        fill(img, outer, (196, 110, 14))
        inner = rounded_poly_mask(img.size, star_points(cx, cy - s * 0.02, s * 0.405, s * 0.2), s * 0.028)
        paste_masked(img, vgradient(img.size, [(0.1, (255, 240, 130)), (0.5, gold), (0.9, (250, 150, 20))]), inner)
        hi = rounded_poly_mask(img.size, star_points(cx - s * 0.02, cy - s * 0.06, s * 0.26, s * 0.13), s * 0.025)
        hi = ImageChops.multiply(hi, vgradient(img.size, [(0, WHITE), (0.5, (0, 0, 0)), (1, (0, 0, 0))]).convert("L"))
        fill(img, hi, WHITE, 120)
    else:
        fill(img, outer, (60, 50, 110))
        inner = rounded_poly_mask(img.size, star_points(cx, cy + s * 0.015, s * 0.4, s * 0.2), s * 0.028)
        fill(img, inner, (38, 32, 78))
    return finish(img, size)


def icon(name, size=96):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    c = s / 2
    W = (255, 255, 255, 255)
    lw = int(s * 0.1)

    if name == "Play":
        fill(img, rounded_poly_mask(img.size, [(s * 0.3, s * 0.2), (s * 0.82, c), (s * 0.3, s * 0.8)], s * 0.04), WHITE)
    elif name == "Pause":
        d.rounded_rectangle([s * 0.24, s * 0.2, s * 0.43, s * 0.8], radius=s * 0.06, fill=W)
        d.rounded_rectangle([s * 0.57, s * 0.2, s * 0.76, s * 0.8], radius=s * 0.06, fill=W)
    elif name == "Close":
        d.line([(s * 0.26, s * 0.26), (s * 0.74, s * 0.74)], fill=W, width=int(s * 0.14))
        d.line([(s * 0.74, s * 0.26), (s * 0.26, s * 0.74)], fill=W, width=int(s * 0.14))
        for x, y in [(0.26, 0.26), (0.74, 0.74), (0.74, 0.26), (0.26, 0.74)]:
            d.ellipse([s * x - s * 0.07, s * y - s * 0.07, s * x + s * 0.07, s * y + s * 0.07], fill=W)
    elif name == "Home":
        fill(img, rounded_poly_mask(img.size, [(c, s * 0.14), (s * 0.9, s * 0.52), (s * 0.1, s * 0.52)], s * 0.03), WHITE)
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([s * 0.24, s * 0.46, s * 0.76, s * 0.84], radius=s * 0.05, fill=W)
        d.rounded_rectangle([s * 0.43, s * 0.6, s * 0.57, s * 0.85], radius=s * 0.02, fill=(0, 0, 0, 0))
    elif name == "Retry":
        d.arc([s * 0.2, s * 0.2, s * 0.8, s * 0.8], start=-20, end=270, fill=W, width=int(s * 0.12))
        d.polygon([(s * 0.5, s * 0.06), (s * 0.5, s * 0.42), (s * 0.74, s * 0.24)], fill=W)
    elif name == "Next":
        fill(img, rounded_poly_mask(img.size, [(s * 0.2, s * 0.22), (s * 0.56, c), (s * 0.2, s * 0.78)], s * 0.035), WHITE)
        fill(img, rounded_poly_mask(img.size, [(s * 0.5, s * 0.22), (s * 0.86, c), (s * 0.5, s * 0.78)], s * 0.035), WHITE)
    elif name == "Gear":
        teeth = 8
        for i in range(teeth):
            a = math.radians(i * 360 / teeth)
            x, y = c + math.cos(a) * s * 0.34, c + math.sin(a) * s * 0.34
            tooth = Image.new("L", img.size, 0)
            ImageDraw.Draw(tooth).rounded_rectangle([x - s * 0.085, y - s * 0.085, x + s * 0.085, y + s * 0.085], radius=s * 0.02, fill=255)
            tooth = tooth.rotate(-i * 360 / teeth, center=(x, y), resample=Image.BICUBIC)
            fill(img, tooth, WHITE)
        d = ImageDraw.Draw(img)
        d.ellipse([c - s * 0.3, c - s * 0.3, c + s * 0.3, c + s * 0.3], fill=W)
        d.ellipse([c - s * 0.12, c - s * 0.12, c + s * 0.12, c + s * 0.12], fill=(0, 0, 0, 0))
    elif name in ("SoundOn", "SoundOff"):
        d.polygon([(s * 0.14, s * 0.38), (s * 0.3, s * 0.38), (s * 0.52, s * 0.2), (s * 0.52, s * 0.8), (s * 0.3, s * 0.62), (s * 0.14, s * 0.62)], fill=W)
        if name == "SoundOn":
            d.arc([s * 0.4, s * 0.32, s * 0.72, s * 0.68], start=-55, end=55, fill=W, width=int(s * 0.075))
            d.arc([s * 0.36, s * 0.2, s * 0.9, s * 0.8], start=-50, end=50, fill=W, width=int(s * 0.075))
        else:
            d.line([(s * 0.62, s * 0.38), (s * 0.86, s * 0.62)], fill=W, width=int(s * 0.085))
            d.line([(s * 0.86, s * 0.38), (s * 0.62, s * 0.62)], fill=W, width=int(s * 0.085))
    elif name in ("MusicOn", "MusicOff"):
        d.line([(s * 0.4, s * 0.24), (s * 0.4, s * 0.7)], fill=W, width=int(s * 0.07))
        d.line([(s * 0.76, s * 0.16), (s * 0.76, s * 0.62)], fill=W, width=int(s * 0.07))
        d.polygon([(s * 0.365, s * 0.2), (s * 0.795, s * 0.11), (s * 0.795, s * 0.27), (s * 0.365, s * 0.36)], fill=W)
        d.ellipse([s * 0.2, s * 0.6, s * 0.435, s * 0.8], fill=W)
        d.ellipse([s * 0.56, s * 0.52, s * 0.795, s * 0.72], fill=W)
        if name == "MusicOff":
            d.line([(s * 0.16, s * 0.14), (s * 0.86, s * 0.88)], fill=(0, 0, 0, 0), width=int(s * 0.16))
            d.line([(s * 0.18, s * 0.16), (s * 0.84, s * 0.86)], fill=W, width=int(s * 0.075))
    elif name == "Lock":
        d.arc([s * 0.3, s * 0.12, s * 0.7, s * 0.62], start=180, end=360, fill=W, width=int(s * 0.1))
        d.rounded_rectangle([s * 0.2, s * 0.42, s * 0.8, s * 0.86], radius=s * 0.09, fill=W)
        d.ellipse([c - s * 0.06, s * 0.54, c + s * 0.06, s * 0.66], fill=(0, 0, 0, 0))
        d.rectangle([c - s * 0.025, s * 0.6, c + s * 0.025, s * 0.75], fill=(0, 0, 0, 0))
    elif name == "Check":
        d.line([(s * 0.2, s * 0.52), (s * 0.42, s * 0.74), (s * 0.82, s * 0.28)], fill=W, width=int(s * 0.15), joint="curve")
        for x, y in [(0.2, 0.52), (0.82, 0.28)]:
            d.ellipse([s * x - s * 0.075, s * y - s * 0.075, s * x + s * 0.075, s * y + s * 0.075], fill=W)
    return finish(img, size)


def soft_circle(size=64, power=1.6):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            dist = math.hypot(x - c, y - c) / c
            px[x, y] = (255, 255, 255, int(255 * max(0.0, 1 - dist) ** power))
    return img


def ring(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.ellipse([s * 0.06, s * 0.06, s * 0.94, s * 0.94], outline=(255, 255, 255, 255), width=int(s * 0.07))
    return finish(img.filter(ImageFilter.GaussianBlur(s * 0.008)), size)


def sparkle(size=64):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).polygon(star_points(s / 2, s / 2, s * 0.48, s * 0.1, points=4), fill=(255, 255, 255, 255))
    glow = soft_circle(size, 2.2).resize(img.size)
    glow.putalpha(glow.split()[3].point(lambda v: int(v * 0.5)))
    out = Image.alpha_composite(glow, img)
    return finish(out, size)


def shard(size=32):
    img = canvas(size)
    s = img.size[0]
    fill(img, rounded_poly_mask(img.size, [(s * 0.12, s * 0.28), (s * 0.84, s * 0.12), (s * 0.72, s * 0.88), (s * 0.22, s * 0.74)], s * 0.04), WHITE)
    return finish(img, size)


def beam(w=128, h=32):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    for y in range(h):
        v = 1 - abs((y - (h - 1) / 2) / (h / 2))
        for x in range(w):
            u = x / (w - 1)
            a = (v ** 1.5) * (u ** 0.8)
            px[x, y] = (255, 255, 255, int(255 * max(0, min(1, a))))
    return img


def background(top, mid, bottom, seed, w=540, h=960):
    img = vgradient((w, h), [(0, top), (0.55, mid), (1, bottom)])
    rng = random.Random(seed)
    blobs = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    bd = ImageDraw.Draw(blobs)
    for _ in range(9):
        r = rng.uniform(w * 0.18, w * 0.5)
        x, y = rng.uniform(-0.1 * w, 1.1 * w), rng.uniform(0, h)
        bd.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, rng.randint(10, 26)))
    img.alpha_composite(blobs.filter(ImageFilter.GaussianBlur(w * 0.09)))
    rays = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    rd = ImageDraw.Draw(rays)
    for i in range(6):
        x0 = rng.uniform(-0.2 * w, 1.0 * w)
        width = rng.uniform(w * 0.05, w * 0.16)
        rd.polygon([(x0, -10), (x0 + width, -10), (x0 + width + h * 0.35, h * 0.75), (x0 + h * 0.35, h * 0.75)], fill=(255, 255, 255, 13))
    img.alpha_composite(rays.filter(ImageFilter.GaussianBlur(w * 0.03)))
    dots = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dd = ImageDraw.Draw(dots)
    for _ in range(46):
        r = rng.uniform(2, 8)
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        dd.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, rng.randint(14, 44)))
    img.alpha_composite(dots.filter(ImageFilter.GaussianBlur(1.2)))
    vig = Image.new("L", (w, h), 0)
    ImageDraw.Draw(vig).ellipse([-w * 0.35, -h * 0.12, w * 1.35, h * 1.12], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(w * 0.2))
    dark = Image.new("RGBA", (w, h), (10, 8, 40, 110))
    dark.putalpha(ImageChops.multiply(dark.split()[3], ImageChops.invert(vig)))
    img.alpha_composite(dark)
    return img.convert("RGB")


def app_icon(size=512):
    bg = background((120, 88, 240), (70, 110, 250), (40, 170, 240), seed=3, w=size, h=size).convert("RGBA")
    block = draw_block(COLORS[0][1], 0, 0).resize((int(size * 0.5), int(size * 0.5)), Image.LANCZOS)
    b2 = draw_block(COLORS[1][1], 1, 0).resize((int(size * 0.5), int(size * 0.5)), Image.LANCZOS)
    b3 = draw_block(COLORS[2][1], 2, 0).resize((int(size * 0.5), int(size * 0.5)), Image.LANCZOS)
    bg.alpha_composite(b3.rotate(12, resample=Image.BICUBIC, expand=True), (int(size * 0.45), int(size * 0.08)))
    bg.alpha_composite(b2.rotate(-10, resample=Image.BICUBIC, expand=True), (int(size * 0.02), int(size * 0.1)))
    bg.alpha_composite(block.resize((int(size * 0.6), int(size * 0.6)), Image.LANCZOS), (int(size * 0.2), int(size * 0.34)))
    return bg.convert("RGB")


def main():
    names = ("Blocks", "Boosters", "Board", "FX", "UI", "Icons", "Backgrounds")
    out = {k: ROOT / k for k in names}
    for p in out.values():
        p.mkdir(parents=True, exist_ok=True)
        for old in p.glob("*.png"):
            old.unlink()

    for ci, (name, rgb) in enumerate(COLORS):
        for tier, tname in enumerate(TIER_NAMES):
            draw_block(rgb, ci, tier).save(out["Blocks"] / f"Block_{name}_{tname}.png")
        draw_disco(rgb).save(out["Boosters"] / f"Disco_{name}.png")

    draw_rocket().save(out["Boosters"] / "Rocket.png")
    draw_bomb().save(out["Boosters"] / "Bomb.png")
    draw_box(False).save(out["Boosters"] / "Box.png")
    draw_box(True).save(out["Boosters"] / "Box_Reinforced.png")

    board_frame().save(out["Board"] / "BoardFrame_9s.png")
    flat_rrect(64, 16).save(out["Board"] / "Cell_9s.png")

    for name, rgb in [("Green", (88, 200, 60)), ("Blue", (52, 150, 255)), ("Red", (240, 78, 84)),
                      ("Yellow", (255, 190, 36)), ("Purple", (150, 96, 240)), ("Grey", (150, 156, 180))]:
        button(rgb).save(out["UI"] / f"Button_{name}_9s.png")
    panel().save(out["UI"] / "Panel_9s.png")
    panel(face=(52, 44, 120), border=(110, 130, 255)).save(out["UI"] / "PanelDark_9s.png")
    flat_rrect(64, 30).save(out["UI"] / "Pill_9s.png")
    flat_rrect(64, 18).save(out["UI"] / "Rounded_9s.png")
    ribbon().save(out["UI"] / "Ribbon.png")
    star(True).save(out["UI"] / "Star_Full.png")
    star(False).save(out["UI"] / "Star_Empty.png")

    for name in ("Play", "Pause", "Close", "Home", "Retry", "Next", "Gear", "SoundOn", "SoundOff",
                 "MusicOn", "MusicOff", "Lock", "Check"):
        icon(name).save(out["Icons"] / f"Icon_{name}.png")

    soft_circle(64).save(out["FX"] / "SoftCircle.png")
    sparkle(64).save(out["FX"] / "Sparkle.png")
    shard(32).save(out["FX"] / "Shard.png")
    ring(128).save(out["FX"] / "Ring.png")
    beam().save(out["FX"] / "Beam.png")

    background((104, 74, 226), (62, 104, 240), (36, 164, 236), seed=11).save(out["Backgrounds"] / "BG_Game.png")
    background((150, 70, 220), (96, 84, 240), (60, 130, 250), seed=23).save(out["Backgrounds"] / "BG_Menu.png")

    docs = REPO / "Docs"
    docs.mkdir(exist_ok=True)
    app_icon().save(docs / "app_icon.png")
    icon_dir = REPO / "Assets" / "_Project" / "Art" / "AppIcon"
    icon_dir.mkdir(parents=True, exist_ok=True)
    app_icon().save(icon_dir / "AppIcon.png")

    cols = 6
    sheet = Image.open(out["Backgrounds"] / "BG_Game.png").convert("RGBA").resize((TILE * cols, TILE * 7))
    for ci, (name, _) in enumerate(COLORS):
        for tier, tname in enumerate(TIER_NAMES):
            sheet.alpha_composite(Image.open(out["Blocks"] / f"Block_{name}_{tname}.png"), (ci * TILE, tier * TILE))
        sheet.alpha_composite(Image.open(out["Boosters"] / f"Disco_{name}.png"), (ci * TILE, 4 * TILE))
    for i, n in enumerate(["Rocket", "Bomb", "Box", "Box_Reinforced"]):
        sheet.alpha_composite(Image.open(out["Boosters"] / f"{n}.png"), (i * TILE, 5 * TILE))
    sheet.alpha_composite(Image.open(out["UI"] / "Star_Full.png"), (4 * TILE, 5 * TILE))
    sheet.alpha_composite(Image.open(out["UI"] / "Star_Empty.png"), (5 * TILE, 5 * TILE))
    x = 0
    for n in ["Green", "Blue", "Red", "Yellow"]:
        sheet.alpha_composite(Image.open(out["UI"] / f"Button_{n}_9s.png"), (x, 6 * TILE + 8))
        x += 118
    sheet.alpha_composite(Image.open(out["UI"] / "Panel_9s.png").resize((120, 120)), (x, 6 * TILE + 4))
    sheet.alpha_composite(Image.open(out["Board"] / "BoardFrame_9s.png").resize((120, 120)), (x + 124, 6 * TILE + 4))
    sheet.convert("RGB").save(docs / "blocks_sheet.png")

    print("done ->", ROOT)


if __name__ == "__main__":
    main()
