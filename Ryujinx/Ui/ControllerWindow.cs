using Gtk;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration.Hid;
using GUI = Gtk.Builder.ObjectAttribute;

namespace Ryujinx.Ui
{
    public class ControllerWindow : Window
    {
        private ControllerId _controllerId;
        private static bool _listeningForKeypress;

#pragma warning disable CS0649
#pragma warning disable IDE0044
        [GUI] Window       _controllerWin;
        [GUI] ComboBoxText _inputDevice;
        [GUI] ToggleButton _refreshInputDevicesButton;
        [GUI] Box          _settingsBox;
        [GUI] ComboBoxText _controllerType;
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
        [GUI] Box          _deadZoneBox;
        [GUI] Box          _triggerThresholdBox;
        [GUI] Image        _controllerImage;
#pragma warning restore CS0649
#pragma warning restore IDE0044

        public ControllerWindow(ControllerId controllerId) : this(new Builder("Ryujinx.Ui.ControllerWindow.glade"), controllerId) { }

        private ControllerWindow(Builder builder, ControllerId controllerId) : base(builder.GetObject("_controllerWin").Handle)
        {
            builder.Autoconnect(this);

            _controllerId = controllerId;

            _controllerWin.Icon     = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");
            
            //Bind Events
            _lStickUp.Clicked       += (sender, args) => Button_Pressed(sender, args, _lStickUp);
            _lStickDown.Clicked     += (sender, args) => Button_Pressed(sender, args, _lStickDown);
            _lStickLeft.Clicked     += (sender, args) => Button_Pressed(sender, args, _lStickLeft);
            _lStickRight.Clicked    += (sender, args) => Button_Pressed(sender, args, _lStickRight);
            _lStickButton.Clicked   += (sender, args) => Button_Pressed(sender, args, _lStickButton);
            _dpadUp.Clicked         += (sender, args) => Button_Pressed(sender, args, _dpadUp);
            _dpadDown.Clicked       += (sender, args) => Button_Pressed(sender, args, _dpadDown);
            _dpadLeft.Clicked       += (sender, args) => Button_Pressed(sender, args, _dpadLeft);
            _dpadRight.Clicked      += (sender, args) => Button_Pressed(sender, args, _dpadRight);
            _minus.Clicked          += (sender, args) => Button_Pressed(sender, args, _minus);
            _l.Clicked              += (sender, args) => Button_Pressed(sender, args, _l);
            _zL.Clicked             += (sender, args) => Button_Pressed(sender, args, _zL);
            _rStickUp.Clicked       += (sender, args) => Button_Pressed(sender, args, _rStickUp);
            _rStickDown.Clicked     += (sender, args) => Button_Pressed(sender, args, _rStickDown);
            _rStickLeft.Clicked     += (sender, args) => Button_Pressed(sender, args, _rStickLeft);
            _rStickRight.Clicked    += (sender, args) => Button_Pressed(sender, args, _rStickRight);
            _rStickButton.Clicked   += (sender, args) => Button_Pressed(sender, args, _rStickButton);
            _a.Clicked              += (sender, args) => Button_Pressed(sender, args, _a);
            _b.Clicked              += (sender, args) => Button_Pressed(sender, args, _b);
            _x.Clicked              += (sender, args) => Button_Pressed(sender, args, _x);
            _y.Clicked              += (sender, args) => Button_Pressed(sender, args, _y);
            _plus.Clicked           += (sender, args) => Button_Pressed(sender, args, _plus);
            _r.Clicked              += (sender, args) => Button_Pressed(sender, args, _r);
            _zR.Clicked             += (sender, args) => Button_Pressed(sender, args, _zR);
            _sL.Clicked             += (sender, args) => Button_Pressed(sender, args, _sL);
            _sR.Clicked             += (sender, args) => Button_Pressed(sender, args, _sR);

            // Setup current values
            UpdateInputDeviceList();
            SetAvailableOptions();
        }

        private void UpdateInputDeviceList()
        {
            _inputDevice.RemoveAll();
            _inputDevice.Append("Disabled", "Disabled");
            _inputDevice.Append("Keyboard", "Keyboard");

            for (int i = 0; i != int.MaxValue; i++)
            {
                if (!Joystick.GetCapabilities(i).IsConnected)
                {
                    break;
                }

                _inputDevice.Append(i.ToString(), Joystick.GetGuid(i).ToString());
            }

            _inputDevice.SetActiveId("Disabled"); // replace with current device

            _refreshInputDevicesButton.SetStateFlags(0, true);
        }

        private void SetAvailableOptions()
        {
            if (_inputDevice.ActiveId == "Disabled")
            {
                _settingsBox.Hide();
            }
            else if (_inputDevice.ActiveId == "Keyboard")
            {
                _settingsBox.Show();
                _deadZoneBox.Hide();
                _triggerThresholdBox.Hide();

                SetCurrentValues();
            }
            else
            {
                _settingsBox.Show();
                _deadZoneBox.Show();
                _triggerThresholdBox.Show();

                SetCurrentValues();
            }

        }

