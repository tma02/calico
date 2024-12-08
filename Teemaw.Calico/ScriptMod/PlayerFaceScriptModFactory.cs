using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class PlayerFaceScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("PlayerFaceScriptMod")
            .Patching("res://Scenes/Entities/Player/Face/player_face.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("reset_time")
                .Matching(CreateGdSnippetPattern("reset_time -= 1"))
                .Do(ReplaceAll)
                .With("reset_time -= 60 * delta")
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("blink_time")
                .Matching(CreateGdSnippetPattern("blink_time -= 1"))
                .Do(ReplaceAll)
                .With("blink_time -= 60 * delta")
                .Times(2)
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("emote_time")
                .Matching(CreateGdSnippetPattern("emote_time -= 1"))
                .Do(ReplaceAll)
                .With("emote_time -= 60 * delta")
            )
            .Build();
    }
}