//
//  main.c
//  baye-ios
//
//  Created by loong on 16/6/29.
//
//

#include	"inc/dictsys.h"
#include	"baye/comm.h"
#include	"baye/bind-objects.h"
#include	"baye/data-bind.h"
#include	"baye/script.h"

#include	"exportjs.c"

FAR U8 GamConInit(void);
FAR void GamBaYeEng(void);

void GamSetLcdFlushCallback(void(*lcd_fluch_cb)(char*buffer));

static void _lcd_flush_cb(char*buffer) {
    static int flushed = 0;
    if (flushed == 0) {
        printf("[web] first lcd flush callback\n");
    }
    flushed += 1;
    EM_ASM({
        if (typeof window !== "undefined" && window.bayeFlushLcdBuffer) {
            window.bayeFlushLcdBuffer($0);
        } else if (typeof globalThis !== "undefined" && globalThis.bayeFlushLcdBuffer) {
            globalThis.bayeFlushLcdBuffer($0);
        }
    }, buffer);
}

void baye_init_for_js(void) {
    GamSetResourcePath((U8*)"/rom/dat.lib", (U8*)"/rom/fonts");
    GamSetAltLibPath((U8*)"/data/dat.lib");
    GamSetDataDir((U8*)"/data/");
    GamSetLcdFlushCallback(_lcd_flush_cb);
    GamConInit();
}

int main(int argc, char*argv[])
{
    EM_ASM({
        if (typeof window !== "undefined" && window.bayeStart) {
            window.bayeStart();
        } else if (typeof globalThis !== "undefined" && globalThis.bayeStart) {
            globalThis.bayeStart();
        }
    });
    emscripten_sleep(1); // give javascript chance to run init
    baye_init_for_js();
    GamBaYeEng();

    EM_ASM({
        if (typeof window !== "undefined" && window.bayeExit) {
            window.bayeExit();
        } else if (typeof globalThis !== "undefined" && globalThis.bayeExit) {
            globalThis.bayeExit();
        }
    });
    return 0;
}
