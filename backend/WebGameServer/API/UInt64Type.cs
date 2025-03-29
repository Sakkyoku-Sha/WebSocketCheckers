using System;
using HotChocolate.Language;
using HotChocolate.Types;

public class UInt64Type : ScalarType<ulong, StringValueNode>
{
    public UInt64Type() : base("UInt64") { }

    protected override ulong ParseLiteral(StringValueNode valueNode)
    {
        return ulong.Parse(valueNode.Value);
    }

    protected override StringValueNode ParseValue(ulong runtimeValue)
    {
        return new StringValueNode(runtimeValue.ToString());
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            ulong ulongValue => new StringValueNode(ulongValue.ToString()),
            string strValue when ulong.TryParse(strValue, out var parsed) => new StringValueNode(parsed.ToString()),
            _ => throw new ArgumentException("Invalid UInt64 value")
        };
    }
}