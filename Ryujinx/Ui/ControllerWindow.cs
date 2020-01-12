using Gtk;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration.Hid;

using GUI = Gtk.Builder.ObjectAttribute;
using Key = Ryujinx.Configuration.Hid.Key;

namespace Ryujinx.Ui
{
    public class ControllerWindow : Window
    {
        private static ControllerId _controllerId;
        private static ToggleButton _toggleButton;

#pragma warning disable CS0649
#pragma warning disable IDE0044
        [GUI] Window       _controllerWin;
        [GUI] Adjustment   _controllerDeadzoneLeft;
        [GUI] Adjustment   _controllerDeadzoneRight;
        [GUI] Adjustment   _controllerTriggerThreshold;
        [GUI] ComboBoxText _inputDevice;
        [GUI] ToggleButton _refreshInputDevicesButton;
        [GUI] Box          _settingsBox;
        [GUI] ComboBoxText _controllerType;
        [GUI] ToggleButton _lStickX;
        [GUI] ToggleButton _lStickY;
        [GUI] ToggleButton _lStickUp;
        [GUI] ToggleButton _lStickDown;
        [GUI] ToggleButton _lStickLeft;
        [GUI] ToggleButton _lStickRight;
        [GUI] ToggleButton _lStickButton;
        [GUI] ToggleButton _dpadUp;
        [GUI] ToggleButton _dpadDown;
        [GUI] ToggleButton _dpadLeft;
        [GUI] ToggleButton _dpadRight;
        [GUI] ToggleButton _minus;
        [GUI] ToggleButton _l;
        [GUI] ToggleButton _zL;
        [GUI] ToggleButton _rStickX;
        [GUI] ToggleButton _rStickY;
        [GUI] ToggleButton _rStickUp;
        [GUI] ToggleButton _rStickDown;
        [GUI] ToggleButton _rStickLeft;
        [GUI] ToggleButton _rStickRight;
        [GUI] ToggleButton _rStickButton;
        [GUI] ToggleButton _a;
        [GUI] ToggleButton _b;
        [GUI] ToggleButton _x;
        [GUI] ToggleButton _y;
        [GUI] ToggleButton _plus;
        [GUI] ToggleButton _r;
        [GUI] ToggleButton _zR;
        [GUI] ToggleButton _sL;
        [GUI] ToggleButton _sR;
        [GUI] Grid         _keyboardStickBox;
        [GUI] Grid         _controllerStickBox;
        [GUI] Box          _deadZoneLeftBox;
        [GUI] Box          _deadZoneRightBox;
        [GUI] Box          _triggerThresholdBox;
        [GUI] Image        _controllerImage;
#pragma warning restore CS0649
#pragma warning restore IDE0044

        public ControllerWindow(ControllerId controllerId) : this(new Builder("Ryujinx.Ui.ControllerWindow.glade"), controllerId) { }

        private ControllerWindow(Builder builder, ControllerId controllerId) : base(builder.GetObject("_controllerWin").Handle)
        {
            builder.Autoconnect(this);

            _controllerId = controllerId;

            _controllerWin.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            //Bind Events
            _lStickX.Clicked        += Button_Pressed;
            _lStickY.Clicked        += Button_Pressed;
            _lStickUp.Clicked       += Button_Pressed;
            _lStickDown.Clicked     += Button_Pressed;
            _lStickLeft.Clicked     += Button_Pressed;
            _lStickRight.Clicked    += Button_Pressed;
            _lStickButton.Clicked   += Button_Pressed;
            _dpadUp.Clicked         += Button_Pressed;
            _dpadDown.Clicked       += Button_Pressed;
            _dpadLeft.Clicked       += Button_Pressed;
            _dpadRight.Clicked      += Button_Pressed;
            _minus.Clicked          += Button_Pressed;
            _l.Clicked              += Button_Pressed;
            _zL.Clicked             += Button_Pressed;
            _sL.Clicked             += Button_Pressed;
            _rStickX.Clicked        += Button_Pressed;
            _rStickY.Clicked        += Button_Pressed;
            _rStickUp.Clicked       += Button_Pressed;
            _rStickDown.Clicked     += Button_Pressed;
            _rStickLeft.Clicked     += Button_Pressed;
            _rStickRight.Clicked    += Button_Pressed;
            _rStickButton.Clicked   += Button_Pressed;
            _a.Clicked              += Button_Pressed;
            _b.Clicked              += Button_Pressed;
            _x.Clicked              += Button_Pressed;
            _y.Clicked              += Button_Pressed;
            _plus.Clicked           += Button_Pressed;
            _r.Clicked              += Button_Pressed;
            _zR.Clicked             += Button_Pressed;
            _sR.Clicked             += Button_Pressed;

            // Setup current values
            _inputDevice.SetActiveId("disabled");
            UpdateInputDeviceList();
            SetAvailableOptions();
        }

