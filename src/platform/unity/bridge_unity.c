#include "ibaye_unity_bridge.h"

#include "ibaye_godot_bridge.h"

int ibaye_unity_set_paths(const char *dat_path, const char *font_dir, const char *data_dir)
{
    return ibaye_godot_set_paths(dat_path, font_dir, data_dir);
}

int ibaye_unity_set_screen_size(int width, int height)
{
    return ibaye_godot_set_screen_size(width, height);
}

int ibaye_unity_start(void)
{
    return ibaye_godot_start();
}

int ibaye_unity_is_running(void)
{
    return ibaye_godot_is_running();
}

const char *ibaye_unity_last_error(void)
{
    return ibaye_godot_last_error();
}

int ibaye_unity_get_engine_state(void)
{
    return ibaye_godot_get_engine_state();
}

const char *ibaye_unity_get_engine_state_name(void)
{
    return ibaye_godot_get_engine_state_name();
}

void ibaye_unity_send_key(int key)
{
    ibaye_godot_send_key(key);
}

void ibaye_unity_send_touch(int event, int x, int y)
{
    ibaye_godot_send_touch(event, x, y);
}

int ibaye_unity_get_frame_bytes(void)
{
    return ibaye_godot_get_frame_bytes();
}

int ibaye_unity_copy_frame(
    uint8_t *out_rgba,
    int out_len,
    int *out_width,
    int *out_height,
    uint32_t *out_frame_id
)
{
    return ibaye_godot_copy_frame(out_rgba, out_len, out_width, out_height, out_frame_id);
}

int ibaye_unity_get_current_period(void)
{
    return ibaye_godot_get_current_period();
}

int ibaye_unity_load_period(int period)
{
    return ibaye_godot_load_period(period);
}

int ibaye_unity_get_city_count(void)
{
    return ibaye_godot_get_city_count();
}

int ibaye_unity_get_city_name_bytes(uint8_t city_index, uint8_t *out_buf, int out_len)
{
    return ibaye_godot_get_city_name_bytes(city_index, out_buf, out_len);
}

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
)
{
    return ibaye_godot_get_city_stats(
        city_index,
        out_belong,
        out_satrap,
        out_money,
        out_food,
        out_mothball_arms,
        out_population,
        out_people_devotion,
        out_farming,
        out_commerce,
        out_state,
        out_persons
    );
}

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
)
{
    return ibaye_godot_get_runtime_state(
        out_player_king,
        out_city_cursor,
        out_city_setx,
        out_city_sety,
        out_fight_mode,
        out_fight_city,
        out_fight_over,
        out_fight_bout,
        out_fight_weather,
        out_fight_active
    );
}
