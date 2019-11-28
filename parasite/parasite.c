#include <stdio.h>
#include <windows.h>

#include "../command.h"
#include "../core.h"
#include "../dynamic.h"
#include "../gfx/video_driver.h"
#include "parasite.h"
#include "../tasks/tasks_internal.h"
#include "../verbosity.h"

dylib_t parasiteDLLHandle;
void (*parasiteInitFunc)();
void (*parasiteClockFunc)(uint64_t, int32_t *, char **, char **);
void (*parasiteGameClockFunc)(uint64_t, size_t, void *, unsigned, unsigned, unsigned, size_t, const void *);

void parasiteInit()
{
   if (parasiteDLLHandle == NULL)
   {
      RARCH_LOG("[PARASITE]: Initializing parasite\n");

      parasiteDLLHandle = dylib_load("ParasiteLib.dll");

      parasiteInitFunc = (void (*)())dylib_proc(parasiteDLLHandle, "Init");
      parasiteClockFunc = (void (*)(uint64_t, int32_t *, char **, char **))dylib_proc(parasiteDLLHandle, "Clock");
      parasiteGameClockFunc = (void (*)(uint64_t, size_t, void *, unsigned, unsigned, unsigned, size_t, const void *))dylib_proc(parasiteDLLHandle, "GameClock");

      parasiteInitFunc();
   }
}

void parasiteClock(uint64_t frameCount)
{
   int32_t command = 0;
   char *arg0 = NULL;
   char *arg1 = NULL;
   parasiteClockFunc(frameCount, &command, &arg0, &arg1);

   if (command == PARASITE_LOAD_ROM)
   {
      parasiteHandleLoadROM(arg0, arg1);
   }

   if (command == PARASITE_PAUSE_TOGGLE)
   {
      parasiteHandlePauseToggle();
   }
}

void parasiteGameClock(uint64_t frameCount)
{
   retro_ctx_size_info_t serializeSizeInfo;
   core_serialize_size(&serializeSizeInfo);
   retro_ctx_serialize_info_t serializeInfo;
   serializeInfo.data = malloc(serializeSizeInfo.size);
   serializeInfo.size = serializeSizeInfo.size;
   bool serializeSuccessful = core_serialize(&serializeInfo);

   size_t pitch;
   unsigned width, height;
   const void *screen = NULL;
   unsigned pixelFormat = video_driver_get_pixel_format();
   video_driver_cached_frame_get(&screen, &width, &height, &pitch);

   parasiteGameClockFunc(frameCount, serializeInfo.size, serializeInfo.data, pixelFormat, width, height, pitch, screen);
}

void parasiteHandleLoadROM(char *corePath, char *romPath)
{
   content_ctx_info_t content_info;
   content_info.argc = 0;
   content_info.argv = NULL;
   content_info.args = NULL;
   content_info.environ_get = NULL;
   task_push_load_content_with_new_core_from_menu(corePath, romPath, &content_info, CORE_TYPE_PLAIN, NULL, NULL);
}

void parasiteHandlePauseToggle()
{
   command_event(CMD_EVENT_PAUSE_TOGGLE, NULL);
}
