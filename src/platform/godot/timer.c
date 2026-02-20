#include "../common/timer.h"

#include <pthread.h>
#include <time.h>

#define TIMER_TICK_MS 5

typedef struct
{
    void (*callback)(void);
    int interval;
    int enabled;
    int tick;
} gam_timer_state_t;

static gam_timer_state_t timers[2] = {
    {NULL, 1, 0, 0},
    {NULL, 1, 0, 0},
};
static gam_timer_state_t timer2 = {NULL, 1, 0, 0};

static pthread_t timer_thread;
static int timer_thread_started = 0;
static int timer_thread_running = 0;
static pthread_mutex_t timer_lock = PTHREAD_MUTEX_INITIALIZER;

static void run_timer(gam_timer_state_t *t, void (**out_cb)(void))
{
    *out_cb = NULL;
    if (!t->enabled) {
        return;
    }
    if (t->tick <= 0) {
        *out_cb = t->callback;
        t->tick = t->interval;
    }
    t->tick--;
}

static void *timer_loop(void *_)
{
    struct timespec ts;
    (void)_;
    ts.tv_sec = 0;
    ts.tv_nsec = TIMER_TICK_MS * 1000 * 1000;

    while (timer_thread_running) {
        void (*cb0)(void);
        void (*cb1)(void);
        void (*cb2)(void);

        nanosleep(&ts, NULL);

        pthread_mutex_lock(&timer_lock);
        run_timer(&timers[0], &cb0);
        run_timer(&timers[1], &cb1);
        run_timer(&timer2, &cb2);
        pthread_mutex_unlock(&timer_lock);

        if (cb0) {
            cb0();
        }
        if (cb1) {
            cb1();
        }
        if (cb2) {
            cb2();
        }
    }
    return NULL;
}

void gam_timer_init()
{
    if (timer_thread_started) {
        return;
    }
    timer_thread_running = 1;
    timer_thread_started = 1;
    pthread_create(&timer_thread, NULL, timer_loop, NULL);
    pthread_detach(timer_thread);
}

void gam_timer_set_callback(U8 n, void (*cb)(void))
{
    if (n >= 2) {
        return;
    }
    pthread_mutex_lock(&timer_lock);
    timers[n].callback = cb;
    pthread_mutex_unlock(&timer_lock);
}

int gam_timer_open(U8 n, int interval)
{
    int prev = 0;
    if (n >= 2) {
        return prev;
    }

    pthread_mutex_lock(&timer_lock);
    if (timers[n].enabled) {
        prev = timers[n].interval;
    }
    if (interval <= 0) {
        timers[n].enabled = 0;
    } else {
        timers[n].interval = interval;
        timers[n].tick = interval;
        timers[n].enabled = 1;
    }
    pthread_mutex_unlock(&timer_lock);
    return prev;
}

void gam_timer_close(U8 n)
{
    if (n >= 2) {
        return;
    }
    pthread_mutex_lock(&timer_lock);
    timers[n].enabled = 0;
    pthread_mutex_unlock(&timer_lock);
}

U8 gam_check_timer_on(U8 n)
{
    U8 on = 0;
    if (n >= 2) {
        return 0;
    }
    pthread_mutex_lock(&timer_lock);
    on = timers[n].enabled ? 1 : 0;
    pthread_mutex_unlock(&timer_lock);
    return on;
}

int gam_timer_interval(U8 n)
{
    int interval = 0;
    if (n >= 2) {
        return 0;
    }
    pthread_mutex_lock(&timer_lock);
    interval = timers[n].interval;
    pthread_mutex_unlock(&timer_lock);
    return interval;
}

void gam_timer_set_interval(U8 n, int interval)
{
    if (n >= 2) {
        return;
    }
    pthread_mutex_lock(&timer_lock);
    timers[n].interval = interval;
    if (timers[n].tick > interval) {
        timers[n].tick = interval;
    }
    pthread_mutex_unlock(&timer_lock);
}

void gam_timer2_open(int interval, void (*callback)())
{
    pthread_mutex_lock(&timer_lock);
    timer2.callback = callback;
    timer2.interval = interval <= 0 ? 1 : interval;
    timer2.tick = timer2.interval;
    timer2.enabled = 1;
    pthread_mutex_unlock(&timer_lock);
}
