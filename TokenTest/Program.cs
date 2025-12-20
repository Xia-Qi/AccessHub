using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Tokens;

class Program
{
    static void Main()
    {
        // 你的 Access Token
        string token = "eyJhbGciOiJBMjU2S1ciLCJlbmMiOiJBMjU2Q0JDLUhTNTEyIiwidHlwIjoiYXQrand0IiwiY3R5IjoiSldUIn0.EZxQdHxAkUQTJQgQFg6rrINxODK__NA88vc6XflojjM3kwzpWm0kQiBm4WVbxiG2Y1ygo6By9EOvwMTqarU3w4xk5ZpIBQKS.6Uif8_C3klWbbLS1or1JRQ.OHGy0LwGWGtO2mTUdh_dsnKbzUhSK_C6OburOuQfDwEQT-6An8EHPPje3lHOR9LBMXot1sfCR6YAyQQgq_fLoKmQ-lE7ufnj32EuRjsZ0vcqkwhMlg6S5NxgoWO9LH7d6rAFKe16caxeWBnEmuMSC87pHj6_8QGZEbn8sAhl5x1_1VDaw17omVimwIwPDHwX0jM8YL4NuumBA7rD3NzH8Jqb8ZAKWNtzsr2InA43ZJX3eZP1GSHop22ZB4JYlESdmJbfe_4j07iK6BW3cwggmf48Uu9OvdLN84VG2IvEgPGj06BS85HzcknkoadNntPywiS4X5Kiuy-N5NCU2oMEA5SOrTh34rpyyItBifAXwYp7GgnFTldK7sdYk83717GJc3wuhI6m7pP_Bg91OCfFW8bzEiaLttOZT5zWNli6DqXAhMvgp4Zl4dJ81B86bn_-HlMyIXenRslw6CQD4ZTG5mbya5FiGdDeLPNoAnCWJ_Szzy0WMkgsJHHpgOSbWC_XHGfGKqQRlMBpWLyPOSX8EpsXYDfwU5VOIeQJvHHDRZPuCdjQUcm4WzjolCAS2kMN2Oljvgeg5eSFHZXRmrgEx1Vr5v-WK-4Ztuxrbvhe41_KXc8xgstJ5NHOquelHsx8nlNxV1FZiJGWQdqVHY5howjsK9wFmE0OA9Yyx0LcqocgGAJWA48X6TUkj5eM_uD6Q0MOosoAQfiz1j8E76C_TgACjBNJW6TqEvPTX9CPtdgytCXPwlNRKaYbOaz2jmI3o2xZkDlexJvDaXyG4xPpMiOo1-SogJU-qN_eg1_z2XlKe4e3QSK61f0Hn_1ufM46C79pKGUfPxSmdsU5EunyneedOPcwi4KNIwyDS8UKkfau8t1EBHNRuS4ibEXCvC7Ri5d5_Y1r0RxlIVFErOFCkQ2uSOhv3URUj6Z1xe1JYk8vV888INu1t-M8WOXpeKWsupMrvWwV3GyXmGqGKyG1eH57i4OTYuWUoLA0iylIpY6GREVv-cd28dJTu5HAM-GNEjjt14p7zXun-lyywjTToNjaKLSa2GCV4Tw-FWpLtkEDwnRQ6y_Dm-hXHM7dqcZySQVH_iLZXlT-LJ8CQ-xtDLHAIL6IhFdnX4xv0c_VXvg.TRBv2VqPouA9pt_Nopl3FZOJCRHmxB9XBBhMzhx5irs"; // 截断，你自己完整填

        // 开发环境对称密钥（Base64）
        string base64Key = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

        var keyBytes = Convert.FromBase64String(base64Key);
        var securityKey = new SymmetricSecurityKey(keyBytes);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = securityKey
        };

        try
        {
            // 验证并解密 token
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            Console.WriteLine("Token 解密成功！");
            Console.WriteLine("Payload Claims:");
            foreach (var claim in principal.Claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("解密失败: " + ex.Message);
        }
        Console.ReadLine();
    }
}
