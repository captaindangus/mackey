namespace MacKey
{
    internal static class AppMessages
    {
        // Registered messages are global to the session, so a second instance
        // can ask the already-running copy to show itself without knowing its
        // window handle.
        public static readonly uint Show = Native.RegisterWindowMessage("MacKey.ShowWindow");
    }
}