        private void UpdateInputDeviceList()
        {
            string currentInputDevice = _inputDevice.ActiveId;

            _inputDevice.RemoveAll();
            _inputDevice.Append("disabled", "Disabled");
            _inputDevice.Append("keyboard", "Keyboard");

            /*for (int i = 0; Keyboard.GetState(i).IsConnected; i++)
            {
                _inputDevice.Append($"keyboard/{i}", $"Keyboard/{i}");
            }*/

            for (int i = 0; GamePad.GetState(i).IsConnected; i++)
            {
                _inputDevice.Append($"controller/{i}", $"Controller/{i} ({GamePad.GetName(i)})");
            }

            _inputDevice.SetActiveId(currentInputDevice);
        }

        private void SetAvailableOptions()
        {
            if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("keyboard"))
            {
                _settingsBox.Show();
                _keyboardStickBox.Show();
                _controllerStickBox.Hide();
                _deadZoneLeftBox.Hide();
                _deadZoneRightBox.Hide();
                _triggerThresholdBox.Hide();

                SetCurrentValues();
            }
            else if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("controller"))
            {
                _keyboardStickBox.Hide();
                _controllerStickBox.Show();
                _settingsBox.Show();
                _deadZoneLeftBox.Show();
                _deadZoneRightBox.Show();
                _triggerThresholdBox.Show();

                SetCurrentValues();
            }
            else
            {
                _settingsBox.Hide();
            }
        }

        private void SetCurrentValues()
        {
            ClearValues();

            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                NpadKeyboard keyboardConfig = ConfigurationState.Instance.Hid.KeyboardConfig.Value.Find(controller => controller.ControllerId == _controllerId);

                if (keyboardConfig == null) return;

                _controllerType.SetActiveId(keyboardConfig.ControllerType.ToString());

                _lStickUp.Label     = keyboardConfig.LeftJoycon.StickUp.ToString();
                _lStickDown.Label   = keyboardConfig.LeftJoycon.StickDown.ToString();
                _lStickLeft.Label   = keyboardConfig.LeftJoycon.StickLeft.ToString();
                _lStickRight.Label  = keyboardConfig.LeftJoycon.StickRight.ToString();
                _lStickButton.Label = keyboardConfig.LeftJoycon.StickButton.ToString();
                _dpadUp.Label       = keyboardConfig.LeftJoycon.DPadUp.ToString();
                _dpadDown.Label     = keyboardConfig.LeftJoycon.DPadDown.ToString();
                _dpadLeft.Label     = keyboardConfig.LeftJoycon.DPadLeft.ToString();
                _dpadRight.Label    = keyboardConfig.LeftJoycon.DPadRight.ToString();
                _minus.Label        = keyboardConfig.LeftJoycon.ButtonMinus.ToString();
                _l.Label            = keyboardConfig.LeftJoycon.ButtonL.ToString();
                _zL.Label           = keyboardConfig.LeftJoycon.ButtonZl.ToString();
                _sL.Label           = keyboardConfig.LeftJoycon.ButtonSl.ToString();
                _rStickUp.Label     = keyboardConfig.RightJoycon.StickUp.ToString();
                _rStickDown.Label   = keyboardConfig.RightJoycon.StickDown.ToString();
                _rStickLeft.Label   = keyboardConfig.RightJoycon.StickLeft.ToString();
                _rStickRight.Label  = keyboardConfig.RightJoycon.StickRight.ToString();
                _rStickButton.Label = keyboardConfig.RightJoycon.StickButton.ToString();
                _a.Label            = keyboardConfig.RightJoycon.ButtonA.ToString();
                _b.Label            = keyboardConfig.RightJoycon.ButtonB.ToString();
                _x.Label            = keyboardConfig.RightJoycon.ButtonX.ToString();
                _y.Label            = keyboardConfig.RightJoycon.ButtonY.ToString();
                _plus.Label         = keyboardConfig.RightJoycon.ButtonPlus.ToString();
                _r.Label            = keyboardConfig.RightJoycon.ButtonR.ToString();
                _zR.Label           = keyboardConfig.RightJoycon.ButtonZr.ToString();
                _sR.Label           = keyboardConfig.RightJoycon.ButtonSr.ToString();
            }
            else if(_inputDevice.ActiveId.StartsWith("controller"))
            {
                NpadController controllerConfig = ConfigurationState.Instance.Hid.JoystickConfig.Value.Find(controller => controller.ControllerId == _controllerId);

                if (controllerConfig == null) return;

                _controllerType.SetActiveId(controllerConfig.ControllerType.ToString());

                _lStickX.Label                    = controllerConfig.LeftJoycon.StickX.ToString();
                _lStickY.Label                    = controllerConfig.LeftJoycon.StickY.ToString();
                _lStickButton.Label               = controllerConfig.LeftJoycon.StickButton.ToString();
                _dpadUp.Label                     = controllerConfig.LeftJoycon.DPadUp.ToString();
                _dpadDown.Label                   = controllerConfig.LeftJoycon.DPadDown.ToString();
                _dpadLeft.Label                   = controllerConfig.LeftJoycon.DPadLeft.ToString();
                _dpadRight.Label                  = controllerConfig.LeftJoycon.DPadRight.ToString();
                _minus.Label                      = controllerConfig.LeftJoycon.ButtonMinus.ToString();
                _l.Label                          = controllerConfig.LeftJoycon.ButtonL.ToString();
                _zL.Label                         = controllerConfig.LeftJoycon.ButtonZl.ToString();
                _sL.Label                         = controllerConfig.LeftJoycon.ButtonSl.ToString();
                _rStickX.Label                    = controllerConfig.RightJoycon.StickX.ToString();
                _rStickY.Label                    = controllerConfig.RightJoycon.StickY.ToString();
                _rStickButton.Label               = controllerConfig.RightJoycon.StickButton.ToString();
                _a.Label                          = controllerConfig.RightJoycon.ButtonA.ToString();
                _b.Label                          = controllerConfig.RightJoycon.ButtonB.ToString();
                _x.Label                          = controllerConfig.RightJoycon.ButtonX.ToString();
                _y.Label                          = controllerConfig.RightJoycon.ButtonY.ToString();
                _plus.Label                       = controllerConfig.RightJoycon.ButtonPlus.ToString();
                _r.Label                          = controllerConfig.RightJoycon.ButtonR.ToString();
                _zR.Label                         = controllerConfig.RightJoycon.ButtonZr.ToString();
                _sR.Label                         = controllerConfig.RightJoycon.ButtonSr.ToString();
                _controllerDeadzoneLeft.Value     = controllerConfig.DeadzoneLeft;
                _controllerDeadzoneRight.Value    = controllerConfig.DeadzoneRight;
                _controllerTriggerThreshold.Value = controllerConfig.TriggerThreshold;
            }
            else return;

            SetControllerImage();
        }

        private void SetControllerImage()
        {
            switch (_controllerType.ActiveId)
            {
                case "ProController":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.ProCon.png", 500, 500);
                    break;
                case "NpadLeft":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.BlueCon.png", 500, 500);
                    break;
                case "NpadRight":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.RedCon.png", 500, 500);
                    break;
                default:
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.JoyCon.png", 500, 500);
                    break;
            }
        }

        private void ClearValues()
        {
            _lStickX.Label                    = "";
            _lStickY.Label                    = "";
            _lStickUp.Label                   = "";
            _lStickDown.Label                 = "";
            _lStickLeft.Label                 = "";
            _lStickRight.Label                = "";
            _lStickButton.Label               = "";
            _dpadUp.Label                     = "";
            _dpadDown.Label                   = "";
            _dpadLeft.Label                   = "";
            _dpadRight.Label                  = "";
            _minus.Label                      = "";
            _l.Label                          = "";
            _zL.Label                         = "";
            _sL.Label                         = "";
            _rStickUp.Label                   = "";
            _rStickDown.Label                 = "";
            _rStickLeft.Label                 = "";
            _rStickRight.Label                = "";
            _rStickButton.Label               = "";
            _a.Label                          = "";
            _b.Label                          = "";
            _x.Label                          = "";
            _y.Label                          = "";
            _plus.Label                       = "";
            _r.Label                          = "";
            _zR.Label                         = "";
            _sR.Label                         = "";
            _controllerDeadzoneLeft.Value     = 0;
            _controllerDeadzoneRight.Value    = 0;
            _controllerTriggerThreshold.Value = 0;
        }

        private static bool IsAnyKeyPressed(out Key pressedKey, int index = 0)
        {
            KeyboardState keyboardState = Keyboard.GetState(index);

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (keyboardState.IsKeyDown((OpenTK.Input.Key)key))
                {
                    pressedKey = key;
                    return true;
                }
            }

            pressedKey = default;
            return false;
        }

        private static bool IsAnyButtonPressed(int index, double triggerThreshold, out ControllerInputId pressedButton)
        {
            JoystickState joystickState = Joystick.GetState(index);

            //Buttons
            for (int i = 0; i != Joystick.GetCapabilities(index).ButtonCount; i++)
            {
                if (joystickState.IsButtonDown(i))
                {
                    Enum.TryParse($"Button{i}", out pressedButton);
                    return true;
                }
            }

            //Axis
            for (int i = 0; i != Joystick.GetCapabilities(index).AxisCount; i++)
            {
                if (joystickState.GetAxis(i) > triggerThreshold)
                {
                    Enum.TryParse($"Axis{i}", out pressedButton);
                    return true;
                }
            }

            //Hats
            for (int i = 0; i != Joystick.GetCapabilities(index).HatCount; i++)
            {
                JoystickHatState hatState = joystickState.GetHat((JoystickHat)i);
                string pos = null;

                if (hatState.IsUp)    pos = "Up";
                if (hatState.IsDown)  pos = "Down";
                if (hatState.IsLeft)  pos = "Left";
                if (hatState.IsRight) pos = "Right";
                if (pos == null)      continue;

                Enum.TryParse($"Hat{i}{pos}", out pressedButton);
                return true;
            }

            pressedButton = ControllerInputId.Button0;
            return false;
        }

        //Events
        private void InputDevice_Changed(object sender, EventArgs args)
        {
            SetAvailableOptions();
        }

        private void Controller_Changed(object sender, EventArgs args)
        {
            SetControllerImage();
        }

        private void RefreshInputDevicesButton_Pressed(object sender, EventArgs args)
        {
            UpdateInputDeviceList();

            _refreshInputDevicesButton.SetStateFlags(0, true);
        }

        private void Button_Pressed(object sender, EventArgs args)
        {
            _toggleButton = (ToggleButton)sender;

            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                KeyPressEvent += OnKeyPress;

                /*Key pressedKey;

                while (!IsAnyKeyPressed(out pressedKey)) { }

                _toggleButton.Label = pressedKey.ToString();
                _toggleButton.SetStateFlags(0, true);*/
            }
            else if (_inputDevice.ActiveId.StartsWith("controller"))
            {
                ControllerInputId pressedButton;

                while (!IsAnyButtonPressed(int.Parse(_inputDevice.ActiveId.Split("/")[1]), _controllerTriggerThreshold.Value, out pressedButton)) { }

                _toggleButton.Label = pressedButton.ToString();
                _toggleButton.SetStateFlags(0, true);
            }
        }

        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            string key    = args.Event.Key.ToString();
            string capKey = key.First().ToString().ToUpper() + key.Substring(1);

            if (Enum.IsDefined(typeof(Key), capKey))
            {
                _toggleButton.Label = capKey;
            }
            else if (GtkToOpenTkInput.ContainsKey(key))
            {
                _toggleButton.Label = GtkToOpenTkInput[key];
            }
            else
            {
                _toggleButton.Label = "Unknown";
            }

            _toggleButton.SetStateFlags(0, true);

            KeyPressEvent -= OnKeyPress;
        }

        private void SaveToggle_Activated(object sender, EventArgs args)
        {
            NpadController controllerConfig = ConfigurationState.Instance.Hid.JoystickConfig.Value.Find(controller => controller.ControllerId == _controllerId);
            NpadKeyboard keyboardConfig     = ConfigurationState.Instance.Hid.KeyboardConfig.Value.Find(controller => controller.ControllerId == _controllerId);

            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                if (keyboardConfig == null)
                {
                    keyboardConfig = new NpadKeyboard();

                    ConfigurationState.Instance.Hid.KeyboardConfig.Value.Add(keyboardConfig);
                }
                if (controllerConfig != null)
                {
                    ConfigurationState.Instance.Hid.JoystickConfig.Value.Remove(controllerConfig);
                }

                keyboardConfig.LeftJoycon = new NpadKeyboardLeft
                {
                    StickUp     = Enum.Parse<Key>(_lStickUp.Label),
                    StickDown   = Enum.Parse<Key>(_lStickDown.Label),
                    StickLeft   = Enum.Parse<Key>(_lStickLeft.Label),
                    StickRight  = Enum.Parse<Key>(_lStickRight.Label),
                    StickButton = Enum.Parse<Key>(_lStickButton.Label),
                    DPadUp      = Enum.Parse<Key>(_dpadUp.Label),
                    DPadDown    = Enum.Parse<Key>(_dpadDown.Label),
                    DPadLeft    = Enum.Parse<Key>(_dpadLeft.Label),
                    DPadRight   = Enum.Parse<Key>(_dpadRight.Label),
                    ButtonMinus = Enum.Parse<Key>(_minus.Label),
                    ButtonL     = Enum.Parse<Key>(_l.Label),
                    ButtonZl    = Enum.Parse<Key>(_zL.Label),
                    ButtonSl    = Enum.Parse<Key>(_sL.Label)
                };

                keyboardConfig.RightJoycon = new NpadKeyboardRight
                {
                    StickUp     = Enum.Parse<Key>(_rStickUp.Label),
                    StickDown   = Enum.Parse<Key>(_rStickDown.Label),
                    StickLeft   = Enum.Parse<Key>(_rStickLeft.Label),
                    StickRight  = Enum.Parse<Key>(_rStickRight.Label),
                    StickButton = Enum.Parse<Key>(_rStickButton.Label),
                    ButtonA     = Enum.Parse<Key>(_a.Label),
                    ButtonB     = Enum.Parse<Key>(_b.Label),
                    ButtonX     = Enum.Parse<Key>(_x.Label),
                    ButtonY     = Enum.Parse<Key>(_y.Label),
                    ButtonPlus  = Enum.Parse<Key>(_plus.Label),
                    ButtonR     = Enum.Parse<Key>(_r.Label),
                    ButtonZr    = Enum.Parse<Key>(_zR.Label),
                    ButtonSr    = Enum.Parse<Key>(_sR.Label)
                };

                keyboardConfig.ControllerType = Enum.Parse<ControllerType>(_controllerType.ActiveId);
                keyboardConfig.ControllerId   = _controllerId;
            }
            else if (_inputDevice.ActiveId.StartsWith("controller"))
            {
                if (controllerConfig == null)
                {
                    controllerConfig = new NpadController();

                    ConfigurationState.Instance.Hid.JoystickConfig.Value.Add(controllerConfig);
                }
                if (keyboardConfig != null)
                {
                    ConfigurationState.Instance.Hid.KeyboardConfig.Value.Remove(keyboardConfig);
                }

                controllerConfig.LeftJoycon = new NpadControllerLeft
                {
                    StickX      = Enum.Parse<ControllerInputId>(_lStickX.Label),
                    StickY      = Enum.Parse<ControllerInputId>(_lStickY.Label),
                    StickButton = Enum.Parse<ControllerInputId>(_lStickButton.Label),
                    DPadUp      = Enum.Parse<ControllerInputId>(_dpadUp.Label),
                    DPadDown    = Enum.Parse<ControllerInputId>(_dpadDown.Label),
                    DPadLeft    = Enum.Parse<ControllerInputId>(_dpadLeft.Label),
                    DPadRight   = Enum.Parse<ControllerInputId>(_dpadRight.Label),
                    ButtonMinus = Enum.Parse<ControllerInputId>(_minus.Label),
                    ButtonL     = Enum.Parse<ControllerInputId>(_l.Label),
                    ButtonZl    = Enum.Parse<ControllerInputId>(_zL.Label),
                    ButtonSl    = Enum.Parse<ControllerInputId>(_sL.Label)
                };

                controllerConfig.RightJoycon = new NpadControllerRight
                {
                    StickX      = Enum.Parse<ControllerInputId>(_rStickX.Label),
                    StickY      = Enum.Parse<ControllerInputId>(_rStickY.Label),
                    StickButton = Enum.Parse<ControllerInputId>(_rStickButton.Label),
                    ButtonA     = Enum.Parse<ControllerInputId>(_a.Label),
                    ButtonB     = Enum.Parse<ControllerInputId>(_b.Label),
                    ButtonX     = Enum.Parse<ControllerInputId>(_x.Label),
                    ButtonY     = Enum.Parse<ControllerInputId>(_y.Label),
                    ButtonPlus  = Enum.Parse<ControllerInputId>(_plus.Label),
                    ButtonR     = Enum.Parse<ControllerInputId>(_r.Label),
                    ButtonZr    = Enum.Parse<ControllerInputId>(_zR.Label),
                    ButtonSr    = Enum.Parse<ControllerInputId>(_sR.Label)
                };

                controllerConfig.Index            = int.Parse(_inputDevice.ActiveId.Split("/")[1]);
                controllerConfig.DeadzoneLeft     = (float)_controllerDeadzoneLeft.Value;
                controllerConfig.DeadzoneRight    = (float)_controllerDeadzoneRight.Value;
                controllerConfig.TriggerThreshold = (float)_controllerTriggerThreshold.Value;
                controllerConfig.ControllerType   = Enum.Parse<ControllerType>(_controllerType.ActiveId);
                controllerConfig.ControllerId     = _controllerId;
            }
            else
            {
                if (controllerConfig != null)
                {
                    ConfigurationState.Instance.Hid.JoystickConfig.Value.Remove(controllerConfig);
                }
                if (keyboardConfig != null)
                {
                    ConfigurationState.Instance.Hid.KeyboardConfig.Value.Remove(keyboardConfig);
                }
            }

            Dispose();
        }

        private void CloseToggle_Activated(object sender, EventArgs args)
        {
            Dispose();
        }

        public readonly Dictionary<string, string> GtkToOpenTkInput = new Dictionary<string, string>()
        {
            { "Alt_L",       "AltLeft"        },
            { "Alt_R",       "AltRight"       },
            { "Control_L",   "ControlLeft"    },
            { "Control_R",   "ControlRight"   },
            { "KP_0",        "Keypad0"        },
            { "KP_1",        "Keypad1"        },
            { "KP_2",        "Keypad2"        },
            { "KP_3",        "Keypad3"        },
            { "KP_4",        "Keypad4"        },
            { "KP_5",        "Keypad5"        },
            { "KP_6",        "Keypad6"        },
            { "KP_7",        "Keypad7"        },
            { "KP_8",        "Keypad8"        },
            { "KP_9",        "Keypad9"        },
            { "KP_Add",      "KeypadAdd"      },
            { "KP_Decimal",  "KeypadDecimal"  },
            { "KP_Divide",   "KeypadDivide"   },
            { "KP_Down",     "Down"           },
            { "KP_Enter",    "KeypadEnter"    },
            { "KP_Left",     "Left"           },
            { "KP_Multiply", "KeypadMultiply" },
            { "KP_Right",    "Right"          },
            { "KP_Subtract", "KeypadSubtract" },
            { "KP_Up",       "Up"             },
            { "Key_0",       "Number0"        },
            { "Key_1",       "Number1"        },
            { "Key_2",       "Number2"        },
            { "Key_3",       "Number3"        },
            { "Key_4",       "Number4"        },
            { "Key_5",       "Number5"        },
            { "Key_6",       "Number6"        },
            { "Key_7",       "Number7"        },
            { "Key_8",       "Number8"        },
            { "Key_9",       "Number9"        },
            { "Meta_L",      "WinLeft"        },
            { "Meta_R",      "WinRight"       },
            { "Next",        "PageDown"       },
            { "Num_Lock",    "NumLock"        },
            { "Page_Down",   "PageDown"       },
            { "Page_Up",     "PageUp"         },
            { "Prior",       "PageUp"         },
            { "Return",      "Enter"          },
            { "Shift_L",     "ShiftLeft"      },
            { "Shift_R",     "ShiftRight"     },
            { "VoidSymbol",  "CapsLock"       },
            { "backslash",   "BackSlash"      },
            { "bracketleft", "BracketLeft"    },
            { "bracketright","BracketRight"   },
            { "downarrow",   "Down"           },
            { "equal",       "Plus"           },
            { "leftarrow",   "Left"           },
            { "quoteleft",   "Grave"          },
            { "rightarrow",  "Right"          },
            { "uparrow",     "Up"             }
        };
    }
}