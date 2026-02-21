#include "ibaye_godot_bridge.h"

#include <pthread.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "baye/comm.h"
#include "baye/consdef.h"
#include "baye/attribute.h"
#include "baye/paccount.h"
#include "baye/fight.h"
#include "inc/dictsys.h"
#include "frontend_api.h"

extern U8 g_PIdx;
extern CityType g_Cities[256];
extern PersonID g_PlayerKing;
extern CitySetType g_CityPos;
extern FGTJK g_FgtParam;
extern U8 g_FgtOver;
extern U16 g_FgtBoutCnt;
extern U8 g_FgtWeather;
void GetCityName(U8 city, U8 *str);
void LoadPeriod(U8 period);

FAR U8 GamConInit(void);
FAR void GamBaYeEng(void);
void GamSetLcdFlushCallback(void (*lcd_fluch_cb)(char *buffer));

static pthread_t g_engine_thread;
static int g_engine_started = 0;
static int g_engine_running = 0;
static int g_engine_ready = 0;
static int g_engine_state = IBAYE_ENGINE_STATE_IDLE;
static int g_pending_period = 0;

static pthread_mutex_t g_state_lock = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t g_frame_lock = PTHREAD_MUTEX_INITIALIZER;

static char g_last_error[256] = {0};
static char g_dat_path[1024] = "dat.lib";
static char g_font_dir[1024] = ".";
static char g_data_dir[1024] = ".";

static uint8_t *g_frame = NULL;
static int g_frame_cap = 0;
static int g_frame_width = 0;
static int g_frame_height = 0;
static uint32_t g_frame_id = 0;

static const char *engine_state_name(int state)
{
    switch (state) {
    case IBAYE_ENGINE_STATE_IDLE:
        return "idle";
    case IBAYE_ENGINE_STATE_BOOTING:
        return "booting";
    case IBAYE_ENGINE_STATE_READY:
        return "ready";
    case IBAYE_ENGINE_STATE_RUNNING:
        return "running";
    case IBAYE_ENGINE_STATE_EXITED:
        return "exited";
    case IBAYE_ENGINE_STATE_ERROR:
        return "error";
    default:
        return "unknown";
    }
}

static void set_error(const char *msg)
{
    pthread_mutex_lock(&g_state_lock);
    snprintf(g_last_error, sizeof(g_last_error), "%s", msg ? msg : "");
    pthread_mutex_unlock(&g_state_lock);
}

static int copy_path(char *dst, size_t dst_len, const char *src)
{
    int n;
    if (!src) {
        return 0;
    }
    n = snprintf(dst, dst_len, "%s", src);
    if (n < 0 || (size_t)n >= dst_len) {
        return -1;
    }
    return 0;
}

static void lcd_flush_cb(char *buffer)
{
    int i;
    int scale = (g_scale > 0) ? (int)g_scale : 1;
    int width = (int)g_screenWidth * scale;
    int height = (int)g_screenHeight * scale;
    int bytes;

    if (width <= 0 || height <= 0) {
        return;
    }
    bytes = width * height * 4;

    pthread_mutex_lock(&g_frame_lock);
    if (g_frame_cap < bytes) {
        uint8_t *new_buf = (uint8_t *)realloc(g_frame, (size_t)bytes);
        if (!new_buf) {
            pthread_mutex_unlock(&g_frame_lock);
            set_error("failed to allocate frame buffer");
            return;
        }
        g_frame = new_buf;
        g_frame_cap = bytes;
    }

    memcpy(g_frame, buffer, (size_t)bytes);
    for (i = 3; i < bytes; i += 4) {
        g_frame[i] = 0xFF;
    }

    g_frame_width = width;
    g_frame_height = height;
    g_frame_id++;
    pthread_mutex_unlock(&g_frame_lock);
}

static void *engine_main(void *_)
{
    (void)_;

    GamSetResourcePath((U8 *)g_dat_path, (U8 *)g_font_dir);
    GamSetDataDir((U8 *)g_data_dir);
    GamSetLcdFlushCallback(lcd_flush_cb);

    if (GamConInit()) {
        set_error("GamConInit failed (check dat.lib/font.bin paths)");
        pthread_mutex_lock(&g_state_lock);
        g_engine_ready = 0;
        g_engine_running = 0;
        g_engine_state = IBAYE_ENGINE_STATE_ERROR;
        pthread_mutex_unlock(&g_state_lock);
        return NULL;
    }

    pthread_mutex_lock(&g_state_lock);
    g_engine_ready = 1;
    g_engine_running = 0;
    g_engine_state = IBAYE_ENGINE_STATE_READY;
    if (g_pending_period > 0) {
        int period = g_pending_period;
        g_pending_period = 0;
        pthread_mutex_unlock(&g_state_lock);
        LoadPeriod((U8)period);
    } else {
        pthread_mutex_unlock(&g_state_lock);
    }

    pthread_mutex_lock(&g_state_lock);
    g_engine_running = 1;
    g_engine_state = IBAYE_ENGINE_STATE_RUNNING;
    pthread_mutex_unlock(&g_state_lock);

    GamBaYeEng();

    pthread_mutex_lock(&g_state_lock);
    g_engine_ready = 0;
    g_engine_running = 0;
    if (g_engine_state != IBAYE_ENGINE_STATE_ERROR) {
        g_engine_state = IBAYE_ENGINE_STATE_EXITED;
    }
    pthread_mutex_unlock(&g_state_lock);
    return NULL;
}

