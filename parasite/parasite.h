#ifndef __RARCH_PARASITE_H
#define __RARCH_PARASITE_H

enum parasiteMessageType
{
   PARASITE_PING = 0x01,
   PARASITE_PONG = 0x02,
   PARASITE_PAUSE = 0x03,
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

void parasiteConnectPipe();
void parasiteSendMessage(struct parasiteMessage *message);
struct parasiteMessage *parasiteReceiveMessage();
void parasiteCheckForMessage();
void parasite_test(void);

#endif /* __RARCH_PARASITE_H */
