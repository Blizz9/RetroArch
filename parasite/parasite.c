#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "../core.h"
#include "../verbosity.h"

void parasite_test(void)
{
   // RARCH_LOG("parasite_test()\n");

   // char *message = "Hello Pipe";
   // FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "w");
   // fwrite(message, sizeof(char), strlen(message) + 1, file);
   // fclose(file);

   // RARCH_LOG("DONE\n");

   /*
   retro_ctx_size_info_t info;
   void *data  = NULL;

   core_serialize_size(&info);

   if (info.size == 0)
      return false;

      RARCH_LOG("%s: \"%s\".\n",
            msg_hash_to_str(MSG_SAVING_STATE),
            path);

   data = get_serialized_data(path, info.size) ;


      if (!data)
      {
         RARCH_ERR("%s \"%s\".\n",
               msg_hash_to_str(MSG_FAILED_TO_SAVE_STATE_TO),
               path);
         return false;
      }

      RARCH_LOG("%s: %d %s.\n",
            msg_hash_to_str(MSG_STATE_SIZE),
            (int)info.size,
            msg_hash_to_str(MSG_BYTES));
   */

   RARCH_LOG("HERE 1\n");

   retro_ctx_size_info_t info;
   core_serialize_size(&info);

   if (info.size == 0)
      return;

   RARCH_LOG("HERE 2: %d\n", info.size);

   retro_ctx_serialize_info_t serial_info;
   bool ret    = false;
   void *data  = NULL;

   data = malloc(info.size);

   if (!data)
      return;

   RARCH_LOG("HERE 3\n");

   serial_info.data = data;
   serial_info.size = info.size;
   ret = core_serialize(&serial_info);
   if (!ret)
   {
      free(data);
      return;
   }

   RARCH_LOG("HERE 4\n");

   FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "w");
   fwrite(data, sizeof(char), info.size, file);
   fclose(file);

   RARCH_LOG("DONE\n");

   //return data;
}
