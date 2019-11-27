#include <stdio.h>
#include <windows.h>

#include "../command.h"
#include "../core.h"
#include "../dynamic.h"
#include "../gfx/video_driver.h"
#include "parasite.h"
#include "../tasks/tasks_internal.h"
#include "../verbosity.h"

HANDLE parasitePipe;

dylib_t parasiteDLLHandle;
bool gameLoaded;

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

void parasitePingDriver(uint64_t frameCount)
{
   parasiteConnectPipe();

   struct parasiteMessage pingMessage;
   pingMessage.type = PARASITE_PING;

   retro_ctx_size_info_t serializeSizeInfo;
   core_serialize_size(&serializeSizeInfo);

   pingMessage.payloadSize = sizeof(frameCount) + serializeSizeInfo.size;
   pingMessage.payload = malloc(pingMessage.payloadSize);

   int i = 0;
   i = parasitePackUint64(pingMessage.payload, i, frameCount);

   retro_ctx_serialize_info_t serializeInfo;
   serializeInfo.data = pingMessage.payload + i;
   serializeInfo.size = serializeSizeInfo.size;
   bool serializeSuccessful = core_serialize(&serializeInfo);

   parasiteSendMessage(&pingMessage);

   struct parasiteMessage *receivedMessage = parasiteReceiveMessage();
   switch (receivedMessage->type)
   {
      case PARASITE_PAUSE_TOGGLE:
         parasiteHandlePauseToggle(receivedMessage);
         break;

      case PARASITE_REQUEST_STATE:
         parasiteHandleRequestState(receivedMessage);
         break;

      case PARASITE_REQUEST_SCREEN:
         parasiteHandleRequestScreen(receivedMessage);
         break;

      case PARASITE_PONG:
         parasiteHandlePong(receivedMessage);
         break;

      default:
         parasitePipe = NULL;
   }

   if (receivedMessage->payloadSize > 0)
   {
      free(receivedMessage->payload);
   }
   free(receivedMessage);
}

void parasiteSendMessage(struct parasiteMessage *message)
{
   size_t sizeOfMessage = sizeof(message->type) + sizeof(message->payloadSize) + message->payloadSize;
   uint8_t *messageBuffer = malloc(sizeOfMessage);

   int i = 0;
   i = parasitePackUint8(messageBuffer, i, message->type);
   i = parasitePackSize(messageBuffer, i, message->payloadSize);
   if (message->payloadSize > 0)
      i = parasitePackBytes(messageBuffer, i, message->payload, message->payloadSize);

   DWORD writtenByteCount;
   WriteFile(parasitePipe, messageBuffer, sizeOfMessage, &writtenByteCount, NULL);
}

struct parasiteMessage *parasiteReceiveMessage()
{
   uint8_t messageHeaderBuffer[sizeof(uint8_t) + sizeof(size_t)];
   DWORD readByteCount;

   ReadFile(parasitePipe, messageHeaderBuffer, sizeof(messageHeaderBuffer), &readByteCount, NULL);
   size_t messagePayloadSize = messageHeaderBuffer[1] + (messageHeaderBuffer[2] << 8) + (messageHeaderBuffer[3] << 16) + (messageHeaderBuffer[4] << 24) + ((uint64_t)messageHeaderBuffer[5] << 32) + ((uint64_t)messageHeaderBuffer[6] << 40) + ((uint64_t)messageHeaderBuffer[7] << 48) + ((uint64_t)messageHeaderBuffer[8] << 56);

   struct parasiteMessage *message = (struct parasiteMessage *)malloc(sizeof(struct parasiteMessage));
   message->type = messageHeaderBuffer[0];
   message->payloadSize = messagePayloadSize;

   if (messagePayloadSize > 0)
   {
      message->payload = (uint8_t *)malloc(message->payloadSize * sizeof(uint8_t));
      ReadFile(parasitePipe, message->payload, sizeof(messagePayloadSize), &readByteCount, NULL);
   }

   return (message);
}

void parasiteHandlePong(struct parasiteMessage *message)
{
}

