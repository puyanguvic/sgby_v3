//
//  sys.c
//  baye-ios
//
//  Created by loong on 15/8/15.
//
//

#include <stdio.h>
#include "baye/compa.h"
#include "baye/comm.h"
#include "baye/fsys.h"
#include "timer.h"

static void(*_lcd_fluch_cb)(char*buffer);

#define SCR_W SCR_WID
#define SCR_H SCR_HGT
#define BYTES_PERLINE (SCR_LINE * 8 * AX_SCALE)

#define DOT 1
#define CLR 0
static char *static_buffer;
static char *backup_buffer;
static char isLcdDirty = 0;
static char *buffer;
static char *scr_buffer;
static size_t buffer_size;
U8 g_FlipDrawing = 0;

void screen_buffer_init(void) {
    static_buffer = gam_malloc(MAX_SCR_BUF_LEN);
    backup_buffer = gam_malloc(MAX_SCR_BUF_LEN);
    buffer = static_buffer;
    scr_buffer = static_buffer;
    buffer_size = MAX_SCR_BUF_LEN;
}

static U8 _insideScreen(PT x, PT y) {
    return x >= 0 && y >= 0 && x < g_screenWidth && y < g_screenHeight;
}

void GamSetLcdFlushCallback(void(*lcd_fluch_cb)(char*buffer))
{
    _lcd_fluch_cb = lcd_fluch_cb;
}

static void timed_flush_lcd()
{
    if (isLcdDirty && _lcd_fluch_cb) {
        _lcd_fluch_cb(scr_buffer);
        isLcdDirty = 0;
    }
}

FAR void logLcd()
{
    int x, y;
    int perLine = BYTES_PERLINE;
    for (y = 0; y < SCR_H; y++) {
        printf("%03d ", y);
        for (x = 0; x < SCR_W; x++) {
            bool pixel = buffer[perLine*y + x];
            printf("%s ", pixel ? "." : "#");
        }
        printf("\n");
    }
    printf("\n");
    printf("---------------%dx%d-------------------\n", SCR_W, SCR_H);
    printf("\n");
}

FAR void flushLcd()
{
    if (_lcd_fluch_cb && buffer == scr_buffer) {
        isLcdDirty = 1;
    }
}

FAR void logPicture(U8 wid, U8 hgt, U8* pic)
{
    int x, y;
    {
        int perLine = (wid + 7) / 8;
        for (y = 0; y < hgt; y++) {
            for (x = 0; x < wid; x++) {
                if (pic) {
                    bool pixel = pic[perLine*y + x/8] & (128 >> (x%8));
                    printf("%s", pixel ? "  " : "##");
                } else {
                    printf("@");
                }
            }
            printf("\n");
        }
        printf("\n");
        printf("----------------------------------\n");
        printf("\n");
    }
}


FAR void SysAscii(PT x,PT y,U8 asc)
{
}

FAR U8 SysGetKey(void)
{
    MsgType msg;
    GuiGetMsg(&msg);
    return msg.param;
}

FAR U8   SysGetKeySound(void)
{
    return 0;
}

FAR	U8 SysGetTimer1Number(void)
{
    return (U8)gam_timer_interval();
}

FAR	void SysIconAllClear(void)						/*清除所有icon显示,系统的除外 */
{
}

FAR void SysIconBattery(U8 data)
{
}

FAR	void SysLCDVoltage(U8 voltage)		/*voltage: 0 - 63 */
{
}

static void _dot(PT x, PT y, U8 color) {
    x *= AX_SCALE;
    y *= AX_SCALE;
    for (int i = 0; i < AX_SCALE; i++) {
        for (int j = 0; j < AX_SCALE; j++) {
            int ind = BYTES_PERLINE * (y+i) + (x+j);
            buffer[ind] = color;
        }
    }
}

static void _rdot(PT x, PT y) {
    x *= AX_SCALE;
    y *= AX_SCALE;
    for (int i = 0; i < AX_SCALE; i++) {
        for (int j = 0; j < AX_SCALE; j++) {
            int ind = BYTES_PERLINE * (y+i) + (x+j);
            buffer[ind] = !buffer[ind];
        }
    }
}

FAR void SysLcdPartClear(PT x1,PT y1,PT x2,PT y2)
{
    PT x, y;
    for (y = y1; y <= y2; y++) {
        for (x = x1; x <= x2; x++) {
            if (!_insideScreen(x, y)) continue;
            _dot(x, y, CLR);
        }
    }
    flushLcd();
}

FAR void SysLcdReverse(PT x1,PT y1,PT x2,PT y2)
{
    PT x, y;
    for (y = y1; y <= y2; y++) {
        for (x = x1; x <= x2; x++) {
            if (!_insideScreen(x, y)) continue;

            _rdot(x, y);
        }
    }
    flushLcd();
}

FAR void SysLine(PT x1,PT y1,PT x2,PT y2)
{
}

static void _timercb()
{
    MsgType msg;
    msg.type = VM_TIMER;
    GuiPushMsg(&msg);
}

FAR	void	SysMemInit(U16 start,U16 len)
{
    gam_timer_init();
    gam_timer_set_callback(_timercb);
    gam_timer2_open(3, timed_flush_lcd);
}

