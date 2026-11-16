namespace LegAid.Api.Presentation.Endpoints;

// Kształt JSON-a w API. Celowo oddzielony od klas z Model/, żeby zmiana formatu API nie zmieniała domeny.

/// <param name="Message">Pytanie prawne albo treść umowy lub pisma.</param>
/// <param name="History">Wcześniejsze wiadomości rozmowy, od najstarszej. Opcjonalne.</param>
public sealed record AskRequest(string? Message, List<ChatTurnDto>? History = null);

/// <param name="Role">"user" albo "assistant".</param>
/// <param name="Text">Treść wiadomości.</param>
public sealed record ChatTurnDto(string Role, string Text);

/// <param name="Answer">Odpowiedź agenta.</param>
/// <param name="Model">Model, który wygenerował odpowiedź.</param>
public sealed record AskResponse(string Answer, string? Model);
