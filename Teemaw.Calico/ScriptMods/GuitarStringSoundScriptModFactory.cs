using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static GDWeave.Godot.TokenType;
using static Teemaw.Calico.Util.WaiterDefinitions;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMods;

public class GuitarStringSoundScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new CalicoScriptMod(mod, "GuitarStringSoundScriptMod",
            "res://Scenes/Entities/Player/guitar_string_sound.gdc", [
                new ScriptPatchDescriptor("globals", CreateGlobalsChecks(),
                    ScriptTokenizer.Tokenize(
                        """

                        var calico_playing_count = 0

                        """).ToArray()),
                new ScriptPatchDescriptor("add_in_ready", [
                        t => t is IdentifierToken { Name: "add_child" },
                        t => t.Type is ParenthesisOpen,
                        t => t is IdentifierToken { Name: "new" },
                        t => t.Type is ParenthesisClose
                    ], [], PatchOperation.ReplaceAll),
                new ScriptPatchDescriptor("call_guard", CreateFunctionDefinitionChecks("_call"),
                    ScriptTokenizer.Tokenize(
                        """

                        if calico_playing_count == 0: return

                        """, 1)),
                new ScriptPatchDescriptor("node_play", [
                        t => t is IdentifierToken { Name: "node" },
                        t => t.Type is Period,
                        t => t is IdentifierToken { Name: "play" },
                        t => t.Type is ParenthesisOpen,
                        t => t is IdentifierToken { Name: "point" },
                        t => t.Type is ParenthesisClose
                    ],
                    ScriptTokenizer.Tokenize(
                        """
                        
                        add_child(node)
                        calico_playing_count += 1

                        """, 3), PatchOperation.Prepend),
                new ScriptPatchDescriptor("node_stopped", [
                        t => t is IdentifierToken { Name: "sound" },
                        t => t.Type is Period,
                        t => t is IdentifierToken { Name: "playing" },
                        t => t.Type is OpAssign,
                        t => t is ConstantToken c && c.Value.Equals(new BoolVariant(false))
                    ],
                    ScriptTokenizer.Tokenize(
                        """
                        
                        remove_child(sound)
                        calico_playing_count -= 1

                        """, 3)),
            ]);
    }
}