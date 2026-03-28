#!/usr/bin/env python3
"""
迷路小ブロック（180×90マス）のパターン生成スクリプト

- 白 = 通路、黒 = 壁
- 接続点（出入口）ルール：
  - 上辺出口: X=90 (中央)
  - 下辺出口: X=90 (中央)
  - 左辺出口: Y=45 (中央)
  - 右辺出口: Y=45 (中央)
- 出入口は幅3マス（中央±1）で確保

生成アルゴリズム: 再帰的分割法をベースに、
   直線と分岐がバランスよく混ざるように調整
"""

import random
import os
from PIL import Image, ImageDraw

# --- 設定 ---
WIDTH = 180    # 小ブロックの幅（マス数）
HEIGHT = 90    # 小ブロックの高さ（マス数）
GATE_WIDTH = 5 # 出入口（ゲート）の幅（マス数）
NUM_PATTERNS = 6  # 生成するパターンの数
PIXEL_SCALE = 4   # 1マスを何ピクセルで描画するか

# 出入口の中心座標（0-indexed）
GATE_TOP_X = WIDTH // 2      # 90
GATE_BOTTOM_X = WIDTH // 2   # 90
GATE_LEFT_Y = HEIGHT // 2    # 45
GATE_RIGHT_Y = HEIGHT // 2   # 45

# 出力ディレクトリ
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Docs", "02_Specifications", "MazePatterns")
os.makedirs(OUTPUT_DIR, exist_ok=True)


def create_empty_maze(width, height):
    """全体を壁（0）で初期化した2Dグリッドを生成"""
    return [[0 for _ in range(width)] for _ in range(height)]


def carve_gates(maze, width, height):
    """4辺の出入口（ゲート）を通路にする"""
    half_gate = GATE_WIDTH // 2

    # 上辺ゲート（y=0）
    for dx in range(-half_gate, half_gate + 1):
        x = GATE_TOP_X + dx
        if 0 <= x < width:
            maze[0][x] = 1

    # 下辺ゲート（y=height-1）
    for dx in range(-half_gate, half_gate + 1):
        x = GATE_BOTTOM_X + dx
        if 0 <= x < width:
            maze[height - 1][x] = 1

    # 左辺ゲート（x=0）
    for dy in range(-half_gate, half_gate + 1):
        y = GATE_LEFT_Y + dy
        if 0 <= y < height:
            maze[y][0] = 1

    # 右辺ゲート（x=width-1）
    for dy in range(-half_gate, half_gate + 1):
        y = GATE_RIGHT_Y + dy
        if 0 <= y < height:
            maze[y][width - 1] = 1


