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
                                     Sdl.InitGamecontroller | Sdl.InitJoystick);
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

                engine.ProcessFrame();
                engine.RenderFrame();

                gameRenderer.RenderTextCrossPlatform($"Scor: {score}", 20, 20);

                Thread.Sleep(13);
            }
        }
    }
}
