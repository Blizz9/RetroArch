#ifndef __RARCH_PARASITE_H
#define __RARCH_PARASITE_H

enum parasiteMessageType
{
   PARASITE_PING = 0x01,
   PARASITE_NO_OP = 0x02,
   PARASITE_PAUSE = 0x03,
   PARASITE_REQUEST_STATE = 0x04,
   PARASITE_STATE = 0x05,
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
