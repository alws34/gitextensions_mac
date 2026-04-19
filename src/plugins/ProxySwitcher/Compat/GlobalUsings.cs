// On non-Windows platforms, expose the WinForms shim namespace and System.Drawing
// as global usings so all files in this project can use the shim types transparently.
#if !WINDOWS
global using System.Drawing;
global using System.Windows.Forms;
#endif
