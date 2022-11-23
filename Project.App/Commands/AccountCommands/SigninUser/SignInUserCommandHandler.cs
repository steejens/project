using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Project.Core.Enums.Enitity;
using Project.Core.Extensions;
using Project.Core.Models;
using Project.DataAccess.Repository.UserRepository;
using Project.Domain.Entities.Identity;
using Project.Infrastructure.Configurations.Commands;
using Project.Infrastructure.Services;

namespace Project.Application.Commands.AccountCommands.SigninUser
{
    public class SignInUserCommandHandler : ICommandHandler<SignInUserCommand, SignInUserResponse>
    {

        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly TokenService _tokenService;


        public SignInUserCommandHandler(IMediator mediator, IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository, TokenService tokenService)
        {

            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;

            _tokenService = tokenService;
        }

        public async Task<SignInUserResponse> Handle(SignInUserCommand command, CancellationToken cancellationToken)
        {

            var emailOrUsername = command.Request.EmailOrUsername;
            var password = command.Request.Password;
            var isEmail = emailOrUsername.IsEmail();
            var isPinRequired = false;

            User user;
            if (isEmail)
                user = await _userRepository.GetUserByEmailAsync(emailOrUsername)
                    .ConfigureAwait(false);
            else
                user = await _userRepository.GetUserByNameAsync(emailOrUsername)
                    .ConfigureAwait(false);
            return await GetSigninTokenAsync(user, cancellationToken);
        }

        private async Task<SignInUserResponse> GetSigninTokenAsync(User user, CancellationToken cancellationToken)
        {

            var applicationHeader = _httpContextAccessor.HttpContext.GeApplicationHeaderValue();


            var applicationClaim = new Claim(CustomClaimTypes.Application, applicationHeader);


            var claims = new List<Claim>()
            {
                applicationClaim,
            };

            var accessToken = _tokenService.GenerateToken(user, claims.ToArray());
            await _tokenService.RemoveOldRefreshTokensAsync(user.Id, applicationClaim.Value, string.Empty, removeAll: true);
            var refreshToken = await _tokenService.AddRefreshTokenAsync(user.Id, new TokenClaim(claims));


            return new SignInUserResponse()
            {
                RequiredConfirm = false,
                IsSigned = true,
                Token = accessToken,
                RefreshToken = refreshToken.Token
            };
        }
    }
}







