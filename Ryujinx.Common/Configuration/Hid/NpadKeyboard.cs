namespace Ryujinx.Common.Configuration.Hid
{
    public class NpadKeyboard
    {
        /// <summary>
        /// Controller Device Index
        /// </summary>
        public int Index;

        /// <summary>
        ///  Controller's Type
        /// </summary>
        public ControllerType ControllerType;

        /// <summary>
        ///  Controller's ID
        /// </summary>
        public ControllerId ControllerId;

        /// <summary>
        /// Left JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardLeft LeftJoycon;

        /// <summary>
        /// Right JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardRight RightJoycon;

        /// <summary>
        /// Hotkey Keyboard Bindings
        /// </summary>
        public KeyboardHotkeys Hotkeys;
    }
}
