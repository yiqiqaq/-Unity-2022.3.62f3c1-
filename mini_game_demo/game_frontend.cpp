#include "game_frontend.h"
#include <cstdio>
#include <cmath>
#ifdef _MSC_VER
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")
#endif

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

GameFrontend* GameFrontend::Instance = nullptr;

// ─── 颜色 ────────────────────────────────────────────────
static COLORREF ColBin       = RGB(80, 180, 80);
static COLORREF ColBinEdge   = RGB(40, 120, 40);
static COLORREF ColBottle    = RGB(200, 60, 60);
static COLORREF ColBag       = RGB(220, 220, 240);
static COLORREF ColOil       = RGB(60, 60, 60);
static COLORREF ColFish      = RGB(255, 180, 50);
static COLORREF ColReed      = RGB(50, 160, 50);
static COLORREF ColGrass     = RGB(80, 200, 100);
static COLORREF ColWhite     = RGB(255, 255, 255);
static COLORREF ColBlack     = RGB(0, 0, 0);
static COLORREF ColRed       = RGB(255, 60, 60);
static COLORREF ColGreen     = RGB(60, 200, 60);
static COLORREF ColYellow    = RGB(255, 220, 60);
static COLORREF ColHudBg     = RGB(20, 20, 40);

// ─── 窗口过程 ────────────────────────────────────────────
LRESULT CALLBACK GameFrontend::WndProc(HWND h, UINT m, WPARAM w, LPARAM l) {
    if (!Instance) return DefWindowProcW(h, m, w, l);
    auto* self = Instance;

    switch (m) {
    case WM_SIZE:
        self->OnResize(LOWORD(l), HIWORD(l));
        return 0;
    case WM_LBUTTONDOWN: {
        float mx = (float)LOWORD(l) * WINDOW_W / self->cliW_;
        float my = (float)HIWORD(l) * WINDOW_H / self->cliH_;
        self->mouseX_ = mx; self->mouseY_ = my;
        // 结算画面点击重开
        if (self->game_.State() == GameState::Passed ||
            self->game_.State() == GameState::Failed) {
            self->game_.Reset();
        } else {
            self->game_.OnMouseDown(mx, my);
        }
        return 0;
    }
    case WM_MOUSEMOVE: {
        float mx = (float)LOWORD(l) * WINDOW_W / self->cliW_;
        float my = (float)HIWORD(l) * WINDOW_H / self->cliH_;
        self->mouseX_ = mx; self->mouseY_ = my;
        if (self->game_.IsDragging())
            self->game_.OnMouseMove(mx, my);
        return 0;
    }
    case WM_LBUTTONUP: {
        float mx = (float)LOWORD(l) * WINDOW_W / self->cliW_;
        float my = (float)HIWORD(l) * WINDOW_H / self->cliH_;
        self->game_.OnMouseUp(mx, my);
        return 0;
    }
    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(h, m, w, l);
}

// ─── 窗口初始化 ──────────────────────────────────────────
bool GameFrontend::InitWindow(HINSTANCE hInst) {
    WNDCLASSW wc{};
    wc.lpfnWndProc   = WndProc;
    wc.hInstance      = hInst;
    wc.lpszClassName  = L"WaterPurificationDemo";
    wc.hCursor        = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground  = (HBRUSH)GetStockObject(BLACK_BRUSH);
    RegisterClassW(&wc);

    RECT rc = {0, 0, WINDOW_W, WINDOW_H};
    AdjustWindowRect(&rc, WS_OVERLAPPEDWINDOW, FALSE);
    hwnd_ = CreateWindowExW(0, L"WaterPurificationDemo",
        L"水质净化小游戏 Demo",
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT, CW_USEDEFAULT,
        rc.right - rc.left, rc.bottom - rc.top,
        NULL, NULL, hInst, NULL);
    return hwnd_ != nullptr;
}

void GameFrontend::OnResize(int w, int h) {
    cliW_ = w > 0 ? w : 1;
    cliH_ = h > 0 ? h : 1;
    // 重建离屏缓冲
    if (backBuf_) DeleteObject(backBuf_);
    if (memDC_)   DeleteDC(memDC_);
    HDC screenDC = GetDC(hwnd_);
    memDC_   = CreateCompatibleDC(screenDC);
    backBuf_ = CreateCompatibleBitmap(screenDC, WINDOW_W, WINDOW_H);
    SelectObject(memDC_, backBuf_);
    ReleaseDC(hwnd_, screenDC);
}

// ─── 主循环 ──────────────────────────────────────────────
void GameFrontend::MainLoop() {
    LARGE_INTEGER freq, prev, now;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&prev);

    MSG msg{};
    while (true) {
        if (PeekMessageW(&msg, NULL, 0, 0, PM_REMOVE)) {
            if (msg.message == WM_QUIT) break;
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
        QueryPerformanceCounter(&now);
        float dt = (float)(now.QuadPart - prev.QuadPart) / freq.QuadPart;
        prev = now;
        if (dt > 0.05f) dt = 0.05f; // 上限防跳帧

        totalTime_ += dt;
        game_.Update(dt);

        // 渲染到离屏缓冲，再拉伸到窗口
        HDC dc = GetDC(hwnd_);
        if (memDC_) {
            Render(memDC_);
            SetStretchBltMode(dc, HALFTONE);
            StretchBlt(dc, 0, 0, cliW_, cliH_,
                       memDC_, 0, 0, WINDOW_W, WINDOW_H, SRCCOPY);
        }
        ReleaseDC(hwnd_, dc);

        Sleep(1); // 让出 CPU
    }
}

int GameFrontend::Run(HINSTANCE hInst) {
    Instance = this;
    if (!InitWindow(hInst)) return 1;
    OnResize(WINDOW_W, WINDOW_H);
    MainLoop();
    if (backBuf_) DeleteObject(backBuf_);
    if (memDC_)   DeleteDC(memDC_);
    return 0;
}

// ═══════════════════════════════════════════════════════════
//  渲 染
// ═══════════════════════════════════════════════════════════

static void FillRect2(HDC hdc, int x, int y, int w, int h, COLORREF c) {
    HBRUSH br = CreateSolidBrush(c);
    RECT rc = {x, y, x+w, y+h};
    FillRect(hdc, &rc, br);
    DeleteObject(br);
}
static void DrawEllipse2(HDC hdc, int cx, int cy, int rx, int ry, COLORREF fill, COLORREF edge) {
    HBRUSH br = CreateSolidBrush(fill);
    HPEN   pn = CreatePen(PS_SOLID, 2, edge);
    HBRUSH oldBr = (HBRUSH)SelectObject(hdc, br);
    HPEN   oldPn = (HPEN)SelectObject(hdc, pn);
    Ellipse(hdc, cx-rx, cy-ry, cx+rx, cy+ry);
    SelectObject(hdc, oldBr);
    SelectObject(hdc, oldPn);
    DeleteObject(br);
    DeleteObject(pn);
}
static void DrawRoundRect2(HDC hdc, int x, int y, int w, int h, COLORREF fill, COLORREF edge) {
    HBRUSH br = CreateSolidBrush(fill);
    HPEN   pn = CreatePen(PS_SOLID, 2, edge);
    HBRUSH oldBr = (HBRUSH)SelectObject(hdc, br);
    HPEN   oldPn = (HPEN)SelectObject(hdc, pn);
    RoundRect(hdc, x, y, x+w, y+h, 12, 12);
    SelectObject(hdc, oldBr);
    SelectObject(hdc, oldPn);
    DeleteObject(br);
    DeleteObject(pn);
}
static void DrawText2(HDC hdc, int x, int y, const wchar_t* txt, COLORREF c, int size = 16) {
    HFONT f = CreateFontW(-size, 0, 0, 0, FW_BOLD, 0, 0, 0,
        DEFAULT_CHARSET, 0, 0, CLEARTYPE_QUALITY, 0, L"Microsoft YaHei");
    HFONT old = (HFONT)SelectObject(hdc, f);
    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, c);
    TextOutW(hdc, x, y, txt, (int)wcslen(txt));
    SelectObject(hdc, old);
    DeleteObject(f);
}

