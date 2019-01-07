#include <stdio.h>
#include <windows.h>

#include "../command.h"
#include "../core.h"
#include "../gfx/video_driver.h"
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
   uint8_t messageBytes[headerSize + message->payloadSize];
   memcpy(messageBytes, typeByte, sizeof(typeByte));
   memcpy(messageBytes + sizeof(typeByte), payloadSizeBytes, sizeof(payloadSizeBytes));
   if (message->payloadSize > 0)
      memcpy(messageBytes + headerSize, message->payload, message->payloadSize);

   DWORD writtenByteCount;
   WriteFile(parasitePipe, &messageBytes, sizeof(messageBytes), &writtenByteCount, NULL);
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

   struct parasiteMessage pingMessage;
   pingMessage.type = PARASITE_PING;
   pingMessage.payloadSize = 0;
   parasiteSendMessage(&pingMessage);
   // RARCH_LOG("[parasite]: sent message %02X|%d\n", sendMessage.type, sendMessage.payloadSize);

   struct parasiteMessage *receivedMessage = parasiteReceiveMessage();
   // uint32_t receiveMessagePayloadUInt32 = receiveMessage->payload[0] + (receiveMessage->payload[1] << 8) + (receiveMessage->payload[2] << 16) + (receiveMessage->payload[3] << 24);
   // RARCH_LOG("[parasite]: received message: %02X|%d|%d\n", receiveMessage->type, receiveMessage->payloadSize, receiveMessagePayloadUInt32);

   if (receivedMessage->type == PARASITE_PAUSE)
   {
      RARCH_LOG("[parasite]: received message to toggle pause\n");
      command_event(CMD_EVENT_PAUSE_TOGGLE, NULL);
   }
   else if (receivedMessage->type == PARASITE_REQUEST_STATE)
   {
      RARCH_LOG("[parasite]: received message to send state\n");

      struct parasiteMessage stateMessage;
      stateMessage.type = PARASITE_STATE;

      retro_ctx_size_info_t serializeSizeInfo;
      core_serialize_size(&serializeSizeInfo);
      retro_ctx_serialize_info_t serializeInfo;
      void *payload = malloc(serializeSizeInfo.size);
      serializeInfo.data = payload;
      serializeInfo.size = serializeSizeInfo.size;
      bool serializeSuccessful = core_serialize(&serializeInfo);

      stateMessage.payloadSize = serializeSizeInfo.size;
      stateMessage.payload = payload;

      parasiteSendMessage(&stateMessage);
   }
   else if (receivedMessage->type == PARASITE_REQUEST_SCREEN)
   {
      RARCH_LOG("[parasite]: received message to send screen\n");

      size_t pitch;
      unsigned width, height;
      const void *screen = NULL;

      video_driver_cached_frame_get(&screen, &width, &height, &pitch);

      unsigned pixelFormat = video_driver_get_pixel_format();
      uint8_t pixelFormatBytes[sizeof(pixelFormat)];
      for (int i = 0; i < sizeof(pixelFormat); i++)
         pixelFormatBytes[i] = (pixelFormat >> (i * CHAR_BIT));
      
      uint8_t widthBytes[sizeof(width)];
      for (int i = 0; i < sizeof(width); i++)
         widthBytes[i] = (width >> (i * CHAR_BIT));
      
      uint8_t heightBytes[sizeof(height)];
      for (int i = 0; i < sizeof(height); i++)
         heightBytes[i] = (height >> (i * CHAR_BIT));
      
      uint8_t pitchBytes[sizeof(pitch)];
      for (int i = 0; i < sizeof(pitch); i++)
         pitchBytes[i] = (pitch >> (i * CHAR_BIT));

      void *payload = malloc(sizeof(pixelFormat) + sizeof(width) + sizeof(height) + (pitch * height));
      memcpy(payload, pixelFormatBytes, sizeof(pixelFormat));
      memcpy(payload + sizeof(pixelFormat), widthBytes, sizeof(width));
      memcpy(payload + sizeof(pixelFormat) + sizeof(width), heightBytes, sizeof(height));
      memcpy(payload + sizeof(pixelFormat) + sizeof(width) + sizeof(height), pitchBytes, sizeof(height));
      memcpy(payload + sizeof(pixelFormat) + sizeof(width) + sizeof(height) + sizeof(pitch), screen, pitch * height);

      struct parasiteMessage screenMessage;
      screenMessage.type = PARASITE_SCREEN;
      screenMessage.payloadSize = sizeof(pixelFormat) + sizeof(width) + sizeof(height) + sizeof(pitch) + (pitch * height);
      screenMessage.payload = payload;
      parasiteSendMessage(&screenMessage);
   }
   else if (receivedMessage->type == PARASITE_PONG)
   {
   }
   else
   {
      parasitePipe = NULL;
   }

   if (receivedMessage->payloadSize > 0)
   {
      free(receivedMessage->payload);
   }
   free(receivedMessage);
}
