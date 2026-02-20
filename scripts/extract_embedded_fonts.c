#include <errno.h>
#include <stdio.h>
#include <string.h>

typedef unsigned char U8;

static const U8 font24_cn_1[] =
#include "../src/platform/js/font24.1.bin.c"
;
static const U8 font24_cn_2[] =
#include "../src/platform/js/font24.2.bin.c"
;
static const U8 font24_cn_3[] =
#include "../src/platform/js/font24.3.bin.c"
;
static const U8 font24_cn_4[] =
#include "../src/platform/js/font24.4.bin.c"
;
static const U8 font24_en_1[] =
#include "../src/platform/js/font24_ascii.1.c"
;
static const U8 font24_en_2[] =
#include "../src/platform/js/font24_ascii.2.c"
;

static int write_blob(const char *out_dir, const char *name, const U8 *data, size_t len)
{
    char path[2048];
    FILE *fp;
    size_t written;

    if (snprintf(path, sizeof(path), "%s/%s", out_dir, name) >= (int)sizeof(path)) {
        fprintf(stderr, "path too long: %s/%s\n", out_dir, name);
        return 1;
    }

    fp = fopen(path, "wb");
    if (!fp) {
        fprintf(stderr, "open failed: %s (%s)\n", path, strerror(errno));
        return 1;
    }

    written = fwrite(data, 1, len, fp);
    fclose(fp);
    if (written != len) {
        fprintf(stderr, "write failed: %s\n", path);
        return 1;
    }
    return 0;
}

int main(int argc, char **argv)
{
    const char *out_dir;
    int rc = 0;

    if (argc < 2) {
        fprintf(stderr, "Usage: %s <output-dir>\n", argv[0]);
        return 2;
    }
    out_dir = argv[1];

    rc |= write_blob(out_dir, "font24.cn.1", font24_cn_1, sizeof(font24_cn_1));
    rc |= write_blob(out_dir, "font24.cn.2", font24_cn_2, sizeof(font24_cn_2));
    rc |= write_blob(out_dir, "font24.cn.3", font24_cn_3, sizeof(font24_cn_3));
    rc |= write_blob(out_dir, "font24.cn.4", font24_cn_4, sizeof(font24_cn_4));
    rc |= write_blob(out_dir, "font24.en.1", font24_en_1, sizeof(font24_en_1));
    rc |= write_blob(out_dir, "font24.en.2", font24_en_2, sizeof(font24_en_2));

    return rc ? 1 : 0;
}