int ibaye_godot_set_paths(const char *dat_path, const char *font_dir, const char *data_dir)
{
    if (g_engine_started) {
        set_error("cannot set paths after engine start");
        return -1;
    }

    if (copy_path(g_dat_path, sizeof(g_dat_path), dat_path) != 0 ||
        copy_path(g_font_dir, sizeof(g_font_dir), font_dir) != 0 ||
        copy_path(g_data_dir, sizeof(g_data_dir), data_dir) != 0) {
        set_error("path too long");
        return -1;
    }
    return 0;
}

int ibaye_godot_set_screen_size(int width, int height)
{
    if (width <= 0 || height <= 0) {
        set_error("invalid screen size");
        return -1;
    }
    g_screenWidth = (U16)width;
    g_screenHeight = (U16)height;
    SysAdjustLCDBuffer(width, height);
    return 0;
}

int ibaye_godot_start(void)
{
    if (g_engine_started) {
        return 0;
    }
    g_engine_started = 1;
    pthread_mutex_lock(&g_state_lock);
    g_engine_ready = 0;
    g_engine_running = 0;
    g_engine_state = IBAYE_ENGINE_STATE_BOOTING;
    g_last_error[0] = 0;
    pthread_mutex_unlock(&g_state_lock);

    if (pthread_create(&g_engine_thread, NULL, engine_main, NULL) != 0) {
        g_engine_started = 0;
        pthread_mutex_lock(&g_state_lock);
        g_engine_state = IBAYE_ENGINE_STATE_ERROR;
        pthread_mutex_unlock(&g_state_lock);
        set_error("failed to create engine thread");
        return -1;
    }
    pthread_detach(g_engine_thread);
    return 0;
}

int ibaye_godot_is_running(void)
{
    int running;
    pthread_mutex_lock(&g_state_lock);
    running = g_engine_running;
    pthread_mutex_unlock(&g_state_lock);
    return running;
}

const char *ibaye_godot_last_error(void)
{
    return g_last_error;
}

int ibaye_godot_get_engine_state(void)
{
    int state;
    pthread_mutex_lock(&g_state_lock);
    state = g_engine_state;
    pthread_mutex_unlock(&g_state_lock);
    return state;
}

const char *ibaye_godot_get_engine_state_name(void)
{
    return engine_state_name(ibaye_godot_get_engine_state());
}

void ibaye_godot_send_key(int key)
{
    GamFrontendSendKey(key);
}

void ibaye_godot_send_touch(int event, int x, int y)
{
    GamFrontendSendTouch(event, x, y);
}

int ibaye_godot_get_frame_bytes(void)
{
    int bytes;
    pthread_mutex_lock(&g_frame_lock);
    bytes = g_frame_width * g_frame_height * 4;
    pthread_mutex_unlock(&g_frame_lock);
    return bytes;
}

int ibaye_godot_copy_frame(
    uint8_t *out_rgba,
    int out_len,
    int *out_width,
    int *out_height,
    uint32_t *out_frame_id
)
{
    int bytes;

    pthread_mutex_lock(&g_frame_lock);
    bytes = g_frame_width * g_frame_height * 4;
    if (out_width) {
        *out_width = g_frame_width;
    }
    if (out_height) {
        *out_height = g_frame_height;
    }
    if (out_frame_id) {
        *out_frame_id = g_frame_id;
    }

    if (bytes <= 0) {
        pthread_mutex_unlock(&g_frame_lock);
        return 0;
    }
    if (!out_rgba || out_len < bytes) {
        pthread_mutex_unlock(&g_frame_lock);
        return -bytes;
    }

    memcpy(out_rgba, g_frame, (size_t)bytes);
    pthread_mutex_unlock(&g_frame_lock);
    return bytes;
}

int ibaye_godot_get_current_period(void)
{
    return (int)g_PIdx;
}

int ibaye_godot_load_period(int period)
{
    int state;

    if (period <= 0) {
        set_error("invalid period");
        return -1;
    }

    pthread_mutex_lock(&g_state_lock);
    state = g_engine_state;
    if (state == IBAYE_ENGINE_STATE_RUNNING) {
        pthread_mutex_unlock(&g_state_lock);
        set_error("load period during runtime is not supported");
        return -2;
    }
    g_pending_period = period;
    pthread_mutex_unlock(&g_state_lock);
    return 0;
}

