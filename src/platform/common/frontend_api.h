#ifndef FRONTEND_API_H
#define FRONTEND_API_H

#include "inc/dictsys.h"

FAR void GamFrontendSendKey(int key);
FAR void GamFrontendSendTouch(int event, int x, int y);
FAR int GamFrontendCopyFrameRGBA(U8 *outBuf, int outLen, int *outW, int *outH);

#endif