// ─── 水面背景 ────────────────────────────────────────────
void GameFrontend::DrawWater(HDC hdc) {
    float t_anim = totalTime_;
    // 海蓝渐变背景
    for (int y = 0; y < WINDOW_H; y += 2) {
        float t = (float)y / WINDOW_H;
        // 浅海蓝 → 深海蓝
        int r = (int)(20 + 10 * (1 - t));    // 20→30
        int g = (int)(70 + 100 * (1 - t));   // 70→170
        int b = (int)(80 + 140 * (1 - t));   // 80→220
        FillRect2(hdc, 0, y, WINDOW_W, 2, RGB(r, g, b));
    }

    // 阳光波纹 caustic 效果（模拟阳光透过水面折射）
    for (int y = 0; y < WINDOW_H; y += 3) {
        float ty = (float)y / WINDOW_H;
        float depthFade = (1.0f - ty) * (1.0f - ty); // 越浅越亮
        for (int x = 0; x < WINDOW_W; x += 3) {
            float u = (float)x / WINDOW_W;
            // 叠加两组移动的正弦波形成 caustic 网格
            float c1 = sinf(u * 20.0f + ty * 14.0f + t_anim * 0.8f)
                     * cosf(ty * 12.0f - u * 10.0f + t_anim * 0.6f);
            float c2 = sinf(u * 16.0f - ty * 18.0f + t_anim * 1.0f + 2.0f)
                     * cosf(ty * 15.0f + u * 8.0f - t_anim * 0.5f);
            float caustic = (c1 + c2) * 0.5f;
            if (caustic > 0.0f) {
                float bright = caustic * 0.25f * depthFade;
                int cr = (int)(bright * 100);
                int cg = (int)(bright * 180);
                int cb = (int)(bright * 255);
                // 叠加到背景上
                COLORREF bg = GetPixel(hdc, x, y);
                int br = (std::min)(255, (int)GetRValue(bg) + cr);
                int bg2 = (std::min)(255, (int)GetGValue(bg) + cg);
                int bb = (std::min)(255, (int)GetBValue(bg) + cb);
                FillRect2(hdc, x, y, 3, 3, RGB(br, bg2, bb));
            }
        }
    }

    // 阳光光柱（从水面射入）
    for (int ray = 0; ray < 5; ++ray) {
        float rx = 100.0f + ray * 260.0f + sinf(t_anim * 0.3f + ray * 1.5f) * 40.0f;
        float rWidth = 30.0f + sinf(t_anim * 0.5f + ray) * 10.0f;
        for (int y = 0; y < WINDOW_H - 50; y++) {
            float ty = (float)y / WINDOW_H;
            float alpha = (1.0f - ty) * (1.0f - ty) * 0.08f;
            float spread = 1.0f + ty * 0.6f;
            float dx = rWidth * spread;
            for (int x = (int)(rx - dx); x <= (int)(rx + dx); x++) {
                if (x < 0 || x >= WINDOW_W) continue;
                float dist = fabsf(x - rx) / dx;
                if (dist > 1.0f) continue;
                float a = alpha * (1.0f - dist * dist);
                if (a < 0.005f) continue;
                COLORREF bg = GetPixel(hdc, x, y);
                int cr = (std::min)(255, (int)GetRValue(bg) + (int)(a * 150));
                int cg = (std::min)(255, (int)GetGValue(bg) + (int)(a * 200));
                int cb = (std::min)(255, (int)GetBValue(bg) + (int)(a * 255));
                SetPixel(hdc, x, y, RGB(cr, cg, cb));
            }
        }
    }

    // 水面波纹线（动态）
    for (int wave = 0; wave < 4; ++wave) {
        int yBase = 100 + wave * 160;
        POINT pts[25];
        for (int i = 0; i < 25; ++i) {
            float x = (float)i * 54;
            float yOff = 12.0f * sinf(i * 0.6f + wave * 1.2f + t_anim * (0.3f + wave * 0.1f));
            pts[i].x = (int)x;
            pts[i].y = yBase + (int)yOff;
        }
        HPEN pen = CreatePen(PS_SOLID, 1, RGB(180, 220, 255));
        HPEN old = (HPEN)SelectObject(hdc, pen);
        SetBkMode(hdc, TRANSPARENT);
        Polyline(hdc, pts, 25);
        SelectObject(hdc, old);
        DeleteObject(pen);
    }
}

