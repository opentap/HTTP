using OpenTap;
namespace HTTP;

public enum RequestBodyType
{
    [Display("none")]
    None,
    [Display("raw")]
    Raw,
    [Display("form-data")]
    FormData,
    [Display("binary")]
    Binary,
    [Display("x-www-form-urlencoded")]
    FormUrlEncoded
}