using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Graphics.OpenGL;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using System;
using System.Threading;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Ryujinx.Ui
{
    public class GlScreen : GameWindow
    {
        private const int TouchScreenWidth  = 1280;
        private const int TouchScreenHeight = 720;

        private const int TargetFps = 60;

        private Switch _device;

        private Renderer _renderer;

        private HotkeyButtons[] _prevHotkeyButtons;

        private KeyboardState? _keyboard = null;

        private MouseState? _mouse = null;

        private Thread _renderThread;

        private bool _resizeEvent;

        private bool _titleEvent;

        private string _newTitle;

        public GlScreen(Switch device)
            : base(1280, 720,
            new GraphicsMode(), "Ryujinx", 0,
            DisplayDevice.Default, 3, 3,
            GraphicsContextFlags.ForwardCompatible)
        {
            _device = device;

            if (!(device.Gpu.Renderer is Renderer))
            {
                throw new NotSupportedException($"GPU renderer must be an OpenGL renderer when using GlScreen!");
            }

            _renderer = (Renderer)device.Gpu.Renderer;

            _prevHotkeyButtons = new HotkeyButtons[ConfigurationState.Instance.Hid.InputConfig.Value.Count];

            Location = new Point(
                (DisplayDevice.Default.Width  / 2) - (Width  / 2),
                (DisplayDevice.Default.Height / 2) - (Height / 2));
        }

        private void RenderLoop()
        {
            MakeCurrent();

            _renderer.Initialize();

            Stopwatch chrono = new Stopwatch();

            chrono.Start();

            long ticksPerFrame = Stopwatch.Frequency / TargetFps;

            long ticks = 0;

            while (Exists && !IsExiting)
            {
                if (_device.WaitFifo())
                {
                    _device.ProcessFrame();
                }

                if (_resizeEvent)
                {
                    _resizeEvent = false;

                    _renderer.Window.SetSize(Width, Height);
                }

                ticks += chrono.ElapsedTicks;

                chrono.Restart();

                if (ticks >= ticksPerFrame)
                {
                    RenderFrame();

                    // Queue max. 1 vsync
                    ticks = Math.Min(ticks - ticksPerFrame, ticksPerFrame);
                }
            }

            _device.DisposeGpu();
        }

        public void MainLoop()
        {
            VSync = VSyncMode.Off;

            Visible = true;

            Context.MakeCurrent(null);

            // OpenTK doesn't like sleeps in its thread, to avoid this a renderer thread is created
            _renderThread = new Thread(RenderLoop)
            {
                Name = "GUI.RenderThread"
            };

            _renderThread.Start();

            while (Exists && !IsExiting)
            {
                ProcessEvents();

                if (!IsExiting)
                {
                    UpdateFrame();

                    if (_titleEvent)
                    {
                        _titleEvent = false;

                        Title = _newTitle;
                    }
                }

                // Polling becomes expensive if it's not slept
                Thread.Sleep(1);
            }
        }

        private new void UpdateFrame()
        {
            int numControllers = ConfigurationState.Instance.Hid.InputConfig.Value.Count;

            HotkeyButtons[] currentHotkeyButtons = new HotkeyButtons[numControllers];
            ControllerButtons[] currentButton    = new ControllerButtons[numControllers];
            JoystickPosition[]  leftJoystick     = new JoystickPosition[numControllers];
            JoystickPosition[]  rightJoystick    = new JoystickPosition[numControllers];
            HLE.Input.Keyboard?[] hidKeyboard    = new HLE.Input.Keyboard?[numControllers];

            int[] leftJoystickDx  = new int[numControllers];
            int[] leftJoystickDy  = new int[numControllers];
            int[] rightJoystickDx = new int[numControllers];
            int[] rightJoystickDy = new int[numControllers];

            for (int i = 0; i < numControllers; i++)
            {
                if (ConfigurationState.Instance.Hid.InputConfig.Value[i] is NpadKeyboard keyboardController)
                {
                    // Keyboard Input
                    KeyboardController keyboardInput = new KeyboardController(keyboardController);

                    if (_keyboard.HasValue)
                    {
                        KeyboardState keyboard = _keyboard.Value;

                        currentHotkeyButtons[i] = keyboardInput.GetHotkeyButtons(keyboard);
                        currentButton[i]        = keyboardInput.GetButtons(keyboard);

                        if (ConfigurationState.Instance.Hid.EnableKeyboard)
                        {
                            hidKeyboard[i] = keyboardInput.GetKeysDown(keyboard);
                        }

                        (leftJoystickDx[i],  leftJoystickDy[i])  = keyboardInput.GetLeftStick(keyboard);
                        (rightJoystickDx[i], rightJoystickDy[i]) = keyboardInput.GetRightStick(keyboard);
                    }

                    if (hidKeyboard[i] == null)
                    {
                        hidKeyboard[i] = new HLE.Input.Keyboard
                        {
                            Modifier = 0,
                            Keys     = new int[0x8]
                        };
                    }

                    if (ConfigurationState.Instance.Hid.EnableKeyboard && hidKeyboard[i] != null)
                    {
                        _device.Hid.WriteKeyboard(hidKeyboard[i].Value);
                    }

                    // Toggle vsync
                    if (currentHotkeyButtons[i].HasFlag(HotkeyButtons.ToggleVSync) &&
                        !_prevHotkeyButtons[i].HasFlag(HotkeyButtons.ToggleVSync))
                    {
                        _device.EnableDeviceVsync = !_device.EnableDeviceVsync;
                    }

                    _prevHotkeyButtons[i] = currentHotkeyButtons[i];
                }
                else if (ConfigurationState.Instance.Hid.InputConfig.Value[i] is Common.Configuration.Hid.NpadController joystickController)
                {
                    // Controller Input
                    JoystickController controllerInput = new JoystickController(joystickController);

                    currentButton[i] |= controllerInput.GetButtons();

                    (leftJoystickDx[i],  leftJoystickDy[i])  = controllerInput.GetLeftStick();
                    (rightJoystickDx[i], rightJoystickDy[i]) = controllerInput.GetRightStick();
                }

                leftJoystick[i] = new JoystickPosition
                {
                    Dx = leftJoystickDx[i],
                    Dy = leftJoystickDy[i]
                };

                rightJoystick[i] = new JoystickPosition
                {
                    Dx = rightJoystickDx[i],
                    Dy = rightJoystickDy[i]
                };

                currentButton[i] |= _device.Hid.UpdateStickButtons(leftJoystick[i], rightJoystick[i]);

                _device.Hid.Controllers[i].SendInput(currentButton[i], leftJoystick[i], rightJoystick[i]);
            }

            //Touchscreen
            bool hasTouch = false;

            // Get screen touch position from left mouse click
            // OpenTK always captures mouse events, even if out of focus, so check if window is focused.
            if (Focused && _mouse?.LeftButton == ButtonState.Pressed)
            {
                MouseState mouse = _mouse.Value;

                int scrnWidth  = Width;
                int scrnHeight = Height;

                if (Width > (Height * TouchScreenWidth) / TouchScreenHeight)
                {
                    scrnWidth = (Height * TouchScreenWidth) / TouchScreenHeight;
                }
                else
                {
                    scrnHeight = (Width * TouchScreenHeight) / TouchScreenWidth;
                }

                int startX = (Width  - scrnWidth)  >> 1;
                int startY = (Height - scrnHeight) >> 1;

                int endX = startX + scrnWidth;
                int endY = startY + scrnHeight;

                if (mouse.X >= startX &&
                    mouse.Y >= startY &&
                    mouse.X <  endX   &&
                    mouse.Y <  endY)
                {
                    int scrnMouseX = mouse.X - startX;
                    int scrnMouseY = mouse.Y - startY;

                    int mX = (scrnMouseX * TouchScreenWidth)  / scrnWidth;
                    int mY = (scrnMouseY * TouchScreenHeight) / scrnHeight;

                    TouchPoint currentPoint = new TouchPoint
                    {
                        X = mX,
                        Y = mY,

                        // Placeholder values till more data is acquired
                        DiameterX = 10,
                        DiameterY = 10,
                        Angle     = 90
                    };

                    hasTouch = true;

                    _device.Hid.SetTouchPoints(currentPoint);
                }
            }

            if (!hasTouch)
            {
                _device.Hid.SetTouchPoints();
            }
        }

        private new void RenderFrame()
        {
            _device.PresentFrame(SwapBuffers);

            _device.Statistics.RecordSystemFrameTime();

            double hostFps = _device.Statistics.GetSystemFrameRate();
            double gameFps = _device.Statistics.GetGameFrameRate();

            string titleNameSection = string.IsNullOrWhiteSpace(_device.System.TitleName) ? string.Empty
                : " | " + _device.System.TitleName;

            string titleIdSection = string.IsNullOrWhiteSpace(_device.System.TitleIdText) ? string.Empty
                : " | " + _device.System.TitleIdText.ToUpper();

            _newTitle = $"Ryujinx{titleNameSection}{titleIdSection} | Host FPS: {hostFps:0.0} | Game FPS: {gameFps:0.0} | " +
                $"Game Vsync: {(_device.EnableDeviceVsync ? "On" : "Off")}";

            _titleEvent = true;

            _device.System.SignalVsync();

            _device.VsyncEvent.Set();
        }

        protected override void OnUnload(EventArgs e)
        {
            _renderThread.Join();

            base.OnUnload(e);
        }

        protected override void OnResize(EventArgs e)
        {
            _resizeEvent = true;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            bool toggleFullscreen = e.Key == Key.F11 ||
                (e.Modifiers.HasFlag(KeyModifiers.Alt) && e.Key == Key.Enter);

            if (WindowState == WindowState.Fullscreen)
            {
                if (e.Key == Key.Escape || toggleFullscreen)
                {
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                if (e.Key == Key.Escape)
                {
                    Exit();
                }

                if (toggleFullscreen)
                {
                    WindowState = WindowState.Fullscreen;
                }
            }

            _keyboard = e.Keyboard;
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            _keyboard = e.Keyboard;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            _mouse = e.Mouse;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _mouse = e.Mouse;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            _mouse = e.Mouse;
        }
    }
}
