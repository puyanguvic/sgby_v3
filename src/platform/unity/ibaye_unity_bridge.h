#ifndef IBAYE_UNITY_BRIDGE_H
#define IBAYE_UNITY_BRIDGE_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Match VM touch definitions in baye/comm.h */
#define IBAYE_UNITY_TOUCH_DOWN 1
#define IBAYE_UNITY_TOUCH_UP 2
#define IBAYE_UNITY_TOUCH_MOVE 3
#define IBAYE_UNITY_TOUCH_CANCEL 4

/* Match window key mapping in platform/win/window.c */
#define IBAYE_UNITY_KEY_ENTER 0x27
#define IBAYE_UNITY_KEY_EXIT 0x28
#define IBAYE_UNITY_KEY_UP 0x22
#define IBAYE_UNITY_KEY_DOWN 0x23
#define IBAYE_UNITY_KEY_LEFT 0x24
#define IBAYE_UNITY_KEY_RIGHT 0x25
#define IBAYE_UNITY_KEY_PGUP 0x20
#define IBAYE_UNITY_KEY_PGDN 0x21

/* Engine lifecycle states. */
#define IBAYE_UNITY_ENGINE_STATE_ERROR (-1)
#define IBAYE_UNITY_ENGINE_STATE_IDLE 0
#define IBAYE_UNITY_ENGINE_STATE_BOOTING 1
#define IBAYE_UNITY_ENGINE_STATE_READY 2
#define IBAYE_UNITY_ENGINE_STATE_RUNNING 3
#define IBAYE_UNITY_ENGINE_STATE_EXITED 4

/* Call before start; passing NULL keeps the current/default value. */
int ibaye_unity_set_paths(const char *dat_path, const char *font_dir, const char *data_dir);

/* Optional. Default is current engine config values. */
int ibaye_unity_set_screen_size(int width, int height);

/* Start engine loop in a background thread. */
int ibaye_unity_start(void);

/* 1 if engine thread is running, otherwise 0. */
int ibaye_unity_is_running(void);

/* Human-readable last error string. */
const char *ibaye_unity_last_error(void);

/* Engine lifecycle state and state label for UI/status panels. */
int ibaye_unity_get_engine_state(void);
const char *ibaye_unity_get_engine_state_name(void);

/* Input bridge. */
void ibaye_unity_send_key(int key);
void ibaye_unity_send_touch(int event, int x, int y);

/* Current frame byte size (RGBA8888). Returns 0 before first frame. */
int ibaye_unity_get_frame_bytes(void);

/*
 * Copies current frame to out_rgba.
 * Returns:
 *   >0 copied bytes
 *    0 no frame available yet
 *   <0 output buffer too small; required size is -return_value
 */
int ibaye_unity_copy_frame(
    uint8_t *out_rgba,
    int out_len,
    int *out_width,
    int *out_height,
    uint32_t *out_frame_id
);

/* Basic game-state bridge for shell UI. */
int ibaye_unity_get_current_period(void);
int ibaye_unity_load_period(int period);
int ibaye_unity_get_city_count(void);

/* GBK bytes from engine data. Returns byte length copied (without trailing '\0'). */
int ibaye_unity_get_city_name_bytes(uint8_t city_index, uint8_t *out_buf, int out_len);

/*
 * Returns 0 on success.
 * All out pointers are optional (can be NULL).
 */
int ibaye_unity_get_city_stats(
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
);

/*
 * Returns 0 on success.
 * Exposes runtime hints for HUD/operation routing.
 * All out pointers are optional.
 */
int ibaye_unity_get_runtime_state(
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
);

#ifdef __cplusplus
}
#endif

#endif
