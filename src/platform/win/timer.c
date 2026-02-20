#include "../common/timer.h"
#include <windows.h>

#define IDT_TIMER 100
#define precise 5

typedef struct
{
    const char *name;
    void (*callback)(void);
    int interval;
    int enabled;
    int tick;
} timer_t;

static timer_t timers[2] = {
    {"timer0", NULL, 1, 0, 0},
    {"timer1", NULL, 1, 0, 0},
};
static timer_t timer2 = {"timer2", NULL, 1, 0, 0};

static void run_timer(timer_t *t)
{
    if (t->enabled) {
        if (t->tick <= 0) {
            if (t->callback) {
                t->callback();
            }
            t->tick = t->interval;
        }
        t->tick--;
    }
}

void CALLBACK TimerProc(HWND hwnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
{
    run_timer(&timers[0]);
    run_timer(&timers[1]);
    run_timer(&timer2);
}

void gam_timer_init()
{
}

void winInitTimer()
{
    static int initialized = 0;
    if (!initialized) {
        SetTimer(NULL, IDT_TIMER, precise, (TIMERPROC)TimerProc);
        initialized = 1;
    }
}

void gam_timer_set_callback(U8 n, void (*cb)(void))
{
    if (n < 2) {
        timers[n].callback = cb;
    }
}

int gam_timer_open(U8 n, int interval)
{
    int prev = 0;
    if (n >= 2) {
        return prev;
    }
    if (timers[n].enabled) {
        prev = timers[n].interval;
    }
    if (interval <= 0) {
        gam_timer_close(n);
        return prev;
    }
    timers[n].interval = interval;
    timers[n].tick = interval;
    timers[n].enabled = 1;
    return prev;
}

void gam_timer_close(U8 n)
{
    if (n < 2) {
        timers[n].enabled = 0;
    }
}

U8 gam_check_timer_on(U8 n)
{
    return (n < 2 && timers[n].enabled) ? 1 : 0;
}

int gam_timer_interval(U8 n)
{
    return (n < 2) ? timers[n].interval : 0;
}

void gam_timer_set_interval(U8 n, int interval)
{
    if (n < 2) {
        timers[n].interval = interval;
        if (timers[n].tick > interval) {
            timers[n].tick = interval;
        }
    }
}

void gam_timer2_open(int interval, void (*callback)())
{
    timer2.callback = callback;
    timer2.interval = interval;
    timer2.tick = interval;
    timer2.enabled = 1;
}