// ─── 回收箱 ──────────────────────────────────────────────
void GameFrontend::DrawBin(HDC hdc) {
    DrawRoundRect2(hdc, BIN_X, BIN_Y, BIN_W, BIN_H, ColBin, ColBinEdge);
    DrawText2(hdc, BIN_X + 10, BIN_Y + 10, L"回收", ColWhite, 20);
    DrawText2(hdc, BIN_X + 15, BIN_Y + 40, L"箱", ColWhite, 20);
    // 回收箭头
    HPEN pen = CreatePen(PS_SOLID, 3, ColWhite);
    HPEN old = (HPEN)SelectObject(hdc, pen);
    int cx = BIN_X + BIN_W / 2, cy = BIN_Y + BIN_H / 2 + 40;
    Arc(hdc, cx-30, cy-30, cx+30, cy+30, cx+30, cy, cx-30, cy);
    // 箭头头部
    MoveToEx(hdc, cx-30, cy, NULL);
    LineTo(hdc, cx-38, cy-8);
    MoveToEx(hdc, cx-30, cy, NULL);
    LineTo(hdc, cx-22, cy-8);
    SelectObject(hdc, old);
    DeleteObject(pen);
}

// ─── 单个实体 ────────────────────────────────────────────
void GameFrontend::DrawEntity(HDC hdc, const Entity& e) {
    if (!e.alive) return;
    int ix = (int)e.x, iy = (int)e.y;
    int r = ENTITY_RADIUS;

    switch (e.type) {
    case EntityType::Bottle: {
        // 塑料瓶：半透明瓶身 + 蓝色瓶盖
        DrawEllipse2(hdc, ix, iy, r, r*1.2f, RGB(190, 210, 220), RGB(150, 170, 180));
        // 瓶内液体
        DrawEllipse2(hdc, ix, iy+2, r-4, r-4, RGB(150, 180, 80), RGB(130, 160, 70));
        // 瓶颈
        FillRect2(hdc, ix-4, iy-r-6, 8, 12, RGB(180, 200, 210));
        // 瓶盖（蓝色）
        FillRect2(hdc, ix-5, iy-r-10, 10, 6, RGB(50, 100, 190));
        // 高光
        FillRect2(hdc, ix-6, iy-8, 2, 16, RGB(230, 240, 250));
        // 标签
        FillRect2(hdc, ix-8, iy-2, 16, 10, RGB(240, 240, 230));
        FillRect2(hdc, ix-8, iy-2, 16, 3, RGB(50, 120, 200));
        DrawText2(hdc, ix-10, iy-8, L"", ColWhite, 14);
        break;
    }
    case EntityType::Bag: {
        // 塑料袋：半透明白色，水下发青
        DrawEllipse2(hdc, ix, iy, r*0.9f, r*0.75f, RGB(210, 220, 230), RGB(180, 190, 200));
        // 皱褶
        HPEN wp = CreatePen(PS_SOLID, 1, RGB(190, 200, 210));
        HPEN wo = (HPEN)SelectObject(hdc, wp);
        POINT w1[2] = {{ix-r/2, iy-4}, {ix+r/2, iy-2}};
        POINT w2[2] = {{ix-r/2, iy+3}, {ix+r/2, iy+5}};
        Polyline(hdc, w1, 2);
        Polyline(hdc, w2, 2);
        SelectObject(hdc, wo); DeleteObject(wp);
        // 气泡
        DrawEllipse2(hdc, ix-8, iy-6, 2, 2, RGB(200, 230, 250), RGB(180, 210, 230));
        DrawText2(hdc, ix-8, iy-8, L"", RGB(100, 100, 110), 14);
        break;
    }
    case EntityType::OilSlick: {
        // 油污：深色不规则形状 + 虹彩
        DrawEllipse2(hdc, ix, iy, r*1.3f, r*0.7f, ColOil, RGB(30, 30, 30));
        // 虹彩光泽
        DrawEllipse2(hdc, ix-10, iy-4, 7, 4, RGB(70, 120, 140), RGB(60, 100, 120));
        DrawEllipse2(hdc, ix+6, iy+3, 5, 3, RGB(100, 70, 130), RGB(80, 60, 110));
        DrawEllipse2(hdc, ix-2, iy+5, 6, 2, RGB(60, 130, 90), RGB(50, 110, 80));
        DrawText2(hdc, ix-8, iy-8, L"", ColYellow, 14);
        break;
    }
    case EntityType::Fish: {
        // 鱼：银白色写实鱼
        COLORREF fishBody = RGB(185, 200, 210);
        COLORREF fishBack = RGB(120, 140, 155);
        COLORREF fishEdge = RGB(140, 160, 170);
        // 尾巴
        POINT tail[3] = {{ix-r-8, iy}, {ix-r-24, iy-14}, {ix-r-24, iy+14}};
        HBRUSH br = CreateSolidBrush(RGB(140, 165, 190));
        HPEN pn = CreatePen(PS_SOLID, 1, fishEdge);
        HBRUSH ob = (HBRUSH)SelectObject(hdc, br);
        HPEN op = (HPEN)SelectObject(hdc, pn);
        Polygon(hdc, tail, 3);
        SelectObject(hdc, ob); SelectObject(hdc, op);
        DeleteObject(br); DeleteObject(pn);
        // 身体
        DrawEllipse2(hdc, ix, iy, r*1.1f, r*0.7f, fishBody, fishEdge);
        // 背部深色
        DrawEllipse2(hdc, ix, iy+4, r*0.9f, r*0.35f, fishBack, fishBack);
        // 肚皮
        DrawEllipse2(hdc, ix+2, iy-4, r*0.6f, r*0.25f, RGB(230, 235, 240), fishEdge);
        // 背鳍
        POINT fin[3] = {{ix-5, (int)(iy+r*0.6f)}, {ix+5, (int)(iy+r*0.6f)}, {ix+2, iy+r+8}};
        br = CreateSolidBrush(RGB(130, 150, 170));
        ob = (HBRUSH)SelectObject(hdc, br);
        Polygon(hdc, fin, 3);
        SelectObject(hdc, ob); DeleteObject(br);
        // 眼睛（金色虹膜）
        DrawEllipse2(hdc, ix+10, iy-3, 5, 5, ColWhite, fishEdge);
        DrawEllipse2(hdc, ix+11, iy-3, 3, 3, RGB(215, 180, 50), RGB(180, 150, 40));
        DrawEllipse2(hdc, ix+11, iy-3, 1, 1, ColBlack, ColBlack);
        // 侧线
        HPEN lp = CreatePen(PS_SOLID, 1, RGB(150, 165, 175));
        HPEN lo = (HPEN)SelectObject(hdc, lp);
        POINT sideLine[2] = {{(int)(ix-r*0.6f), iy}, {(int)(ix+r*0.5f), iy+1}};
        Polyline(hdc, sideLine, 2);
        SelectObject(hdc, lo); DeleteObject(lp);
        DrawText2(hdc, ix-8, iy+r+2, L"鱼", RGB(130, 180, 210), 12);
        break;
    }
    case EntityType::Reed: {
        // 芦苇：自然弯曲的茎 + 芦花
        COLORREF stemDark = RGB(55, 90, 40);
        COLORREF stemLight = RGB(80, 130, 55);
        // 茎（多段弯曲）
        HPEN sp = CreatePen(PS_SOLID, 3, stemDark);
        HPEN so = (HPEN)SelectObject(hdc, sp);
        POINT stem[4] = {{ix, iy+r}, {ix-2, iy+r/2}, {ix+2, iy-r/2}, {ix, iy-r}};
        Polyline(hdc, stem, 4);
        SelectObject(hdc, so); DeleteObject(sp);
        // 芦花
        DrawEllipse2(hdc, ix, iy-r-8, 7, 14, RGB(140, 100, 45), RGB(110, 80, 35));
        // 花穗
        for (int i = 0; i < 5; i++) {
            int gx = ix + (int)(sin(i * 1.3f) * 5);
            int gy = iy - r - 4 + i * 4;
            DrawEllipse2(hdc, gx, gy, 2, 2, RGB(120, 85, 35), RGB(100, 70, 30));
        }
        // 叶片
        HPEN lp2 = CreatePen(PS_SOLID, 2, stemLight);
        HPEN lo2 = (HPEN)SelectObject(hdc, lp2);
        POINT leaf1[3] = {{ix, iy+r/3}, {ix+12, iy+r/3-6}, {ix+16, iy+r/3-12}};
        POINT leaf2[3] = {{ix, iy+r/2}, {ix-10, iy+r/2-4}, {ix-14, iy+r/2-8}};
        Polyline(hdc, leaf1, 3);
        Polyline(hdc, leaf2, 3);
        SelectObject(hdc, lo2); DeleteObject(lp2);
        DrawText2(hdc, ix-8, iy+r+2, L"芦苇", RGB(80, 130, 55), 12);
        break;
    }
    case EntityType::Grass: {
        // 水草：自然弯曲的多条叶片
        COLORREF grassDark = RGB(40, 110, 50);
        COLORREF grassMid = RGB(55, 140, 65);
        COLORREF grassLight = RGB(70, 160, 80);
        // 5 条叶片，不同颜色和弯曲度
        HPEN pen = CreatePen(PS_SOLID, 3, grassMid);
        HPEN old = (HPEN)SelectObject(hdc, pen);
        POINT blades[5][4] = {
            {{ix-12, iy+r}, {ix-16, iy+r/2}, {ix-10, iy-r/2}, {ix-5, iy-r}},
            {{ix-5, iy+r},  {ix-8, iy+r/3},  {ix-2, iy-r/3},  {ix+3, iy-r+8}},
            {{ix+2, iy+r},  {ix+5, iy+r/4},  {ix+3, iy-r/4},  {ix-2, iy-r+5}},
            {{ix+9, iy+r},  {ix+13, iy+r/2}, {ix+8, iy-r/2},  {ix+5, iy-r}},
            {{ix-8, iy+r},  {ix-12, iy+r/3}, {ix-6, iy-r/4},  {ix-3, iy-r+12}},
        };
        // 先画暗色底
        HPEN darkPen = CreatePen(PS_SOLID, 4, grassDark);
        SelectObject(hdc, darkPen);
        Polyline(hdc, blades[0], 4);
        Polyline(hdc, blades[2], 4);
        SelectObject(hdc, pen);
        Polyline(hdc, blades[1], 4);
        Polyline(hdc, blades[3], 4);
        Polyline(hdc, blades[4], 4);
        SelectObject(hdc, old);
        DeleteObject(pen);
        DeleteObject(darkPen);
        DrawText2(hdc, ix-8, iy+r+2, L"水草", grassLight, 12);
        break;
    }
    }

    // 拖拽高亮
    if (e.dragging) {
        HPEN pen = CreatePen(PS_DASH, 1, ColYellow);
        HPEN old = (HPEN)SelectObject(hdc, pen);
        HBRUSH old2 = (HBRUSH)SelectObject(hdc, GetStockObject(NULL_BRUSH));
        Ellipse(hdc, ix-r-6, iy-r-6, ix+r+6, iy+r+6);
        SelectObject(hdc, old);
        SelectObject(hdc, old2);
        DeleteObject(pen);
    }
}

