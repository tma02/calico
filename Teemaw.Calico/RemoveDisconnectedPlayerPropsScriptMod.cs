using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class RemoveDisconnectedPlayerPropsScriptMod : IScriptMod
{
    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        func calico_user_disconnected(user_id):
        	print("[calico] User disconnected, cleaning up props by owner: ", user_id)
        	for actor in get_tree().get_nodes_in_group("actor"):
        		if actor.owner_id == user_id:
        			actor.queue_free()

        """);
    
    private readonly IEnumerable<Token> _connectUserDisconnectSignal = ScriptTokenizer.Tokenize(
        """

        Network.connect("_user_disconnected", self, "calico_user_disconnected")

        """, 1);

    public bool ShouldRun(string path) => path == "res://Scenes/World/world.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ]);
        MultiTokenWaiter readyWaiter = new([
            t => t.Type is PrFunction,
            t => t is IdentifierToken { Name: "_ready" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);

        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _globals)
                    yield return t1;
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _connectUserDisconnectSignal)
                    yield return t1;
            }
            else
            {
                yield return t;
            }
        }
    }
}