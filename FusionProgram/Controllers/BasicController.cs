using CAP;
using DapperSQL;
using FusionProgram.Models;
using JWT_Authentication.Authorization;
using JwtSwaggerHc.API.V1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Redis.Service;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FusionProgram.Controllers
{
    /// <summary>
    /// 测试控制器
    /// </summary>
    [ApiController]
    [Route(("api/[controller]/[action]"))]
    public class BasicController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IRedisServer _redisServer;
        private readonly Publisher _cAPPublisher;
        private readonly ILogger<BasicController> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// WeatherForecast
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="redisServer"></param>
        /// /// <param name="_cAPPublisher"></param>
        public BasicController(ILogger<BasicController> logger
            , IRedisServer redisServer
            , Publisher _cAPPublisher
            ,IConfiguration configuration

            )
        {
            _logger = logger;
            _redisServer = redisServer;
            this._cAPPublisher = _cAPPublisher;
            _configuration = configuration;
        }
        /// <summary>
        /// 获取天气信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = Policies.Admin)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("GetWeatherForecast");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        /// <summary>
        /// 获取Redis值
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = Policies.Admin)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public string GetRedisVelue(string Key)
        {
            _cAPPublisher.PublishMessage("测试");
            return _redisServer.GetData(Key);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Login(LoginRequest loginRequest)
        {
            User user = AuthenticateUser(loginRequest);
            if (user == null)
            {
                return Unauthorized();
            }

            var token = GenerateJwt(user);

            return Ok(new LoginResponse
            {
                Token = token,
                User = user
            });
        }

        /// <summary>
        ///  Retrieve token credentials
        /// </summary>
        /// <param name="token">Request token</param>
        /// <returns>Credential information</returns>
        [HttpPost("Token/Claims")]
        [ProducesResponseType(typeof(ClaimsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClaims(string token)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = securityKey
            };

            try
            {
                ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

                var result = new ClaimsResponse
                {
                    Name = claimsPrincipal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value,
                    Role = claimsPrincipal.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value,
                    Jti = claimsPrincipal.FindFirst("jti").Value
                };
                return Ok(new JsonResult(result));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Invalid token: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userLogin"></param>
        /// <returns></returns>
        private User AuthenticateUser(LoginRequest userLogin)
        {
            return new User { Username = userLogin.Username, Password = userLogin.Password, Role = Policies.Admin };
        }

        private string GenerateJwt(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha384);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),//token30分钟过期
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}