using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using Teemaw.Calico.Util;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMod;

public static class GuitarStringSoundScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptMod(mod, "GuitarStringSoundScriptMod",
            "res://Scenes/Entities/Player/guitar_string_sound.gdc", [
                new TransformationRule("globals", CreateGlobalsPattern(),
                    ScriptTokenizer.Tokenize(
                        """

                        var calico_playing_count = 0

                        """)),
                new TransformationRule("add_child_in_ready", CreateGdSnippetPattern("add_child(new)"), [],
                    ReplaceAll),
                new TransformationRule("call_guard", CreateFunctionDefinitionPattern("_call"),
                    ScriptTokenizer.Tokenize(
                        """

                        if calico_playing_count == 0: return

                        """, 1)),
                new TransformationRule("node_play", CreateGdSnippetPattern("node.play(point)"),
                    ScriptTokenizer.Tokenize(
                        """

                        add_child(node)
                        calico_playing_count += 1

                        """, 3), Prepend),
                new TransformationRule("node_stopped", CreateGdSnippetPattern("sound.playing = false"),
                    ScriptTokenizer.Tokenize(
                        """

                        remove_child(sound)
                        calico_playing_count -= 1

                        """, 3)),
            ]);
    }
}