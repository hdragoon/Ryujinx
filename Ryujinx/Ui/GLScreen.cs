using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Configuration;
using Ryujinx.Graphics.OpenGL;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using System;
using System.Linq;
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

        private HotkeyButtons _prevHotkeyButtons = 0;

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
            HotkeyButtons currentHotkeyButtons = 0;

            ControllerButtons currentButtonKeyboard = 0;
            JoystickPosition  leftJoystickKeyboard;
            JoystickPosition  rightJoystickKeyboard;

            ControllerButtons[] currentButtonController = new ControllerButtons[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];
            JoystickPosition[]  leftJoystickController  = new JoystickPosition[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];
            JoystickPosition[]  rightJoystickController = new JoystickPosition[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];

            HLE.Input.Keyboard? hidKeyboard = null;

            int leftJoystickDxKeyboard  = 0;
            int leftJoystickDyKeyboard  = 0;
            int rightJoystickDxKeyboard = 0;
            int rightJoystickDyKeyboard = 0;

            int[] leftJoystickDxController  = new int[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];
            int[] leftJoystickDyController  = new int[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];
            int[] rightJoystickDxController = new int[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];
            int[] rightJoystickDyController = new int[ConfigurationState.Instance.Hid.JoystickConfig.Value.Count];

            // Keyboard Input
            if (_keyboard.HasValue)
            {
                KeyboardState keyboard = _keyboard.Value;

                // Normal Input
                currentHotkeyButtons  = KeyboardControls.GetHotkeyButtons(ConfigurationState.Instance.Hid.KeyboardConfig, keyboard);
                currentButtonKeyboard = KeyboardControls.GetButtons(ConfigurationState.Instance.Hid.KeyboardConfig, keyboard);

                if (ConfigurationState.Instance.Hid.EnableKeyboard)
                {
                    hidKeyboard = KeyboardControls.GetKeysDown(ConfigurationState.Instance.Hid.KeyboardConfig, keyboard);
                }

                (leftJoystickDxKeyboard,  leftJoystickDyKeyboard)  = KeyboardControls.GetLeftStick(ConfigurationState.Instance.Hid.KeyboardConfig, keyboard);
                (rightJoystickDxKeyboard, rightJoystickDyKeyboard) = KeyboardControls.GetRightStick(ConfigurationState.Instance.Hid.KeyboardConfig, keyboard);
            }

            if (!hidKeyboard.HasValue)
            {
                hidKeyboard = new HLE.Input.Keyboard
                {
                    Modifier = 0,
                    Keys     = new int[0x8]
                };
            }
            
            leftJoystickKeyboard = new JoystickPosition
            {
                Dx = leftJoystickDxKeyboard,
                Dy = leftJoystickDyKeyboard
            };

            rightJoystickKeyboard = new JoystickPosition
            {
                Dx = rightJoystickDxKeyboard,
                Dy = rightJoystickDyKeyboard
            };

            currentButtonKeyboard |= _device.Hid.UpdateStickButtons(leftJoystickKeyboard, rightJoystickKeyboard);

            BaseController keyboardController = _device.Hid.KeyboardController;
            keyboardController.SendInput(currentButtonKeyboard, leftJoystickKeyboard, rightJoystickKeyboard);

            // Controller Input
            for (int i = 0; i < ConfigurationState.Instance.Hid.JoystickConfig.Value.Count; i++)
            {
                Input.NpadController controllerInput = new Input.NpadController(ConfigurationState.Instance.Hid.JoystickConfig.Value[i]);

                currentButtonController[i] |= controllerInput.GetButtons();

                (leftJoystickDxController[i],  leftJoystickDyController[i])  = controllerInput.GetLeftStick();
                (rightJoystickDxController[i], rightJoystickDyController[i]) = controllerInput.GetRightStick();

                leftJoystickController[i] = new JoystickPosition
                {
                    Dx = leftJoystickDxController[i],
                    Dy = leftJoystickDyController[i]
                };

                rightJoystickController[i] = new JoystickPosition
                {
                    Dx = rightJoystickDxController[i],
                    Dy = rightJoystickDyController[i]
                };

                currentButtonController[i] |= _device.Hid.UpdateStickButtons(leftJoystickController[i], rightJoystickController[i]);

                BaseController controller = _device.Hid.Controllers[i];
                controller.SendInput(currentButtonController[i], leftJoystickController[i], rightJoystickController[i]);
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

            if (ConfigurationState.Instance.Hid.EnableKeyboard && hidKeyboard.HasValue)
            {
                _device.Hid.WriteKeyboard(hidKeyboard.Value);
            }

            // Toggle vsync
            if (currentHotkeyButtons.HasFlag(HotkeyButtons.ToggleVSync) &&
                !_prevHotkeyButtons.HasFlag(HotkeyButtons.ToggleVSync))
            {
                _device.EnableDeviceVsync = !_device.EnableDeviceVsync;
            }

            _prevHotkeyButtons = currentHotkeyButtons;
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
