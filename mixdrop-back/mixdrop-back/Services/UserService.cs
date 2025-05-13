﻿using Microsoft.EntityFrameworkCore;
using mixdrop_back.Models.DTOs;
using mixdrop_back.Models.Entities;
using mixdrop_back.Models.Helper;
using mixdrop_back.Models.Mappers;
using mixdrop_back.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace mixdrop_back.Services;

public class UserService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly UserMapper _userMapper;

    Regex emailRegex = new(@"^([\w\.-]+)@([\w-]+)((\.(\w){2,3})+)$");

    public UserService(UnitOfWork unitOfWork, UserMapper userMapper)
    {
        _unitOfWork = unitOfWork;
        _userMapper = userMapper;
    }


    // buscar usuario
    public async Task<List<UserDto>> SearchUser(string search)
    {

        string searchSinTildes = Regex.Replace(search.Normalize(NormalizationForm.FormD), @"[^a-zA-z0-9 ]+", "");

        var users = await _unitOfWork.UserRepository.SearchUser(searchSinTildes.ToLower());

        return _userMapper.ToDto(users).ToList();
    }

    public async Task<List<UserDto>> GetRankingAsync()
    {
        var users = await _unitOfWork.UserRepository.GetRankingAsync();
        return _userMapper.ToDto(users).ToList();
    }

    public async Task<List<UserDto>> GetAllUsersAsync(User currentUser)
    {
        var users = await _unitOfWork.UserRepository.GetAllUsersAsync(currentUser.Id);
        return _userMapper.ToDto(users).ToList();
    }

    public async Task<User> GetBasicUserByIdAsync(int userId)
    {
        return await _unitOfWork.UserRepository.GetByIdAsync(userId);
    }

    public async Task<User> GetFullUserByIdAsync(int userId)
    {
        return await _unitOfWork.UserRepository.GetUserById(userId);
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return null;
        }
        return _userMapper.ToDto(user);
    }

    public async Task<UserDto> GetUserByNicknameAsync(string nickname)
    {
        var user = await _unitOfWork.UserRepository.GetByNicknameAsync(nickname);
        if (user == null)
        {
            return null;
        }
        return _userMapper.ToDto(user);
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.UserRepository.GetUserById(id);

        if (user == null)
        {
            return null;
        }

        var battles = await _unitOfWork.BattleRepository.GetEndedBattlesByUserAsync(id);

        BattleMapper battleMapper = new BattleMapper();

        UserDto userDto = _userMapper.ToDto(user);
        userDto.Battles = battleMapper.ToDtoWithAllInfo(battles);

        return userDto;
    }


    // INICIO DE SESION
    public async Task<User> LoginAsync(LoginRequest loginRequest)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailOrNickname(loginRequest.EmailOrNickname.ToLower());

        if (user == null || user.Password != PasswordHelper.Hash(loginRequest.Password))
        {
            return null;
        }

        return user;
    }

    // REGISTRO 
    public async Task<User> RegisterAsync(RegisterDto model)
    {
        var state = await _unitOfWork.StateRepositoty.GetByIdAsync(2);

        // validacion email

        if (!emailRegex.IsMatch(model.Email))
        {
            throw new Exception("Email no valido.");
        }

        if (model.Password == null || model.Password.Length < 6)
        {
            throw new Exception("Contraseña no válida");
        }

        try
        {

            // Verifica si el usuario ya existe
            var existingUser = await GetUserByEmailAsync(model.Email.ToLower());

            if (existingUser != null)
            {
                throw new Exception("El usuario ya existe.");
            }

            ImageService imageService = new ImageService();

            var newUser = new User
            {
                Email = model.Email.ToLower(),
                Nickname = model.Nickname.ToLower(),
                AvatarPath = "",
                Role = "User", // Rol por defecto
                Password = PasswordHelper.Hash(model.Password),
                IsInQueue = false,  // por defecto al crearse
                StateId = state.Id,
                State = state
            };

            if (model.Image != null)
            {
                newUser.AvatarPath = "/" + await imageService.InsertAsync(model.Image);
            }
            else
            {
                newUser.AvatarPath = "/avatar/user.png";
            }

            await _unitOfWork.UserRepository.InsertAsync(newUser);
            await _unitOfWork.SaveAsync();

            return newUser;

        }
        catch (DbUpdateException ex)
        {
            // Log más detallado del error
            Console.WriteLine($"Error al guardar el usuario: {ex.InnerException?.Message}");
            throw new Exception("Error al registrar el usuario. Verifica los datos ingresados.");
        }
    }

    public async Task ConnectUser(User user)
    {
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveAsync();
    }

    public async Task<UserDto> UpdateUser(RegisterDto model, User existingUser, string role)
    {
        // validacion email

        if (!emailRegex.IsMatch(model.Email))
        {
            throw new Exception("Email no valido.");
        }

        if (model.Password != null && model.Password != "" && model.Password.Length < 6)
        {
            throw new Exception("La contraseña no es válida");
        }

        try
        {
            // Verifica si el usuario ya existe
            if (!model.Email.Equals(existingUser.Email))
            {
                var otherUser = await GetUserByEmailAsync(model.Email.ToLower());

                if (otherUser != null)
                {
                    throw new Exception("El usuario ya existe.");
                }
            }

            ImageService imageService = new ImageService();

            existingUser.Email = model.Email.ToLower();
            existingUser.Nickname = model.Nickname.ToLower();

            // Si han pasado que la imagen debe cambiar y no es nula, guardo la imagen, pero si es nula, borro la que ya tenía
            if (model.ChangeImage.Equals("true"))
            {
                if (model.Image != null)
                {
                    existingUser.AvatarPath = await imageService.InsertAsync(model.Image);
                }
                else
                {
                    existingUser.AvatarPath = "";
                }
            }

            existingUser.Role = role;

            if (model.Password != null && model.Password != "")
            {
                existingUser.Password = PasswordHelper.Hash(model.Password);
            }

            UserSocket socket = WebSocketHandler.USER_SOCKETS.FirstOrDefault(u => u.User.Id == existingUser.Id);
            if (socket != null)
            {
                socket.User = existingUser;
            }

            _unitOfWork.UserRepository.Update(existingUser);
            await _unitOfWork.SaveAsync();

            return _userMapper.ToDto(existingUser);

        }
        catch (DbUpdateException ex)
        {
            // Log más detallado del error
            Console.WriteLine($"Error al guardar el usuario: {ex.InnerException?.Message}");
            throw new Exception("Error al registrar el usuario. Verifica los datos ingresados.");
        }
    }

    // Modificar el rol del usuario
    public async Task ModifyUserRoleAsync(int userId, string newRole)
    {
        var existingUser = await _unitOfWork.UserRepository.GetUserById(userId);


        if (existingUser == null)
        {
            throw new InvalidOperationException("Usuario con ID:" + userId + "no encontrado.");
        }

        // Console.WriteLine("ID del usuario: " + existingUser.Id);

        if (!string.IsNullOrEmpty(newRole))
        {
            existingUser.Role = newRole;
        }
        else
        {
            return;
        }

        UserSocket socket = WebSocketHandler.USER_SOCKETS.FirstOrDefault(u => u.User.Id == userId);
        if (socket != null)
        {
            socket.User.Role = newRole;
        }

        _unitOfWork.UserRepository.Update(existingUser);
        await _unitOfWork.SaveAsync();
    }

    // Banear usuario
    public async Task BanUserAsync(int userId)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("El usuario no existe.");
        }

        user.Banned = !user.Banned;
        Console.WriteLine("Estado de baneo: ", user.Banned);

        _unitOfWork.UserRepository.Update(user);

        await _unitOfWork.SaveAsync();
    }

    public UserDto ToDto(User user)
    {
        return _userMapper.ToDto(user);
    }
}
