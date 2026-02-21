#include <stdio.h>
#include <string.h>

#include "baye/comm.h"

FAR void GamBaYeEng(void);

static void usage(const char *prog)
{
    fprintf(
        stderr,
        "Usage: %s [--dat <path>] [--font-dir <path>] [--data-dir <path>] [--run-engine]\n"
        "\n"
        "Default mode: init-only smoke test (GamConInit + GamConRst).\n"
        "Use --run-engine to enter full engine loop (Godot runtime style).\n",
        prog
    );
}

int main(int argc, char **argv)
{
    const char *dat_path = "dist-win/dat.lib";
    const char *font_dir = "dist-win";
    const char *data_dir = ".";
    int run_engine = 0;
    int i;

    for (i = 1; i < argc; i++) {
        if (strcmp(argv[i], "--dat") == 0 && (i + 1) < argc) {
            dat_path = argv[++i];
            continue;
        }
        if (strcmp(argv[i], "--font-dir") == 0 && (i + 1) < argc) {
            font_dir = argv[++i];
            continue;
        }
        if (strcmp(argv[i], "--data-dir") == 0 && (i + 1) < argc) {
            data_dir = argv[++i];
            continue;
        }
        if (strcmp(argv[i], "--run-engine") == 0) {
            run_engine = 1;
            continue;
        }
        if (strcmp(argv[i], "-h") == 0 || strcmp(argv[i], "--help") == 0) {
            usage(argv[0]);
            return 0;
        }

        fprintf(stderr, "Unknown argument: %s\n", argv[i]);
        usage(argv[0]);
        return 1;
    }

    printf("[debug-host] dat=%s\n", dat_path);
    printf("[debug-host] font_dir=%s\n", font_dir);
    printf("[debug-host] data_dir=%s\n", data_dir);
    printf("[debug-host] mode=%s\n", run_engine ? "full-engine" : "init-only");

    GamSetResourcePath((const U8 *)dat_path, (const U8 *)font_dir);
    GamSetDataDir((const U8 *)data_dir);

    if (run_engine) {
        GamBaYeEng();
        return 0;
    }

    if (GamConInit() != 0) {
        fprintf(stderr, "[debug-host] GamConInit failed\n");
        return 2;
    }
    printf("[debug-host] GamConInit ok\n");
    GamConRst();
    printf("[debug-host] GamConRst ok\n");
    return 0;
}
