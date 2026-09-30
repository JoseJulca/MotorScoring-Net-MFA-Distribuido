using QRCoder;

namespace MotorScoring.Web.Services;

public sealed class QrCodeService
{
    public string GenerateBase64Png(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(12);
        return Convert.ToBase64String(bytes);
    }
}
