#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "../core.h"
#include "../verbosity.h"

void parasite_test(void)
{
   retro_ctx_size_info_t sizeInfo;
   core_serialize_size(&sizeInfo);
   if (sizeInfo.size == 0)
      return;

   void *data = NULL;
   data = malloc(sizeInfo.size);
   if (!data)
      return;

   retro_ctx_serialize_info_t serializeInfo;
   serializeInfo.data = data;
   serializeInfo.size = sizeInfo.size;

   if (!core_serialize(&serializeInfo))
   {
      free(data);
      return;
   }

   FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "wb");
   fwrite(data, sizeof(char), sizeInfo.size, file);
   fclose(file);

   free(data);
}