// ─── HUD ─────────────────────────────────────────────────
void GameFrontend::DrawHUD(HDC hdc) {
    // 顶部栏
    FillRect2(hdc, 0, 0, WINDOW_W, 50, ColHudBg);

    // 倒计时
    wchar_t buf[128];
    int sec = (int)game_.Remaining();
    swprintf(buf, 128, L"TIME %d:%02d", sec / 60, sec % 60);
    COLORREF timeCol = sec > 20 ? ColWhite : (sec > 10 ? ColYellow : ColRed);
    DrawText2(hdc, 20, 14, buf, timeCol, 22);

    // 进度
    swprintf(buf, 128, L"已清理: %d / %d  (%.0f%%)",
        game_.Cleaned(), game_.TotalPoll(),
        game_.CleanRatio() * 100.0f);
    DrawText2(hdc, 200, 14, buf, ColWhite, 18);

    // 误触
    swprintf(buf, 128, L"误触: %d / %d", game_.MisTouches(), MAX_MIS_TOUCHES);
    COLORREF mtCol = game_.MisTouches() <= MAX_MIS_TOUCHES ? ColWhite : ColRed;
    DrawText2(hdc, 550, 14, buf, mtCol, 18);

    // 目标提示
    swprintf(buf, 128, L"目标: 80%% 清理率 | 误触 ≤ 2");
    DrawText2(hdc, 800, 14, buf, RGB(180, 180, 200), 16);

    // 底部操作提示
    FillRect2(hdc, 0, WINDOW_H - 36, WINDOW_W, 36, ColHudBg);
    DrawText2(hdc, 20, WINDOW_H - 28,
        L"拖动污染物(瓶/袋/油污)到右侧回收箱 | 请勿触碰水生生物(鱼/芦苇/水草)",
        RGB(200, 200, 220), 15);
}