def recursive_backtracker(maze, width, height, start_x, start_y):
    """
    再帰バックトラッカー法で通路を掘る。
    奇数座標のセルを対象にし、2マスずつ移動して壁を壊す。
    """
    stack = [(start_x, start_y)]
    maze[start_y][start_x] = 1  # 開始地点を通路にする

    directions = [(0, -2), (0, 2), (-2, 0), (2, 0)]

    while stack:
        cx, cy = stack[-1]
        random.shuffle(directions)
        carved = False
        for dx, dy in directions:
            nx, ny = cx + dx, cy + dy
            if 1 <= nx < width - 1 and 1 <= ny < height - 1 and maze[ny][nx] == 0:
                # 間の壁を壊す
                maze[cy + dy // 2][cx + dx // 2] = 1
                maze[ny][nx] = 1
                stack.append((nx, ny))
                carved = True
                break
        if not carved:
            stack.pop()


def connect_gates_to_maze(maze, width, height):
    """
    ゲート位置から迷路の内部通路まで直線で接続する。
    ゲートから内側に向かって通路が見つかるまで掘り進める。
    """
    half_gate = GATE_WIDTH // 2

    # 上ゲート → 下方向へ掘る
    for dx in range(-half_gate, half_gate + 1):
        x = GATE_TOP_X + dx
        if 0 <= x < width:
            for y in range(0, height):
                if maze[y][x] == 1 and y > 0:
                    break
                maze[y][x] = 1

    # 下ゲート → 上方向へ掘る
    for dx in range(-half_gate, half_gate + 1):
        x = GATE_BOTTOM_X + dx
        if 0 <= x < width:
            for y in range(height - 1, -1, -1):
                if maze[y][x] == 1 and y < height - 1:
                    break
                maze[y][x] = 1

    # 左ゲート → 右方向へ掘る
    for dy in range(-half_gate, half_gate + 1):
        y = GATE_LEFT_Y + dy
        if 0 <= y < height:
            for x in range(0, width):
                if maze[y][x] == 1 and x > 0:
                    break
                maze[y][x] = 1

    # 右ゲート → 左方向へ掘る
    for dy in range(-half_gate, half_gate + 1):
        y = GATE_RIGHT_Y + dy
        if 0 <= y < height:
            for x in range(width - 1, -1, -1):
                if maze[y][x] == 1 and x < width - 1:
                    break
                maze[y][x] = 1


def add_extra_passages(maze, width, height, ratio=0.05):
    """
    追加で壁をランダムに壊して回遊性を高める。
    分岐や複数経路を生み出し、「一本道」感を減らす。
    """
    wall_cells = []
    for y in range(2, height - 2):
        for x in range(2, width - 2):
            if maze[y][x] == 0:
                # 隣接する通路が2つ以上あれば壊す候補
                adj_paths = 0
                for dx, dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
                    if maze[y + dy][x + dx] == 1:
                        adj_paths += 1
                if adj_paths >= 2:
                    wall_cells.append((x, y))

    num_to_remove = int(len(wall_cells) * ratio)
    random.shuffle(wall_cells)
    for x, y in wall_cells[:num_to_remove]:
        maze[y][x] = 1


def generate_maze_block(seed=None):
    """1つの迷路小ブロックを生成する"""
    if seed is not None:
        random.seed(seed)

    maze = create_empty_maze(WIDTH, HEIGHT)

    # 奇数座標をスタート地点として再帰バックトラッカーで掘る
    start_x = 1 if WIDTH % 2 == 0 else 1
    start_y = 1 if HEIGHT % 2 == 0 else 1
    recursive_backtracker(maze, WIDTH, HEIGHT, start_x, start_y)

    # ゲートを開ける
    carve_gates(maze, WIDTH, HEIGHT)

    # ゲートから迷路内部への接続
    connect_gates_to_maze(maze, WIDTH, HEIGHT)

    # 追加通路で回遊性アップ
    add_extra_passages(maze, WIDTH, HEIGHT, ratio=0.04)

    return maze


def maze_to_image(maze, width, height, scale=PIXEL_SCALE):
    """迷路データを画像に変換する"""
    img_w = width * scale
    img_h = height * scale
    img = Image.new("RGB", (img_w, img_h), (40, 40, 40))
    draw = ImageDraw.Draw(img)

    half_gate = GATE_WIDTH // 2

    for y in range(height):
        for x in range(width):
            px = x * scale
            py = y * scale
            if maze[y][x] == 1:
                # 通路は白
                color = (230, 230, 230)

                # ゲート部分を特別な色（青系）で表示
                is_gate = False
                # 上辺ゲート
                if y == 0 and abs(x - GATE_TOP_X) <= half_gate:
                    is_gate = True
                # 下辺ゲート
                if y == height - 1 and abs(x - GATE_BOTTOM_X) <= half_gate:
                    is_gate = True
                # 左辺ゲート
                if x == 0 and abs(y - GATE_LEFT_Y) <= half_gate:
                    is_gate = True
                # 右辺ゲート
                if x == width - 1 and abs(y - GATE_RIGHT_Y) <= half_gate:
                    is_gate = True

                if is_gate:
                    color = (80, 180, 255)  # 水色でゲートを強調

                draw.rectangle([px, py, px + scale - 1, py + scale - 1], fill=color)

    return img


def main():
    print(f"迷路パターン生成を開始します...")
    print(f"  ブロックサイズ: {WIDTH}×{HEIGHT} マス")
    print(f"  ゲート幅: {GATE_WIDTH} マス")
    print(f"  生成パターン数: {NUM_PATTERNS}")
    print(f"  出力先: {OUTPUT_DIR}")
    print()

    # Unity用CSVの出力先（StreamingAssetsかResourcesに配置可能）
    csv_output_dir = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "MazeData")
    os.makedirs(csv_output_dir, exist_ok=True)

    for i in range(NUM_PATTERNS):
        seed = 1000 + i * 42  # 再現可能なシード値
        maze = generate_maze_block(seed=seed)
        img = maze_to_image(maze, WIDTH, HEIGHT)

        # 画像出力
        filename = f"maze_block_pattern_{i + 1:02d}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        img.save(filepath)

        # CSV出力（Unity TextAsset用）
        csv_filename = f"maze_block_{i + 1:02d}.csv"
        csv_filepath = os.path.join(csv_output_dir, csv_filename)
        with open(csv_filepath, "w") as f:
            # ヘッダ行: 幅,高さ
            f.write(f"{WIDTH},{HEIGHT}\n")
            for row in maze:
                f.write(",".join(str(cell) for cell in row) + "\n")

        # 通路率の計算
        total_cells = WIDTH * HEIGHT
        path_cells = sum(sum(row) for row in maze)
        path_ratio = path_cells / total_cells * 100

        print(f"  パターン {i + 1}: {filename} + {csv_filename} (通路率: {path_ratio:.1f}%)")

    print(f"\n完了！ {NUM_PATTERNS} パターンの迷路画像を生成しました。")
    print(f"出力先: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
