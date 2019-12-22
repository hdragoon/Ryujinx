namespace Ryujinx.UI.Input
{
    public class NpadKeyboard
    {
        /// <summary>
        ///  Controller's Type
        /// </summary>
        public Configuration.Hid.ControllerType ControllerType;

        /// <summary>
        ///  Controller's ID
        /// </summary>
        public Configuration.Hid.ControllerId ControllerId;

        /// <summary>
        /// Left JoyCon Keyboard Bindings
        /// </summary>
        public Configuration.Hid.NpadKeyboardLeft LeftJoycon;

        /// <summary>
        /// Right JoyCon Keyboard Bindings
        /// </summary>
        public Configuration.Hid.NpadKeyboardRight RightJoycon;

        /// <summary>
        /// Hotkey Keyboard Bindings
        /// </summary>
        public Configuration.Hid.KeyboardHotkeys Hotkeys;
    }
}
