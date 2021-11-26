using AuthServer.Core.Configuration;
using AuthServer.Core.DTOs;
using AuthServer.Core.Models;
using AuthServer.Core.Repositories;
using AuthServer.Core.Services;
using AuthServer.Core.UnitOfWork;
using AuthServer.SharedLibrary.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Service.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly List<Client> _clients;
        private readonly ITokenService _tokenService;
        private readonly UserManager<UserApp> _userManager;
        private readonly IGenericRepository<UserRefreshToken> _userRefreshToken;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationService(IOptions<List<Client>> optionsClient,ITokenService tokenService,UserManager<UserApp> userManager,IGenericRepository<UserRefreshToken> userRefreshToken,IUnitOfWork unitOfWork)
        {
            _clients = optionsClient.Value;
            _tokenService = tokenService;
            _userManager = userManager;
            _userRefreshToken = userRefreshToken;
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<TokenDto>> CreateTokenAsync(LoginDto loginDto)
        {
            if (loginDto==null) throw new ArgumentNullException(nameof(loginDto));
            
            var user = await _userManager.FindByNameAsync(loginDto.Email);
            if (user==null) return Response<TokenDto>.Fail("Email or Password is wrong", 400, true);
            if(!await _userManager.CheckPasswordAsync(user,loginDto.Password)) return Response<TokenDto>.Fail("Email or Password is wrong", 400, true);
            var token = _tokenService.CreateToken(user);
            var userRefreshToken = await _userRefreshToken.Where(x => x.UserId == user.Id).SingleOrDefaultAsync();
            if (userRefreshToken == null)
            {
                await _userRefreshToken.AddAsync(new UserRefreshToken { UserId = user.Id, RefreshTokenCode = token.RefreshToken, Expiration = token.RefreshTokenExpiration });
            }
            else
            {
                userRefreshToken.RefreshTokenCode = token.RefreshToken;
                userRefreshToken.Expiration = token.RefreshTokenExpiration;
            }
            await _unitOfWork.CommitAsync();
            return Response<TokenDto>.Success(token, 200);
        }

        public Response<ClientTokenDto> CreateTokenByClient(ClientLoginDto clientLoginDto)
        {
            var client = _clients.SingleOrDefault(x => x.Id == clientLoginDto.ClientId && x.Secret == clientLoginDto.ClientSecret);
            if (client == null)
            {
                return Response<ClientTokenDto>.Fail("ClientId or ClientSecret not found", 404, true);
            }
            var token = _tokenService.CreateTokenByClient(client);
            return Response<ClientTokenDto>.Success(token, 200);
        }

        public async Task<Response<TokenDto>> CreateTokenByRefreshToken(string refreshToken)
        {
            var existRefreshToken = await _userRefreshToken.Where(x => x.RefreshTokenCode == refreshToken).SingleOrDefaultAsync();
            if (existRefreshToken==null)
            {
                return Response<TokenDto>.Fail("Refresh token not found", 404, true);

            }
            var user = await _userManager.FindByIdAsync(existRefreshToken.UserId);
            if (user==null)
            {
                return Response<TokenDto>.Fail("User ıd not found", 404, true);

            }
            var tokenDto = _tokenService.CreateToken(user);

            existRefreshToken.RefreshTokenCode = tokenDto.RefreshToken;
            existRefreshToken.Expiration = tokenDto.RefreshTokenExpiration;

            await _unitOfWork.CommitAsync();

            return Response<TokenDto>.Success(tokenDto, 200);
        }

        public async Task<Response<NoDataDto>> RevokeRefreshToken(string refreshToken)
        {
            var existRefreshToken = await _userRefreshToken.Where(x => x.RefreshTokenCode == refreshToken).SingleOrDefaultAsync();
            if (existRefreshToken==null)
            {
                return Response<NoDataDto>.Fail("Refresh token not found", 404, true);
            }

            _userRefreshToken.Remove(existRefreshToken);
            await _unitOfWork.CommitAsync();

            return Response<NoDataDto>.Success(200);
        }
    }
}
