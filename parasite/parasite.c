#include <stdio.h>
#include <string.h>

#include "../verbosity.h"

void parasite_test(void)
{
   RARCH_LOG("parasite_test()\n");

   char *message = "Hello Pipe";
   FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "w");
   fwrite(message, sizeof(char), strlen(message) + 1, file);
   fclose(file);

   RARCH_LOG("DONE\n");
}
