#include "frontend_api.h"

#include "baye/comm.h"
#include "touch.h"

FAR int GamCopyFrameRGBA(U8 *outBuf, int outLen, int *outW, int *outH);

FAR void GamFrontendSendKey(int key)
{
    MsgType msg;
    msg.type = VM_CHAR_FUN;
    msg.param = (U16)key;
    msg.param2.u32 = 0;
    GuiPushMsg(&msg);
}

FAR void GamFrontendSendTouch(int event, int x, int y)
{
    touchSendTouchEvent((U16)event, (I16)x, (I16)y);
}

FAR int GamFrontendCopyFrameRGBA(U8 *outBuf, int outLen, int *outW, int *outH)
{
    return GamCopyFrameRGBA(outBuf, outLen, outW, outH);
}
