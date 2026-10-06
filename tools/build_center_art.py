"""Generate the three catalog images required by Firawynix Center."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "packaging" / "windows" / "center" / "art"
OUT.mkdir(parents=True, exist_ok=True)
KNOT = Image.open(ROOT / "assets" / "celtic-knot-transparent.png").convert("RGBA")
APP = Image.open(ROOT / "release" / "preview.png").convert("RGBA")
FONT = Path("C:/Windows/Fonts/segoeui.ttf")
BOLD = Path("C:/Windows/Fonts/segoeuib.ttf")


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(BOLD if bold else FONT), size)


def background(size: tuple[int, int]) -> Image.Image:
    w, h = size
    image = Image.new("RGB", size)
    pixels = image.load()
    for y in range(h):
        for x in range(w):
            t = y / h
            right = x / w
            pixels[x, y] = (
                int(6 + t * 5 + right * 2),
                int(20 + t * 6 + right * 7),
                int(29 + t * 10 + right * 8),
            )
    glow = Image.new("RGBA", size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((int(w * 0.47), int(-h * 0.6), int(w * 1.35), int(h * 0.85)), fill=(0, 208, 226, 63))
    glow = glow.filter(ImageFilter.GaussianBlur(max(35, w // 14)))
    return Image.alpha_composite(image.convert("RGBA"), glow)


def paste_contained(base: Image.Image, source: Image.Image, box: tuple[int, int, int, int]) -> None:
    x, y, w, h = box
    scale = min(w / source.width, h / source.height)
    resized = source.resize((round(source.width * scale), round(source.height * scale)), Image.Resampling.LANCZOS)
    base.alpha_composite(resized, (x + (w - resized.width) // 2, y + (h - resized.height) // 2))


def app_card(base: Image.Image, box: tuple[int, int, int, int]) -> None:
    x, y, w, h = box
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), radius=20, fill=255)
    fitted = ImageOps.fit(APP, (w, h), Image.Resampling.LANCZOS, centering=(0.5, 0.0))
    base.paste(fitted, (x, y), mask)
    ImageDraw.Draw(base).rounded_rectangle((x, y, x + w - 1, y + h - 1), radius=20, outline=(59, 223, 232, 185), width=2)


def cover() -> None:
    image = background((600, 800))
    d = ImageDraw.Draw(image)
    d.rounded_rectangle((38, 35, 180, 69), radius=17, fill=(18, 198, 216, 36), outline=(25, 209, 226, 110), width=1)
    d.text((55, 42), "FIRAWYNIX", font=font(14, True), fill="#7DE8F1")
    paste_contained(image, KNOT, (385, 30, 164, 164))
    d.text((42, 154), "Organizador", font=font(55, True), fill="#F1FBFD")
    d.text((42, 218), "de Janelas", font=font(55, True), fill="#40D6E3")
    d.text((44, 299), "Cada janela no seu lugar.", font=font(23), fill="#C0D4DC")
    d.rounded_rectangle((30, 375, 570, 725), radius=27, fill=(25, 45, 57, 255), outline=(45, 100, 110, 170), width=2)
    app_card(image, (44, 389, 512, 321))
    d.text((43, 748), "ÁREAS  /  REGRAS  /  MONITORES", font=font(16, True), fill="#A2E8ED")
    image.convert("RGB").save(OUT / "foj-cover-600x800.png", optimize=True)


def banner() -> None:
    image = background((1920, 620))
    d = ImageDraw.Draw(image)
    for x in range(0, 1920, 92):
        d.line((x, 0, x, 620), fill=(42, 93, 103, 30), width=1)
    d.rounded_rectangle((88, 72, 378, 114), radius=21, fill=(17, 170, 188, 40), outline=(31, 203, 218, 120), width=2)
    d.text((111, 80), "FIRAWYNIX  •  WINDOWS", font=font(17, True), fill="#82E9F1")
    d.text((89, 157), "Organizador", font=font(91, True), fill="#F1FBFD")
    d.text((89, 259), "de Janelas", font=font(91, True), fill="#40D6E3")
    d.text((96, 395), "Escolha a área. Salve por programa.", font=font(34), fill="#D0E4E9")
    d.text((96, 447), "Reconheça cada tipo de janela.", font=font(34), fill="#D0E4E9")
    d.rounded_rectangle((89, 537, 368, 579), radius=20, fill=(26, 199, 215, 37), outline=(26, 199, 215, 130), width=1)
    d.text((111, 546), "lab.firawynix.com.br/foj", font=font(16, True), fill="#A8EDF1")
    d.rounded_rectangle((1020, 158, 1849, 555), radius=28, fill=(18, 43, 55, 255), outline=(56, 175, 186, 130), width=2)
    app_card(image, (1041, 179, 787, 355))
    paste_contained(image, KNOT, (1654, 5, 202, 202))
    image.convert("RGB").save(OUT / "foj-banner-1920x620.png", optimize=True)


def icon() -> None:
    image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    paste_contained(image, KNOT, (17, 17, 222, 222))
    image.save(OUT / "foj-icon-256x256.png", optimize=True)


cover()
banner()
icon()
