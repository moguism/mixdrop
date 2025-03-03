﻿using mixdrop_back.Models.DTOs;
using mixdrop_back.Models.Entities;

namespace mixdrop_back.Sockets;

public class GayNetwork // GameNetwork :3
{
    public static readonly ICollection<GayHandler> _handlers = new List<GayHandler>();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


    public static async Task<UserBattleDto> StartGame(Battle battle, User user, UnitOfWork unitOfWork, IServiceProvider serviceProvider) // 🦙📲🦙🔥
    {
        await _semaphore.WaitAsync();

        GayHandler handler = _handlers.FirstOrDefault(warden => warden.Battle.Id == battle.Id);
        if (handler == null)
        {
            handler = new GayHandler();
            handler.Battle = battle;
            _handlers.Add(handler);
        }

        UserBattleDto userBattleDto = await handler.AddParticipant(battle, user.Id, unitOfWork, serviceProvider);

        await WebSocketHandler.SendStatsMessage();

        _semaphore.Release();

        return userBattleDto;
    }

    public static async Task PlayCard(Models.DTOs.Action action, int userId, UnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        await _semaphore.WaitAsync();

        GayHandler handler = _handlers
            .Where(handler => handler._participants.Any(u => u.UserId == userId))
            .FirstOrDefault();

        if (handler == null)
        {
            Console.WriteLine("No existe un handler para este usuario");
            _semaphore.Release();
            return;
        }
        else
        {
            await handler.PlayCard(action, userId, unitOfWork, serviceProvider);
        }

        _semaphore.Release();
    }

    public static void DeleteHandler(int userId)
    {
        GayHandler handler = _handlers.FirstOrDefault(h => h._participants.Any(p => p.UserId == userId));
        if (handler != null)
        {
            _handlers.Remove(handler);
        }
    }
}
