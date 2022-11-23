using System.ComponentModel.DataAnnotations;
using Project.Core.Resources;

namespace Project.Application.Commands.AccountCommands.SigninUser
{
    public class SignInUserRequest
    {
        /// <summary>
        /// User email or Username
        /// </summary>
        [Required(ErrorMessage = ResourceKey.Required)]
        public string EmailOrUsername { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        [Required(ErrorMessage = "Password is not defined")]
        public string Password { get; set; }

    }
}