void parasiteHandlePauseToggle(struct parasiteMessage *message)
{
   RARCH_LOG("[parasite]: received message to toggle pause\n");
   command_event(CMD_EVENT_PAUSE_TOGGLE, NULL);
}

void parasiteHandleRequestState(struct parasiteMessage *message)
{
   // RARCH_LOG("[parasite]: received message to send state\n");
   
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

void parasiteHandleRequestScreen(struct parasiteMessage *message)
{
   // RARCH_LOG("[parasite]: received message to send screen\n");
   
   size_t pitch;
   unsigned width, height;
   const void *screen = NULL;
   
   unsigned pixelFormat = video_driver_get_pixel_format();
   video_driver_cached_frame_get(&screen, &width, &height, &pitch);
   
   size_t sizeOfPayload = sizeof(pixelFormat) + sizeof(width) + sizeof(height) + sizeof(pitch) + (pitch * height);
   uint8_t *payload = malloc(sizeOfPayload);
   
   int i = 0;
   i = parasitePackUnsigned(payload, i, pixelFormat);
   i = parasitePackUnsigned(payload, i, width);
   i = parasitePackUnsigned(payload, i, height);
   i = parasitePackUnsigned(payload, i, pitch);
   i = parasitePackBytes(payload, i, (uint8_t *)screen, (pitch * height));
   
   struct parasiteMessage screenMessage;
   screenMessage.type = PARASITE_SCREEN;
   screenMessage.payloadSize = sizeOfPayload;
   screenMessage.payload = payload;
   parasiteSendMessage(&screenMessage);
}

int parasitePackBytes(void *buffer, int index, uint8_t *bytes, size_t sizeOfBytes)
{
   memcpy(buffer + index, bytes, sizeOfBytes);
   return (index + sizeOfBytes);
}

int parasitePackUint8(void *buffer, int index, uint8_t value)
{
   uint8_t bytes[1] = { value };
   memcpy(buffer + index, bytes, sizeof(uint8_t));
   
   return (index + sizeof(uint8_t));
}

int parasitePackUint64(void *buffer, int index, uint64_t value)
{
   uint8_t bytes[sizeof(uint64_t)];
   for (int i = 0; i < sizeof(uint64_t); i++)
      bytes[i] = (value >> (i * CHAR_BIT));

   memcpy(buffer + index, bytes, sizeof(uint64_t));

   return (index + sizeof(uint64_t));
}

int parasitePackSize(void *buffer, int index, size_t value)
{
   uint8_t bytes[sizeof(size_t)];
   for (int i = 0; i < sizeof(size_t); i++)
      bytes[i] = (value >> (i * CHAR_BIT));

   memcpy(buffer + index, bytes, sizeof(size_t));

   return (index + sizeof(size_t));
}

int parasitePackUnsigned(void *buffer, int index, unsigned value)
{
   uint8_t bytes[sizeof(unsigned)];
   for (int i = 0; i < sizeof(unsigned); i++)
      bytes[i] = (value >> (i * CHAR_BIT));

   memcpy(buffer + index, bytes, sizeof(unsigned));

   return (index + sizeof(unsigned));
}

void parasiteInit()
{
   if (parasiteDLLHandle == NULL)
   {
      // parasiteInited = true;
      RARCH_LOG("[PARASITE]: Initializing parasite\n");
      // parasiteDLLHandle = dylib_load("D:\\Development\\C++\\RetroArch\\ParasiteTest\\bin\\x64\\Release\\ParasiteTest.dll");
      parasiteDLLHandle = dylib_load("ParasiteLib.dll");
      // void (*beginThreadFunc)();
      // beginThreadFunc = (void (*)())dylib_proc(parasiteDLL, "BeginThread");
      // beginThreadFunc();
      // dylib_close(parasiteDLL);
      // RARCH_LOG("[PARASITE]: Thread started\n");

      void (*initFunc)();
      initFunc = (void (*)())dylib_proc(parasiteDLLHandle, "Init");
      initFunc();
   }
}

/*
void parasiteCheckCounter()
{
   if (parasiteDLLHandle == NULL)
   {
      return;
   }

   dylib_t parasiteDLL = dylib_load("D:\\Development\\C++\\RetroArch\\ParasiteTest\\bin\\x64\\Release\\ParasiteTest.dll");
   int (*getCounterFunc)();
   getCounterFunc = (int (*)())dylib_proc(parasiteDLL, "GetCounter");
   int counter = getCounterFunc();
   dylib_close(parasiteDLL);
   RARCH_LOG("[PARASITE]: Counter: %d\n", counter);
}
*/

void parasiteClock()
{
   if (parasiteDLLHandle == NULL)
   {
      return;
   }

   void (*clockFunc)();
   clockFunc = (void (*)())dylib_proc(parasiteDLLHandle, "Clock");
   clockFunc();

   if (!gameLoaded)
   {
      content_ctx_info_t content_info;
      content_info.argc = 0;
      content_info.argv = NULL;
      content_info.args = NULL;
      content_info.environ_get = NULL;
      task_push_load_content_from_playlist_from_menu("D:\\Development\\C++\\RetroArch\\cores\\nestopia_libretro.dll", "D:\\Development\\C++\\RetroArch\\roms\\Super Mario Bros..zip", NULL, &content_info, NULL, NULL);
      gameLoaded = true;
   }
}

void parasiteGameClock(uint64_t frameCount)
{
   if (parasiteDLLHandle == NULL)
   {
      return;
   }

   retro_ctx_size_info_t serializeSizeInfo;
   core_serialize_size(&serializeSizeInfo);
   retro_ctx_serialize_info_t serializeInfo;
   // void *payload = malloc(serializeSizeInfo.size);
   serializeInfo.data = malloc(serializeSizeInfo.size);
   serializeInfo.size = serializeSizeInfo.size;
   bool serializeSuccessful = core_serialize(&serializeInfo);

   size_t pitch;
   unsigned width, height;
   const void *screen = NULL;
   
   unsigned pixelFormat = video_driver_get_pixel_format();
   video_driver_cached_frame_get(&screen, &width, &height, &pitch);
   
   // size_t sizeOfPayload = sizeof(pixelFormat) + sizeof(width) + sizeof(height) + sizeof(pitch) + (pitch * height);
   // uint8_t *payload = malloc(sizeOfPayload);
   
   // int i = 0;
   // i = parasitePackUnsigned(payload, i, pixelFormat);
   // i = parasitePackUnsigned(payload, i, width);
   // i = parasitePackUnsigned(payload, i, height);
   // i = parasitePackUnsigned(payload, i, pitch);
   // i = parasitePackBytes(payload, i, (uint8_t *)screen, (pitch * height));
   
   // struct parasiteMessage screenMessage;
   // screenMessage.type = PARASITE_SCREEN;
   // screenMessage.payloadSize = sizeOfPayload;
   // screenMessage.payload = payload;
   // parasiteSendMessage(&screenMessage);

   void (*gameClockFunc)(uint64_t frameCount, size_t stateSize, void *stateData, unsigned pixelFormat, unsigned width, unsigned height, size_t pitch, const void *screen);
   gameClockFunc = (void (*)(uint64_t, size_t, void *, unsigned, unsigned, unsigned, size_t, const void *))dylib_proc(parasiteDLLHandle, "GameClock");
   gameClockFunc(frameCount, serializeInfo.size, serializeInfo.data, pixelFormat, width, height, pitch, screen);
}

// void parasiteConsumeLogMessage()
// {
//    if (parasiteDLLHandle == NULL)
//    {
//       return;
//    }

//    const char *(*consumeLogMessageFunc)();
//    consumeLogMessageFunc = (const char *(*)())dylib_proc(parasiteDLLHandle, "ConsumeLogMessage");
//    const char *logMessage = consumeLogMessageFunc();
//    if (logMessage != NULL)
//    {
//       RARCH_LOG("[PARASITE]: LIB Log: %s\n", logMessage);
//    }
// }