int ibaye_godot_get_city_count(void)
{
    if (g_engineConfig.citiesCount <= 0) {
        return 0;
    }
    return (int)g_engineConfig.citiesCount;
}

int ibaye_godot_get_city_name_bytes(uint8_t city_index, uint8_t *out_buf, int out_len)
{
    U8 name[64] = {0};
    int i = 0;
    int city_count = ibaye_godot_get_city_count();
    if ((int)city_index < 0 || (int)city_index >= city_count) {
        return -1;
    }

    GetCityName(city_index, name);
    while (name[i] != 0) {
        i++;
    }
    if (!out_buf || out_len <= 0) {
        return i;
    }
    if (out_len <= i) {
        return -2;
    }
    memcpy(out_buf, name, (size_t)i);
    out_buf[i] = 0;
    return i;
}

int ibaye_godot_get_city_stats(
    uint8_t city_index,
    int *out_belong,
    int *out_satrap,
    int *out_money,
    int *out_food,
    int *out_mothball_arms,
    int *out_population,
    int *out_people_devotion,
    int *out_farming,
    int *out_commerce,
    int *out_state,
    int *out_persons
)
{
    CityType *c;
    int city_count = ibaye_godot_get_city_count();
    if ((int)city_index < 0 || (int)city_index >= city_count) {
        return -1;
    }

    c = &g_Cities[city_index];

    if (out_belong) {
        *out_belong = c->Belong;
    }
    if (out_satrap) {
        *out_satrap = c->SatrapId;
    }
    if (out_money) {
        *out_money = c->Money;
    }
    if (out_food) {
        *out_food = c->Food;
    }
    if (out_mothball_arms) {
        *out_mothball_arms = c->MothballArms;
    }
    if (out_population) {
        *out_population = (int)c->Population;
    }
    if (out_people_devotion) {
        *out_people_devotion = c->PeopleDevotion;
    }
    if (out_farming) {
        *out_farming = c->Farming;
    }
    if (out_commerce) {
        *out_commerce = c->Commerce;
    }
    if (out_state) {
        *out_state = c->State;
    }
    if (out_persons) {
        *out_persons = c->Persons;
    }
    return 0;
}

int ibaye_godot_get_runtime_state(
    int *out_player_king,
    int *out_city_cursor,
    int *out_city_setx,
    int *out_city_sety,
    int *out_fight_mode,
    int *out_fight_city,
    int *out_fight_over,
    int *out_fight_bout,
    int *out_fight_weather,
    int *out_fight_active
)
{
    int running = ibaye_godot_is_running();
    int fight_active = 0;
    int city_cursor = -1;

    if (!g_engine_started || !running) {
        if (out_player_king) {
            *out_player_king = 0;
        }
        if (out_city_cursor) {
            *out_city_cursor = -1;
        }
        if (out_city_setx) {
            *out_city_setx = (int)g_CityPos.setx;
        }
        if (out_city_sety) {
            *out_city_sety = (int)g_CityPos.sety;
        }
        if (out_fight_mode) {
            *out_fight_mode = 0;
        }
        if (out_fight_city) {
            *out_fight_city = -1;
        }
        if (out_fight_over) {
            *out_fight_over = 0;
        }
        if (out_fight_bout) {
            *out_fight_bout = 0;
        }
        if (out_fight_weather) {
            *out_fight_weather = 0;
        }
        if (out_fight_active) {
            *out_fight_active = 0;
        }
        return -1;
    }

    if ((g_FgtParam.GenArray[0] != 0 || g_FgtParam.GenArray[FGT_PLAMAX] != 0) &&
        g_FgtOver == FGT_COMON) {
        fight_active = 1;
    }

    if (out_player_king) {
        *out_player_king = (int)g_PlayerKing;
    }
    if (out_city_cursor) {
        *out_city_cursor = city_cursor;
    }
    if (out_city_setx) {
        *out_city_setx = (int)g_CityPos.setx;
    }
    if (out_city_sety) {
        *out_city_sety = (int)g_CityPos.sety;
    }
    if (out_fight_mode) {
        *out_fight_mode = (int)g_FgtParam.Mode;
    }
    if (out_fight_city) {
        *out_fight_city = (int)g_FgtParam.CityIndex;
    }
    if (out_fight_over) {
        *out_fight_over = (int)g_FgtOver;
    }
    if (out_fight_bout) {
        *out_fight_bout = (int)g_FgtBoutCnt;
    }
    if (out_fight_weather) {
        *out_fight_weather = (int)g_FgtWeather;
    }
    if (out_fight_active) {
        *out_fight_active = fight_active;
    }
    return 0;
}
