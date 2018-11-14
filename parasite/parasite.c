#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>

#include "../core.h"
#include "../verbosity.h"

HANDLE parasitePipe;

void parasite_test(void)
{
   // retro_ctx_size_info_t sizeInfo;
   // core_serialize_size(&sizeInfo);
   // if (sizeInfo.size == 0)
   //    return;

   // void *data = NULL;
   // data = malloc(sizeInfo.size);
   // if (!data)
   //    return;

   // retro_ctx_serialize_info_t serializeInfo;
   // serializeInfo.data = data;
   // serializeInfo.size = sizeInfo.size;

   // if (!core_serialize(&serializeInfo))
   // {
   //    free(data);
   //    return;
   // }

   // FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "wb");
   // fwrite(data, sizeof(char), sizeInfo.size, file);
   // fclose(file);

   // free(data);

   if (parasitePipe == NULL)
   {
      RARCH_LOG("Parasite pipe is NULL, initializing it.\n");
      parasitePipe = CreateFile(TEXT("\\\\.\\pipe\\RetroArchParasite"), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
      if (parasitePipe == INVALID_HANDLE_VALUE)
      {
         RARCH_LOG("ERROR: Unable ot create parasite pipe.\n");
      }
   }
   else
   {
      RARCH_LOG("Parasite pipe is already initialized.\n");
   }

   retro_ctx_size_info_t sizeInfo;
   core_serialize_size(&sizeInfo);

   uint8_t commandByte[1] = { 0x01 };

   uint8_t sizeBytes[sizeof(sizeInfo.size)];
   for (int i = 0; i < sizeof(sizeInfo.size); i++)
      sizeBytes[i] = (sizeInfo.size >> (i * CHAR_BIT));

   int headerSize = sizeof(commandByte) + sizeof(sizeBytes);
   uint8_t headerBytes[headerSize];
   memcpy(headerBytes, commandByte, sizeof(commandByte));
   memcpy(headerBytes + sizeof(commandByte), sizeBytes, sizeof(sizeBytes));

   DWORD writtenByteCount;
   WriteFile(parasitePipe, &headerBytes, sizeof(headerBytes), &writtenByteCount, NULL);
   RARCH_LOG("Wrote to parasite pipe.\n");

   char readBuffer[12];
   DWORD dwRead;
   RARCH_LOG("Reading from parasite pipe...\n");
   // while (ReadFile(parasitePipe, readBuffer, sizeof(readBuffer), &dwRead, NULL) != FALSE)
   // {
   //    RARCH_LOG("Read data from parasite pipe.\n");
   //    RARCH_LOG("%s\n", readBuffer);
   //    break;
   // }

   ReadFile(parasitePipe, readBuffer, sizeof(readBuffer), &dwRead, NULL);
   RARCH_LOG("Read data from parasite pipe.\n");
   RARCH_LOG("%s\n", readBuffer);
}
