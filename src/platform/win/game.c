//
//  main.c
//  baye-ios
//
//  Created by loong on 16/6/29.
//
//

#include "inc/dictsys.h"
#include "baye/comm.h"
#include "baye/bind-objects.h"
#include "baye/data-bind.h"
#include "baye/script.h"

#include "../js/exportjs.c"

#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <wchar.h>

FAR U8 GamConInit(void);
FAR void GamBaYeEng(void);
void bayeShowStartupError(const wchar_t* msg);

#ifdef _WIN32
#ifndef WINAPI
#define WINAPI __stdcall
#endif
extern unsigned long WINAPI GetModuleFileNameW(void* hModule, wchar_t* lpFilename, unsigned long nSize);
extern int WINAPI SetCurrentDirectoryW(const wchar_t* lpPathName);
#endif

static int resolve_exe_dir(wchar_t* out, size_t outSize)
{
    wchar_t modulePath[1024] = {0};
    wchar_t* p1;
    wchar_t* p2;
    wchar_t* sep;
    size_t len;
#ifdef _WIN32
    unsigned long modulePathLen = GetModuleFileNameW(NULL, modulePath, sizeof(modulePath) / sizeof(modulePath[0]));
    if (modulePathLen == 0 || modulePathLen >= (sizeof(modulePath) / sizeof(modulePath[0]))) {
        return -1;
    }
    p1 = wcsrchr(modulePath, L'\\');
    p2 = wcsrchr(modulePath, L'/');
    sep = p1 > p2 ? p1 : p2;
    if (!sep) {
        return -1;
    }
#endif
    len = (size_t)(sep - modulePath);
    if (len == 0 || len >= outSize) {
        return -1;
    }
    wmemcpy(out, modulePath, len);
    out[len] = L'\0';
    return 0;
}

static void show_startup_error(const wchar_t* exeDir)
{
    wchar_t msg[1024];
    swprintf(
        msg,
        sizeof(msg) / sizeof(msg[0]),
        L"iBaye initialization failed.\n\n"
        L"Please make sure these files exist:\n"
        L"dat.lib\n"
        L"font.bin\n\n"
        L"Executable directory:\n%ls",
        exeDir
    );
    bayeShowStartupError(msg);
}

static void baye_init_for_win(void)
{
    wchar_t exeDir[1024] = L".";
    if (resolve_exe_dir(exeDir, sizeof(exeDir) / sizeof(exeDir[0])) == 0) {
        SetCurrentDirectoryW(exeDir);
    }

    GamSetResourcePath((U8*)"dat.lib", (U8*)".");
    // GamSetAltLibPath((U8*)"dat.lib");
    GamSetDataDir((U8*)".");
    if (GamConInit()) {
        fprintf(stderr, "init res failed!\n");
        show_startup_error(exeDir);
        exit(1);
    }
}

void baye_init_for_js(void) {
}

static void* bayeMain(void*_)
{
    baye_init_for_win();
    GamBaYeEng();
    exit(0);
    return NULL;
}

void bayeStart(void)
{
    pthread_t t;
    pthread_create(&t, NULL, bayeMain, NULL);
}
