using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Security;

public class JwtProvider
{
    private readonly IConfiguration _config;
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expireMinutes;

    public JwtProvider(IConfiguration configuration)
    {
        _config = configuration;
        _key = configuration.GetValue<string>("Jwt:Key") ?? "";

        if (string.IsNullOrEmpty(_key))
        {
            throw new Exception("Invalid Jwt key");
        }

        _issuer = configuration.GetValue<string>("Jwt:Issuer") ?? "";

        if (string.IsNullOrEmpty(_issuer))
        {
            throw new Exception("Invalid Jwt Issuer");
        }

        _audience = configuration.GetValue<string>("Jwt:Audience") ?? "";

        if (string.IsNullOrEmpty(_audience))
        {
            throw new Exception("Invalid Jwt Audience");
        }

        _expireMinutes = configuration.GetValue<double>("Jwt:ExpireMinutes");
    }

    public string Generate(Guid userId, string username)
    {
        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("username", username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
            signingCredentials: credential);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
