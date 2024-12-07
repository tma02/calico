using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static GDWeave.Godot.TokenType;
using static Teemaw.Calico.Util.WaiterDefinitions;

namespace Teemaw.Calico.ScriptMods;

public class GuitarStringSoundScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new CalicoScriptMod(mod, "GuitarStringSoundScriptMod",
            "res://Scenes/Entities/Player/guitar_string_sound.gdc", [
                new ScriptPatchDescriptor("globals", CreateGlobalsChecks(),
                    """

                    var calico_playing_count = 0

                    """),
                new ScriptPatchDescriptor("node_play", [
                        t => t is IdentifierToken { Name: "node" },
                        t => t.Type is Period,
                        t => t is IdentifierToken { Name: "play" },
                        t => t.Type is ParenthesisOpen,
                        t => t is IdentifierToken { Name: "point" },
                        t => t.Type is ParenthesisClose
                    ],
                    """

                    calico_playing_count += 1

                    """, PatchOperation.Prepend),
            ]);
    }
}