static void _decodePic(U8* dst, const U8*pic, PT w, PT h, U8 scale) {
    int ppl = (w + 7) / 8;
    PT x, y, W, H, X, Y;

    W = w * scale;
    H = h * scale;

    for (y = 0; y < h; y++) {
        for (x = 0; x < w; x++) {
            X = x * scale;
            Y = y * scale;
            
            U8 p = pic[ppl*y + x/8] & (128 >> (x%8));

            for (int j = 0; j < scale; j++) {
                for (int i = 0; i < scale; i++) {
                    dst[W*(Y+j) + X+i] = p;
                }
            }
        }
    }
}

FAR void SysPicture(PT sX, PT sY, PT eX, PT eY, U8*pic , U8 flag, U8 scale)
{
    int wid = eX - sX + 1;
    int hgt = eY - sY + 1;
    int x, y, X, Y;
    int scrPerLine = BYTES_PERLINE;
    static U8 *_buf = NULL;
    int pixs = 0;

    if (_buf == NULL) {
        _buf = gam_malloc(MAX_SCR_BUF_LEN);
    } 

    if (pic) {
        _decodePic(_buf, pic, wid, hgt, scale);
        pic = _buf;
    }

    wid *= scale;
    hgt *= scale;

    {
        int picPerLine = wid;

        for (y = 0; y < hgt; y++) {
            Y = sY*AX_SCALE + y;
            for (x = 0; x < wid; x++) {
                X = sX*AX_SCALE + x;
                unsigned char pixel0, pixel1;
                int ind = scrPerLine * Y + X;
                if (!_insideScreen(X/AX_SCALE, Y/AX_SCALE)) {
                    continue;
                }

                if (flag == 4) {
                    pixel1 = 0;
                }
                else {
                    
                    if (pic) {
                        PT rx, ry;
                        rx = (g_FlipDrawing & 0x01) ? wid-x-1 : x;
                        ry = (g_FlipDrawing & 0x02) ? hgt-y-1 : y;
                        pixel0 = pic[picPerLine*ry + rx];
                    }
                    else {
                        pixel0 = 0;
                    }
                    
                    {
                        pixel1 = buffer[ind];
                        
                        switch (flag) {
                            case 0: // Normal
                                pixel1 = pixel0;
                                break;
                            case 1: // &
                                pixel1 = pixel0 && pixel1;
                                break;
                            case 2: // |
                                pixel1 = pixel0 || pixel1;
                                break;
                            case 4: // clear
                                break;
                            default:
                                break;
                        }
                    }
                }
                buffer[ind] = !!pixel1;
                pixs ++;
            }
        }
    }
    flushLcd();
}

static inline void _pixel(PT x,PT y,U8 data)
{
    if (!_insideScreen(x, y)) return;

    _dot(x, y, data);
}

FAR void SysPutPixel(PT x,PT y,U8 data)
{
    _pixel(x, y, data);
    flushLcd();
}

FAR void SysRect(PT x1,PT y1,PT x2,PT y2)
{
    int x, y;
    y = y1;
    for (x = x1; x <= x2; x++) {
        _pixel(x, y, DOT);
    }
    y = y2;
    for (x = x1; x <= x2; x++) {
        _pixel(x, y, DOT);
    }
    x = x1;
    for (y = y1; y <= y2; y++) {
        _pixel(x, y, DOT);
    }
    x = x2;
    for (y = y1; y <= y2; y++) {
        _pixel(x, y, DOT);
    }
    flushLcd();
}

FAR void SysRectClear(PT x1,PT y1,PT x2,PT y2)
{
    SysLcdPartClear(x1, y1, x2, y2);
    flushLcd();
}

FAR void SysSetKeySound(U8 keySoundFlag)
{
}

FAR void SysTimer1Close(void)
{
    gam_timer_close();
}

FAR void SysTimer1Open(U8 times)
{
    gam_timer_open(times);
}

FAR void SysSaveScreen()
{
    memcpy(backup_buffer, scr_buffer, buffer_size);
}

FAR void SysRestoreScreen()
{
    memcpy(scr_buffer, backup_buffer, buffer_size);
}

FAR void SysAdjustLCDBuffer(int wid, int height)
{
    size_t sz = wid * height;

    if (sz <= buffer_size) {
        memset(scr_buffer, 0, sz);
        return;
    }

    if (scr_buffer && scr_buffer != static_buffer) {
        free(scr_buffer);
    }

    scr_buffer = gam_malloc(sz);
    memset(scr_buffer, 0, sz);
    buffer_size = sz;
}

FAR void SysSelectScreen(U8*scr)
{
    buffer = scr ? (char*)scr : scr_buffer;
}

FAR void SysCopyScreen(U8*scr)
{
    memcpy(scr_buffer, scr, buffer_size);
    isLcdDirty = 1;
}

FAR U8 *gam_freadall(gam_FILE *fhandle, U32 *datalen)
{
    U32 alloced = 1024*400;
    U8 buf[1024];
    U8 *rv = gam_malloc(alloced);
    U32 offset = 0;
    U32 cnt = 0;
    
    do {
        cnt = gam_fread(buf, 1, 1024, fhandle);
        if (offset + cnt > alloced) {
            alloced *= 2;
            rv = gam_realloc(rv, alloced);
        }
        memcpy(rv + offset, buf, cnt);
        offset += cnt;
    } while (cnt > 0);

    if (offset == alloced) {
        rv = gam_realloc(rv, offset+1);
    }
    rv[offset] = 0;

    if (datalen) {
        *datalen = offset;
    }

    printf("fread all:%d\n", offset);
    return rv;
}
