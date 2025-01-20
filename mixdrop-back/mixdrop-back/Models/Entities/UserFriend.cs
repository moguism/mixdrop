﻿namespace mixdrop_back.Models.Entities;

public class UserFriend
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int FriendId { get; set; }
    public bool Receiver { get; set; }
    public User User { get; set; }
    public Friendship Friendships { get; set; }
}
