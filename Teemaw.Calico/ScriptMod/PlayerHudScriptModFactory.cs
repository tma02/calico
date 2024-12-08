using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class PlayerHudScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("PlayerHudScriptMod")
            .Patching("res://Scenes/HUD/playerhud.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("interact_timer")
                .Matching(CreateGdSnippetPattern(
                    """
                    if interact:
                    	interact_timer += 2
                    elif interact_timer > 0:
                    	if interact_timer > 28: interact_timer = 28
                    	interact_timer -= 2
                    """, 1
                ))
                .Do(ReplaceAll)
                .With(
                    // (0) is a hack to prevent the 0 from being tokenized as an identifier.
                    """
                    if interact:
                    	interact_timer += 60 * delta
                    elif interact_timer > 0:
                    	if interact_timer > 28: interact_timer = 28
                    	interact_timer -= 60 * delta
                    	if interact_timer < 0: interact_timer = (0)
                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("dialog_decrement")
                .Matching(CreateGdSnippetPattern("dialogue_cooldown -= 1"))
                .Do(ReplaceAll)
                .With("dialogue_cooldown -= 60 * delta")
            )
            .Build();
    }
}