// ─── 结算画面 ────────────────────────────────────────────
void GameFrontend::DrawResult(HDC hdc) {
    // 深色遮罩覆盖全屏
    FillRect2(hdc, 0, 0, WINDOW_W, WINDOW_H, RGB(5, 5, 20));
    // 对话框背景

    DrawRoundRect2(hdc, 260, 190, 760, 340, RGB(20, 20, 50), RGB(80, 80, 160));

    if (game_.State() == GameState::Passed) {
        DrawText2(hdc, 480, 230, L"[OK] 挑战成功！", ColGreen, 36);
        wchar_t buf[128];
        swprintf(buf, 128, L"清理率: %.0f%%  |  误触: %d / %d",
            game_.CleanRatio() * 100.0f, game_.MisTouches(), MAX_MIS_TOUCHES);
        DrawText2(hdc, 400, 300, buf, ColWhite, 22);
        DrawText2(hdc, 380, 360, L"淮河生态治理，你贡献了一份力量！", RGB(180, 220, 180), 18);
    } else {
        DrawText2(hdc, 480, 230, L"[X] 挑战失败", ColRed, 36);
        wchar_t buf[128];
        if (game_.MisTouches() > MAX_MIS_TOUCHES) {
            swprintf(buf, 128, L"原因: 误触水生生物超过 %d 次", MAX_MIS_TOUCHES);
        } else {
            swprintf(buf, 128, L"原因: 清理率 %.0f%% 未达 80%%", game_.CleanRatio() * 100.0f);
        }
        DrawText2(hdc, 380, 300, buf, ColWhite, 22);
    }
    DrawText2(hdc, 440, 440, L"点击任意位置重新挑战", RGB(160, 160, 180), 18);
}

// ─── 总渲染入口 ──────────────────────────────────────────
void GameFrontend::Render(HDC hdc) {
    // 背景
    DrawWater(hdc);

    // 实体
    for (const auto& e : game_.Entities()) {
        DrawEntity(hdc, e);
    }

    // 回收箱（最上层）
    DrawBin(hdc);

    // HUD
    DrawHUD(hdc);

    // 结算遮罩
    if (game_.State() != GameState::Running) {
        DrawResult(hdc);
    }
}
