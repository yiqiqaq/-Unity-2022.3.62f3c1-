#include "sorting_frontend.h"
#include <cstdio>
#include <cmath>
#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

SortingFrontend* SortingFrontend::Instance = nullptr;

// ─── 颜色 ────────────────────────────────────────────────
static COLORREF ColBg         = RGB(45, 45, 55);
static COLORREF ColConv       = RGB(80, 80, 90);
static COLORREF ColConvEdge   = RGB(120, 120, 130);
static COLORREF ColConvLine   = RGB(100, 100, 110);
static COLORREF ColPassZone   = RGB(40, 120, 60);
static COLORREF ColPassEdge   = RGB(60, 200, 80);
static COLORREF ColFailZone   = RGB(140, 80, 20);
static COLORREF ColFailEdge   = RGB(220, 140, 40);
static COLORREF ColWhite      = RGB(255, 255, 255);
static COLORREF ColBlack      = RGB(0, 0, 0);
static COLORREF ColRed        = RGB(255, 60, 60);
static COLORREF ColGreen      = RGB(60, 200, 60);
static COLORREF ColYellow     = RGB(255, 220, 60);
static COLORREF ColHudBg      = RGB(20, 20, 35);
static COLORREF ColSelectRing  = RGB(100, 200, 255);

// 物料颜色
static COLORREF ColSolar      = RGB(50, 130, 220);  // 光伏蓝
static COLORREF ColEco        = RGB(60, 180, 90);   // 环保绿
static COLORREF ColRecycle    = RGB(80, 200, 200);   // 循环青
static COLORREF ColSlag       = RGB(140, 100, 70);   // 废渣棕
static COLORREF ColToxic      = RGB(180, 50, 50);    // 污染红
static COLORREF ColDefect     = RGB(160, 160, 60);   // 次品黄

// ─── GDI 辅助 ────────────────────────────────────────────
static void FillR(HDC hdc, int x, int y, int w, int h, COLORREF c) {
    HBRUSH br = CreateSolidBrush(c);
    RECT rc = {x, y, x + w, y + h};
    FillRect(hdc, &rc, br);
    DeleteObject(br);
}
static void DrawRR(HDC hdc, int x, int y, int w, int h, COLORREF fill, COLORREF edge, int penW = 2) {
    HBRUSH br = CreateSolidBrush(fill);
    HPEN pn = CreatePen(PS_SOLID, penW, edge);
    HBRUSH ob = (HBRUSH)SelectObject(hdc, br);
    HPEN op = (HPEN)SelectObject(hdc, pn);
    RoundRect(hdc, x, y, x + w, y + h, 10, 10);
    SelectObject(hdc, ob); SelectObject(hdc, op);
    DeleteObject(br); DeleteObject(pn);
}
static void DrawText2(HDC hdc, int x, int y, const wchar_t* txt, COLORREF c, int sz = 16) {
    HFONT f = CreateFontW(-sz, 0, 0, 0, FW_BOLD, 0, 0, 0,
        DEFAULT_CHARSET, 0, 0, CLEARTYPE_QUALITY, 0, L"Microsoft YaHei");
    HFONT old = (HFONT)SelectObject(hdc, f);
    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, c);
    TextOutW(hdc, x, y, txt, (int)wcslen(txt));
    SelectObject(hdc, old);
    DeleteObject(f);
}
static void DrawCenterText(HDC hdc, int cx, int cy, const wchar_t* txt, COLORREF c, int sz) {
    HFONT f = CreateFontW(-sz, 0, 0, 0, FW_BOLD, 0, 0, 0,
        DEFAULT_CHARSET, 0, 0, CLEARTYPE_QUALITY, 0, L"Microsoft YaHei");
    HFONT old = (HFONT)SelectObject(hdc, f);
    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, c);
    SIZE extent;
    GetTextExtentPoint32W(hdc, txt, (int)wcslen(txt), &extent);
    TextOutW(hdc, cx - extent.cx / 2, cy - extent.cy / 2, txt, (int)wcslen(txt));
    SelectObject(hdc, old);
    DeleteObject(f);
}