        private void SetCurrentValues()
        {
            _controllerType.SetActiveId(ConfigurationState.Instance.Hid.KeyboardConfig.Value.ControllerType.ToString());

            _lStickUp.Label      = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.StickUp.ToString();
            _lStickDown.Label    = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.StickDown.ToString();
            _lStickLeft.Label    = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.StickLeft.ToString();
            _lStickRight.Label   = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.StickRight.ToString();
            _lStickButton.Label  = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.StickButton.ToString();
            _dpadUp.Label        = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.DPadUp.ToString();
            _dpadDown.Label      = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.DPadDown.ToString();
            _dpadLeft.Label      = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.DPadLeft.ToString();
            _dpadRight.Label     = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.DPadRight.ToString();
            _minus.Label         = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.ButtonMinus.ToString();
            _l.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.ButtonL.ToString();
            _zL.Label            = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.ButtonZl.ToString();
            _sL.Label            = ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon.ButtonSl.ToString();
            _rStickUp.Label      = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.StickUp.ToString();
            _rStickDown.Label    = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.StickDown.ToString();
            _rStickLeft.Label    = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.StickLeft.ToString();
            _rStickRight.Label   = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.StickRight.ToString();
            _rStickButton.Label  = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.StickButton.ToString();
            _a.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonA.ToString();
            _b.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonB.ToString();
            _x.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonX.ToString();
            _y.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonY.ToString();
            _plus.Label          = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonPlus.ToString();
            _r.Label             = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonR.ToString();
            _zR.Label            = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonZr.ToString();
            _sR.Label            = ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon.ButtonSr.ToString();

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
        }

        private void Button_Pressed(object sender, EventArgs args, ToggleButton button)
        {
            if (_listeningForKeypress == false)
            {
                KeyPressEvent += OnKeyPress;

                _listeningForKeypress = true;

                void OnKeyPress(object o, KeyPressEventArgs keyPressed)
                {
                    string key    = keyPressed.Event.Key.ToString();
                    string capKey = key.First().ToString().ToUpper() + key.Substring(1);

                    if (Enum.IsDefined(typeof(Configuration.Hid.Key), capKey))
                    {
                        button.Label = capKey;
                    }
                    else if (GdkToOpenTkInput.ContainsKey(key))
                    {
                        button.Label = GdkToOpenTkInput[key];
                    }
                    else
                    {
                        button.Label = "Space";
                    }

                    button.SetStateFlags(0, true);

                    KeyPressEvent -= OnKeyPress;

                    _listeningForKeypress = false;
                }
            }
            else
            {
                button.SetStateFlags(0, true);
            }
        }

        private void SaveToggle_Activated(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Hid.KeyboardConfig.Value.LeftJoycon = new NpadKeyboardLeft()
            {
                StickUp       = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _lStickUp.Label),
                StickDown     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _lStickDown.Label),
                StickLeft     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _lStickLeft.Label),
                StickRight    = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _lStickRight.Label),
                StickButton   = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _lStickButton.Label),
                DPadUp        = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _dpadUp.Label),
                DPadDown      = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _dpadDown.Label),
                DPadLeft      = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _dpadLeft.Label),
                DPadRight     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _dpadRight.Label),
                ButtonMinus   = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _minus.Label),
                ButtonL       = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _l.Label),
                ButtonZl      = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _zL.Label),
                ButtonSl      = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _sL.Label)
            };

            ConfigurationState.Instance.Hid.KeyboardConfig.Value.RightJoycon = new NpadKeyboardRight()
            {
                StickUp     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _rStickUp.Label),
                StickDown   = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _rStickDown.Label),
                StickLeft   = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _rStickLeft.Label),
                StickRight  = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _rStickRight.Label),
                StickButton = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _rStickButton.Label),
                ButtonA     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _a.Label),
                ButtonB     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _b.Label),
                ButtonX     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _x.Label),
                ButtonY     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _y.Label),
                ButtonPlus  = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _plus.Label),
                ButtonR     = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _r.Label),
                ButtonZr    = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _zR.Label),
                ButtonSr    = (Configuration.Hid.Key)Enum.Parse(typeof(Configuration.Hid.Key), _sR.Label)
            };

            ConfigurationState.Instance.Hid.KeyboardConfig.Value.ControllerType = (ControllerType)Enum.Parse(typeof(ControllerType), _controllerType.ActiveId);
            ConfigurationState.Instance.Hid.KeyboardConfig.Value.ControllerId   = _controllerId;

            Dispose();
        }

        private void CloseToggle_Activated(object sender, EventArgs args)
        {
            Dispose();
        }

        public readonly Dictionary<string, string> GdkToOpenTkInput = new Dictionary<string, string>()
        {
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
            { "equal",       "Plus"           },
            { "uparrow",     "Up"             },
            { "downarrow",   "Down"           },
            { "leftarrow",   "Left"           },
            { "rightarrow",  "Right"          },
            { "Control_L",   "ControlLeft"    },
            { "Control_R",   "ControlRight"   },
            { "Shift_L",     "ShiftLeft"      },
            { "Shift_R",     "ShiftRight"     },
            { "Alt_L",       "AltLeft"        },
            { "Alt_R",       "AltRight"       },
            { "Page_Up",     "PageUp"         },
            { "Page_Down",   "PageDown"       },
            { "KP_Enter",    "KeypadEnter"    },
            { "KP_Up",       "Up"             },
            { "KP_Down",     "Down"           },
            { "KP_Left",     "Left"           },
            { "KP_Right",    "Right"          },
            { "KP_Divide",   "KeypadDivide"   },
            { "KP_Multiply", "KeypadMultiply" },
            { "KP_Subtract", "KeypadSubtract" },
            { "KP_Add",      "KeypadAdd"      },
            { "KP_Decimal",  "KeypadDecimal"  },
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
        };
    }
}