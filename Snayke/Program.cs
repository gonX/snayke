using System;
using System.Collections.Generic;
using System.Threading;

// no copyright, just attribute me if you use parts of my code - if it's open source, attribute me with at least an email in the code, if it's closed source, attribute me on the start screen of the program, regardless of it having any start screen or not
// Sebastian 'gonX' Jensen <gonX@overclocked.net>
// DISCLAIMER: there are no I/O operations other than writing to a console window 

namespace Snayke //TODO: add highscores
{
    class Program
    {
        //width by height, int arrays have 0 as default value
        //frame has 4 values, starting from 0: blank space, powerup, wall, snake body
        static int[,] frame; // used for internal collision detection
        static int VSize; //vertical size of window
        static int HSize; //horizontal size of window
        static int snakePos; // frame.GetLength(0) * 3 + 3 = upper left with 1 unit margin
        static IList<KeyValuePair<KeyValuePair<int, int>, int>> redrawtiles = new List<KeyValuePair<KeyValuePair<int, int>, int>>(); //TODO: seriously clean up this atrocity.. it probably works, but it's also probably slow as hell...

        static void Main()
        {
            Console.Title = "Snayke";
            Console.TreatControlCAsInput = true; // exception will occur if loop is terminated improperly, so this is necessary

            HSize = Console.WindowWidth;
            VSize = Console.WindowHeight;

            bool pausecheat = false;
            bool invertcolors = false;
            int hzupdaterate = 12; //updaterate in hertz

            bool menuactive = true;

            while (menuactive)
            {
                Console.CursorVisible = true;

                if (invertcolors && Console.BackgroundColor != ConsoleColor.White)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (!invertcolors && Console.BackgroundColor != ConsoleColor.Black)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                bool startgame = false;

                Console.Clear();

                Console.WriteLine("Welcome to Snayke!\n");

                Console.WriteLine("1: Start Game");
                Console.WriteLine("2: Difficulty: {0}", hzupdaterate);
                Console.WriteLine("3: Pause Cheat: {0}", pausecheat);
                Console.WriteLine("4: Invert Colors: {0}", invertcolors);
                Console.WriteLine("5: Exit\n");

                Console.WriteLine("You can additionally resize the console.\nIt is currently {0} by {1}\nYou cannot resize the console in-game\n", HSize, VSize);
                // Console.WriteLine("Hold down SHIFT to speed up your Snayke!");

                Console.Write("Option: ");
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo menuCki = Console.ReadKey();

                    int switchcase;
                    if (int.TryParse(menuCki.KeyChar.ToString(), out switchcase))
                    {
                        switch (switchcase)
                        {
                            case 1:
                                startgame = true;
                                break;
                            case 2:
                                Console.Write("\nEnter Difficulty: ");
                                int.TryParse(Console.ReadLine().Split('\n')[0], out hzupdaterate);
                                break;
                            case 3:
                                pausecheat = !pausecheat;
                                break;
                            case 4:
                                invertcolors = !invertcolors;
                                break;
                            case 5:
                                menuactive = false;
                                break;
                        }
                    }
                }

                Thread.Sleep(100);

                HSize = Console.WindowWidth;
                VSize = Console.WindowHeight;

                if (startgame)
                {
                    int totalframes = 0;
                    int length = 6; //arbitrary number
                    int actuallength = 2; //actual units for snake

                    Console.CursorVisible = false;

                    frame = new int[HSize, VSize - 1]; // minus one since the Windows console expects an empty line

                    List<KeyValuePair<int, int>> snakebits = new List<KeyValuePair<int, int>>(); //positions of current snake bits
                    if (generateFrame() != 0)
                        throwError("Could not generate frame!");

                    int direction = -1; // 0=up, 1=down, 2=left, 3=right, -1 = not moving, only used for starting position

                    snakePos = GenerateValidRandomSpot();
                    int lastposition = snakePos;
                    //tail of snake, set to snakePos since specifically this is only initialized once

                    redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>(
                                        new KeyValuePair<int, int>(getVPos(ref snakePos), getHPos(ref snakePos)),
                                        3)); //set snake field

                    bool gameactive = true;
                    MakeNewPowerUp();

                    while (gameactive) //game loop
                    {
                        //TODO: add speed-up button

                        if (Console.WindowHeight != VSize || Console.WindowWidth != HSize) //enforce window size
                            Console.SetWindowSize(HSize, VSize);

                        bool gamepaused = false;

                        if (Console.KeyAvailable)
                        {
                            ConsoleKeyInfo cki = Console.ReadKey(true);
                            if (cki.Key == ConsoleKey.UpArrow && direction != 1)
                                // noteq statements set in place to avoid turning into yourself
                                direction = 0;
                            else if (cki.Key == ConsoleKey.DownArrow && direction != 0)
                                direction = 1;
                            else if (cki.Key == ConsoleKey.LeftArrow && direction != 3)
                                direction = 2;
                            else if (cki.Key == ConsoleKey.RightArrow && direction != 2)
                                direction = 3;
                            else if (cki.Key == ConsoleKey.P)
                                gamepaused = true;
                            else if (cki.Key == ConsoleKey.C && cki.Modifiers == ConsoleModifiers.Control)
                                gameactive = false;
                            else if (cki.Key == ConsoleKey.Escape)
                            {
                                Console.Clear();
                                Console.WriteLine("\n  Do you want to stop playing? (y/N)");
                                if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                {
                                    gameactive = false;
                                }
                                else
                                {
                                    draw();
                                }
                            }
                        }

                        Thread.Sleep(1000 / hzupdaterate); //"set framerate"


                        if (gamepaused)
                        {
                            if (!pausecheat)
                            {
                                Console.Clear();
                            }
                            const string pausestring = "--- GAME PAUSED ---";
                            Console.SetCursorPosition((HSize / 2 - pausestring.Length / 2), VSize / 2);
                            Console.Write(pausestring);
                            while (gamepaused)
                            {
                                Thread.Sleep(200);
                                if (Console.KeyAvailable)
                                {
                                    ConsoleKeyInfo ck = Console.ReadKey(true);
                                    if (ck.Key == ConsoleKey.P || ck.Key == ConsoleKey.Escape)
                                    {
                                        Console.WriteLine("");
                                        for (int i = (pausecheat ? 100 : 300); i != 0; i--)
                                        {
                                            Console.SetCursorPosition(HSize / 2 - pausestring.Length / 2, VSize / 2 + 1);
                                            Console.WriteLine("Resuming in {0}...", i);
                                            Thread.Sleep(10);
                                        }

                                        gamepaused = false;
                                        draw();
                                    }
                                    else if (ck.Key == ConsoleKey.C && ck.Modifiers == ConsoleModifiers.Control) // control+C kills the game while paused
                                    {
                                        gamepaused = false;
                                        gameactive = false;
                                    }
                                }
                            }
                        }

                        switch (direction)
                        {
                            case -1:
                                redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>( //this is a bit of a hack, this is to ensure that we don't collide with ourselves before we have moved at all
                                                    new KeyValuePair<int, int>(getVPos(ref snakePos),
                                                                               getHPos(ref snakePos)),
                                                    0));
                                break;
                            case 0: //up
                                snakePos = snakePos - 1;
                                break;
                            case 1: //down
                                snakePos = snakePos + 1;
                                break;
                            case 2: //left
                                snakePos = snakePos - frame.GetLength(0);
                                break;
                            case 3: //right
                                snakePos = snakePos + frame.GetLength(0);
                                break;
                        }


                        if (frame[getVPos(ref snakePos), getHPos(ref snakePos)] >= 2)
                        // best collision detection ever, if non-powerup or non-blank space hit, you die
                        {
                            gameactive = false; // game over son
                            Console.Clear();
                            Console.WriteLine("lol you died\n\nTotal Frames Played: {0}\nLength: {1}\nDifficulty: {2}\nPause Cheat: {3}\n", totalframes, actuallength - 1,
                                              hzupdaterate, pausecheat);
                            Console.WriteLine("Returning to main menu in 5 seconds... or press enter to return now");
                            for (int i = 0; i <= 25; i++) //wait loop 
                            {
                                Thread.Sleep(200);
                                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                                    i = 60000;
                            }
                        }
                        else
                        {
                            
                            if (frame[getVPos(ref snakePos), getHPos(ref snakePos)] == 1)
                            //check for powerup, generate new if one is found.
                            {
                                length++;
                                MakeNewPowerUp();
                            }

                            KeyValuePair<int, int> currentPosKVP = new KeyValuePair<int, int>(getVPos(ref snakePos),
                                                                                              getHPos(ref snakePos));
                            // temp var

                            if (!snakebits.Contains(currentPosKVP)) //add current position to snake body parts
                                snakebits.Add(currentPosKVP);


                            if ((direction >= 0))
                            {
                                totalframes++;
                                if (actuallength == length)
                                {
                                    //frame[getVPos(ref lastposition), getHPos(ref lastposition)] = 0;
                                    redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>(
                                                        new KeyValuePair<int, int>(getVPos(ref lastposition),
                                                                                   getHPos(ref lastposition)),
                                                        0));

                                    snakebits.Remove(new KeyValuePair<int, int>(getVPos(ref lastposition),
                                                                                getHPos(ref lastposition)));
                                    lastposition = (snakebits[0].Key * frame.GetLength(0) + snakebits[0].Value);

                                    redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>(
                                                        new KeyValuePair<int, int>(getVPos(ref snakePos),
                                                                                   getHPos(ref snakePos)),
                                                        3));
                                }
                                else if (actuallength > length) // unit test
                                {
                                    throwError("somehow I ended up being longer than I should... game bug yo");
                                }
                                else
                                {
                                    //frame[getVPos(ref snakePos), getHPos(ref snakePos)] = 3; //update snake head location
                                    redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>(
                                                        new KeyValuePair<int, int>(getVPos(ref snakePos),
                                                                                   getHPos(ref snakePos)),
                                                        3));
                                    actuallength++;
                                }
                            }

