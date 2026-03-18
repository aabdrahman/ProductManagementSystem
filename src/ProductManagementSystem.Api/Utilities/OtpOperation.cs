using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Utilities.Contracts;
using System.Security.Cryptography;

namespace ProductManagementSystem.Api.Utilities;

public class OtpOperation : IOtpOperation
{
	private readonly OtpSettingsConfig _otpSettingsConfig;

    public OtpOperation(IOptionsMonitor<OtpSettingsConfig> otpSettingsConfigOptionsMonitor)
    {
		_otpSettingsConfig = otpSettingsConfigOptionsMonitor.CurrentValue;
    }

    public string GenerateOtp(int length = 6)
    {
		try
		{
			var randNumByte = new byte[4];

			using(var randNumGenerator = RandomNumberGenerator.Create())
			{
				randNumGenerator.GetBytes(randNumByte);
			}

			uint integerValue = BitConverter.ToUInt32(randNumByte, 0);

			string generatedOtp = (integerValue % Math.Pow(10, _otpSettingsConfig.OtpLength)).ToString().PadLeft(6, '0');

			return generatedOtp;
		}
		catch (Exception ex)
		{
			return string.Empty;
		}
    }
}
