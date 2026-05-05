#pragma once
#include <vector>
#include <string>
#include <random>
#include <functional>

// ─── 常量 ────────────────────────────────────────────────
constexpr float GAME_DURATION     = 90.0f;
constexpr float REQUIRED_RATIO    = 0.8f;
constexpr int   MAX_MIS_TOUCHES   = 2;
constexpr int   WINDOW_W          = 1280;
constexpr int   WINDOW_H          = 720;
constexpr int   BIN_X             = 1150;   // 回收箱区域
constexpr int   BIN_Y             = 200;
constexpr int   BIN_W             = 110;
constexpr int   BIN_H             = 320;
constexpr int   ENTITY_RADIUS     = 28;

// ─── 枚举 ────────────────────────────────────────────────
enum class GameState   { Idle, Running, Passed, Failed };
enum class EntityType  { Bottle, Bag, OilSlick, Fish, Reed, Grass };

struct Entity {
    int        id;
    EntityType type;
    float      x, y;          // 当前位置
    float      vx, vy;        // 漂移速度
    float      baseX, baseY;  // 锚点（用于小幅摆动）
    float      phase;         // 摆动相位
    bool       alive   = true;
    bool       dragging = false;
};

// ─── 后端：纯逻辑，无渲染依赖 ──────────────────────────
class GameBackend {
public:
    GameBackend();

    void  Reset();
    void  Update(float dt);

    // 输入
    void  OnMouseDown(float mx, float my);
    void  OnMouseMove(float mx, float my);
    void  OnMouseUp  (float mx, float my);

    // 查询（供前端渲染）
    GameState               State()       const { return state_; }
    float                   Remaining()   const { return remaining_; }
    int                     TotalPoll()   const { return totalPollutants_; }
    int                     Cleaned()     const { return cleaned_; }
    int                     MisTouches()  const { return misTouches_; }
    float                   CleanRatio()  const;
    const std::vector<Entity>& Entities() const { return entities_; }

    // 拖拽状态
    bool  IsDragging()  const { return dragId_ >= 0; }
    int   DragId()      const { return dragId_; }
    float DragOffX()    const { return dragOffX_; }
    float DragOffY()    const { return dragOffY_; }

private:
    void  SpawnEntities();
    void  SpawnOne(bool isPollutant);
    Entity MakeEntity(EntityType t);

    GameState state_ = GameState::Idle;
    float     remaining_      = GAME_DURATION;
    int       totalPollutants_ = 0;
    int       cleaned_         = 0;
    int       misTouches_      = 0;
    int       nextId_          = 0;

    std::vector<Entity> entities_;
    int   dragId_   = -1;
    float dragOffX_ = 0, dragOffY_ = 0;

    std::mt19937 rng_;

    int targetCount_;   // 本局需生成的污染物总数
    int spawnedPoll_;   // 已生成污染物数
    float spawnTimer_;  // 下一个生成计时
};