// ─── 物料名称和颜色 ──────────────────────────────────────
static const wchar_t* ItemLabel(ItemType t) {
    switch (t) {
    case ItemType::SolarPanel:        return L"光伏";
    case ItemType::EcoBuilding:       return L"建材";
    case ItemType::RecyclablePlastic: return L"塑料";
    case ItemType::IndustrialSlag:    return L"废渣";
    case ItemType::ToxicWaste:        return L"废料";
    case ItemType::DefectivePart:     return L"零件";
    }
    return L"?";
}
static COLORREF ItemColor(ItemType t) {
    switch (t) {
    case ItemType::SolarPanel:        return ColSolar;
    case ItemType::EcoBuilding:       return ColEco;
    case ItemType::RecyclablePlastic: return ColRecycle;
    case ItemType::IndustrialSlag:    return ColSlag;
    case ItemType::ToxicWaste:        return ColToxic;
    case ItemType::DefectivePart:     return ColDefect;
    }
    return ColWhite;
}

// ─── 窗口过程 ────────────────────────────────────────────
LRESULT CALLBACK SortingFrontend::WndProc(HWND h, UINT m, WPARAM w, LPARAM l) {
    if (!Instance) return DefWindowProcW(h, m, w, l);
    auto* self = Instance;
    switch (m) {
    case WM_SIZE:
        self->OnResize(LOWORD(l), HIWORD(l));
        return 0;
    case WM_LBUTTONDOWN: {
        float mx = (float)LOWORD(l) * WINDOW_W / self->cliW_;
        float my = (float)HIWORD(l) * WINDOW_H / self->cliH_;
        if (self->game_.State() == GameState::Passed ||
            self->game_.State() == GameState::Failed) {
            self->game_.Reset();
            self->gameTime_ = 0.0f;
        } else {
            self->game_.OnMouseDown(mx, my);
        }
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

bool SortingFrontend::InitWindow(HINSTANCE hInst) {
    WNDCLASSW wc{};
    wc.lpfnWndProc  = WndProc;
    wc.hInstance     = hInst;
    wc.lpszClassName = L"GreenFactorySortingDemo";
    wc.hCursor       = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    RegisterClassW(&wc);
    RECT rc = {0, 0, WINDOW_W, WINDOW_H};
    AdjustWindowRect(&rc, WS_OVERLAPPEDWINDOW, FALSE);
    hwnd_ = CreateWindowExW(0, L"GreenFactorySortingDemo",
        L"绿色工厂分拣 Demo",
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT, CW_USEDEFAULT,
        rc.right - rc.left, rc.bottom - rc.top,
        NULL, NULL, hInst, NULL);
    return hwnd_ != nullptr;
}

void SortingFrontend::OnResize(int w, int h) {
    cliW_ = w > 0 ? w : 1;
    cliH_ = h > 0 ? h : 1;
    if (backBuf_) DeleteObject(backBuf_);
    if (memDC_)   DeleteDC(memDC_);
    HDC sdc = GetDC(hwnd_);
    memDC_   = CreateCompatibleDC(sdc);
    backBuf_ = CreateCompatibleBitmap(sdc, WINDOW_W, WINDOW_H);
    SelectObject(memDC_, backBuf_);
    ReleaseDC(hwnd_, sdc);
}

void SortingFrontend::MainLoop() {
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
        if (dt > 0.05f) dt = 0.05f;
        gameTime_ += dt;
        game_.Update(dt);
        HDC dc = GetDC(hwnd_);
        if (memDC_) {
            Render(memDC_);
            SetStretchBltMode(dc, HALFTONE);
            StretchBlt(dc, 0, 0, cliW_, cliH_, memDC_, 0, 0, WINDOW_W, WINDOW_H, SRCCOPY);
        }
        ReleaseDC(hwnd_, dc);
        Sleep(1);
    }
}

int SortingFrontend::Run(HINSTANCE hInst) {
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

void SortingFrontend::DrawBackground(HDC hdc) {
    // 工厂车间灰色渐变
    for (int y = 0; y < WINDOW_H; y += 4) {
        float t = (float)y / WINDOW_H;
        int g = (int)(55 + 20 * t);
        FillR(hdc, 0, y, WINDOW_W, 4, RGB(g, g, g + 5));
    }
    // 地面
    FillR(hdc, 0, CONV_Y + CONV_H + 40, WINDOW_W, WINDOW_H - CONV_Y - CONV_H - 40, RGB(50, 48, 45));
}

void SortingFrontend::DrawConveyorBelt(HDC hdc, float time) {
    // 传送带主体
    FillR(hdc, 0, CONV_Y - CONV_H / 2, WINDOW_W, CONV_H, ColConv);
    // 上下边缘线
    HPEN pn = CreatePen(PS_SOLID, 2, ColConvEdge);
    HPEN old = (HPEN)SelectObject(hdc, pn);
    MoveToEx(hdc, 0, CONV_Y - CONV_H / 2, NULL);
    LineTo(hdc, WINDOW_W, CONV_Y - CONV_H / 2);
    MoveToEx(hdc, 0, CONV_Y + CONV_H / 2, NULL);
    LineTo(hdc, WINDOW_W, CONV_Y + CONV_H / 2);
    // 滚轮线（动画效果）
    HPEN pn2 = CreatePen(PS_SOLID, 1, ColConvLine);
    SelectObject(hdc, pn2);
    int offset = (int)(time * 80) % 40;
    for (int x = -offset; x < WINDOW_W; x += 40) {
        MoveToEx(hdc, x, CONV_Y - CONV_H / 2 + 5, NULL);
        LineTo(hdc, x, CONV_Y + CONV_H / 2 - 5);
    }
    SelectObject(hdc, old);
    DeleteObject(pn);
    DeleteObject(pn2);
}

void SortingFrontend::DrawItem(HDC hdc, const SortingItem& it) {
    if (!it.alive) return;
    int ix = (int)it.x, iy = (int)it.y;
    int hw = ITEM_W / 2, hh = ITEM_H / 2;
    COLORREF col = ItemColor(it.type);
    COLORREF edge = IsGoodItem(it.type) ? ColPassEdge : ColFailEdge;
    DrawRR(hdc, ix - hw, iy - hh, ITEM_W, ITEM_H, col, edge, 2);
    // 文字标签
    DrawCenterText(hdc, ix, iy, ItemLabel(it.type), ColWhite, 16);
    // 选中高亮
    if (it.selected) {
        HPEN pn = CreatePen(PS_SOLID, 3, ColSelectRing);
        HPEN old = (HPEN)SelectObject(hdc, pn);
        HBRUSH ob = (HBRUSH)SelectObject(hdc, GetStockObject(NULL_BRUSH));
        RoundRect(hdc, ix - hw - 6, iy - hh - 6, ix + hw + 6, iy + hh + 6, 14, 14);
        SelectObject(hdc, old);
        SelectObject(hdc, ob);
        DeleteObject(pn);
    }
}

void SortingFrontend::DrawZones(HDC hdc) {
    // 合格品区
    DrawRR(hdc, PASS_ZONE_X, PASS_ZONE_Y, PASS_ZONE_W, PASS_ZONE_H,
           RGB(20, 60, 30), ColPassEdge, 3);
    DrawCenterText(hdc, PASS_ZONE_X + PASS_ZONE_W / 2, PASS_ZONE_Y + 30,
                   L"合格品区", ColPassEdge, 26);
    DrawCenterText(hdc, PASS_ZONE_X + PASS_ZONE_W / 2, PASS_ZONE_Y + 70,
                   L"光伏 / 建材 / 塑料", RGB(150, 220, 160), 16);
    // 小图标
    DrawRR(hdc, PASS_ZONE_X + 30, PASS_ZONE_Y + 110, 60, 40, ColSolar, ColPassEdge, 1);
    DrawCenterText(hdc, PASS_ZONE_X + 60, PASS_ZONE_Y + 130, L"光伏", ColWhite, 12);
    DrawRR(hdc, PASS_ZONE_X + 110, PASS_ZONE_Y + 110, 60, 40, ColEco, ColPassEdge, 1);
    DrawCenterText(hdc, PASS_ZONE_X + 140, PASS_ZONE_Y + 130, L"建材", ColWhite, 12);
    DrawRR(hdc, PASS_ZONE_X + 190, PASS_ZONE_Y + 110, 60, 40, ColRecycle, ColPassEdge, 1);
    DrawCenterText(hdc, PASS_ZONE_X + 220, PASS_ZONE_Y + 130, L"塑料", ColWhite, 12);

    // 回收处理区
    DrawRR(hdc, FAIL_ZONE_X, FAIL_ZONE_Y, FAIL_ZONE_W, FAIL_ZONE_H,
           RGB(60, 35, 10), ColFailEdge, 3);
    DrawCenterText(hdc, FAIL_ZONE_X + FAIL_ZONE_W / 2, FAIL_ZONE_Y + 30,
                   L"回收处理区", ColFailEdge, 26);
    DrawCenterText(hdc, FAIL_ZONE_X + FAIL_ZONE_W / 2, FAIL_ZONE_Y + 70,
                   L"废渣 / 废料 / 零件", RGB(220, 180, 120), 16);
    DrawRR(hdc, FAIL_ZONE_X + 30, FAIL_ZONE_Y + 110, 60, 40, ColSlag, ColFailEdge, 1);
    DrawCenterText(hdc, FAIL_ZONE_X + 60, FAIL_ZONE_Y + 130, L"废渣", ColWhite, 12);
    DrawRR(hdc, FAIL_ZONE_X + 110, FAIL_ZONE_Y + 110, 60, 40, ColToxic, ColFailEdge, 1);
    DrawCenterText(hdc, FAIL_ZONE_X + 140, FAIL_ZONE_Y + 130, L"废料", ColWhite, 12);
    DrawRR(hdc, FAIL_ZONE_X + 190, FAIL_ZONE_Y + 110, 60, 40, ColDefect, ColFailEdge, 1);
    DrawCenterText(hdc, FAIL_ZONE_X + 220, FAIL_ZONE_Y + 130, L"零件", ColWhite, 12);
}

void SortingFrontend::DrawHUD(HDC hdc) {
    FillR(hdc, 0, 0, WINDOW_W, 50, ColHudBg);
    wchar_t buf[256];
    int sec = (int)game_.Remaining();
    swprintf(buf, 256, L"TIME %d:%02d", sec / 60, sec % 60);
    COLORREF tc = sec > 15 ? ColWhite : (sec > 5 ? ColYellow : ColRed);
    DrawText2(hdc, 20, 14, buf, tc, 22);

    swprintf(buf, 256, L"已分拣: %d / %d", game_.TotalSorted(), MIN_SORTED_COUNT);
    DrawText2(hdc, 200, 14, buf, ColWhite, 18);

    float acc = game_.Accuracy() * 100.0f;
    swprintf(buf, 256, L"准确率: %.0f%%", acc);
    COLORREF ac = game_.TotalSorted() < MIN_SORTED_COUNT ? ColWhite :
                  (acc >= MIN_ACCURACY * 100 ? ColGreen : ColRed);
    DrawText2(hdc, 450, 14, buf, ac, 18);

    swprintf(buf, 256, L"目标: ≥50 件 且 准确率 ≥85%%");
    DrawText2(hdc, 700, 14, buf, RGB(180, 180, 200), 16);

    // 底部提示
    FillR(hdc, 0, WINDOW_H - 36, WINDOW_W, 36, ColHudBg);
    DrawText2(hdc, 20, WINDOW_H - 28,
        L"点击传送带上的物料 -> 点击左侧[合格品区]或右侧[回收处理区]进行分拣",
        RGB(200, 200, 220), 15);
}

void SortingFrontend::DrawResult(HDC hdc) {
    FillR(hdc, 0, 0, WINDOW_W, WINDOW_H, RGB(5, 5, 20));
    DrawRR(hdc, 260, 180, 760, 360, RGB(20, 20, 50), RGB(80, 80, 160));

    if (game_.State() == GameState::Passed) {
        DrawCenterText(hdc, WINDOW_W / 2, 230, L"分拣完成！", ColGreen, 36);
        wchar_t buf[256];
        swprintf(buf, 256, L"分拣: %d 件  |  准确率: %.0f%%",
            game_.TotalSorted(), game_.Accuracy() * 100.0f);
        DrawCenterText(hdc, WINDOW_W / 2, 300, buf, ColWhite, 22);
        DrawCenterText(hdc, WINDOW_W / 2, 350,
            L"绿色制造，循环经济，你做到了！", RGB(180, 220, 180), 18);
    } else {
        DrawCenterText(hdc, WINDOW_W / 2, 230, L"分拣失败", ColRed, 36);
        wchar_t buf[256];
        if (game_.TotalSorted() < MIN_SORTED_COUNT) {
            swprintf(buf, 256, L"原因: 仅分拣 %d 件（需 ≥50 件）", game_.TotalSorted());
        } else {
            swprintf(buf, 256, L"原因: 准确率 %.0f%% 未达 85%%", game_.Accuracy() * 100.0f);
        }
        DrawCenterText(hdc, WINDOW_W / 2, 300, buf, ColWhite, 22);
    }
    DrawCenterText(hdc, WINDOW_W / 2, 440, L"点击任意位置重新挑战", RGB(160, 160, 180), 18);
}

void SortingFrontend::Render(HDC hdc) {
    DrawBackground(hdc);
    DrawConveyorBelt(hdc, gameTime_);
    for (const auto& it : game_.Items())
        DrawItem(hdc, it);
    DrawZones(hdc);
    DrawHUD(hdc);
    if (game_.State() != GameState::Running)
        DrawResult(hdc);
}
