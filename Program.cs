using Silk.NET.SDL;
using System.Diagnostics;
using Thread = System.Threading.Thread;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                                     Sdl.InitGamecontroller |
                                     Sdl.InitJoystick);
        if (sdlInitResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        using (var gameWindow = new GameWindow(sdl))
        {
            var input = new Input(sdl);
            var gameRenderer = new GameRenderer(sdl, gameWindow);
            var engine = new Engine(gameRenderer, input);

            engine.SetupWorld();

            int score = 0;
            int lives = 5;
            bool quit = false;
            var timer = new Stopwatch();
            timer.Start();
           
            while (!quit)
            {
                if (timer.Elapsed.TotalSeconds >= 15)
                {
                    Console.WriteLine("Timpul a expirat!");
                    Console.WriteLine($"Scor final: {score}");
                    Environment.Exit(0);
                }
                quit = input.ProcessInput();
                if (quit) break;

                if (input.IsSpacePressed())
                {
                    score++;
                    Console.WriteLine("Scor: " + score);
                    Thread.Sleep(150);
                }

                if (input.IsKeySPressed())
                {
                    if (lives > 0)
                    {
                        lives--;

                        if (lives == 0)
                        {
                            Console.WriteLine("Ai pierdut o viata! Vieti ramase: 0");
                            Console.WriteLine("Game Over!");
                            Console.WriteLine($"Scor final: {score}");
                            Console.WriteLine($"Timp total: {timer.Elapsed.Seconds} secunde");
                            Environment.Exit(0);

                        }
                        else
                        {
                            Console.WriteLine($"Ai pierdut o viata! Vieti ramase: {lives}");
                        }

                        Thread.Sleep(150);
                    }
                }

                engine.ProcessFrame();
                engine.RenderFrame();

                Thread.Sleep(13);
            }
        }
    }
}
