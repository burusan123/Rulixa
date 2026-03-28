using Rulixa.Domain.Packs;

namespace Rulixa.Application.Ports;

public interface IContextPackRenderer
{
    string Render(ContextPack contextPack);
}
