using System.ComponentModel.DataAnnotations;
using System.Text;

namespace UniCore.Api.Modules.Seguridad.Requests;

/// <summary>Valida el límite de entrada de BCrypt sin truncar silenciosamente la contraseña.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxUtf8BytesAttribute(int maxBytes) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is null || value is string text && Encoding.UTF8.GetByteCount(text) <= maxBytes;
    }
}
