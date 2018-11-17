#include <stdio.h>
#include <windows.h>

#include "../command.h"
#include "../core.h"
#include "parasite.h"
#include "../verbosity.h"

HANDLE parasitePipe;

void parasiteConnectPipe()
{
   if (parasitePipe == NULL)
   {
      RARCH_LOG("[parasite]: pipe is not yet connected, connecting it\n");
      parasitePipe = CreateFile(TEXT("\\\\.\\pipe\\RetroArchParasite"), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
      if (parasitePipe == INVALID_HANDLE_VALUE)
      {
         RARCH_ERR("[parasite]: unable to connect the pipe\n");
      }
   }
}

void parasiteSendMessage(struct parasiteMessage *message)
{
   uint8_t typeByte[1] = { message->type };

   uint8_t payloadSizeBytes[sizeof(size_t)];
   for (int i = 0; i < sizeof(size_t); i++)
      payloadSizeBytes[i] = (message->payloadSize >> (i * CHAR_BIT));

   int headerSize = sizeof(typeByte) + sizeof(payloadSizeBytes);
   uint8_t headerBytes[headerSize];
   memcpy(headerBytes, typeByte, sizeof(typeByte));
   memcpy(headerBytes + sizeof(typeByte), payloadSizeBytes, sizeof(payloadSizeBytes));

   DWORD writtenByteCount;
   WriteFile(parasitePipe, &headerBytes, sizeof(headerBytes), &writtenByteCount, NULL);
   // RARCH_LOG("[parasite]: sent message %02X|%d\n", message->type, message->payloadSize);
}

struct parasiteMessage *parasiteReceiveMessage()
{
   uint8_t messageHeaderBuffer[9];
   DWORD readByteCount;

   // RARCH_LOG("[parasite]: waiting to receive message header...");
   ReadFile(parasitePipe, messageHeaderBuffer, sizeof(messageHeaderBuffer), &readByteCount, NULL);
   size_t messagePayloadSize = messageHeaderBuffer[1] + (messageHeaderBuffer[2] << 8) + (messageHeaderBuffer[3] << 16) + (messageHeaderBuffer[4] << 24) + ((uint64_t)messageHeaderBuffer[5] << 32) + ((uint64_t)messageHeaderBuffer[6] << 40) + ((uint64_t)messageHeaderBuffer[7] << 48) + ((uint64_t)messageHeaderBuffer[8] << 56);
   // RARCH_LOG("message header received: %02X|%d\n", messageHeaderBuffer[0], messagePayloadSize);

   struct parasiteMessage *message = (struct parasiteMessage *)malloc(sizeof(struct parasiteMessage));
   message->type = messageHeaderBuffer[0];
   message->payloadSize = messagePayloadSize;

   if (messagePayloadSize > 0)
   {
      message->payload = (uint8_t *)malloc(message->payloadSize * sizeof(uint8_t));
      // RARCH_LOG("[parasite]: waiting to receive message payload...");
      ReadFile(parasitePipe, message->payload, sizeof(messagePayloadSize), &readByteCount, NULL);
      // RARCH_LOG("message payload received\n");
   }

   return (message);
}

void parasiteCheckForMessage()
{
   parasiteConnectPipe();

   struct parasiteMessage sendMessage;
   sendMessage.type = 0x01;
   sendMessage.payloadSize = 0;
   parasiteSendMessage(&sendMessage);
   // RARCH_LOG("[parasite]: sent message %02X|%d\n", sendMessage.type, sendMessage.payloadSize);

   struct parasiteMessage *receiveMessage = parasiteReceiveMessage();
   uint32_t receiveMessagePayloadUInt32 = receiveMessage->payload[0] + (receiveMessage->payload[1] << 8) + (receiveMessage->payload[2] << 16) + (receiveMessage->payload[3] << 24);
   // RARCH_LOG("[parasite]: received message: %02X|%d|%d\n", receiveMessage->type, receiveMessage->payloadSize, receiveMessagePayloadUInt32);

   if (receiveMessage->type == 0x03)
   {
      RARCH_LOG("[parasite]: received command to toggle pause\n");
      command_event(CMD_EVENT_PAUSE_TOGGLE, NULL);
   }

   if (receiveMessage->payloadSize > 0)
   {
      free(receiveMessage->payload);
   }
   free(receiveMessage);
}
