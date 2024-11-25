using GDWeave.Godot;
using GDWeave.Modding;

namespace Teemaw.Calico;

public class ActorScriptMod: IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/actor.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        throw new NotImplementedException();
    }
}