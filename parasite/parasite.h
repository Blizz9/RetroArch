#ifndef __RARCH_PARASITE_H
#define __RARCH_PARASITE_H

enum parasiteCommandType
{
   PARASITE_LOAD_ROM = 1,
   PARASITE_PAUSE_TOGGLE = 2,
};

void parasiteInit();
void parasiteClock(uint64_t);
void parasiteGameClock(uint64_t);
void parasiteHandleLoadROM(char *, char *);
void parasiteHandlePauseToggle();

#endif /* __RARCH_PARASITE_H */
