#include "game_backend.h"
#include <cmath>
#include <algorithm>

// ─── 辅助 ────────────────────────────────────────────────
static bool PointInRect(float px, float py, float rx, float ry, float rw, float rh) {
    return px >= rx && px <= rx + rw && py >= ry && py <= ry + rh;
}
static bool CircleHit(float ax, float ay, float bx, float by, float r) {
    float dx = ax - bx, dy = ay - by;
    return dx * dx + dy * dy <= r * r;
}

// ─── 构造 / 重置 ─────────────────────────────────────────
GameBackend::GameBackend() : rng_(std::random_device{}()) { Reset(); }

void GameBackend::Reset() {
    state_            = GameState::Running;
    remaining_        = GAME_DURATION;
    totalPollutants_  = 0;
    cleaned_          = 0;
    misTouches_       = 0;
    dragId_           = -1;
    entities_.clear();
    nextId_           = 0;
    targetCount_      = 15;
    spawnedPoll_      = 0;
    spawnTimer_       = 0.0f;
    SpawnEntities();
}

float GameBackend::CleanRatio() const {
    if (totalPollutants_ <= 0) return 0.0f;
    return static_cast<float>(cleaned_) / totalPollutants_;
}

// ─── 生成 ────────────────────────────────────────────────
Entity GameBackend::MakeEntity(EntityType t) {
    std::uniform_real_distribution<float> dx(80.0f, 1050.0f);
    std::uniform_real_distribution<float> dy(120.0f, 600.0f);
    std::uniform_real_distribution<float> dv(-6.0f, 6.0f);
    std::uniform_real_distribution<float> dp(0.0f, 6.283f);

    float ex = dx(rng_), ey = dy(rng_);
    Entity e;
    e.id      = nextId_++;
    e.type    = t;
    e.x = e.baseX = ex;
    e.y = e.baseY = ey;
    e.vx = dv(rng_);
    e.vy = dv(rng_) * 0.3f;
    e.phase   = dp(rng_);
    e.alive   = true;
    e.dragging = false;
    return e;
}

void GameBackend::SpawnOne(bool isPollutant) {
    if (isPollutant) {
        std::uniform_int_distribution<int> dt(0, 2);
        EntityType t = static_cast<EntityType>(dt(rng_));
        entities_.push_back(MakeEntity(t));
        totalPollutants_++;
        spawnedPoll_++;
    } else {
        std::uniform_int_distribution<int> dt(3, 5);
        EntityType t = static_cast<EntityType>(dt(rng_));
        entities_.push_back(MakeEntity(t));
    }
}

void GameBackend::SpawnEntities() {
    // 初始生成一批：6 污染物 + 4 水生生物
    for (int i = 0; i < 6; i++) SpawnOne(true);
    for (int i = 0; i < 4; i++) SpawnOne(false);
}

// ─── 每帧更新 ────────────────────────────────────────────
void GameBackend::Update(float dt) {
    if (state_ != GameState::Running) return;

    remaining_ -= dt;

    // 胜利判定
    if (totalPollutants_ > 0 && misTouches_ <= MAX_MIS_TOUCHES &&
        CleanRatio() >= REQUIRED_RATIO) {
        state_ = GameState::Passed;
        return;
    }
    // 失败：误触超限
    if (misTouches_ > MAX_MIS_TOUCHES) {
        state_ = GameState::Failed;
        return;
    }
    // 失败：时间耗尽
    if (remaining_ <= 0.0f) {
        remaining_ = 0.0f;
        if (CleanRatio() >= REQUIRED_RATIO)
            state_ = GameState::Passed;
        else
            state_ = GameState::Failed;
        return;
    }

    // 持续生成新污染物
    spawnTimer_ -= dt;
    if (spawnTimer_ <= 0.0f && spawnedPoll_ < targetCount_) {
        SpawnOne(true);
        // 偶尔附带一个水生生物
        std::uniform_int_distribution<int> coin(0, 2);
        if (coin(rng_) == 0) SpawnOne(false);
        spawnTimer_ = 3.0f;
    }

    // 水面漂移
    float time = GAME_DURATION - remaining_;
    for (auto& e : entities_) {
        if (!e.alive || e.dragging) continue;
        e.phase += dt * 0.5f;
        float speedMul = (e.type == EntityType::Fish) ? 0.3f : 0.6f;
        e.x = e.baseX + std::sin(e.phase) * 15.0f + e.vx * time * 0.03f * speedMul;
        e.y = e.baseY + std::cos(e.phase * 0.7f) * 6.0f + e.vy * time * 0.03f * speedMul;
        // 边界反弹
        if (e.x < 30)  { e.baseX += 60; e.vx = std::abs(e.vx); }
        if (e.x > 1100){ e.baseX -= 60; e.vx = -std::abs(e.vx); }
        if (e.y < 80)  { e.baseY += 40; e.vy = std::abs(e.vy); }
        if (e.y > 640) { e.baseY -= 40; e.vy = -std::abs(e.vy); }
    }
}

// ─── 输入处理 ────────────────────────────────────────────
void GameBackend::OnMouseDown(float mx, float my) {
    if (state_ != GameState::Running) return;
    // 从上到下找最前面的实体
    for (int i = (int)entities_.size() - 1; i >= 0; --i) {
        auto& e = entities_[i];
        if (!e.alive) continue;
        if (CircleHit(mx, my, e.x, e.y, ENTITY_RADIUS + 4)) {
            e.dragging = true;
            dragId_    = e.id;
            dragOffX_  = e.x - mx;
            dragOffY_  = e.y - my;
            return;
        }
    }
}

void GameBackend::OnMouseMove(float mx, float my) {
    if (dragId_ < 0) return;
    for (auto& e : entities_) {
        if (e.id == dragId_ && e.alive) {
            e.x = mx + dragOffX_;
            e.y = my + dragOffY_;
            e.baseX = e.x;
            e.baseY = e.y;
            return;
        }
    }
}

void GameBackend::OnMouseUp(float mx, float my) {
    if (dragId_ < 0) return;
    for (auto& e : entities_) {
        if (e.id == dragId_ && e.alive) {
            e.dragging = false;
            // 投入回收箱？
            if (PointInRect(e.x, e.y, BIN_X, BIN_Y, BIN_W, BIN_H)) {
                if (e.type <= EntityType::OilSlick) {
                    // 正确：污染物
                    e.alive = false;
                    cleaned_++;
                    if (totalPollutants_ > 0 && misTouches_ <= MAX_MIS_TOUCHES &&
                        CleanRatio() >= REQUIRED_RATIO) {
                        state_ = GameState::Passed;
                    }
                } else {
                    // 错误：水生生物投入回收箱 = 误触
                    e.alive = false;
                    misTouches_++;
                    if (misTouches_ > MAX_MIS_TOUCHES) {
                        state_ = GameState::Failed;
                    }
                }
            }
            dragId_ = -1;
            return;
        }
    }
    dragId_ = -1;
}
