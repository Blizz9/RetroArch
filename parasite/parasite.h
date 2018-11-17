#ifndef __RARCH_PARASITE_H
#define __RARCH_PARASITE_H

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
