using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class GuitarStringSoundScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("GuitarStringSoundScriptMod")
            .Patching("res://Scenes/Entities/Player/guitar_string_sound.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_playing_count = 0

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("add_child_in_ready")
                .Matching(CreateGdSnippetPattern("add_child(new)"))
                .Do(ReplaceAll)
                .With([])
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("call_guard")
                .Matching(CreateFunctionDefinitionPattern("_call"))
                .Do(Append)
                .With(
                    """

                    if calico_playing_count == 0: return

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("node_play")
                .Matching(CreateGdSnippetPattern("node.play(point)"))
                .Do(Prepend)
                .With(
                    """

                    add_child(node)
                    calico_playing_count += 1

                    """, 3
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("node_stopped")
                .Matching(CreateGdSnippetPattern("sound.playing = false"))
                .Do(Append)
                .With(
                    """

                    remove_child(sound)
                    calico_playing_count -= 1

                    """, 3
                )
            )
            .Build();
    }
}