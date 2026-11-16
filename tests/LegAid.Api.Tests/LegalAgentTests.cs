using LegAid.Api.Logic;
using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

public class LegalAgentTests
{
    private readonly FakeChatClient _chat = new(_ => "Odpowiedź modelu");

    private LegalAgent CreateAgent(bool knowledgeEnabled = true) =>
        new(_chat, TestAgents.Messages(knowledgeEnabled), new FakeAnonymizer(), NullLogger<LegalAgent>.Instance);

    [Fact]
    public async Task SendsInstructionsKnowledgeHistoryAndMessage()
    {
        var answer = await CreateAgent().AskAsync("a co z § 7?",
            [new(ChatTurnRole.User, "umowa"), new(ChatTurnRole.Assistant, "analiza")]);

        var messages = Assert.Single(_chat.Calls);
        Assert.Equal([ChatRole.System, ChatRole.System, ChatRole.User, ChatRole.Assistant, ChatRole.User], messages.Select(m => m.Role));
        Assert.Equal(LegalAgentPrompts.System, messages[0].Text);
        Assert.Contains("[KC art. 483]", messages[1].Text);
        Assert.Equal("a co z § 7?", messages[^1].Text);
        Assert.Equal("Odpowiedź modelu", answer.Text);
    }

    [Fact]
    public async Task SkipsKnowledgeWhenDisabled()
    {
        await CreateAgent(knowledgeEnabled: false).AskAsync("pytanie");

        Assert.Equal([ChatRole.System, ChatRole.User], Assert.Single(_chat.Calls).Select(m => m.Role));
    }

    [Fact]
    public async Task SendsOnlyLast20Turns()
    {
        var history = Enumerable.Range(1, 25).Select(i => new ChatTurn(ChatTurnRole.User, $"wiadomość {i}")).ToList();

        await CreateAgent().AskAsync("pytanie", history);

        var messages = Assert.Single(_chat.Calls);
        Assert.Equal(2 + 20 + 1, messages.Count);
        Assert.Equal("wiadomość 6", messages[2].Text);
    }

    [Fact]
    public async Task AnonymizesHistoryAndMessageBeforeSendingToModel()
    {
        await CreateAgent().AskAsync("Czy Jan Kowalski może wypowiedzieć umowę?", [new(ChatTurnRole.User, "Wynajmuje mi Jan Kowalski.")]);

        var messages = Assert.Single(_chat.Calls);
        Assert.DoesNotContain(messages, m => m.Text.Contains("Jan Kowalski"));
        Assert.Equal("Czy [OSOBA_1] może wypowiedzieć umowę?", messages[^1].Text);
    }

    [Fact]
    public async Task StreamsAnswerInChunks()
    {
        List<string> chunks = [];
        await foreach (var chunk in CreateAgent().AskStreamingAsync("pytanie"))
        {
            chunks.Add(chunk);
        }

        Assert.True(chunks.Count > 1);
        Assert.Equal("Odpowiedź modelu", string.Concat(chunks).Trim());
    }
}
