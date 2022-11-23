using Project.Infrastructure.Configurations.Commands;

namespace Project.Application.Commands.AccountCommands.SigninUser
{
    public class SignInUserCommand : CommandBase<SignInUserResponse>
    {
        public SignInUserCommand(SignInUserRequest request)
        {
            Request = request;
        }
        public SignInUserRequest Request { get; set; }
    }
}
