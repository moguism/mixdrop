﻿using mixdrop_back.Models.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mixdrop_back.Sockets.Game;

public class RemoveAFKPlayers : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(2);
    private bool _shouldDelete = false;

    public readonly UserBattle _playerInTurn;
    public readonly UserBattle _otherPlayer;
    public readonly Battle _battle;
    public readonly GayHandler _handler;
    private readonly IServiceProvider _serviceProvider;

    public RemoveAFKPlayers(UserBattle playerInTurn, UserBattle otherUser, Battle battle, GayHandler handler, IServiceProvider serviceProvider)
    {
        _playerInTurn = playerInTurn;
        _otherPlayer = otherUser;
        _battle = battle;
        _handler = handler;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Para que la primera vez no haga la verificación
            if (!_shouldDelete)
            {
                _shouldDelete = true;
            }
            else
            {
                await EndBattle();
                GayNetwork._handlers.Remove(_handler);
                await StopAsync(stoppingToken);
            }

            await Task.Delay(_cleanupInterval, stoppingToken);

        }
    }

    public async Task EndBattle()
    {
        try
        {
            if (_battle.BattleStateId == 3)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
                    _battle.BattleStateId = 4;

                    _otherPlayer.BattleResultId = 1;
                    _playerInTurn.BattleResultId = 2;

                    _otherPlayer.Cards = new List<Card>();
                    _playerInTurn.Cards = new List<Card>();

                    _battle.BattleUsers = [_otherPlayer, _playerInTurn];

                    _unitOfWork.BattleRepository.Update(_battle);

                    Dictionary<object, object> dict = new Dictionary<object, object>
                    {
                        { "messageType", MessageType.DisconnectedFromBattle },
                        { "reported", false }, // Para saber quién es el que se desconecta
                    };

                    JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                    options.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                    await WebSocketHandler.NotifyOneUser(JsonSerializer.Serialize(dict, options), _otherPlayer.UserId);

                    dict["reported"] = true;
                    await WebSocketHandler.NotifyOneUser(JsonSerializer.Serialize(dict, options), _playerInTurn.UserId);
                    await _unitOfWork.SaveAsync();
                }

            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
