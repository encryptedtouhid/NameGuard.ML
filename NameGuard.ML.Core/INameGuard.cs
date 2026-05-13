using NameGuard.ML.Core.Models;

namespace NameGuard.ML.Core;

public interface INameGuard
{
    NamePrediction Check(string name);
}
