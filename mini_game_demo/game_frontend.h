#pragma once
#include <windows.h>
#include "game_backend.h"

// ─── 前端：Win32 窗口 + GDI 渲染 ───────────────────────
class GameFrontend {
public:
    int  Run(HINSTANCE hInstance);

    // 供窗口过程回调
    static LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);
    static GameFrontend*    Instance;

private:
    bool InitWindow(HINSTANCE hInstance);
    void MainLoop();
    void Render(HDC hdc);
    void OnResize(int w, int h);

    // 绘制辅助
    void DrawWater(HDC hdc);
    void DrawBin(HDC hdc);
    void DrawEntity(HDC hdc, const Entity& e);
    void DrawHUD(HDC hdc);
    void DrawResult(HDC hdc);

    HWND        hwnd_    = nullptr;
    HBITMAP     backBuf_ = nullptr;
    HDC         memDC_   = nullptr;
    int         cliW_    = WINDOW_W;
    int         cliH_    = WINDOW_H;

    GameBackend game_;
    float       totalTime_ = 0.0f;

    // 鼠标在客户区的坐标
    float mouseX_ = 0, mouseY_ = 0;
};
