using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ParasiteDriver
{
    public class SMB
    {
        //private const byte RAM_OFFSET = 0x5D; // FCEUmm
        private const byte RAM_OFFSET = 0x38; // Nestopia
        private const short RAM_LENGTH = 0x0800;
        private const short MOVING_DIRECTION = 0x0045;
        private const short CURRENT_SCREEN = 0x071A;
        private const short INPUTS = 0x074A;
        private const short PLAYER_STATE = 0x0754; // 0 = big, 1 = small
        private const short POWERUP_STATE = 0x0756; // 0 = small, 1 = big, >=2 = fiery
        private const short LIVES = 0x075A;
        private const short COINS = 0x075E;
        private const short WORLD = 0x075F;
        private const short LEVEL = 0x0760;
        private const short GAME_LOADING_STATE = 0x0770; // 0 = not started, 1 = started
        private const short LEVEL_LOADING_STATE = 0x0772; // 0 = restart, 1 = starting, 3 = in-level
        private const short PLAYER_COUNT = 0x077A;
        private const short LEVEL_LOADING_TIMER = 0x07A0;
        private const short SCORE_MILLIONS = 0x07DD;
        private const short SCORE_HUNDRED_THOUSANDS = 0x07DE;
        private const short SCORE_TEN_THOUSANDS = 0x07DF;
        private const short SCORE_THOUSANDS = 0x07E0;
        private const short SCORE_HUNDREDS = 0x07E1;
        private const short SCORE_TENS = 0x07E2;
        private const short COINS_DISPLAY_TENS = 0x07ED;
        private const short COINS_DISPLAY_ONES = 0x07EE;

        private byte[] _lastFrameRAM;

        bool _deathOccurred;

        private class TourStopManifest
        {
            public int World;
            public int WorldDisplay;
            public int Level;
            public int LevelDisplay;
            public int CheckpointScreen;

            public TourStopManifest(int world, int worldDisplay, int level, int levelDisplay, int checkpointScreen)
            {
                World = world;
                WorldDisplay = worldDisplay;
                Level = level;
                LevelDisplay = levelDisplay;
                CheckpointScreen = checkpointScreen;
            }
        }

        private List<TourStopManifest> _tourStopManifests;

        public SMB(Driver driver)
        {
            _tourStopManifests = new List<TourStopManifest>();
            _tourStopManifests.Add(new TourStopManifest(0, 1, 0, 1, 5));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 0, 1, 0));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 2, 2, 6));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 2, 2, 0));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 3, 3, 4));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 3, 3, 0));
            _tourStopManifests.Add(new TourStopManifest(0, 1, 4, 4, 0));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 0, 1, 6));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 0, 1, 0));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 2, 2, 5));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 2, 2, 0));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 3, 3, 7));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 3, 3, 0));
            _tourStopManifests.Add(new TourStopManifest(1, 2, 4, 4, 0));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 0, 1, 6));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 0, 1, 0));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 1, 2, 6));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 1, 2, 0));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 2, 3, 4));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 2, 3, 0));
            _tourStopManifests.Add(new TourStopManifest(2, 3, 3, 4, 0));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 0, 1, 6));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 0, 1, 0));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 2, 2, 6));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 2, 2, 0));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 3, 3, 4));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 3, 3, 0));
            _tourStopManifests.Add(new TourStopManifest(3, 4, 4, 4, 0));

            driver.GameClock += handleFrame;
        }

        private void handleFrame(long frameCount, byte[] state)
        {
            byte[] ram = new byte[RAM_LENGTH];
            Array.Copy(state, RAM_OFFSET, ram, 0, RAM_LENGTH);

            if ((ram[LEVEL_LOADING_STATE] == 0) && ((_lastFrameRAM[LIVES] - 1) == ram[LIVES]))
                _deathOccurred = true;

            if (_deathOccurred && (ram[LEVEL_LOADING_STATE] == 1))
            {
                _deathOccurred = false;

                ram[PLAYER_STATE] = 0;
                ram[POWERUP_STATE] = 2;
                ram[LIVES] = 4;
                ram[COINS] = 90;
                ram[SCORE_MILLIONS] = 0;
                ram[SCORE_HUNDRED_THOUSANDS] = 0;
                ram[SCORE_TEN_THOUSANDS] = 0;
                ram[SCORE_THOUSANDS] = 0;
                ram[SCORE_HUNDREDS] = 0;
                ram[SCORE_TENS] = 0;
                ram[COINS_DISPLAY_TENS] = 9;
                ram[COINS_DISPLAY_ONES] = 0;

                Array.Copy(ram, 0, state, RAM_OFFSET, RAM_LENGTH);

                TourStopManifest tourStopManifest = _tourStopManifests.Where(tsm => ((tsm.World == ram[WORLD]) && (tsm.Level == ram[LEVEL]))).FirstOrDefault();
                bool isPastCheckpoint = false;
                if (tourStopManifest.CheckpointScreen > 0)
                    isPastCheckpoint = ram[CURRENT_SCREEN] >= tourStopManifest.CheckpointScreen ? true : false;

                string stateName = string.Format(@"{0}-{1}{2}.state", tourStopManifest.WorldDisplay, tourStopManifest.LevelDisplay, isPastCheckpoint ? " (checkpoint)" : "");

                File.WriteAllBytes(stateName, state);

                Console.WriteLine("Wrote state file: " + stateName);
            }

            _lastFrameRAM = ram;
        }
    }
}
