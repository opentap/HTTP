using System.Security;
using OpenTap;
namespace HTTP;

[Display("HTTP Header")]
[MixinBuilder(typeof(RequestStep))]
public class HeaderMixinBuilder : IMixinBuilder
{
    public string Name { get; set; }

    public bool IsSecure { get; set; }

    public void Initialize(ITypeData targetType)
    {
        
    }
    public MixinMemberData ToDynamicMember(ITypeData targetType)
    {
        
        return new MixinMemberData(this)
        {
            Name = Name,
            Attributes = new object[]{new DisplayAttribute(Name, "a HTTP header", Group:"Headers"), new HeaderAttribute()},
            TypeDescriptor = TypeData.FromType(IsSecure ? typeof(SecureString) : typeof(string)),
            DeclaringType = TypeData.FromType(typeof(RequestStep))
        };
    }
    public IMixinBuilder Clone()
    {
        return (IMixinBuilder) MemberwiseClone();
    }
}
