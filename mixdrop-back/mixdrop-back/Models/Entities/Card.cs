﻿namespace mixdrop_back.Models.Entities;

public class Card
{
    public int Id { get; set; }
    public string ImagePath { get; set; }
    public int Level { get; set; }
    public int TrackId { get; set; }
    public int CardTypeId { get; set; }
    public int CollectionId { get; set; }
    public Track Track { get; set; } = null;
    public CardType CardType { get; set; } = null; // Comodín, color, etc
    public string Effect { get; set; } = "";
}
