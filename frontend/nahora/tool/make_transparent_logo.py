"""Turns the supplied logo into a transparent PNG the app can sit on any ground.

The artwork arrives on an opaque near-black square. Dropped onto the app's
lighter background it reads as a mismatched block, so the ground has to go.

A flood fill from the edges rather than a colour swap: the icon's own dark
shapes — the clock outline, the hands, the silhouette — are the same near-black
as the background, and a global "make dark pixels transparent" would punch holes
straight through the logo. Filling inward from the border only ever reaches
pixels that are actually outside the artwork, because the amber tile encloses
everything within it.

Kept in the repository so the asset can be regenerated from a new drop of the
artwork without anybody having to remember how it was done.

    python tool/make_transparent_logo.py source.png assets/images/nahora-logo.png
"""

import sys
from collections import deque

from PIL import Image

# How far a pixel may drift from the sampled background and still count as
# background. Generous, because the export carries compression noise and a soft
# drop shadow — and safe, because the nearest real content is amber, which is
# nowhere near this distance.
TOLERANCE = 70

# Left around the artwork after cropping, so the mark never touches the edge of
# its own bitmap.
PADDING = 8


def is_background(pixel, reference):
    return max(abs(a - b) for a, b in zip(pixel[:3], reference[:3])) <= TOLERANCE


def clear_ground(image):
    """Makes every pixel reachable from the border transparent."""
    width, height = image.size
    pixels = image.load()

    reference = pixels[0, 0]

    seen = bytearray(width * height)
    queue = deque()

    def enqueue(x, y):
        index = (y * width) + x
        if seen[index]:
            return
        if not is_background(pixels[x, y], reference):
            return
        seen[index] = 1
        queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        pixels[x, y] = (0, 0, 0, 0)

        if x > 0:
            enqueue(x - 1, y)
        if x < width - 1:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y < height - 1:
            enqueue(x, y + 1)


def is_amber(pixel):
    """The brand amber: strongly red, mid green, almost no blue."""
    red, green, blue, alpha = pixel

    return alpha != 0 and red > 180 and 110 < green < 220 and blue < 110


def find_icon_bounds(image):
    """The bounding box of the amber tile.

    The **largest connected** amber region, not every amber pixel: the wordmark
    is amber too, and so is the rule beneath it, so a global bounding box would
    span the whole image and protect the very letters this exists to clean.

    Everything inside the tile is artwork, including its dark shapes. Everything
    below it is the wordmark, where dark can only be ground.
    """
    width, height = image.size
    pixels = image.load()

    seen = bytearray(width * height)
    best = None
    best_size = 0

    for start_y in range(height):
        for start_x in range(width):
            index = (start_y * width) + start_x

            if seen[index] or not is_amber(pixels[start_x, start_y]):
                continue

            seen[index] = 1
            queue = deque([(start_x, start_y)])

            size = 0
            left = right = start_x
            top = bottom = start_y

            while queue:
                x, y = queue.popleft()
                size += 1
                left, right = min(left, x), max(right, x)
                top, bottom = min(top, y), max(bottom, y)

                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if not (0 <= nx < width and 0 <= ny < height):
                        continue

                    neighbour = (ny * width) + nx
                    if seen[neighbour] or not is_amber(pixels[nx, ny]):
                        continue

                    seen[neighbour] = 1
                    queue.append((nx, ny))

            if size > best_size:
                best_size = size
                best = (left, top, right, bottom)

    return best


def clear_enclosed_ground(image, icon_bounds):
    """Clears background trapped inside letters.

    A flood from the border cannot reach the counters of an "a" or an "o" — the
    strokes enclose them — so they stay opaque and the letters read as filled
    in. Outside the icon tile there is no dark artwork to protect, so any
    remaining background-coloured pixel there is ground.
    """
    if icon_bounds is None:
        return

    width, height = image.size
    pixels = image.load()
    reference = (16, 16, 17, 255)

    _, _, _, icon_bottom = icon_bounds

    for y in range(icon_bottom + 1, height):
        for x in range(width):
            pixel = pixels[x, y]

            if pixel[3] != 0 and is_background(pixel, reference):
                pixels[x, y] = (0, 0, 0, 0)


def trim(image):
    """Crops to what is actually drawn, leaving a little air."""
    bounds = image.getbbox()
    if bounds is None:
        return image

    left, top, right, bottom = bounds
    width, height = image.size

    return image.crop((
        max(0, left - PADDING),
        max(0, top - PADDING),
        min(width, right + PADDING),
        min(height, bottom + PADDING),
    ))


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 1

    source, destination = sys.argv[1], sys.argv[2]

    image = Image.open(source).convert("RGBA")
    clear_ground(image)
    clear_enclosed_ground(image, find_icon_bounds(image))
    image = trim(image)
    image.save(destination)

    print(f"{destination}: {image.size[0]}x{image.size[1]}, transparent")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
