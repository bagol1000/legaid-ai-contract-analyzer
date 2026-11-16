namespace LegAid.Api.Model;

public enum ChatTurnRole
{
    User,
    Assistant
}

/// <summary>Jedna wcześniejsza wiadomość rozmowy.</summary>
public sealed record ChatTurn(ChatTurnRole Role, string Text);
