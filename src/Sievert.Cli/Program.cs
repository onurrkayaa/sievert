using System.Reflection;
using System.Runtime.InteropServices;

using Sievert.Core;

string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

foreach (BannerLine line in Banner.Render(version, RuntimeInformation.FrameworkDescription))
{
    Console.ForegroundColor = line.IsTitle ? ConsoleColor.Blue : ConsoleColor.Gray;
    Console.WriteLine(line.Text);
}

Console.ResetColor();
