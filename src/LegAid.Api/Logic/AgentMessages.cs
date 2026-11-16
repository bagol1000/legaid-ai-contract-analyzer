using LegAid.Api.Abstractions;
using LegAid.Api.Logic.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LegAid.Api.Logic;

// Wspólny początek rozmowy każdego agenta: instrukcje roli i, przy włączonej bazie, wszystkie przepisy.
// Baza idzie zaraz po instrukcjach, żeby początek zapytania był stały i mógł trafić w cache promptu.
public sealed class AgentMessages(IKnowledgeSource knowledgeSource, IOptions<KnowledgeOptions> knowledgeOptions)
{
    public List<ChatMessage> Start(string instructions)
    {
        List<ChatMessage> messages = [new(ChatRole.System, instructions)];

        if (knowledgeOptions.Value.Enabled)
        {
            messages.Add(new(ChatRole.System, LegalAgentPrompts.KnowledgeContext(knowledgeSource.GetAll())));
        }

        return messages;
    }
}
