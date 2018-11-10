#include <windows.h> 
#include <stdio.h> 
#include <tchar.h>

#include "../verbosity.h"

HANDLE pipe;
bool initialized = false;

void parasite_test(void)
{
   RARCH_LOG("parasite_test()\n");

/*
   HANDLE hPipe;
   DWORD dwWritten;

   hPipe = CreateFile(TEXT("\\\\.\\pipe\\RetroArchParasite"), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
   if (hPipe == INVALID_HANDLE_VALUE)
   {
      RARCH_LOG("Error: %d\n", GetLastError());
   }
   else
   {
      WriteFile(hPipe, "Hello Pipe\n", 12, &dwWritten, NULL);
      CloseHandle(hPipe);
   }
*/

/*
   char buf[100];
   LPTSTR lpszPipename1 = TEXT("\\\\.\\pipe\\LogPipe");
   DWORD cbWritten;
   DWORD dwBytesToWrite = (DWORD)strlen(buf);
   HANDLE hPipe1 = CreateFile(lpszPipename1, GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);

   if (hPipe1 == NULL || hPipe1 == INVALID_HANDLE_VALUE)
   {
      RARCH_LOG("Error: %d\n", GetLastError());
   }

   WriteFile(hPipe1, buf, dwBytesToWrite, &cbWritten, NULL);
   memset(buf,0xCC,100);
   
   CloseHandle(hPipe1);
*/

/*
   if (!initialized)
   {
      pipe = CreateFile("\\\\.\\pipe\\RetroArchParasite", GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

      if (pipe == INVALID_HANDLE_VALUE)
      {
         RARCH_LOG("Error: %d\n", GetLastError());
      }

      initialized = true;
   }

   char message [] = "Hi\n";

   DWORD numWritten;
   
   WriteFile(pipe, message, 3, &numWritten, NULL);

   //CloseHandle(pipe);
*/

   // if (file == NULL)
   FILE *file = fopen("\\\\.\\pipe\\RetroArchParasite", "w");
   RARCH_LOG("Size of: %d\n", sizeof(char));
   RARCH_LOG("Size of: %d\n", sizeof("Hello Pipe\n"));
   fwrite("Hello Pipe\n", sizeof(char), sizeof("Hello Pipe\n"), file);
   fclose(file);

/*
   int fd;
   char * myfifo = "\\\\.\\pipe\\LogPipe";
   
   // mkfifo(myfifo, 0666);
   
   fd = fopen(myfifo, O_WRONLY);
   fwrite(fd, "Hi", sizeof("Hi"));
   fclose(fd);
   
   // fp = fopen( myfifo , "w" );
   // fwrite("Hi" , 1 , sizeof("Hi") , fp );
   // fclose(fp);

   // unlink(myfifo);
*/
   RARCH_LOG("DONE\n");
}