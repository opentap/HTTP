using System;
using OpenTap;
namespace HTTP;

[Flags]
public enum ResponseAction
{
    [Display("Run JavaScript")]
    RunJavaScript = 2,
    [Display("Print to log")]
    Print = 4,
    [Display("Save to file")]
    SaveToFile = 8,
    [Display("To output")]
    ToOutput = 16
}