#ifndef __RARCH_PARASITE_H
#define __RARCH_PARASITE_H

enum parasiteMessageType
{
   PARASITE_PING = 0x01,
   PARASITE_PONG = 0x02,
   PARASITE_PAUSE_TOGGLE = 0x03,
   PARASITE_REQUEST_STATE = 0x04,
   PARASITE_STATE = 0x05,
   PARASITE_REQUEST_SCREEN = 0x06,
   PARASITE_SCREEN = 0x07,
};

struct parasiteMessage
{
   uint8_t type;
   size_t payloadSize;
   uint8_t *payload;
};

struct parasiteScreenPayload
{
   unsigned pixelFormat;
   unsigned width;
   unsigned height;
   unsigned pitch;
   const void *screen;
};

void parasiteConnectPipe();
void parasitePingDriver();
void parasiteSendMessage(struct parasiteMessage *message);
struct parasiteMessage *parasiteReceiveMessage();

void parasiteHandlePong(struct parasiteMessage *message);
void parasiteHandlePauseToggle(struct parasiteMessage *message);
void parasiteHandleRequestState(struct parasiteMessage *message);
void parasiteHandleRequestScreen(struct parasiteMessage *message);

int parasitePackBytes(void *buffer, int index, uint8_t *bytes, size_t sizeOfBytes);
int parasitePackUint8(void *buffer, int index, uint8_t value);
int parasitePackSize(void *buffer, int index, size_t value);
int parasitePackUnsigned(void *buffer, int index, unsigned value);

#endif /* __RARCH_PARASITE_H */