                            if (redrawtiles.Count != 0)
                            {
                                foreach (KeyValuePair<KeyValuePair<int, int>, int> t in redrawtiles)
                                {
                                    Console.SetCursorPosition((t.Key.Key), t.Key.Value);
                                    Console.Write(ChangeTile(t.Value));
                                    frame[t.Key.Key, t.Key.Value] = t.Value;
                                }
                                redrawtiles.Clear();
                            }

                            if (direction == -1)
                            {
                                Console.SetCursorPosition(getVPos(ref snakePos), getHPos(ref snakePos));
                                Console.Write(ChangeTile(3));
                            }
                        }
                    }
                }
            }
        }

        private static int GenerateValidRandomSpot()
        {
            Thread.Sleep(5); //sleep to avoid calculating the same value if the code is run twice in a row in quick succession
            Random rnd = new Random((int)(DateTime.Now.Millisecond + DateTime.Now.Ticks));
            while (true)
            {
                int randomv = rnd.Next(1, frame.GetLength(0) - 1); //avoid creating it in a wall vertically
                int randomh = rnd.Next(1, frame.GetLength(1) - 1); //avoid creating it in a wall horizontally
                if (frame[randomv, randomh] == 0) // must be placed on blank space
                {
                    return randomv * frame.GetLength(0) + randomh;
                }
            }
        }

        private static void MakeNewPowerUp()
        {
            int validspot = GenerateValidRandomSpot();
            redrawtiles.Add(new KeyValuePair<KeyValuePair<int, int>, int>(
                                new KeyValuePair<int, int>(getVPos(ref validspot), getHPos(ref validspot)),
                                1));
        }


        static int getVPos(ref int aPosition) // current int position divided by frameheight
        {
            return aPosition / frame.GetLength(0);
        }

        static int getHPos(ref int aPosition) // current int position modulus by frameheight (the remainder of the division)
        {
            return aPosition % frame.GetLength(0);
        }

        static int generateFrame()
        {
            if (frame.GetLength(0) < 4 || frame.GetLength(1) < 4) //needs to be at least 4x4
                throwError("frame is too small!");

            for (int i = 0; i < frame.GetLength(0); i++)
            {
                int h = 0;
                if (i == 0 || i == frame.GetLength(0) - 1) //if top or bottom of window
                {
                    for (; h < frame.GetLength(1); ) //iterate over all values in this dimension of the array
                    {
                        frame.SetValue(2, i, h);
                        h++;
                    }
                }
                frame.SetValue(2, i, 0); //set leftmost side of this dimension to 1
                frame.SetValue(2, i, frame.GetLength(1) - 1); //set rightmost side to 1
            }
            draw();
            return 0;

        }

        static void throwError(string error)
        {
            Console.Clear();
            if (!error.EndsWith("\n")) //add newline to errors that do not have it
                error += "\n";

            Console.WriteLine("{0}Press any key to close", error);
            Console.ReadKey();
            Environment.Exit(1); //TODO: make descriptive int codes
        }


        private static string ChangeTile(int tile)
        {
            switch (tile)
            { //refer to frame[,] comment for advice on what the values are
                case 0:
                    return " ";
                case 1:
                    return "o";
                case 2:
                    return "X";
                case 3:
                    return "0";
                default:
                    return "?";
            }
        }

        private static void draw()
        {
            for (int v = 0; v < frame.GetLength(0); v++)
                for (int h = 0; h < frame.GetLength(1); h++)
                {
                    Console.SetCursorPosition(v, h);
                    Console.Write(ChangeTile(frame[v, h]));
                }
        }

    }
}
