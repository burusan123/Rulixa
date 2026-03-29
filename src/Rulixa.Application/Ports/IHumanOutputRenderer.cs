using Rulixa.Application.HumanOutputs;

namespace Rulixa.Application.Ports;

public interface IHumanOutputRenderer
{
    string Render(HumanOutputDocument document);
}
